# Development

## Build

Needs the .NET SDK (9 or later) and copies of the game's DLLs:
`<Valheim>/valheim_Data/Managed/*.dll` and `<Valheim>/BepInEx/core/*.dll`, in a folder
laid out as `Managed/` and `BepInEx-core/`. Point the build at it by copying
`local.props.example` to `local.props` (gitignored) and setting `GameRefs`, or pass
`-p:GameRefs=<dir>`.

```
dotnet build -c Release WindRunDisplay.csproj   # -> bin/Release/WindRunDisplay.dll
dotnet test tests/WindRunDisplay.Tests.csproj   # Unity-free math/format tests (net10.0)
```

Debug symbols are embedded in the DLL (`DebugType=embedded`), so it is a single file and
stack traces have line numbers.

## Install a dev build

Copy `bin/Release/WindRunDisplay.dll` into `<Valheim>/BepInEx/plugins/` and restart the
game.

### Hot reload

With [ScriptEngine](https://github.com/BepInEx/BepInEx.Debug) installed (set
`LoadOnStart` and `EnableFileSystemWatcher` to `true` in its config), copy the DLL into
`<Valheim>/BepInEx/scripts/` instead. ScriptEngine reloads it in the running game a few
seconds after the copy, or on its reload key (F6 by default). The plugin unpatches itself
in `OnDestroy`, so reloads don't stack patches. Keep only one copy (plugins or scripts),
or the patches apply twice. The mod's config file is re-read on each reload.

## Testing in game

In a local (self-hosted) world (`wind` is server-only, so not on a dedicated server),
press F5 and run `devcommands`, then `wind <angle> <intensity>` (e.g. `wind 0 1`) to set
a fixed wind, and `resetwind` to go back to normal weather. Running with the wind at
intensity 1 should show the full +25%, across it about +13%, into it +0%.

## Icons

- `assets/icon-source.png`: in-game screenshot of the Wind run status effect.
- `assets/icon-clean.png`: background removed, transparent; ships as the package icon.
  Rebuild with `scripts/make-icon-clean.py` (needs Pillow, numpy, scipy; see its
  docstring).
- `assets/icon.png`: alternative with a blurred-background fill. Rebuild with
  `bash scripts/make-icon.sh`.

## Release

1. Bump the version in `manifest.json`, `src/Plugin.cs` (`Version`) and
   `WindRunDisplay.csproj` (`<Version>`), and add a `CHANGELOG.md` entry.
2. `bash scripts/package.sh` builds `dist/WindRunDisplay-<version>.zip` (Thunderstore
   layout). It fails on a version mismatch between those three files or on failing tests.
3. Upload the zip on Thunderstore (Valheim community) with the categories Mods,
   Client-side, Utility, Deep North Update, AI Generated.

The README is shipped inside the package and rendered on Thunderstore, so links and
images in it must be absolute URLs.
