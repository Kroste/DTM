using Avalonia.Media;
using Avalonia.Media.Immutable;
using DTM.Data.Terminal;
using FluentAssertions;
using Xunit;

namespace DTM.Tests.Terminal;

public class AnsiParserTests
{
    private static AnsiSpan[] Parse(string input)
        => new AnsiParser().Feed(input).ToArray();

    private static AnsiSpan[] ParseAll(AnsiParser parser, params string[] chunks)
    {
        List<AnsiSpan> result = [];
        foreach (string c in chunks)
            result.AddRange(parser.Feed(c));
        return [.. result];
    }

    // --- Plain text ---

    [Fact]
    public void Feed_PlainText_ReturnsSingleSpan()
    {
        var spans = Parse("hello");
        spans.Should().HaveCount(1);
        spans[0].Text.Should().Be("hello");
        spans[0].Style.Should().Be(AnsiStyle.Default);
    }

    [Fact]
    public void Feed_Empty_ReturnsNoSpans()
    {
        Parse(string.Empty).Should().BeEmpty();
    }

    // --- SGR reset ---

    [Fact]
    public void Feed_Reset_ClearsStyle()
    {
        var spans = Parse("\x1b[1m\x1b[0mtext");
        spans.Should().HaveCount(1);
        spans[0].Style.Bold.Should().BeFalse();
        spans[0].Style.Foreground.Should().BeNull();
    }

    // --- Bold/Italic/Underline ---

    [Fact]
    public void Feed_Bold_SetsBold()
    {
        var spans = Parse("\x1b[1mtext");
        spans.Should().HaveCount(1);
        spans[0].Style.Bold.Should().BeTrue();
    }

    [Fact]
    public void Feed_Italic_SetsItalic()
    {
        var spans = Parse("\x1b[3mtext");
        spans[0].Style.Italic.Should().BeTrue();
    }

    [Fact]
    public void Feed_Underline_SetsUnderline()
    {
        var spans = Parse("\x1b[4mtext");
        spans[0].Style.Underline.Should().BeTrue();
    }

    // --- Standard foreground colors (SGR 30-37) ---

    [Theory]
    [InlineData(30, 0)]
    [InlineData(31, 1)]
    [InlineData(32, 2)]
    [InlineData(33, 3)]
    [InlineData(34, 4)]
    [InlineData(35, 5)]
    [InlineData(36, 6)]
    [InlineData(37, 7)]
    public void Feed_FgColor_Standard(int sgr, int paletteIdx)
    {
        var spans = Parse($"\x1b[{sgr}mX");
        spans[0].Style.Foreground.Should().BeSameAs(AnsiPalette.Standard[paletteIdx]);
    }

    // --- Bright foreground colors (SGR 90-97) ---

    [Theory]
    [InlineData(90, 0)]
    [InlineData(91, 1)]
    [InlineData(92, 2)]
    [InlineData(93, 3)]
    [InlineData(94, 4)]
    [InlineData(95, 5)]
    [InlineData(96, 6)]
    [InlineData(97, 7)]
    public void Feed_FgColor_Bright(int sgr, int paletteIdx)
    {
        var spans = Parse($"\x1b[{sgr}mX");
        spans[0].Style.Foreground.Should().BeSameAs(AnsiPalette.Bright[paletteIdx]);
    }

    // --- Background colors ---

    [Fact]
    public void Feed_BgColor_Standard()
    {
        var spans = Parse("\x1b[41mX"); // red background
        spans[0].Style.Background.Should().BeSameAs(AnsiPalette.Standard[1]);
    }

    // --- 256-color ---

    [Fact]
    public void Feed_Color256_Fg()
    {
        var spans = Parse("\x1b[38;5;196mX");
        spans[0].Style.Foreground.Should().NotBeNull();
        // 196 is in RGB cube: n=180, r=5,g=0,b=0 → (255,0,0) red
        var color = ((ImmutableSolidColorBrush)spans[0].Style.Foreground!).Color;
        color.R.Should().Be(255);
        color.G.Should().Be(0);
        color.B.Should().Be(0);
    }

    [Fact]
    public void Feed_Color256_Bg()
    {
        var spans = Parse("\x1b[48;5;0mX");
        spans[0].Style.Background.Should().BeSameAs(AnsiPalette.Standard[0]);
    }

    // --- 24-bit truecolor ---

    [Fact]
    public void Feed_24bit_Fg()
    {
        var spans = Parse("\x1b[38;2;10;20;30mX");
        spans[0].Style.Foreground.Should().NotBeNull();
        var color = ((ImmutableSolidColorBrush)spans[0].Style.Foreground!).Color;
        color.R.Should().Be(10);
        color.G.Should().Be(20);
        color.B.Should().Be(30);
    }

    [Fact]
    public void Feed_24bit_Bg()
    {
        var spans = Parse("\x1b[48;2;100;150;200mX");
        spans[0].Style.Background.Should().NotBeNull();
        var color = ((ImmutableSolidColorBrush)spans[0].Style.Background!).Color;
        color.R.Should().Be(100);
        color.G.Should().Be(150);
        color.B.Should().Be(200);
    }

    // --- Discarded sequences ---

    [Fact]
    public void Feed_OscSequence_Discarded()
    {
        // OSC with BEL terminator: ESC ] 0 ; title BEL
        var spans = Parse("\x1b]0;Window Title\atext");
        spans.Should().HaveCount(1);
        spans[0].Text.Should().Be("text");
    }

    [Fact]
    public void Feed_CsiNonSgr_Discarded()
    {
        // ESC[2J = clear screen CSI, not SGR → discarded
        var spans = Parse("\x1b[2Jtext");
        spans.Should().HaveCount(1);
        spans[0].Text.Should().Be("text");
    }

    [Fact]
    public void Feed_CarriageReturn_Discarded()
    {
        var spans = Parse("a\rb");
        spans.Should().HaveCount(1);
        spans[0].Text.Should().Be("ab");
    }

    [Fact]
    public void Feed_Backspace_Discarded()
    {
        var spans = Parse("a\bb");
        spans.Should().HaveCount(1);
        spans[0].Text.Should().Be("ab");
    }

    // --- Preserved characters ---

    [Fact]
    public void Feed_Newline_Preserved()
    {
        var spans = Parse("a\nb");
        string all = string.Concat(spans.Select(s => s.Text));
        all.Should().Contain("\n");
    }

    [Fact]
    public void Feed_Tab_Preserved()
    {
        var spans = Parse("a\tb");
        string all = string.Concat(spans.Select(s => s.Text));
        all.Should().Contain("\t");
    }

    // --- Streaming ---

    [Fact]
    public void Feed_Streaming_TwoChunks_SameAsOneChunk()
    {
        string full = "\x1b[1mhello world";
        var single = Parse(full);

        var parser = new AnsiParser();
        var multi = ParseAll(parser, "\x1b[1m", "hello world");

        string textSingle = string.Concat(single.Select(s => s.Text));
        string textMulti = string.Concat(multi.Select(s => s.Text));
        textSingle.Should().Be(textMulti);

        // Both should result in bold style
        single.All(s => s.Style.Bold).Should().BeTrue();
        multi.All(s => s.Style.Bold).Should().BeTrue();
    }

    [Fact]
    public void Feed_StylePersists_AcrossChunks()
    {
        var parser = new AnsiParser();
        parser.Feed("\x1b[1m").ToArray(); // set bold, no text yet
        var spans = parser.Feed("text").ToArray();
        spans.Should().HaveCount(1);
        spans[0].Style.Bold.Should().BeTrue();
    }
}
