# Better Clock

A clock, timer and countdown overlay for games. It sits on top of your game, never steals focus,
and you can click straight through it. Everything is customisable.

> Works over games in **borderless / windowed-fullscreen** mode. Games in true "exclusive fullscreen" can hide any overlay, so switch those to borderless.

<img width="187" height="424" alt="image" src="https://github.com/user-attachments/assets/c51e8729-05e9-44b2-a830-8456fec63a81" />
<img width="1338" height="989" alt="image" src="https://github.com/user-attachments/assets/953f247d-13bd-44f3-887d-a43673797fc8" />
<img width="384" height="989" alt="image" src="https://github.com/user-attachments/assets/b47863db-4e2f-498c-aebf-753984a78176" />
<img width="1333" height="983" alt="image" src="https://github.com/user-attachments/assets/9fb66e35-28cb-49a6-a300-c873b17c1fd3" />
<img width="1350" height="993" alt="image" src="https://github.com/user-attachments/assets/e2287e68-4873-43a4-a030-655990f689d3" />
<img width="1314" height="435" alt="image" src="https://github.com/user-attachments/assets/752272fd-4c34-4f78-b2ab-61c374177b71" />
<img width="332" height="164" alt="image" src="https://github.com/user-attachments/assets/75f0c467-dc58-4ace-b37a-83f621973a17" />
<img width="1340" height="490" alt="image" src="https://github.com/user-attachments/assets/d7fbbc33-0e47-4b66-bf7b-3d6a932e6a23" />
<img width="1343" height="539" alt="image" src="https://github.com/user-attachments/assets/69057888-3864-457d-86bf-ee5cd081dadb" />
<img width="1352" height="975" alt="image" src="https://github.com/user-attachments/assets/412d32af-e38f-465b-b299-d21b1e734aa5" />
<img width="1339" height="978" alt="image" src="https://github.com/user-attachments/assets/712e80df-0fda-421e-9d6b-a0a4317ad58e" />
<img width="1319" height="326" alt="image" src="https://github.com/user-attachments/assets/f5ad6632-ca29-4bf5-b8a5-5d5d64aea001" />








## Features

- **Clock** - shows the time for a few seconds (or until you hide it). 12/24-hour, seconds on/off.
- **Timer** - counts up in seconds. Start/stop with one key, reset with another.
- **Countdown** - set the time beforehand. At zero it plays your own sound and stays on screen until you hide it.
- **Global hotkeys** that work inside games, each mode with its own toggle key. All keys are changeable.
- **Each mode has its own look AND its own place**: 3x3 position grid, distance from the edge, monitor choice. All three can be on screen at once.
- **Settings window with tabs and a live preview**: fonts, sizes, colours for every state, background (solid / gradient / none), border, corner roundness, padding, glow, size, show animation and an optional label - separately for Clock, Timer and Countdown.
- **Dark / light / system theme**, accent colour, and an animated interface (can be switched off).
- **Changelog tab** inside the app.
- **Tray icon** with a menu, "start with Windows", and a loading screen when it starts.
- Single file, no installer, no admin rights.

## Default keys

| Key | What it does |
|---|---|
| Ctrl+Alt+C | Show / hide the clock |
| Ctrl+Alt+T | Show / hide the timer |
| Ctrl+Alt+D | Show / hide the countdown |
| Ctrl+Alt+S | Start / stop (timer or countdown, whichever is on screen) |
| Ctrl+Alt+R | Reset (timer back to 00:00, countdown back to full time) |
| Ctrl+Alt+H | Hide all overlays |
| Ctrl+Alt+M | Move mode (drag overlays, press again to lock) |
| Ctrl+Alt+B | Open settings |

Optional keys (unset by default) can control only the Timer or only the Countdown.

Change any of them in **Settings > Keys**.

## Build it yourself (Windows)

1. Double-click `build.bat`.
2. Your app is made at `dist\BetterClock.exe`.

Nothing to install: it uses the C# compiler that is already part of Windows (.NET Framework 4.x).

## Making a GitHub release

1. Run `build.bat` and check `dist\BetterClock.exe` works.
2. On GitHub: **Releases > Draft a new release**.
3. Tag: `v1.1.0`. Title: `Better Clock v1.1.0`. Paste in `RELEASE_NOTES_v1.1.0.md`.
4. Attach `dist\BetterClock.exe`.
5. Publish.

## Settings file

Saved at `%APPDATA%\BetterClock\settings.ini`. You can edit it, or use the settings window.

## FAQ

**The overlay doesn't show in my game.** Set the game to Borderless / Windowed Fullscreen.

**Will it get me banned?** It is a separate window. It does not touch or inject into the game. Some strict anti-cheat systems dislike any overlay or global hotkey, so check your game's rules.

**Windows says "unknown publisher".** The EXE is not code-signed. That is normal for small open-source tools.

## Roadmap

- Saved presets (for example "Gaming" and "Streaming")

## License

MIT
