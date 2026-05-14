using FluentAssertions;
using Xunit;

namespace DTM.Tests.HelperClasses;

public class ServerCredentialTests
{
    [Fact]
    public void Default_constructor_uses_documented_defaults()
    {
        var cred = new ServerCredential();

        cred.Server.Should().Be("FOC-SQL01");
        cred.User.Should().BeEmpty();
        cred.Password.Should().BeEmpty();
        cred.Datenbank.Should().Be("Master");
    }

    [Fact]
    public void Custom_values_are_stored_on_properties()
    {
        var cred = new ServerCredential("srv1", "alice", "pw", "db1");

        cred.Server.Should().Be("srv1");
        cred.User.Should().Be("alice");
        cred.Password.Should().Be("pw");
        cred.Datenbank.Should().Be("db1");
    }

    [Fact]
    public void Properties_are_mutable_after_construction()
    {
        var cred = new ServerCredential();

        cred.Server = "new-server";
        cred.Datenbank = "another";

        cred.Server.Should().Be("new-server");
        cred.Datenbank.Should().Be("another");
    }
}
