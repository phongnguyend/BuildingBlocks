# AI Mapping Sample

Sample application with an ASP.NET Core API backend and a React frontend for importing CSV/XLSX files, detecting the header row, suggesting mappings to predefined DTO fields, letting the user confirm the mapping, and displaying mapped rows in a grid.

## Structure

- `backend/AiMapping.Api` - ASP.NET Core API.
- `frontend` - React + Vite frontend.

## Run

Backend:

```powershell
cd backend/AiMapping.Api
dotnet run
```

Frontend:

```powershell
cd frontend
npm install
npm run dev
```

Optional OpenAI-powered header mapping:

```powershell
cd backend/AiMapping.Api
dotnet user-secrets set "OpenAI:ApiKey" "<your-api-key>"
dotnet user-secrets set "OpenAI:Model" "gpt-4o-mini"
```

Without an API key, the backend uses a deterministic similarity mapper.
