# Cascade for Windows

Local video & audio player (Electron). Same look and features as Cascade for Android.

## Get the installer
1. Create a GitHub repo and upload everything in this folder (keep `.github/workflows/build-windows.yml`).
2. Actions -> **Build Windows installer** -> Run workflow.
3. Download the artifact `Cascade-Windows-Setup` -> `Cascade-Setup-1.3.0.exe` -> run it.

Installing a newer build over an older one updates it in place (same app id), keeping your library and watch progress.

## Run from source
`npm install` then `npm start`
