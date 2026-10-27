using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace WindRunDisplay
{
    // The icon's "TimeText" box is sized for a one-line timer; with several lines the
    // vanilla TMP settings shrink (auto-size) or clip them. When IconFontSize > 0, after
    // Hud.UpdateStatusEffects has written the text, give the Wind run icon's box that
    // fixed font size and let the lines overflow downward; at 0 the box is left vanilla. Hud reuses these icon instances across effects, so any box
    // we changed that now shows a different effect gets its original settings back.
    [HarmonyPatch(typeof(Hud), "UpdateStatusEffects")]
    internal static class HudTextPatch
    {
        private struct Original
        {
            public bool AutoSize;
            public float FontSize;
            public TextWrappingModes Wrapping;
            public TextOverflowModes Overflow;
            public VerticalAlignmentOptions Vertical;
        }

        private static readonly Dictionary<TMP_Text, Original> Modified = new Dictionary<TMP_Text, Original>();
        private static bool _logged;

        private static readonly List<TMP_Text> Dead = new List<TMP_Text>();

        private static void Postfix(List<StatusEffect> statusEffects, List<RectTransform> ___m_statusEffects)
        {
            // Hud destroys and re-instantiates all icons when the effect count changes.
            foreach (TMP_Text key in Modified.Keys)
            {
                if (key == null)
                {
                    Dead.Add(key);
                }
            }
            foreach (TMP_Text key in Dead)
            {
                Modified.Remove(key);
            }
            Dead.Clear();

            int count = Mathf.Min(statusEffects.Count, ___m_statusEffects.Count);
            for (int i = 0; i < count; i++)
            {
                RectTransform icon = ___m_statusEffects[i];
                if (icon == null)
                {
                    continue;
                }
                Transform t = icon.Find("TimeText");
                TMP_Text text = t != null ? t.GetComponent<TMP_Text>() : null;
                if (text == null)
                {
                    continue;
                }

                bool windRun = statusEffects[i] is SE_Stats se && Plugin.IsWindRun(se);
                if (windRun && Plugin.IconFontSize.Value > 0f)
                {
                    Apply(text);
                }
                else if (Modified.TryGetValue(text, out Original orig))
                {
                    Restore(text, orig);
                }
            }
        }

        private static void Apply(TMP_Text text)
        {
            if (!Modified.TryGetValue(text, out Original orig))
            {
                orig = new Original
                {
                    AutoSize = text.enableAutoSizing,
                    FontSize = text.fontSize,
                    Wrapping = text.textWrappingMode,
                    Overflow = text.overflowMode,
                    Vertical = text.verticalAlignment,
                };
                Modified[text] = orig;
                if (!_logged)
                {
                    _logged = true;
                    Vector2 rect = text.rectTransform.rect.size;
                    Plugin.Log.LogInfo($"Vanilla TimeText: autoSize={text.enableAutoSizing} fontSize={text.fontSize} " +
                        $"min={text.fontSizeMin} max={text.fontSizeMax} wrap={text.textWrappingMode} " +
                        $"overflow={text.overflowMode} align={text.alignment} rect={rect.x}x{rect.y}");
                }
            }

            text.enableAutoSizing = false;
            text.fontSize = Plugin.IconFontSize.Value;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            // Grow downward from the box's top edge instead of around its center.
            text.verticalAlignment = VerticalAlignmentOptions.Top;
        }

        private static void Restore(TMP_Text text, Original orig)
        {
            text.enableAutoSizing = orig.AutoSize;
            text.fontSize = orig.FontSize;
            text.textWrappingMode = orig.Wrapping;
            text.overflowMode = orig.Overflow;
            text.verticalAlignment = orig.Vertical;
            Modified.Remove(text);
        }

        // Called on plugin unload (hot reload) so no box is left modified.
        internal static void RestoreAll()
        {
            foreach (KeyValuePair<TMP_Text, Original> kv in new List<KeyValuePair<TMP_Text, Original>>(Modified))
            {
                if (kv.Key != null)
                {
                    Restore(kv.Key, kv.Value);
                }
            }
            Modified.Clear();
        }
    }
}
