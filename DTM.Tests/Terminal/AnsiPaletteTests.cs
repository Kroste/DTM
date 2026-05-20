using Avalonia.Media;
using Avalonia.Media.Immutable;
using DTM.Data.Terminal;
using FluentAssertions;
using Xunit;

namespace DTM.Tests.Terminal;

public class AnsiPaletteTests
{
    private static Color GetColor(int idx)
        => ((ImmutableSolidColorBrush)AnsiPalette.Color256(idx)).Color;

    [Fact]
    public void Color256_Index0to7_ReturnsStandard()
    {
        for (int i = 0; i < 8; i++)
            AnsiPalette.Color256(i).Should().BeSameAs(AnsiPalette.Standard[i]);
    }

    [Fact]
    public void Color256_Index8to15_ReturnsBright()
    {
        for (int i = 8; i < 16; i++)
            AnsiPalette.Color256(i).Should().BeSameAs(AnsiPalette.Bright[i - 8]);
    }

    [Fact]
    public void Color256_Index16_RgbCubeStart()
    {
        // n=0: r=0,g=0,b=0 → Step(0)=0 → black
        var c = GetColor(16);
        c.R.Should().Be(0);
        c.G.Should().Be(0);
        c.B.Should().Be(0);
    }

    [Fact]
    public void Color256_Index231_RgbCubeEnd()
    {
        // n=215: r=5,g=5,b=5 → Step(5)=255 → white
        var c = GetColor(231);
        c.R.Should().Be(255);
        c.G.Should().Be(255);
        c.B.Should().Be(255);
    }

    [Fact]
    public void Color256_Index232_GrayscaleStart()
    {
        // v = 8 + (232-232)*10 = 8
        var c = GetColor(232);
        c.R.Should().Be(8);
        c.G.Should().Be(8);
        c.B.Should().Be(8);
    }

    [Fact]
    public void Color256_Index255_GrayscaleEnd()
    {
        // v = 8 + (255-232)*10 = 8 + 230 = 238
        var c = GetColor(255);
        c.R.Should().Be(238);
        c.G.Should().Be(238);
        c.B.Should().Be(238);
    }

    [Fact]
    public void Color256_BelowZero_Clamps()
    {
        AnsiPalette.Color256(-1).Should().BeSameAs(AnsiPalette.Standard[0]);
    }

    [Fact]
    public void Color256_Above255_Clamps()
    {
        // index 255 is grayscale; above 255 should return the same as 255
        var c256 = GetColor(256);
        var c255 = GetColor(255);
        c256.R.Should().Be(c255.R);
        c256.G.Should().Be(c255.G);
        c256.B.Should().Be(c255.B);
    }
}
