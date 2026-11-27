# Cascade for Windows (native)

A fully native Windows media player: C# + WPF for the interface and the VLC engine (LibVLC) for playback.
No Chromium / Electron / web view. Plays practically every video and audio format (MKV, AVI, WMV, FLV, MP4, MOV,
TS, WebM, AC3/DTS audio, FLAC, Opus...).

## Build (GitHub Actions)
1. Create a repo and upload everything in this folder (keep `.github/workflows`).
2. Actions -> **Build Cascade for Windows (native)** -> Run workflow.
3. Download the `Cascade-Windows` artifact:
   - `Cascade-Setup-2.0.0.exe` - installer (per-user, no admin needed). Adds the right-click menu.
   - `Cascade-Portable.zip` - portable: unzip anywhere and run `Cascade.exe`. Settings stay in a `CascadeData` folder next to it.

## Right-click menu (File Explorer)
Installed version only:
- on any video/audio file: **Play with Cascade** (select several files: they are queued together)
- on a folder: **Play with Cascade** and **Add to Cascade library**
On Windows 11 these appear under **Show more options** (Shift+F10).
Cascade also shows up in **Open with**.

## Features
Library with folders view, hero carousel, search/sort, light/dark theme, resume where you left off, double-click
sides to skip, hold to speed up (configurable), volume up to 200%, subtitles (auto-loads same-name files),
audio tracks, aspect ratio, screenshots (Pictures\Cascade), audio-only mode, fullscreen, always-on-top, sleep timer,
repeat modes, taskbar progress, battery + clock, keyboard shortcuts (see More panel).
