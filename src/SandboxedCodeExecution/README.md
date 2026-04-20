# Sandboxed Code Execution

A minimal ASP.NET Core API that executes user-submitted scripts inside isolated Docker containers.

## Supported Languages

| Language   | Image                              |
|------------|------------------------------------|
| python     | python:3.11                        |
| node       | node:20                            |
| powershell | mcr.microsoft.com/powershell:lts   |
| csharp     | mcr.microsoft.com/dotnet/sdk:10.0  |

## API

### `POST /run`

```json
{
  "code": "print('hello')",
  "language": "python",
  "arguments": ["arg1", "arg2"]
}
```

**Response:**

```json
{
  "success": true,
  "stdout": "hello\n",
  "stderr": ""
}
```

## Examples

### Python

```json
{
  "Code": "print('hello from docker')"
}
```

### Node.js

```json
{
  "Code": "console.log('hello from node');",
  "Language": "node"
}
```

### PowerShell

```json
{
  "Code": "Write-Output 'hello from powershell'",
  "Language": "powershell"
}
```

### C#

```json
{
  "Code": "Console.WriteLine(\"hello from csharp\");",
  "Language": "csharp"
}
```

### C# with arguments

```json
{
  "Code": "Console.WriteLine($\"Arguments: {string.Join(\", \", args)}\");",
  "Language": "csharp",
  "Arguments": ["arg1", "arg2", "arg3"]
}
```

## Running Locally

```bash
dotnet run --project SandboxedCodeExecution
```

