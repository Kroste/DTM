using DTM.Config;
using DTM.ViewModels;
using FluentAssertions;
using Xunit;

namespace DTM.Tests.ViewModels;

public class EditConnectionViewModelTests
{
    [Fact]
    public void ToEntry_MapsAllFieldsCorrectly()
    {
        var vm = new EditConnectionViewModel
        {
            Key = "MSSQL",
            Server = "sql01",
            User = "sa",
            Password = "secret",
            Database = "MyDB",
            ConnectionString = "DSN=x"
        };

        var entry = vm.ToEntry();
        entry.Key.Should().Be("MSSQL");
        entry.Server.Should().Be("sql01");
        entry.User.Should().Be("sa");
        entry.PlainPassword.Should().Be("secret");
        entry.Database.Should().Be("MyDB");
        entry.ConnectionString.Should().Be("DSN=x");
    }

    [Fact]
    public void ToEntry_Password_IsEncrypted_InEntry()
    {
        var vm = new EditConnectionViewModel { Password = "plaintext" };
        var entry = vm.ToEntry();
        entry.PasswordProtected.Should().NotBe("plaintext");
        entry.PasswordProtected.Should().NotBeEmpty();
    }

    [Fact]
    public void FromEntry_RoundTrip_PreservesFields()
    {
        var original = new ConnectionEntry
        {
            Key = "ORACLE",
            Server = "orasrv",
            User = "system",
            Database = "ORCL",
            ConnectionString = "DSN=ora"
        };
        original.PlainPassword = "orapass";

        var vm = new EditConnectionViewModel(original);
        var rebuilt = vm.ToEntry();

        rebuilt.Key.Should().Be("ORACLE");
        rebuilt.Server.Should().Be("orasrv");
        rebuilt.User.Should().Be("system");
        rebuilt.PlainPassword.Should().Be("orapass");
        rebuilt.Database.Should().Be("ORCL");
        rebuilt.ConnectionString.Should().Be("DSN=ora");
    }

    [Fact]
    public void ToEntry_EmptyConnectionString_Preserved()
    {
        var vm = new EditConnectionViewModel { ConnectionString = "" };
        vm.ToEntry().ConnectionString.Should().BeEmpty();
    }
}
