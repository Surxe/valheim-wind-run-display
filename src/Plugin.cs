using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace WindRunDisplay
{
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "surxe.WindRunDisplay";
        public const string Name = "Wind Run Display";
        public const string Version = "1.0.0";

        internal static ConfigEntry<string> Fields;
        internal static ConfigEntry<string> RunSpeedFormat;
        internal static ConfigEntry<string> RunStaminaFormat;
        internal static ConfigEntry<string> WindFormat;
        internal static ConfigEntry<float> IconFontSize;
        internal static ConfigEntry<bool> ExtendTooltip;

        // Parsed Fields, refreshed when the setting changes (e.g. via a config manager).
        internal static List<IconField> IconFields = new List<IconField>();

        internal static BepInEx.Logging.ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            const string placeholder = " '" + IconText.Placeholder + "' is replaced by the value.";
            Fields = Config.Bind("Icon", "Fields", "RunSpeed",
                "Lines shown under the Wind run icon, top to bottom, comma-separated. " +
                "Available: RunSpeed (speed bonus), RunStamina (run stamina reduction), " +
                "Wind (wind intensity, independent of direction). Empty = no text.");
            RunSpeedFormat = Config.Bind("Icon", "RunSpeedFormat", "Speed: %s",
                "Label for the RunSpeed line." + placeholder);
            RunStaminaFormat = Config.Bind("Icon", "RunStaminaFormat", "Run stamina: %s",
                "Label for the RunStamina line." + placeholder);
            WindFormat = Config.Bind("Icon", "WindFormat", "Wind: %s",
                "Label for the Wind line." + placeholder);
            IconFontSize = Config.Bind("Icon", "IconFontSize", 0f,
                "0 = leave the game's text box as is (its own size; it auto-shrinks longer or multi-line text). " +
                "Above 0 = use this fixed font size, with no shrinking and the lines extending below the icon.");
            ExtendTooltip = Config.Bind("Tooltip", "ExtendTooltip", true,
                "Append max/current Wind run speed and stamina values to the status effect tooltip.");

            ParseFields();
            Fields.SettingChanged += (_, __) => ParseFields();

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(typeof(Plugin).Assembly);
        }

        // ScriptEngine hot reload destroys the old plugin instance; patches would
        // otherwise survive and stack with the reloaded copy's.
        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            HudTextPatch.RestoreAll();
        }

        private static void ParseFields()
        {
            var unknown = new List<string>();
            IconFields = IconText.ParseFields(Fields.Value, unknown);
            if (unknown.Count > 0)
            {
                Log.LogWarning($"Ignoring unknown Fields entries: {string.Join(", ", unknown)} " +
                    $"(available: {string.Join(", ", System.Enum.GetNames(typeof(IconField)))})");
            }
        }

        // Direction the game will feed into SE_Stats.ModifySpeed for the local player.
        // Moving: the player's move direction (what Character.UpdateWalking passes).
        // Idle: the flattened camera forward, i.e. where pressing forward would run.
        internal static Vector3 PreviewDir(Character character)
        {
            Vector3 dir = character.GetMoveDir();
            if (dir.sqrMagnitude > 0.01f)
            {
                return dir.normalized;
            }
            GameCamera cam = GameCamera.instance;
            Vector3 fwd = cam != null ? cam.transform.forward : character.transform.forward;
            fwd.y = 0f;
            return fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.forward;
        }

        internal static bool IsWindRun(SE_Stats se)
        {
            return se.m_windMovementModifier != 0f || se.m_windRunStaminaModifier != 0f;
        }

        // Current (speed, stamina) multiplier deltas for this status effect on its character.
        internal static void Current(SE_Stats se, out float speed, out float stamina)
        {
            EnvMan env = EnvMan.instance;
            if (env == null || se.m_character == null)
            {
                speed = 0f;
                stamina = 0f;
                return;
            }
            float dot = Vector3.Dot(PreviewDir(se.m_character), env.GetWindDir());
            float intensity = env.GetWindIntensity();
            speed = WindRunMath.Factor(dot, intensity, se.m_windMovementModifier);
            stamina = WindRunMath.Factor(dot, intensity, se.m_windRunStaminaModifier);
        }
    }
}
