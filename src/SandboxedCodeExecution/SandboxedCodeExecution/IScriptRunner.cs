interface IScriptRunner
{
    Task PullImageAsync(string dockerImage);
    Task<ScriptRunResult> RunAsync(LanguageConfig config, string workDir, string[] extraArguments, int timeoutMs = 600000);
}

record ScriptRunResult(bool Success, string Stdout, string Stderr, bool TimedOut);
