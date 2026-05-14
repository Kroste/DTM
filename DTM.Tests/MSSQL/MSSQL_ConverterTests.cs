using System;
using DTM.MSSQL;
using FluentAssertions;
using Xunit;

namespace DTM.Tests.MSSQL;

public class MSSQL_ConverterTests
{
    [Fact]
    public void ToDouble_null_returns_zero()
    {
        MSSQL_ODBC.ToDouble(null!).Should().Be(0d);
    }

    [Fact]
    public void ToDouble_dbnull_returns_zero()
    {
        MSSQL_ODBC.ToDouble(DBNull.Value).Should().Be(0d);
    }

    [Theory]
    [InlineData(1.5, 1.5)]
    [InlineData(0, 0)]
    [InlineData(-3.25, -3.25)]
    public void ToDouble_numeric_value_is_converted(double input, double expected)
    {
        MSSQL_ODBC.ToDouble(input).Should().Be(expected);
    }

    [Fact]
    public void ToInt_null_returns_zero()
    {
        MSSQL_ODBC.ToInt(null!).Should().Be(0);
    }

    [Fact]
    public void ToInt_dbnull_returns_zero()
    {
        MSSQL_ODBC.ToInt(DBNull.Value).Should().Be(0);
    }

    [Theory]
    [InlineData(42, 42)]
    [InlineData(0, 0)]
    [InlineData(-7, -7)]
    public void ToInt_numeric_value_is_converted(int input, int expected)
    {
        MSSQL_ODBC.ToInt(input).Should().Be(expected);
    }

    [Fact]
    public void ToBool_null_returns_false()
    {
        MSSQL_ODBC.ToBool(null!).Should().BeFalse();
    }

    [Fact]
    public void ToBool_dbnull_returns_false()
    {
        MSSQL_ODBC.ToBool(DBNull.Value).Should().BeFalse();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public void ToBool_value_is_converted(object input, bool expected)
    {
        MSSQL_ODBC.ToBool(input).Should().Be(expected);
    }

    [Fact]
    public void ToDouble_unparseable_string_throws()
    {
        Action act = () => MSSQL_ODBC.ToDouble("nicht eine zahl");
        act.Should().Throw<FormatException>();
    }
}
