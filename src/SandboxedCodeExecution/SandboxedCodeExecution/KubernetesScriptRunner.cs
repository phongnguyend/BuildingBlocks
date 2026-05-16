using System.Diagnostics;
using System.Text.Json;

class KubernetesScriptRunner(string @namespace = "default", int timeoutSeconds = 600) : IScriptRunner
{
    public Task PullImageAsync(string dockerImage)
    {
        Console.WriteLine($"Kubernetes backend: image '{dockerImage}' will be pulled on demand by the cluster.");
        return Task.CompletedTask;
    }

    public async Task<bool> DeleteSessionAsync(string sessionId)
    {
        var workDir = Path.Combine(Path.GetTempPath(), "runner", sessionId);
        if (!Directory.Exists(workDir))
        {
            return false;
        }

        var jobName = $"runner-{sessionId}";
        var configMapName = $"runner-{sessionId}";
        await RunKubectlAsync(["delete", "job", jobName, "-n", @namespace, "--ignore-not-found"]);
        await RunKubectlAsync(["delete", "configmap", configMapName, "-n", @namespace, "--ignore-not-found"]);

        Directory.Delete(workDir, true);
        return true;
    }

    public async Task<ScriptRunResult> RunAsync(LanguageConfig config, string workDir, string sessionId, string[] extraArguments, int timeoutMs = 600000)
    {
        var jobName = $"runner-{sessionId}";
        var configMapName = $"runner-{sessionId}";
        var scriptPath = Path.Combine(workDir, config.FileName);

        // 1. Create ConfigMap from the script file
        // kubectl reads the local file and sends it over the API, so this works with a remote cluster.
        var cmResult = await RunKubectlAsync(["create", "configmap", configMapName, $"--from-file={config.FileName}={scriptPath}", "-n", @namespace]);
        if (!cmResult.Success)
        {
            return new ScriptRunResult(Success: false, Stdout: string.Empty, Stderr: $"Failed to create ConfigMap: {cmResult.Stderr}", TimedOut: false);
        }

        // 2. Build and apply Job YAML
        var commandParts = config.RunCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (extraArguments is { Length: > 0 })
        {
            commandParts.AddRange(extraArguments);
        }

        var commandYaml = string.Join(", ", commandParts.Select(p => $"\"{p}\""));

        var yaml = new System.Text.StringBuilder();
        yaml.AppendLine("apiVersion: batch/v1");
        yaml.AppendLine("kind: Job");
        yaml.AppendLine("metadata:");
        yaml.AppendLine($"  name: {jobName}");
        yaml.AppendLine($"  namespace: {@namespace}");
        yaml.AppendLine("spec:");
        yaml.AppendLine("  backoffLimit: 0");
        yaml.AppendLine("  template:");
        yaml.AppendLine("    spec:");
        yaml.AppendLine("      restartPolicy: Never");
        yaml.AppendLine("      containers:");
        yaml.AppendLine("      - name: runner");
        yaml.AppendLine($"        image: {config.DockerImage}");
        yaml.AppendLine($"        command: [{commandYaml}]");
        yaml.AppendLine("        resources:");
        yaml.AppendLine("          limits:");
        yaml.AppendLine("            memory: \"256Mi\"");
        yaml.AppendLine("            cpu: \"1000m\"");
        yaml.AppendLine("        securityContext:");
        yaml.AppendLine($"          readOnlyRootFilesystem: {(config.ReadOnly ? "true" : "false")}");
        yaml.AppendLine("        volumeMounts:");
        yaml.AppendLine("        - name: script");
        yaml.AppendLine("          mountPath: /app");
        if (!config.ReadOnly)
        {
            yaml.AppendLine("        - name: tmp");
            yaml.AppendLine("          mountPath: /tmp");
        }
        yaml.AppendLine("      volumes:");
        yaml.AppendLine("      - name: script");
        yaml.AppendLine("        configMap:");
        yaml.AppendLine($"          name: {configMapName}");
        if (!config.ReadOnly)
        {
            yaml.AppendLine("      - name: tmp");
            yaml.AppendLine("        emptyDir: {}");
        }

        var applyResult = await RunKubectlAsync(["apply", "-f", "-"], stdin: yaml.ToString());
        if (!applyResult.Success)
        {
            await RunKubectlAsync(["delete", "configmap", configMapName, "-n", @namespace, "--ignore-not-found"]);
            return new ScriptRunResult(Success: false, Stdout: string.Empty, Stderr: $"Failed to create Job: {applyResult.Stderr}", TimedOut: false);
        }

        // 3. Poll for job completion
        using var cts = new CancellationTokenSource(timeoutMs);
        bool? succeeded = null;

        while (!cts.IsCancellationRequested)
        {
            var statusResult = await RunKubectlAsync(["get", "job", jobName, "-n", @namespace, "-o", "json"]);
            if (statusResult.Success && !string.IsNullOrWhiteSpace(statusResult.Stdout))
            {
                using var doc = JsonDocument.Parse(statusResult.Stdout);
                var status = doc.RootElement.GetProperty("status");

                if (status.TryGetProperty("succeeded", out var succeededEl) && succeededEl.GetInt32() > 0)
                {
                    succeeded = true;
                    break;
                }

                if (status.TryGetProperty("failed", out var failedEl) && failedEl.GetInt32() > 0)
                {
                    succeeded = false;
                    break;
                }
            }

            try { await Task.Delay(2000, cts.Token); } catch (OperationCanceledException) { break; }
        }

        if (succeeded == null)
        {
            return new ScriptRunResult(Success: false, Stdout: string.Empty, Stderr: string.Empty, TimedOut: true);
        }

        // 4. Collect logs
        var logsResult = await RunKubectlAsync(["logs", $"job/{jobName}", "-n", @namespace]);

        return new ScriptRunResult(Success: succeeded.Value, Stdout: logsResult.Stdout, Stderr: logsResult.Stderr, TimedOut: false);
    }

    private async Task<(bool Success, string Stdout, string Stderr)> RunKubectlAsync(string[] arguments, string? stdin = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "kubectl",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = stdin != null
        };

        foreach (var arg in arguments)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = psi };
        process.Start();

        if (stdin != null)
        {
            await process.StandardInput.WriteAsync(stdin);
            process.StandardInput.Close();
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return (process.ExitCode == 0, await stdoutTask, await stderrTask);
    }
}
