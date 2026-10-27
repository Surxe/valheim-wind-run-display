using WindRunDisplay;
using Xunit;

public class WindRunMathTests
{
    // Asksvin cloak values per the wiki: +25% speed, -100% run stamina.
    private const float Speed = 0.25f;
    private const float Stamina = -1f;

    [Theory]
    [InlineData(1f, 1f, 0.25f)]   // full tailwind, max wind
    [InlineData(0f, 1f, 0.125f)]  // crosswind gives half
    [InlineData(-1f, 1f, 0f)]     // headwind gives nothing, never negative
    [InlineData(1f, 0.5f, 0.125f)]
    [InlineData(1f, 0f, 0f)]      // calm
    public void SpeedFactor(float dot, float intensity, float expected)
    {
        Assert.Equal(expected, WindRunMath.Factor(dot, intensity, Speed), 5);
    }

    [Fact]
    public void StaminaFactorFullTailwindCancelsDrain()
    {
        Assert.Equal(-1f, WindRunMath.Factor(1f, 1f, Stamina), 5);
    }

    [Theory]
    [InlineData(0.18f, "+18%")]
    [InlineData(-0.72f, "-72%")]
    [InlineData(0f, "+0%")]
    [InlineData(0.125f, "+13%")]
    [InlineData(-0.004f, "+0%")]
    public void Percent(float factor, string expected)
    {
        Assert.Equal(expected, WindRunMath.Percent(factor));
    }

    [Theory]
    [InlineData(0.8f, "80%")]
    [InlineData(0f, "0%")]
    [InlineData(1f, "100%")]
    [InlineData(0.125f, "13%")]
    public void Magnitude(float value, string expected)
    {
        Assert.Equal(expected, WindRunMath.Magnitude(value));
    }
}
