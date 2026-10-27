using System.Collections.Generic;
using HarmonyLib;

namespace WindRunDisplay
{
    // Hud.UpdateStatusEffects calls GetIconText every frame for each buff and draws it
    // in the icon's "TimeText" slot (where timers show). SE_Stats does not override it,
    // so patch the base method and filter to wind-run effects on the local player.
    [HarmonyPatch(typeof(StatusEffect), nameof(StatusEffect.GetIconText))]
    internal static class IconTextPatch
    {
        private static void Postfix(StatusEffect __instance, ref string __result)
        {
            if (!(__instance is SE_Stats se) || !Plugin.IsWindRun(se))
            {
                return;
            }
            if (se.m_character == null || se.m_character != Player.m_localPlayer)
            {
                return;
            }

            Plugin.Current(se, out float speed, out float stamina);
            var lines = new List<string>(4);
            // Keep any vanilla timer text (wind run from the cape has none).
            if (!string.IsNullOrEmpty(__result))
            {
                lines.Add(__result);
            }
            foreach (IconField field in Plugin.IconFields)
            {
                switch (field)
                {
                    case IconField.RunSpeed when se.m_windMovementModifier != 0f:
                        lines.Add(IconText.Format(Plugin.RunSpeedFormat.Value, WindRunMath.Percent(speed)));
                        break;
                    case IconField.RunStamina when se.m_windRunStaminaModifier != 0f:
                        lines.Add(IconText.Format(Plugin.RunStaminaFormat.Value, WindRunMath.Percent(stamina)));
                        break;
                    case IconField.Wind when EnvMan.instance != null:
                        lines.Add(IconText.Format(Plugin.WindFormat.Value, WindRunMath.Magnitude(EnvMan.instance.GetWindIntensity())));
                        break;
                }
            }
            __result = string.Join("\n", lines);
        }
    }

    // The vanilla SE_Stats tooltip lists no wind values at all.
    [HarmonyPatch(typeof(SE_Stats), nameof(SE_Stats.GetTooltipString))]
    internal static class TooltipPatch
    {
        private static void Postfix(SE_Stats __instance, ref string __result)
        {
            if (!Plugin.ExtendTooltip.Value || !Plugin.IsWindRun(__instance))
            {
                return;
            }

            bool live = __instance.m_character != null && __instance.m_character == Player.m_localPlayer;
            float speed = 0f, stamina = 0f;
            if (live)
            {
                Plugin.Current(__instance, out speed, out stamina);
            }

            if (__instance.m_windMovementModifier != 0f)
            {
                __result += Line("Wind run speed", __instance.m_windMovementModifier, live, speed);
            }
            if (__instance.m_windRunStaminaModifier != 0f)
            {
                __result += Line("Wind run stamina", __instance.m_windRunStaminaModifier, live, stamina);
            }
        }

        private static string Line(string label, float max, bool live, float now)
        {
            string line = label + ": up to <color=orange>" + WindRunMath.Percent(max) + "</color>";
            if (live)
            {
                line += " (now <color=orange>" + WindRunMath.Percent(now) + "</color>)";
            }
            return line + "\n";
        }
    }
}
