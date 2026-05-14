using DTM.ORACLE;
using FluentAssertions;
using Xunit;

namespace DTM.Tests.ORACLE;

public class ParseDoubleTests
{
    [Theory]
    [InlineData("1,5", 1.5)]
    [InlineData("1.5", 1.5)]
    [InlineData("0", 0d)]
    [InlineData("-3,25", -3.25)]
    [InlineData("  3,14  ", 3.14)]
    public void Valid_input_is_parsed(string input, double expected)
    {
        ORACLE_ODBC.ParseDouble(input).Should().BeApproximately(expected, 0.0001);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1,2,3")]
    public void Invalid_input_returns_zero(string input)
    {
        ORACLE_ODBC.ParseDouble(input).Should().Be(0d);
    }
}
