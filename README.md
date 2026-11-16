# FreshOS

An experimental **native Windows .NET 8/WPF desktop shell**, inspired by the supplied sky-blue Frutiger Aero / console mockups. This is an app, not a replacement operating system. Written in **C#**, not C++: .NET 8 has no supported C++/CLI WPF single-file deployment path equivalent to this project.

## Build your EXEs

Requirements: Windows 10/11, [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0), and internet for NuGet restore. No Visual Studio or administrator privileges needed. Download/extract this repository, open Command Prompt **inside its extracted folder**, and paste:

```cmd
build.cmd
```

Or from any directory (substitute your extracted folder):

```cmd
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "C:\path\to\freshos-source\build.ps1"
```

Outputs:
- `%USERPROFILE%\Downloads\freshos\FreshOS-Setup.exe` — per-user installer with embedded app, confirmation, desktop shortcut, optional launch.
- `%USERPROFILE%\Downloads\freshos\FreshOS.exe` — portable, self-contained app; the target PC does not need .NET installed.

For ARM64: `build.cmd -Runtime win-arm64`. The default target is x64. Unsigned binaries can trigger SmartScreen; there is no signing certificate bundled. Review the source before running. Do not bypass protections for files you do not trust.

## Included

- Uploaded cloud wallpaper embedded locally; narrow cyan tube rail, scalloped app/clock lenses, mint speech bubble, optional dock (off by default), rounded glass panels, light/dark themes.
- Boot view, illustrated first-run welcome with reflected title, custom WAV boot sound, user-picked wallpaper, atomically written preferences.
- Background shortcut discovery from per-user/common Start Menu and Desktop; real, cached shell icons; searchable, paginated launcher.
- Actual visible-window enumerator/switcher (Ctrl+Tab inside FreshOS).
- File browser with asynchronous folder listing, virtualized rows, path navigation, launch, folder creation, rename, background file copy, confirmed recycling, background ZIP extraction without overwrite.
- Real system-volume slider and mute via Core Audio, with endpoint-volume polling while the panel is open. Reopen the panel after changing the default audio device.
- Yellow speech-bubble volume/brightness controls with vector smiley companion and custom lime/cyan slider track; shell-only dimmer; host network/settings links.
- Optional idle privacy screen (default 10 minutes), breathing-clock screensaver.
- Version button clicked 10 times reveals “Fourth Wall”: optional sign-in startup and host settings.
- Always-accessible exit, restart-app and rest controls; right-click desktop menu.

## Visual refinement • 0.2

This pass moves the default composition toward the supplied handmade references, rather than a conventional desktop taskbar:

- 52-DIP side rail with a horizontal cylindrical-light gradient and hairline rim.
- 48-tooth vector scalloped lenses, radial cyan falloff, inset lens rings, translucent specular sheen.
- Real app icons loaded off the UI thread; six shortcuts per page keep the compact lens navigable.
- Frozen, deterministic micro-grain brush; subtly layered panel edges and cool ambient shadows.
- Short cubic-eased opacity/scale/translation entrances and 110 ms exits. No per-frame desktop render loop or perpetual wallpaper effects.
- Native keyboard focus indicators, named automation controls, glossy toggles, file-row hover/selection treatments.
- System-aware reduced motion, optional material grain, optional dock, responsive panel limits, uniform launcher scaling.

Artwork is resolution-independent WPF drawing, except the embedded cloud photo. This is **not a verified pixel-for-pixel reproduction**: text metrics, DPI, display size, native app icons and your app count vary. The reference itself is not used as a flat screenshot pretending to be interactive UI. No realtime backdrop blur, ray tracing, or universal external-window skinning is claimed.

## Safety and scope

**F12 and Alt+F4 always exit. F11 toggles fullscreen. Escape dismisses panels/privacy screen.** FreshOS does not capture global keys, prevent Task Manager, disable system controls, request elevation, replace system apps, prevent sleep, or terminate other processes. The privacy screen is not an authentication boundary; use the system account lock for security. Apps launched from shortcuts execute with your normal account permissions. File edits affect real data: back up important files.

External apps open as independent real windows with their original borders; universally embedding/reskinning arbitrary apps is not supported. The dock tracks recently launched shortcuts, not a complete live process/taskbar mirror; use the window switcher for running windows. This does not implement every Windows 11 feature, arbitrary archive formats, a custom terminal, or a full browser. Host notifications and tray icons remain on the host desktop. No screenshot is used as a fake working desktop.

There is no forced kiosk mode or secret-only escape. “Rest” displays a screensaver, without killing applications or altering host power policy. Startup is off by default and opt-in per user.

## Uninstall

1. Turn off sign-in launch in Fourth Wall if you enabled it.
2. Exit FreshOS; delete `%LOCALAPPDATA%\Programs\FreshOS` and the desktop shortcut.
3. Optionally delete `%LOCALAPPDATA%\FreshOS` preferences. If needed remove `%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\FreshOS.cmd` manually.

## Development / validation

`FreshOS/` is the WPF app; `FreshOS.Setup/` is the installer. The builder creates the ignored `payload.zip` before building setup. `.github/workflows/build.yml` builds both on a Windows runner and uploads the EXEs. No binaries are checked in.

Both the application and installer were successfully compiled on a Windows GitHub Actions runner. The build caught and resolved a WPF template-target error and an invalid Thickness constructor. Interactive Windows testing is still outstanding. Verify the checklist below before treating it as stable.

Compiled x64 test build: [FreshOS-win-x64 download](https://github.com/srmayanalexander2-cmyk/for-ai-for-making-fresh-os/actions/runs/43892394803/artifacts/14236579518). Sign in to GitHub if prompted, extract the ZIP, and run FreshOS.exe to test without installing. Artifacts expire according to repository retention settings.

### Windows smoke checklist

- Build x64 and launch portable / installer as a standard user.
- First welcome, wallpaper, light/dark, boot WAV, settings persistence.
- Open real shortcut and switch back via Ctrl+Tab; F12 / Alt+F4 exits.
- Use a disposable folder to create, rename, copy, recycle and extract a trusted ZIP.
- Verify volume buttons on default endpoint, dimmer affects only the shell.
- Set idle timeout to 1 minute and verify privacy screen / unlock.
- Enable/disable startup after 10 version clicks; verify startup file is removed.
- Confirm no shell registration, elevation request or machine power-policy changes.

### Automated source checks

```sh
python -m pip install -r tests/requirements.txt
python tests/check_source.py
```

Checks C# syntax, project/XAML XML, duplicate implicit style keys, and embedded wallpaper signature. These passed in the Linux authoring environment; they do **not** type-check WPF, execute COM, or measure animation smoothness.

### Interactive Windows smoke test

After building, run from an interactive Windows desktop:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tests\Smoke.ps1
```

This opens FreshOS, accepts welcome if shown, invokes the launcher/settings, toggles dark mode twice to check visual-tree rebuilding, and requests normal close. It restores the original theme but can complete first-run onboarding. It has **not** been run in the Linux sandbox.

For visual acceptance, test light/dark at 100%, 125%, 150%, and 200% DPI; fullscreen and 800×600 window; launcher pagination/search; keyboard-only operation; reduced motion; volume with/without audio hardware; large folders and ZIPs; reopening panels rapidly; and F12/Alt+F4 exit. There is no zero-bug or frame-rate guarantee without this hardware testing.
