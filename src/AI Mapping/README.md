# AI Mapping Sample

Sample application with an ASP.NET Core API backend and a React frontend for importing CSV/XLSX files, detecting the header row, suggesting mappings to predefined standard fields, letting the user confirm the mapping, and displaying mapped rows in a grid.

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

## Mobile

The frontend is configured with Capacitor for Android.

```powershell
cd frontend
npm install
npm run mobile:sync:android
```

Build a debug APK:

```powershell
npm run mobile:build:android
```

Run on an Android emulator or connected device:

```powershell
npm run mobile:run:android
```

The mobile build uses `frontend/.env.mobile`, which points API requests at `http://10.0.2.2:5088` for Android emulator access to the host machine backend. Use your machine LAN IP instead for a physical device.

For local Android development, `frontend/capacitor.config.json` uses `server.androidScheme = "http"` to avoid mixed-content blocking when the WebView calls the local HTTP backend. For production, use an HTTPS API endpoint and switch the scheme back to `https`.

Debug the Android WebView with Chrome:

```powershell
& "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" devices
```

Open Chrome on the desktop and navigate to:

```text
chrome://inspect/#devices
```

Find the app WebView and click `Inspect`. Use the DevTools `Console` and `Network` tabs to inspect upload failures, CORS errors, mixed-content errors, and calls to `/api/import/analyze`.

If Gradle cannot find Java or the Android SDK, set these values in the current PowerShell session before building:

```powershell
$env:JAVA_HOME = "C:\Program Files\Android\Android Studio\jbr"
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
$env:ANDROID_SDK_ROOT = $env:ANDROID_HOME
$env:Path = "$env:JAVA_HOME\bin;$env:ANDROID_HOME\platform-tools;$env:ANDROID_HOME\emulator;$env:Path"
```

If Gradle still reports `SDK location not found`, create `frontend/android/local.properties` with:

```properties
sdk.dir=C:/Users/phongnguyend/AppData/Local/Android/Sdk
```

`local.properties` is machine-specific and ignored by Git.
