using FluentAssertions;
using Xunit;

namespace DTM.Tests.HelperClasses;

public class DB_ServerTests
{
    [Fact]
    public void Constructor_stores_credentials()
    {
        var cred = new ServerCredential("srv", "u", "p", "db");
        var server = new DB_SERVER(cred);

        server.serverCredential.Should().BeSameAs(cred);
    }

    [Theory]
    [InlineData(DB_SERVER.ServerTyp.MSSQL, "MSSQL")]
    [InlineData(DB_SERVER.ServerTyp.ORACLE, "ORACLE")]
    public void ServerTyp_enum_round_trips_to_string(DB_SERVER.ServerTyp typ, string expected)
    {
        typ.ToString().Should().Be(expected);
    }
}
