# HSLRE (fork) for Honey Select

A drop-in replacement for Joan6694's **HSLRE** (HSLRE.dll 5.2.0.7 beta - lighting and rendering effects, including
HSIBL) with a new settings window and extra tools. Scenes and presets made with the original load unchanged.

## Install

Copy into `Plugins\` (replace the old `HSLRE.dll`):

| File | What it is |
|---|---|
| `HSLRE.dll` | the plugin (same name as the original) |
| `HSUIKit.dll` | the UI library the window is built with - [HSUIKit](https://github.com/mucski/HSUIKit) (required) |

F5 opens the window in Studio, the character maker and H scenes. For a Studio toolbar button, set `ToolbarButton=1` in
`UserData\modprefs.ini`, section `[HSIBL]`.

## What's new

- New window: effects listed with on / off switches, search, an "On only" filter, drag to reorder, a Pinned group
  for favourites.
- **Presets** page with thumbnails.
- **Before / after compare** against a snapshot of the settings.
- **Reset to defaults** per effect, **undo / redo** of settings (Ctrl+Z / Ctrl+Y over the window).
- **Cost per effect**: measures how many milliseconds each enabled effect takes.
- **Color Looks** (color grading presets), **Screen Overlays**, **Animated Sky** (moving clouds).
- Fixes: the character maker picture is no longer upside down while HSLRE is off; the SEGI "Visualize GBuffer"
  dropdown stays open.

## Building

Visual Studio 2019+ / MSBuild, .NET Framework 3.5 target. Clone [HSUIKit](https://github.com/mucski/HSUIKit) next
to this repository and build it first:

```
msbuild ..\HSUIKit\HSUIKit.csproj /p:Configuration=Release /p:HSDir="C:\path\to\HoneySelect"
msbuild HSLRE.csproj /p:Configuration=Release /p:HSDir="C:\path\to\HoneySelect"
```

`Upstream\` holds Joan's HSLRE source; `Kit\` is the new window and the new tools. The overlay and sky shaders are
prebuilt into `Kit\Resources` (Unity 5.3.5 AssetBundle).

## Credits and license

- HSLRE / HSIBL: Joan6694 - the original plugin this fork is based on, including the effects it bundles (Unity
  Standard Assets image effects, SMAA, Amplify Bloom, SEGI and others, each under its own license)
- HSUIKit: see its repository

Licensed under the GNU General Public License v3.0 - see `LICENSE`.
