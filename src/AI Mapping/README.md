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

## Android

The frontend is configured with Capacitor for Android.

```powershell
cd frontend
npm install
npx cap add android
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

The Android mobile build uses `frontend/.env.android`, which points API requests at `http://10.0.2.2:5088` for Android emulator access to the host machine backend. Use your machine LAN IP instead for a physical device.

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

### iOS

The frontend is also configured with Capacitor for iOS. The iOS project can be generated and synced from this repo, but build, simulator run, device test, archive, TestFlight, and App Store publishing require macOS with Xcode and Xcode Command Line Tools.

Capacitor 8 requires Node 22 or higher and Xcode 26.0 or higher for iOS development. The iOS mobile build uses `frontend/.env.ios`, which points API requests at `http://localhost:5088` for iOS simulator access to the Mac host backend. Use your machine LAN IP or an HTTPS API endpoint for a physical iPhone.

On a Mac:

```bash
cd frontend
npm install
npx cap add ios
npm run mobile:sync:ios
```

Open the native project in Xcode:

```bash
npm run mobile:open:ios
```

Run on an iOS simulator or paired device:

```bash
npm run mobile:run:ios
```

In Xcode, select the `App` scheme, choose a simulator or device, configure signing under `Signing & Capabilities`, then run with the play button. For a physical device or TestFlight/App Store distribution, use an Apple Developer account and a valid bundle identifier.

Test locally:

```bash
npm run mobile:sync:ios
npm run mobile:run:ios
```

Archive and publish from Xcode:

1. In Xcode, set the app version and build number.
2. Select a generic iOS device or `Any iOS Device`.
3. Choose `Product > Archive`.
4. In Organizer, validate the archive.
5. Use `Distribute App > App Store Connect > Upload`.
6. In App Store Connect, use TestFlight for beta testing or submit the build for App Review.

Apple TestFlight builds are available for testing for up to 90 days. External TestFlight testers may require beta app review before they can install the build.

Useful references:

- Capacitor iOS setup: https://capacitorjs.com/docs/ios
- Capacitor environment setup: https://capacitorjs.com/docs/getting-started/environment-setup
- Apple archive/upload flow: https://help.apple.com/xcode/mac/current/en.lproj/dev442d7f2ca.html
- TestFlight overview: https://developer.apple.com/help/app-store-connect/test-a-beta-version/testflight-overview/
