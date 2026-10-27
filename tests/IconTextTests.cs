using System.Collections.Generic;
using WindRunDisplay;
using Xunit;

public class IconTextTests
{
    [Fact]
    public void ParseKeepsOrderAndIgnoresCaseAndSpaces()
    {
        Assert.Equal(new[] { IconField.Wind, IconField.RunSpeed },
            IconText.ParseFields(" wind ,RUNSPEED"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData(" , ,")]
    public void ParseEmptyGivesNoFields(string fields)
    {
        Assert.Empty(IconText.ParseFields(fields));
    }

    [Fact]
    public void ParseReportsUnknownNames()
    {
        var unknown = new List<string>();
        var fields = IconText.ParseFields("RunSpeed, Dodge, 7", unknown);
        Assert.Equal(new[] { IconField.RunSpeed }, fields);
        Assert.Equal(new[] { "Dodge", "7" }, unknown);
    }

    [Theory]
    [InlineData("Speed: %s", "+18%", "Speed: +18%")]
    [InlineData("%s run", "+18%", "+18% run")]
    [InlineData("Speed", "+18%", "Speed +18%")]
    [InlineData("", "+18%", "+18%")]
    [InlineData(null, "+18%", "+18%")]
    public void Format(string format, string value, string expected)
    {
        Assert.Equal(expected, IconText.Format(format, value));
    }
}
