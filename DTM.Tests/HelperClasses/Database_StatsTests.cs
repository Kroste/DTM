using FluentAssertions;
using Xunit;

namespace DTM.Tests.HelperClasses;

public class Database_StatsTests
{
    [Fact]
    public void Mssql_stats_is_a_database_stats()
    {
        var mssql = new Database_Stats_MSSQL();
        mssql.Should().BeAssignableTo<Database_Stats>();
    }

    [Fact]
    public void Oracle_stats_is_a_database_stats()
    {
        var oracle = new Database_Stats_ORACLE();
        oracle.Should().BeAssignableTo<Database_Stats>();
    }

    [Fact]
    public void Default_numeric_fields_are_zero()
    {
        var s = new Database_Stats_MSSQL();
        s.DataSizeMB.Should().Be(0);
        s.LogSizeMB.Should().Be(0);
        s.TotalSizeMB.Should().Be(0);
        s.BufferSizeMB.Should().Be(0);
        s.CompatibllityLevel.Should().Be(0);
        s.ActiveConnections.Should().Be(0);
    }

    [Fact]
    public void Default_reference_fields_are_null()
    {
        var s = new Database_Stats_MSSQL();
        s.Name.Should().BeNull();
        s.State.Should().BeNull();
        s.Files.Should().BeNull();
        s.Sessions.Should().BeNull();
        s.LastFullBackup.Should().BeNull();
    }

    [Fact]
    public void Oracle_specific_fields_are_accessible()
    {
        var s = new Database_Stats_ORACLE
        {
            InstanceName = "ORCL",
            HostName = "host.local",
            OracleVersion = "19.0",
            InstanceStatus = "OPEN",
            ArchiveLogMode = "ARCHIVELOG",
            SGASizeMB = 256,
            PGAAllocatedMB = 128,
            Tablespaces = new List<Tablespace>()
        };

        s.InstanceName.Should().Be("ORCL");
        s.HostName.Should().Be("host.local");
        s.OracleVersion.Should().Be("19.0");
        s.InstanceStatus.Should().Be("OPEN");
        s.ArchiveLogMode.Should().Be("ARCHIVELOG");
        s.SGASizeMB.Should().Be(256);
        s.PGAAllocatedMB.Should().Be(128);
        s.Tablespaces.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Session_record_equality_uses_value_semantics()
    {
        var a = new Session { Username = "u", Maschine = "m", Program = "p", Status = "s" };
        var b = new Session { Username = "u", Maschine = "m", Program = "p", Status = "s" };
        a.Should().Be(b);
    }

    [Fact]
    public void Tablespace_record_holds_values()
    {
        var t = new Tablespace
        {
            TableSpaceName = "USERS",
            TotalMB = 1000,
            UsedMB = 800,
            FreeMB = 200,
            UsedPercent = 80
        };

        t.TableSpaceName.Should().Be("USERS");
        t.TotalMB.Should().Be(1000);
        t.UsedMB.Should().Be(800);
        t.FreeMB.Should().Be(200);
        t.UsedPercent.Should().Be(80);
    }

    [Fact]
    public void File_record_holds_values()
    {
        var f = new File
        {
            FileLogicalName = "data1",
            Type = "ROWS",
            FileSizeMB = 500,
            FileMaxSizeMB = -1,
            Growth = 10,
            IsPercentigGrowth = true,
            PysicalName = "/var/lib/data1.mdf"
        };

        f.FileLogicalName.Should().Be("data1");
        f.Type.Should().Be("ROWS");
        f.FileSizeMB.Should().Be(500);
        f.FileMaxSizeMB.Should().Be(-1);
        f.Growth.Should().Be(10);
        f.IsPercentigGrowth.Should().BeTrue();
        f.PysicalName.Should().Be("/var/lib/data1.mdf");
    }
}
