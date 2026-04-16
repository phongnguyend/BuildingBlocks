# Sandboxed Code Execution

A minimal ASP.NET Core API that executes user-submitted scripts inside isolated Docker containers.

## Supported Languages

| Language   | Image                              |
|------------|------------------------------------|
| python     | python:3.11                        |
| node       | node:20                            |
| powershell | mcr.microsoft.com/powershell:lts   |

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

## Running Locally

```bash
dotnet run --project SandboxedCodeExecution
```

