using Avalonia.Media.Immutable;
using DTM.Data.Terminal;
using FluentAssertions;
using Xunit;

namespace DTM.Tests.Terminal;

public class AnsiParserEdgeCaseTests
{
    private static AnsiSpan[] Parse(string input)
        => new AnsiParser().Feed(input).ToArray();

    private static AnsiSpan[] ParseChunks(params string[] chunks)
    {
        var parser = new AnsiParser();
        List<AnsiSpan> result = [];
        foreach (string c in chunks)
            result.AddRange(parser.Feed(c));
        return [.. result];
    }

    // --- Multiple SGR params in one sequence ---

    [Fact]
    public void Feed_MultipleSgrParams_BoldAndFg()
    {
        var spans = Parse("\x1b[1;31mX");
        spans[0].Style.Bold.Should().BeTrue();
        spans[0].Style.Foreground.Should().BeSameAs(AnsiPalette.Standard[1]);
    }

    [Fact]
    public void Feed_MultipleSgrParams_ResetThenFg()
    {
        // First set bold, then reset+green in one sequence
        var parser = new AnsiParser();
        parser.Feed("\x1b[1m").ToArray();
        var spans = parser.Feed("\x1b[0;32mX").ToArray();
        spans[0].Style.Bold.Should().BeFalse();
        spans[0].Style.Foreground.Should().BeSameAs(AnsiPalette.Standard[2]);
    }

    [Fact]
    public void Feed_MultipleSgrParams_BoldAndBg()
    {
        var spans = Parse("\x1b[1;44mX");
        spans[0].Style.Bold.Should().BeTrue();
        spans[0].Style.Background.Should().BeSameAs(AnsiPalette.Standard[4]);
    }

    // --- SGR reset specific attributes (22/23/24) ---

    [Fact]
    public void Feed_SgrBoldOff()
    {
        var parser = new AnsiParser();
        parser.Feed("\x1b[1m").ToArray();
        var spans = parser.Feed("\x1b[22mX").ToArray();
        spans[0].Style.Bold.Should().BeFalse();
    }

    [Fact]
    public void Feed_SgrItalicOff()
    {
        var parser = new AnsiParser();
        parser.Feed("\x1b[3m").ToArray();
        var spans = parser.Feed("\x1b[23mX").ToArray();
        spans[0].Style.Italic.Should().BeFalse();
    }

    [Fact]
    public void Feed_SgrUnderlineOff()
    {
        var parser = new AnsiParser();
        parser.Feed("\x1b[4m").ToArray();
        var spans = parser.Feed("\x1b[24mX").ToArray();
        spans[0].Style.Underline.Should().BeFalse();
    }

    // --- SGR 39/49: reset fg/bg to default (null) ---

    [Fact]
    public void Feed_SgrFgDefault()
    {
        var parser = new AnsiParser();
        parser.Feed("\x1b[31m").ToArray(); // set red fg
        var spans = parser.Feed("\x1b[39mX").ToArray();
        spans[0].Style.Foreground.Should().BeNull();
    }

    [Fact]
    public void Feed_SgrBgDefault()
    {
        var parser = new AnsiParser();
        parser.Feed("\x1b[41m").ToArray(); // set red bg
        var spans = parser.Feed("\x1b[49mX").ToArray();
        spans[0].Style.Background.Should().BeNull();
    }

    // --- Bright background colors (100-107) ---

    [Fact]
    public void Feed_BgBrightColor_101()
    {
        var spans = Parse("\x1b[101mX");
        spans[0].Style.Background.Should().BeSameAs(AnsiPalette.Bright[1]);
    }

    // --- Empty SGR = reset ---

    [Fact]
    public void Feed_EmptySgrParam_ResetsAll()
    {
        var parser = new AnsiParser();
        parser.Feed("\x1b[1;31m").ToArray();
        var spans = parser.Feed("\x1b[mX").ToArray();
        spans[0].Style.Should().Be(AnsiStyle.Default);
    }

    // --- DCS / SOS / APC / PM sequences ---

    [Fact]
    public void Feed_DcsSequence_Discarded()
    {
        // ESC P data ESC \
        string input = "\x1b" + "Psome-dcs-data\x1b\\text";
        var spans = Parse(input);
        string all = string.Concat(spans.Select(s => s.Text));
        all.Should().Be("text");
    }

    [Fact]
    public void Feed_SosSequence_Discarded()
    {
        // ESC _ (SOS) data ESC \
        string input = "\x1b" + "_sos-data\x1b\\text";
        var spans = Parse(input);
        string all = string.Concat(spans.Select(s => s.Text));
        all.Should().Be("text");
    }

    [Fact]
    public void Feed_ApcSequence_Discarded()
    {
        // ESC ^ (APC) data ESC \
        string input = "\x1b" + "^apc-data\x1b\\text";
        var spans = Parse(input);
        string all = string.Concat(spans.Select(s => s.Text));
        all.Should().Be("text");
    }

    [Fact]
    public void Feed_PmSequence_Discarded()
    {
        // ESC X (PM) data ESC \
        string input = "\x1b" + "Xpm-data\x1b\\text";
        var spans = Parse(input);
        string all = string.Concat(spans.Select(s => s.Text));
        all.Should().Be("text");
    }

    // --- OSC with ESC\ (ST) terminator ---

    [Fact]
    public void Feed_OscWithStTerminator_Discarded()
    {
        // ESC ] 0 ; title ESC \
        string input = "\x1b]0;MyTitle\x1b\\text";
        var spans = Parse(input);
        string all = string.Concat(spans.Select(s => s.Text));
        all.Should().Be("text");
    }

    // --- ESC followed by unknown char ---

    [Fact]
    public void Feed_EscUnknownChar_Discarded_TextAfterPreserved()
    {
        // ESC c is a one-char escape (RIS) — discarded, state back to Normal
        var spans = Parse("\x1b" + "ctext");
        string all = string.Concat(spans.Select(s => s.Text));
        all.Should().Be("text");
    }

    // --- Split escape across chunks ---

    [Fact]
    public void Feed_SplitEscapeAtChunkBoundary_BoldApplied()
    {
        var spans = ParseChunks("\x1b", "[1mtext");
        spans.Should().NotBeEmpty();
        spans.All(s => s.Style.Bold).Should().BeTrue();
    }

    [Fact]
    public void Feed_SplitSgrParamAtBoundary()
    {
        // "\x1b[3" ends mid-param, "1mtext" completes it → SGR 31 = red fg
        var spans = ParseChunks("\x1b[3", "1mtext");
        spans.Should().NotBeEmpty();
        spans[0].Style.Foreground.Should().BeSameAs(AnsiPalette.Standard[1]);
    }

    // --- BEL standalone ---

    [Fact]
    public void Feed_BelChar_Discarded()
    {
        var spans = Parse("\atext");
        string all = string.Concat(spans.Select(s => s.Text));
        all.Should().Be("text");
    }

    // --- Text before and after SGR ---

    [Fact]
    public void Feed_TextBeforeAndAfterSgr_TwoSpans()
    {
        var spans = Parse("before\x1b[31mafter");
        spans.Should().HaveCount(2);
        spans[0].Text.Should().Be("before");
        spans[0].Style.Foreground.Should().BeNull();
        spans[1].Text.Should().Be("after");
        spans[1].Style.Foreground.Should().BeSameAs(AnsiPalette.Standard[1]);
    }
}
