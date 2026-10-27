# How Wind run works

The Asksvin cloak gives the **Wind run** status effect. Its numbers come from two
methods in `assembly_valheim` (decompiled from Valheim 1.0.20):
`SE_Stats.ModifySpeed` and `SE_Stats.ModifyRunStaminaDrain`.

```
factor = (dot(moveDir, windDir) + 1) / 2 * windIntensity * modifier
speed    *= 1 + factor   // modifier = m_windMovementModifier   (cloak: +0.25)
runDrain *= 1 + factor   // modifier = m_windRunStaminaModifier (cloak: -1.0)
```

- `dot(moveDir, windDir)` is how well your movement lines up with the wind: `1` running
  with it, `0` across it, `-1` into it. `(dot + 1) / 2` maps that to `0..1`.
- `windIntensity` is the current wind strength, `0..1`. Each weather type has its own
  min/max range, so storms reach higher values than calm weather.
- Result: a full tailwind at max intensity gives the full modifier (+25% speed, -100% run
  stamina), a crosswind gives half, and a headwind gives nothing. It is never a penalty.
- Speed and stamina share the same factor, so they always move together.
- The speed bonus applies to all ground movement and swimming, not only running. The
  stamina part only applies to running.

The mod reads both modifiers from the live status effect rather than hardcoding the
cloak's values, so it also works for anything else that grants a wind-scaled `SE_Stats`.

## Which direction the display uses

- **Moving:** your move direction (`Character.GetMoveDir()`), the same vector the game
  passes to `ModifySpeed`. The number matches what you're actually getting.
- **Standing still:** your camera's facing, flattened to the ground. That previews what
  you'd get by running forward, so you can turn to find the best heading before you run.

## Where it is drawn

- **Icon text:** `Hud.UpdateStatusEffects` calls `StatusEffect.GetIconText()` every frame
  and draws the result in the icon's `TimeText` slot (where buff timers appear). The mod
  postfixes `GetIconText` for wind-run effects on the local player. With `IconFontSize`
  above `0`, a postfix on `Hud.UpdateStatusEffects` also turns off that box's auto-size
  and lets the lines extend below the icon; the box is restored if the HUD reuses it for
  another effect.
- **Tooltip:** a postfix on `SE_Stats.GetTooltipString` adds the max and current speed and
  stamina values; the vanilla tooltip shows neither.
