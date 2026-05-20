using FluentAssertions;
using Xunit;

namespace DTM.Tests.HelperClasses;

public class DbServerTests
{
    [Fact]
    public void Constructor_StoresCredential()
    {
        var cred = new ServerCredential("srv");
        new DB_SERVER(cred).serverCredential.Should().BeSameAs(cred);
    }

    [Fact]
    public void ServerCredential_Property_ReturnsStoredValue()
    {
        var cred = new ServerCredential("sql01", "sa", "pass", "MyDB", "");
        var server = new DB_SERVER(cred);
        server.serverCredential!.Server.Should().Be("sql01");
        server.serverCredential.User.Should().Be("sa");
    }

    [Fact]
    public void ServerTyp_ContainsOracle()
    {
        Enum.GetValues<DB_SERVER.ServerTyp>().Should().Contain(DB_SERVER.ServerTyp.ORACLE);
    }

    [Fact]
    public void ServerTyp_ContainsMssql()
    {
        Enum.GetValues<DB_SERVER.ServerTyp>().Should().Contain(DB_SERVER.ServerTyp.MSSQL);
    }

    [Fact]
    public void ServerTyp_ContainsPostgreSQL()
    {
        Enum.GetValues<DB_SERVER.ServerTyp>().Should().Contain(DB_SERVER.ServerTyp.PostgreSQL);
    }

    [Fact]
    public void ServerTyp_Count_IsThree()
    {
        Enum.GetValues<DB_SERVER.ServerTyp>().Should().HaveCount(3);
    }
}
