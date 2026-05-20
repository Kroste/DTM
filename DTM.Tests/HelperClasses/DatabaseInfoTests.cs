using FluentAssertions;
using Xunit;

namespace DTM.Tests.HelperClasses;

public class DatabaseInfoTests
{
    private static Database_Info Make(string name = "DB", Database_Status status = Database_Status.up,
        string? fqdn = "srv.local")
        => new() { Name = name, Id = "1", FQDN = fqdn, Status = status };

    [Fact]
    public void DatabaseStatus_HasDownUpTransitional()
    {
        var values = Enum.GetValues<Database_Status>();
        values.Should().Contain(Database_Status.down);
        values.Should().Contain(Database_Status.up);
        values.Should().Contain(Database_Status.transitional);
    }

    [Fact]
    public void Record_EqualValues_AreEqual()
    {
        var a = Make("MyDB");
        var b = Make("MyDB");
        a.Should().Be(b);
    }

    [Fact]
    public void Record_DifferentName_NotEqual()
    {
        Make("DB1").Should().NotBe(Make("DB2"));
    }

    [Fact]
    public void Record_DifferentStatus_NotEqual()
    {
        Make(status: Database_Status.up).Should().NotBe(Make(status: Database_Status.down));
    }

    [Fact]
    public void FQDN_CanBeNull()
    {
        Make(fqdn: null).FQDN.Should().BeNull();
    }
}
