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
    ["powershell"] = new("mcr.microsoft.com/powershell:lts", "script.ps1", "pwsh /app/script.ps1", ReadOnly: false),
    ["csharp"] = new("mcr.microsoft.com/dotnet/sdk:10.0", "script.cs", "dotnet run /app/script.cs", DisableNetwork: false, ReadOnly: false),
};

var runnerConfig = builder.Configuration.GetSection("Runner");
var backend = runnerConfig["Backend"] ?? "Docker";

IScriptRunner scriptRunner = backend.Equals("Kubernetes", StringComparison.OrdinalIgnoreCase)
    ? new KubernetesScriptRunner(
        @namespace: runnerConfig["Kubernetes:Namespace"] ?? "default",
        timeoutSeconds: runnerConfig.GetValue<int>("Kubernetes:TimeoutSeconds", 600))
    : new DockerScriptRunner(true);

// Pre-pull container images
foreach (var config in languageConfigs.Values)
{
    await scriptRunner.PullImageAsync(config.DockerImage);
}

app.MapPost("/run", async (ScriptRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Code))
    {
        return Results.BadRequest("Code is empty");
    }

    var language = req.Language ?? "python";

    if (!languageConfigs.TryGetValue(language, out var config))
    {
        return Results.BadRequest($"Unsupported language: '{language}'. Supported: {string.Join(", ", languageConfigs.Keys)}");
    }

    var sessionId = req.SessionId ?? Guid.NewGuid().ToString();
    var workDir = Path.Combine(Path.GetTempPath(), "runner", sessionId);
    Directory.CreateDirectory(workDir);

    var scriptPath = Path.Combine(workDir, config.FileName);
    await File.WriteAllTextAsync(scriptPath, req.Code);

    try
    {
        var result = await scriptRunner.RunAsync(config, workDir, req.Arguments ?? []);

        if (result.TimedOut)
        {
            return Results.Ok(new
            {
                success = false,
                error = "Execution timeout"
            });
        }

        return Results.Ok(new
        {
            sessionId,
            success = result.Success,
            stdout = result.Stdout,
            stderr = result.Stderr
        });
    }
    finally
    {
        // cleanup
        if (!req.KeepSession)
        {
            try
            {
                Directory.Delete(workDir, true);
            }
            catch { }
        }
    }
});

app.MapDelete("/sessions/{sessionId}", (string sessionId) =>
{
    var workDir = Path.Combine(Path.GetTempPath(), "runner", sessionId);

    if (!Directory.Exists(workDir))
    {
        return Results.NotFound($"Session '{sessionId}' not found.");
    }

    try
    {
        Directory.Delete(workDir, true);
        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to delete session: {ex.Message}");
    }
});

app.Run();

record ScriptRequest(string Code, string? Language = null, string[]? Arguments = null, bool KeepSession = false, string? SessionId = null);

record LanguageConfig(string DockerImage, string FileName, string RunCommand, string? ExtraDockerArgs = null, bool DisableNetwork = true, bool ReadOnly = true);
