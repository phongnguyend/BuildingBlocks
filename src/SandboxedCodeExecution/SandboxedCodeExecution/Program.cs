using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var languageConfigs = new Dictionary<string, LanguageConfig>(StringComparer.OrdinalIgnoreCase)
{
    ["python"] = new("python:3.11", "script.py", "python /app/script.py"),
    ["node"] = new("node:20", "script.js", "node /app/script.js"),
    ["powershell"] = new("mcr.microsoft.com/powershell:lts", "script.ps1", "pwsh /app/script.ps1", ExtraDockerArgs: "--tmpfs /tmp"),
};

app.MapPost("/run", async (ScriptRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Code))
        return Results.BadRequest("Code is empty");

    var language = req.Language ?? "python";

    if (!languageConfigs.TryGetValue(language, out var config))
        return Results.BadRequest($"Unsupported language: '{language}'. Supported: {string.Join(", ", languageConfigs.Keys)}");

    var jobId = Guid.NewGuid().ToString();
    var workDir = Path.Combine(Path.GetTempPath(), "runner", jobId);
    Directory.CreateDirectory(workDir);

    var scriptPath = Path.Combine(workDir, config.FileName);
    await File.WriteAllTextAsync(scriptPath, req.Code);

    var scriptArgs = string.Empty;
    if (req.Arguments is { Length: > 0 })
    {
        scriptArgs = " " + string.Join(" ", req.Arguments.Select(a => $"\"{a}\""));
    }

    var dockerArgs = $@"
run --rm
--network none
--memory 128m
--cpus 0.5
--read-only
{config.ExtraDockerArgs ?? ""}
-v ""{workDir}:/app""
{config.DockerImage}
{config.RunCommand}{scriptArgs}
";

    dockerArgs = dockerArgs.Replace(Environment.NewLine, " ");

    var psi = new ProcessStartInfo
    {
        FileName = "docker",
        Arguments = dockerArgs,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    try
    {
        using var process = new Process { StartInfo = psi };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        // timeout 5s
        var timeoutTask = Task.Delay(30000);

        var completed = await Task.WhenAny(
            process.WaitForExitAsync(),
            timeoutTask
        );

        if (completed == timeoutTask)
        {
            try { process.Kill(); } catch { }
            return Results.Ok(new
            {
                success = false,
                error = "Execution timeout"
            });
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        return Results.Ok(new
        {
            success = process.ExitCode == 0,
            stdout,
            stderr
        });
    }
    finally
    {
        // cleanup
        try
        {
            Directory.Delete(workDir, true);
        }
        catch { }
    }
});

app.Run();

record ScriptRequest(string Code, string? Language = null, string[]? Arguments = null);

record LanguageConfig(string DockerImage, string FileName, string RunCommand, string? ExtraDockerArgs = null);
