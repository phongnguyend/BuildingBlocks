interface IScriptRunner
{
    Task PullImageAsync(string dockerImage);
    Task<ScriptRunResult> RunAsync(LanguageConfig config, string workDir, string sessionId, string[] extraArguments, int timeoutMs = 600000);
    Task<bool> DeleteSessionAsync(string sessionId);
}

record ScriptRunResult(bool Success, string Stdout, string Stderr, bool TimedOut);
