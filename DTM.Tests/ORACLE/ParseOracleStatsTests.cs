using DTM.ORACLE;
using FluentAssertions;
using Xunit;

namespace DTM.Tests.ORACLE;

public class ParseOracleStatsTests
{
    private static Database_Stats_ORACLE NewStats() => new()
    {
        Sessions = new List<Session>(),
        Tablespaces = new List<Tablespace>()
    };

    [Fact]
    public void Empty_array_leaves_stats_unchanged()
    {
        var stats = NewStats();

        ORACLE_ODBC.ParseOracleStats(Array.Empty<string>(), stats);

        stats.DataSizeMB.Should().Be(0);
        stats.ActiveConnections.Should().Be(0);
        stats.Sessions.Should().BeEmpty();
        stats.Tablespaces.Should().BeEmpty();
    }

    [Fact]
    public void DBSIZE_sets_DataSizeMB()
    {
        var stats = NewStats();

        ORACLE_ODBC.ParseOracleStats(new[] { "DBSIZE|123,45" }, stats);

        stats.DataSizeMB.Should().BeApproximately(123.45, 0.001);
    }

    [Fact]
    public void SESSIONS_sets_ActiveConnections()
    {
        var stats = NewStats();

        ORACLE_ODBC.ParseOracleStats(new[] { "SESSIONS|7" }, stats);

        stats.ActiveConnections.Should().Be(7);
    }

    [Fact]
    public void SESSIONS_with_non_integer_leaves_ActiveConnections_zero()
    {
        var stats = NewStats();

        ORACLE_ODBC.ParseOracleStats(new[] { "SESSIONS|abc" }, stats);

        stats.ActiveConnections.Should().Be(0);
    }

    [Fact]
    public void SGA_and_PGA_are_set()
    {
        var stats = NewStats();

        ORACLE_ODBC.ParseOracleStats(new[] { "SGA|256", "PGA|128,5" }, stats);

        stats.SGASizeMB.Should().Be(256);
        stats.PGAAllocatedMB.Should().BeApproximately(128.5, 0.001);
    }

    [Fact]
    public void ARCHIVELOG_sets_mode()
    {
        var stats = NewStats();

        ORACLE_ODBC.ParseOracleStats(new[] { "ARCHIVELOG|ARCHIVELOG" }, stats);

        stats.ArchiveLogMode.Should().Be("ARCHIVELOG");
    }

    [Fact]
    public void INSTANCE_with_all_fields_is_parsed()
    {
        var stats = NewStats();

        ORACLE_ODBC.ParseOracleStats(new[] { "INSTANCE|OPEN|ORCL|host.local|19.0.0" }, stats);

        stats.InstanceStatus.Should().Be("OPEN");
        stats.InstanceName.Should().Be("ORCL");
        stats.HostName.Should().Be("host.local");
        stats.OracleVersion.Should().Be("19.0.0");
    }

    [Fact]
    public void INSTANCE_with_only_status_leaves_other_fields_null()
    {
        var stats = NewStats();

        ORACLE_ODBC.ParseOracleStats(new[] { "INSTANCE|OPEN" }, stats);

        stats.InstanceStatus.Should().Be("OPEN");
        stats.InstanceName.Should().BeNull();
        stats.HostName.Should().BeNull();
        stats.OracleVersion.Should().BeNull();
    }

    [Fact]
    public void TS_adds_tablespace_with_all_metrics()
    {
        var stats = NewStats();

        ORACLE_ODBC.ParseOracleStats(new[] { "TS|USERS|1000|800|200|80" }, stats);

        stats.Tablespaces.Should().ContainSingle();
        var ts = stats.Tablespaces![0];
        ts.TableSpaceName.Should().Be("USERS");
        ts.TotalMB.Should().Be(1000);
        ts.UsedMB.Should().Be(800);
        ts.FreeMB.Should().Be(200);
        ts.UsedPercent.Should().Be(80);
    }

    [Fact]
    public void TS_with_insufficient_fields_is_ignored()
    {
        var stats = NewStats();

        ORACLE_ODBC.ParseOracleStats(new[] { "TS|USERS|1000" }, stats);

        stats.Tablespaces.Should().BeEmpty();
    }

    [Fact]
    public void SESS_adds_session()
    {
        var stats = NewStats();

        ORACLE_ODBC.ParseOracleStats(new[] { "SESS|alice|workstation01|sqlplus|ACTIVE" }, stats);

        stats.Sessions.Should().ContainSingle();
        var s = stats.Sessions![0];
        s.Username.Should().Be("alice");
        s.Maschine.Should().Be("workstation01");
        s.Program.Should().Be("sqlplus");
        s.Status.Should().Be("ACTIVE");
    }

    [Fact]
    public void Unknown_prefix_is_ignored()
    {
        var stats = NewStats();

        ORACLE_ODBC.ParseOracleStats(new[] { "FOO|bar" }, stats);

        stats.DataSizeMB.Should().Be(0);
        stats.Sessions.Should().BeEmpty();
        stats.Tablespaces.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_lines_are_ignored(string blank)
    {
        var stats = NewStats();

        ORACLE_ODBC.ParseOracleStats(new[] { blank }, stats);

        stats.DataSizeMB.Should().Be(0);
    }

    [Fact]
    public void Null_line_is_ignored()
    {
        var stats = NewStats();

        ORACLE_ODBC.ParseOracleStats(new string?[] { null }!, stats);

        stats.DataSizeMB.Should().Be(0);
    }

    [Fact]
    public void Mixed_lines_populate_all_relevant_fields()
    {
        var stats = NewStats();
        var lines = new[]
        {
            "DBSIZE|2048,5",
            "SESSIONS|3",
            "SGA|512",
            "PGA|256",
            "ARCHIVELOG|NOARCHIVELOG",
            "INSTANCE|OPEN|ORCL|db01|19.0",
            "TS|USERS|1000|600|400|60",
            "TS|SYSTEM|500|450|50|90",
            "SESS|alice|host01|sqlplus|ACTIVE",
            "SESS|bob|host02|toad|INACTIVE",
            "UNKNOWN|x",
            "   "
        };

        ORACLE_ODBC.ParseOracleStats(lines, stats);

        stats.DataSizeMB.Should().BeApproximately(2048.5, 0.001);
        stats.ActiveConnections.Should().Be(3);
        stats.SGASizeMB.Should().Be(512);
        stats.PGAAllocatedMB.Should().Be(256);
        stats.ArchiveLogMode.Should().Be("NOARCHIVELOG");
        stats.InstanceName.Should().Be("ORCL");
        stats.HostName.Should().Be("db01");
        stats.OracleVersion.Should().Be("19.0");
        stats.Tablespaces.Should().HaveCount(2);
        stats.Sessions.Should().HaveCount(2);
    }
}
