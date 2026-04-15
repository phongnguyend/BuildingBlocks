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

app.MapPost("/run", async (ScriptRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Code))
        return Results.BadRequest("Code is empty");

    var jobId = Guid.NewGuid().ToString();
    var workDir = Path.Combine(Path.GetTempPath(), "runner", jobId);
    Directory.CreateDirectory(workDir);

    var scriptPath = Path.Combine(workDir, "script.py");
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
-v ""{workDir}:/app""
python:3.11
python /app/script.py{scriptArgs}
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
        var timeoutTask = Task.Delay(5000);

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

record ScriptRequest(string Code, string[]? Arguments = null);
