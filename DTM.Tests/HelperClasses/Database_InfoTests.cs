using FluentAssertions;
using Xunit;

namespace DTM.Tests.HelperClasses;

public class Database_InfoTests
{
    private static Database_Info Make(string name = "db1", string id = "1",
        string? fqdn = "host.local", Database_Status status = Database_Status.up)
        => new() { Name = name, Id = id, FQDN = fqdn, Status = status };

    [Fact]
    public void Identical_values_compare_equal_via_record_semantics()
    {
        Make().Should().Be(Make());
    }

    [Fact]
    public void Different_status_makes_records_unequal()
    {
        Make(status: Database_Status.up).Should().NotBe(Make(status: Database_Status.down));
    }

    [Fact]
    public void Different_name_makes_records_unequal()
    {
        Make(name: "a").Should().NotBe(Make(name: "b"));
    }

    [Fact]
    public void Nullable_FQDN_is_supported()
    {
        Make(fqdn: null).FQDN.Should().BeNull();
    }

    [Theory]
    [InlineData(Database_Status.down, 0)]
    [InlineData(Database_Status.up, 1)]
    [InlineData(Database_Status.transitional, 2)]
    public void Enum_values_have_stable_numeric_codes(Database_Status status, int expected)
    {
        ((int)status).Should().Be(expected);
    }
}
