# FreshOS visual implementation notes

## The reference, not a generic reskin

The key silhouette is the narrow cyan tube on the left and the partially off-screen launch lens, over an unobstructed cloud photo. Opening All Apps reveals a larger scalloped lens, an offset clock lens, and a mint status bubble. The bottom dock is available but disabled by default. No new brand banner covers the sky.

The screenshots use a fixed composition. The implementation uses device-independent units and scales the launcher artboard uniformly down when space is limited. App names remain actual app names; screenshots' particular shortcuts are not fake hard-coded apps.

## Material stack

1. Embedded sky photograph, decoded at up to 3840 pixels wide instead of keeping a full 8K decoded texture.
2. Vector cyan radial lenses: pale central light, saturated mid-radius, shaded outer falloff.
3. A 96-vertex outline alternates between 0.5 and 0.474 radius (48 teeth).
4. Faint 0.7/1-DIP rim and inset-ring strokes provide the small surface cues.
5. Panel-only deterministic grain: a frozen 64×64 tiled DrawingBrush, no randomly regenerating animation.
6. Thin inner and outer panel edges and a small blue-tinted ambient shadow.
7. Interactive content, real shortcut icons and keyboard focus indicators.

The tube rail uses a white → cyan → pale cyan → white horizontal ramp. Control bubbles intentionally use warm yellow instead of the main cyan/mint palette, with a vector smiley matching the references' companion idea. The slider retains the lime filled section, cyan remaining section, and a tiny metallic-looking thumb cap.

## Motion contract

- Enter: 280 ms cubic ease-out, scale 0.965→1, translate Y 12→0; opacity enters over 180 ms.
- Leave: 110 ms fade; outgoing elements immediately stop accepting pointer input.
- Repeated open/close operations each remove their own old surface, not a shared reference to the new surface.
- No global wallpaper animation, blur shader or continuous render callback.
- Reduced motion comes from either FreshOS settings or the host's client-area animation setting.
- Audio polling runs only while its panel is loaded and stops before COM resources are released.

## Responsiveness work

App scanning, shortcut icon lookup, directory enumeration, file copying and ZIP extraction run off the UI thread. Icons are cached as frozen BitmapSources and native icon handles are destroyed. File rows use recycling virtualization. A late folder-read result is discarded if its panel has been replaced. Background operations do not steal a newly opened panel when they finish.

## Functional correction • 0.3

A centered 166-DIP launcher was covering the lower side-rail controls at common window sizes. It is now top-anchored below the 394-DIP rail, and rail hit targets have explicit stacking priority. Windows tests check the hit-test ancestry of all four lower rail controls at both supported minimum and normal test sizes.

The former Start-only decorative tray status is now a shortcut to an independent live-window tray. Tray state is separate from the main content panel. A second process supplies a real test window so CI verifies native enumeration and removal when that window closes. `EnumWindows` now uses a concrete Winapi delegate rather than an unmarshalable generic callback.

## Acceptance status

Portable XML, duplicate-resource, wallpaper-signature and C# parser checks pass. Windows CI successfully compiled both EXEs in run 43892394803. COM behavior, rendered composition, UI automation, DPI coverage and frame pacing still need interactive Windows validation. A source parser cannot establish those results. The repository includes build CI and an interactive smoke-test script for that next step.

0.3: Windows build and automated WPF interaction checks passed in run 43896505500, including rail hit targets, independent tray/Start state, real external-window listing, async file listing, dimmer response, theme rebuild and privacy-screen stacking. Test reports and rendered PNGs are published alongside the build. This does not certify all inputs, DPI settings, audio hardware or file-operation outcomes.

## Layout corrections • 0.4

The launcher now uses measured columns and rows instead of an absolute-coordinate artboard: a 396-DIP app lens, separate 250-DIP clock lens, and two-column shortcut buttons. The clock has its own 180-DIP text measure. A persistent desktop clock remains visible with Start closed. The rail is 64 DIPs wide to fit time/date without wrapping.

Vector arrows are separate visual components rather than changing Unicode glyphs. Their explicit `IsOpen` state drives a 240 ms eased 0↔180° rotation; reduced motion immediately sets the same end state. Tray and Start each own their arrow state.

Fullscreen uses native monitor bounds through SetWindowPos, avoiding the taskbar-sized gap left by ordinary maximization. NoResize/None chrome removes the shell’s native resize/maximize affordances. A FreshOS palette controls its own fullscreen/center/half-width positions; it does not intercept other applications' snap or taskbar behavior.

Setup now has dedicated wizard pages with scrolling content and fixed footer navigation. Both binaries share the multi-resolution icon. The installer extracts into a staging directory before replacing an existing installation, creates per-user icon-bearing shortcuts, and marks the installed app's welcome setup complete. This is not an MSI package or an automatic replacement for the system desktop.

Windows build 43901111868 passed the expanded interaction suite, including native fullscreen bounds, time/date text fitting and non-overlap, both arrow animation endpoints, and setup resource loading. This does not establish that every text element at every DPI or every installer failure path is correct; manual visual and installation testing remains required.
