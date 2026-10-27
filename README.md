# valheim-wind-run-display

Client-side BepInEx mod for Valheim that shows how much the **Wind run** effect
(Asksvin cloak) is currently giving you, based on wind direction and strength.

![Wind run speed climbing from +0% to +25% as the player turns to run with the wind](https://raw.githubusercontent.com/Surxe/valheim-wind-run-display/main/docs/wind-run.gif)

- Under the Wind run icon in the status-effect bar: the live speed bonus, by default
  `Speed: +18%`. Which lines appear, their order, their labels, and the font size are all
  configurable (see Config), e.g.:
  ```
  Speed: +18%
  Run stamina: -72%
  Wind: 80%
  ```
- In the effect's tooltip: max and current values for speed and stamina; the vanilla
  tooltip lists neither.

While moving, the number uses your move direction (the same vector the game uses).
While standing still, it previews the camera's facing, i.e. what you'd get by running
forward.

**Compatibility:** Valheim 1.0 (Deep North), BepInEx pack 5.4.2350+. Client-side only;
nothing to install on servers.

**Made by AI:** this mod (code, docs, icon and packaging) was written by an AI coding
assistant (Claude), directed and play-tested by the author.

## How Wind run works (from `assembly_valheim`)

`SE_Stats.ModifySpeed` and `SE_Stats.ModifyRunStaminaDrain`:

```
factor = (dot(moveDir, windDir) + 1) / 2 * windIntensity * modifier
speed *= 1 + factor          // modifier = m_windMovementModifier
runDrain *= 1 + factor       // modifier = m_windRunStaminaModifier
```

So a full tailwind at max intensity gives the full modifier, a crosswind gives half,
and a headwind gives nothing (never a penalty). The modifiers are read from the live
status effect, not hardcoded.

## Build

Needs the .NET SDK and copies of the game's DLLs: `valheim_Data/Managed/*.dll` and
`BepInEx/core/*.dll`, in a folder laid out as `Managed/` and `BepInEx-core/`. Point the
build at it by copying `local.props.example` to `local.props` (gitignored) and setting
`GameRefs`, or pass `-p:GameRefs=<dir>`:

```
dotnet build -c Release WindRunDisplay.csproj                     # -> bin/Release/WindRunDisplay.dll
dotnet test tests/WindRunDisplay.Tests.csproj                     # Unity-free math/format tests
```

## Install

Copy `bin/Release/WindRunDisplay.dll` into `<Valheim>/BepInEx/plugins/` and restart the
game.

### Hot reload while developing

With [ScriptEngine](https://github.com/BepInEx/BepInEx.Debug) installed (set
`LoadOnStart` and `EnableFileSystemWatcher` to `true` in its config), copy the DLL into
`<Valheim>/BepInEx/scripts/` instead. ScriptEngine reloads it in the running game a few
seconds after the copy, or on its reload key (F6 by default). The plugin unpatches itself
in `OnDestroy`, so reloads don't stack patches. Keep only one copy (plugins or scripts),
or the patches apply twice.

## Config

`BepInEx/config/surxe.WindRunDisplay.cfg` (re-read on a ScriptEngine reload, or on game
start):

| Section | Key | Default | Effect |
|---------|-----|---------|--------|
| Icon | `Fields` | `RunSpeed` | Lines under the icon, top to bottom, comma-separated: `RunSpeed`, `RunStamina`, `Wind`. Empty = no text |
| Icon | `RunSpeedFormat` | `Speed: %s` | Label for `RunSpeed`; `%s` is replaced by the value (e.g. `+18%`) |
| Icon | `RunStaminaFormat` | `Run stamina: %s` | Label for `RunStamina` (e.g. `-72%`) |
| Icon | `WindFormat` | `Wind: %s` | Label for `Wind`, the intensity regardless of direction (e.g. `80%`) |
| Icon | `IconFontSize` | `0` | `0` = the game's own text box, untouched (it auto-shrinks longer or multi-line text). Above `0` = that fixed size, no shrinking, lines extend below the icon |
| Tooltip | `ExtendTooltip` | `true` | Add wind run values to the effect tooltip |

A label without `%s` gets the value appended after a space. Unknown `Fields` entries are
skipped with a warning in the BepInEx log.

## Testing in game

In a local (self-hosted) world (`wind` is server-only, so not on a dedicated server),
press F5 and run `devcommands`, then `wind <angle> <intensity>` (e.g. `wind 0 1`) to set
fixed wind, and `resetwind` to go back to normal weather.
