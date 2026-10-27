using System;
using System.Collections.Generic;

namespace WindRunDisplay
{
    // Which lines appear under the Wind run icon, and how each is labelled. Kept free of
    // UnityEngine types so it can be unit-tested off-game.
    public enum IconField
    {
        RunSpeed,
        RunStamina,
        Wind,
    }

    public static class IconText
    {
        public const string Placeholder = "%s";

        // "RunSpeed, Wind" -> [RunSpeed, Wind]. Case-insensitive, order kept, duplicates
        // kept (the user asked for them). Unknown names go to `unknown` so the caller can
        // warn instead of failing.
        public static List<IconField> ParseFields(string fields, List<string> unknown = null)
        {
            var result = new List<IconField>();
            if (string.IsNullOrWhiteSpace(fields))
            {
                return result;
            }
            foreach (string raw in fields.Split(','))
            {
                string name = raw.Trim();
                if (name.Length == 0)
                {
                    continue;
                }
                if (Enum.TryParse(name, ignoreCase: true, out IconField field) && Enum.IsDefined(typeof(IconField), field))
                {
                    result.Add(field);
                }
                else
                {
                    unknown?.Add(name);
                }
            }
            return result;
        }

        // "Speed: %s" + "+18%" -> "Speed: +18%". A format without the placeholder gets the
        // value appended after a space, so a bare label like "Speed" still shows the number.
        public static string Format(string format, string value)
        {
            if (string.IsNullOrEmpty(format))
            {
                return value;
            }
            return format.Contains(Placeholder) ? format.Replace(Placeholder, value) : format + " " + value;
        }
    }
}
