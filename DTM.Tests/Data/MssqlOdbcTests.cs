using System.Data;
using DTM.MSSQL;
using FluentAssertions;
using Xunit;

namespace DTM.Tests.Data;

public class MssqlOdbcTests
{
    // ------------------------------------------------------------------ ToDouble

    [Fact]
    public void ToDouble_Null_ReturnsZero()
        => MSSQL_ODBC.ToDouble(null!).Should().Be(0d);

    [Fact]
    public void ToDouble_DBNull_ReturnsZero()
        => MSSQL_ODBC.ToDouble(DBNull.Value).Should().Be(0d);

    [Fact]
    public void ToDouble_Int_ReturnsDouble()
        => MSSQL_ODBC.ToDouble(42).Should().BeApproximately(42.0, 0.0001);

    [Fact]
    public void ToDouble_Double_ReturnsSame()
        => MSSQL_ODBC.ToDouble(3.14).Should().BeApproximately(3.14, 0.0001);

    [Fact]
    public void ToDouble_Zero_ReturnsZero()
        => MSSQL_ODBC.ToDouble(0).Should().Be(0d);

    // ------------------------------------------------------------------ ToInt

    [Fact]
    public void ToInt_Null_ReturnsZero()
        => MSSQL_ODBC.ToInt(null!).Should().Be(0);

    [Fact]
    public void ToInt_DBNull_ReturnsZero()
        => MSSQL_ODBC.ToInt(DBNull.Value).Should().Be(0);

    [Fact]
    public void ToInt_Int_ReturnsSame()
        => MSSQL_ODBC.ToInt(100).Should().Be(100);

    [Fact]
    public void ToInt_NegativeInt_ReturnsSame()
        => MSSQL_ODBC.ToInt(-5).Should().Be(-5);

    // ------------------------------------------------------------------ ToBool

    [Fact]
    public void ToBool_Null_ReturnsFalse()
        => MSSQL_ODBC.ToBool(null!).Should().BeFalse();

    [Fact]
    public void ToBool_DBNull_ReturnsFalse()
        => MSSQL_ODBC.ToBool(DBNull.Value).Should().BeFalse();

    [Fact]
    public void ToBool_True_ReturnsTrue()
        => MSSQL_ODBC.ToBool(true).Should().BeTrue();

    [Fact]
    public void ToBool_False_ReturnsFalse()
        => MSSQL_ODBC.ToBool(false).Should().BeFalse();

    [Fact]
    public void ToBool_IntOne_ReturnsTrue()
        => MSSQL_ODBC.ToBool(1).Should().BeTrue();

    [Fact]
    public void ToBool_IntZero_ReturnsFalse()
        => MSSQL_ODBC.ToBool(0).Should().BeFalse();
}
