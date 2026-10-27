namespace WindRunDisplay
{
    // Mirrors the wind terms in SE_Stats.ModifySpeed / SE_Stats.ModifyRunStaminaDrain
    // (assembly_valheim, Unity 6000.0.75f1 build):
    //
    //   num = (Vector3.Dot(dir.normalized, windDir) + 1f) / 2f;
    //   num *= windIntensity;
    //   num *= modifier;
    //   value *= 1f + num;
    //
    // Kept free of UnityEngine types so it can be unit-tested off-game.
    public static class WindRunMath
    {
        // dot: dot product of the normalized move direction and the wind direction, in [-1, 1].
        // Returns the multiplier delta, e.g. 0.18 for +18% speed or -0.72 for -72% stamina drain.
        public static float Factor(float dot, float windIntensity, float modifier)
        {
            float alignment = (dot + 1f) / 2f;
            return alignment * windIntensity * modifier;
        }

        // Signed whole-percent label, e.g. "+18%", "-72%", "+0%".
        public static string Percent(float factor)
        {
            int pct = (int)System.Math.Round(factor * 100f, System.MidpointRounding.AwayFromZero);
            return (pct < 0 ? "-" : "+") + System.Math.Abs(pct) + "%";
        }

        // Unsigned whole-percent label for a 0..1 magnitude such as wind intensity, e.g. "80%".
        public static string Magnitude(float value)
        {
            return (int)System.Math.Round(value * 100f, System.MidpointRounding.AwayFromZero) + "%";
        }
    }
}
