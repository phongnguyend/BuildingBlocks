using System.Diagnostics;

class DockerScriptRunner(bool isRemote = false) : IScriptRunner
{
    public async Task PullImageAsync(string dockerImage)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        psi.ArgumentList.Add("pull");
        psi.ArgumentList.Add(dockerImage);

        Console.WriteLine($"Pulling image: {dockerImage}...");

        using var process = Process.Start(psi)!;

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (!string.IsNullOrWhiteSpace(stdout))
        {
            Console.WriteLine(stdout);
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            Console.Error.WriteLine(stderr);
        }

        Console.WriteLine(process.ExitCode == 0
            ? $"Successfully pulled: {dockerImage}"
            : $"Failed to pull: {dockerImage} (exit code {process.ExitCode})");
    }

    public Task<ScriptRunResult> RunAsync(LanguageConfig config, string workDir, string[] extraArguments, int timeoutMs = 600000)
        => isRemote
            ? RunRemoteAsync(config, workDir, extraArguments, timeoutMs)
            : RunLocalAsync(config, workDir, extraArguments, timeoutMs);

    // Local: bind-mount the workDir directly into the container.
    // Fast and simple; requires the Docker daemon to be on the same machine.
    private async Task<ScriptRunResult> RunLocalAsync(LanguageConfig config, string workDir, string[] extraArguments, int timeoutMs)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--rm");

        if (config.DisableNetwork)
        {
            psi.ArgumentList.Add("--network");
            psi.ArgumentList.Add("none");
        }

        psi.ArgumentList.Add("--memory");
        psi.ArgumentList.Add("256m");
        psi.ArgumentList.Add("--cpus");
        psi.ArgumentList.Add("1.0");

        if (config.ReadOnly)
        {
            psi.ArgumentList.Add("--read-only");
        }

        if (!string.IsNullOrWhiteSpace(config.ExtraDockerArgs))
        {
            foreach (var arg in config.ExtraDockerArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                psi.ArgumentList.Add(arg);
            }
        }

        psi.ArgumentList.Add("-v");
        psi.ArgumentList.Add($"{workDir}:/app");
        psi.ArgumentList.Add(config.DockerImage);

        foreach (var part in config.RunCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            psi.ArgumentList.Add(part);
        }

        if (extraArguments is { Length: > 0 })
        {
            foreach (var arg in extraArguments)
            {
                psi.ArgumentList.Add(arg);
            }
        }

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        var timeoutTask = Task.Delay(timeoutMs);

        var completed = await Task.WhenAny(process.WaitForExitAsync(), timeoutTask);

        if (completed == timeoutTask)
        {
            try { process.Kill(); } catch { }
            return new ScriptRunResult(Success: false, Stdout: string.Empty, Stderr: string.Empty, TimedOut: true);
        }

        return new ScriptRunResult(
            Success: process.ExitCode == 0,
            Stdout: await stdoutTask,
            Stderr: await stderrTask,
            TimedOut: false);
    }

    // Remote: docker create → docker cp → docker start --attach.
    // docker cp transfers the script over the Docker API so no local path needs to exist on the daemon host.
    // --read-only is omitted because it disables the container's writable layer, which docker cp requires.
    // A tmpfs at /tmp provides a writable scratch space instead.
    private async Task<ScriptRunResult> RunRemoteAsync(LanguageConfig config, string workDir, string[] extraArguments, int timeoutMs)
    {
        var containerName = $"runner-{Guid.NewGuid().ToString("N")[..16]}";

        // 1. Create the container without starting it (cleanup is explicit in finally)
        var createPsi = new ProcessStartInfo
        {
            FileName = "docker",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        createPsi.ArgumentList.Add("create");
        createPsi.ArgumentList.Add("--name");
        createPsi.ArgumentList.Add(containerName);

        if (config.DisableNetwork)
        {
            createPsi.ArgumentList.Add("--network");
            createPsi.ArgumentList.Add("none");
        }

        createPsi.ArgumentList.Add("--memory");
        createPsi.ArgumentList.Add("256m");
        createPsi.ArgumentList.Add("--cpus");
        createPsi.ArgumentList.Add("1.0");

        if (!string.IsNullOrWhiteSpace(config.ExtraDockerArgs))
        {
            foreach (var arg in config.ExtraDockerArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                createPsi.ArgumentList.Add(arg);
            }
        }

        createPsi.ArgumentList.Add(config.DockerImage);

        foreach (var part in config.RunCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            createPsi.ArgumentList.Add(part);
        }

        if (extraArguments is { Length: > 0 })
        {
            foreach (var arg in extraArguments)
            {
                createPsi.ArgumentList.Add(arg);
            }
        }

        var createResult = await RunProcessAsync(createPsi);
        if (!createResult.Success)
        {
            return new ScriptRunResult(Success: false, Stdout: string.Empty, Stderr: $"Failed to create container: {createResult.Stderr}", TimedOut: false);
        }

        try
        {
            // 2. Copy script directory into the container via the Docker API.
            //    workDir/. copies the contents and creates /app inside the container if it doesn't exist.
            var cpPsi = new ProcessStartInfo
            {
                FileName = "docker",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            cpPsi.ArgumentList.Add("cp");
            cpPsi.ArgumentList.Add($"{workDir}/.");
            cpPsi.ArgumentList.Add($"{containerName}:/app");

            var cpResult = await RunProcessAsync(cpPsi);
            if (!cpResult.Success)
            {
                return new ScriptRunResult(Success: false, Stdout: string.Empty, Stderr: $"Failed to copy script: {cpResult.Stderr}", TimedOut: false);
            }

            // 3. Start the container and attach to its output
            var startPsi = new ProcessStartInfo
            {
                FileName = "docker",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startPsi.ArgumentList.Add("start");
            startPsi.ArgumentList.Add("--attach");
            startPsi.ArgumentList.Add(containerName);

            using var process = new Process { StartInfo = startPsi };
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            var timeoutTask = Task.Delay(timeoutMs);

            var completed = await Task.WhenAny(process.WaitForExitAsync(), timeoutTask);

            if (completed == timeoutTask)
            {
                try { process.Kill(); } catch { }
                return new ScriptRunResult(Success: false, Stdout: string.Empty, Stderr: string.Empty, TimedOut: true);
            }

            return new ScriptRunResult(
                Success: process.ExitCode == 0,
                Stdout: await stdoutTask,
                Stderr: await stderrTask,
                TimedOut: false);
        }
        finally
        {
            // Force-remove the container (stops it first if still running due to timeout)
            var rmPsi = new ProcessStartInfo
            {
                FileName = "docker",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            rmPsi.ArgumentList.Add("rm");
            rmPsi.ArgumentList.Add("-f");
            rmPsi.ArgumentList.Add(containerName);
            await RunProcessAsync(rmPsi);
        }
    }

    private async Task<(bool Success, string Stdout, string Stderr)> RunProcessAsync(ProcessStartInfo psi)
    {
        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return (process.ExitCode == 0, await stdoutTask, await stderrTask);
    }
}
