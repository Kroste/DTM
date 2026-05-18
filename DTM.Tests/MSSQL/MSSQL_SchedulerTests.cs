using System;
using DTM.MSSQL;
using FluentAssertions;
using Xunit;

namespace DTM.Tests.MSSQL;

public class MSSQL_SchedulerTests
{
    private static MSSQL_ODBC Odbc(string user = "testuser", string pw = "testpw", string server = "FOC-SQL01")
        => new(new ServerCredential(server, user, pw));

    // --- EscPs ---

    [Fact]
    public void EscPs_leaves_plain_string_unchanged()
    {
        MSSQL_ODBC.EscPs("NoSpecialChars").Should().Be("NoSpecialChars");
    }

    [Fact]
    public void EscPs_doubles_single_quotes()
    {
        MSSQL_ODBC.EscPs("O'Brien").Should().Be("O''Brien");
    }

    [Fact]
    public void EscPs_doubles_multiple_single_quotes()
    {
        MSSQL_ODBC.EscPs("it's a 'test'").Should().Be("it''s a ''test''");
    }

    // --- BuildRunPs ---

    [Fact]
    public void BuildRunPs_contains_server()
    {
        var ps = Odbc(server: "MY-SQL01").BuildRunPs("DB1", "Database-Backup.ps1");
        ps.Should().Contain("MY-SQL01");
    }

    [Fact]
    public void BuildRunPs_contains_database()
    {
        var ps = Odbc().BuildRunPs("MyDatabase", "Database-Backup.ps1");
        ps.Should().Contain("MyDatabase");
    }

    [Fact]
    public void BuildRunPs_contains_script_name()
    {
        var ps = Odbc().BuildRunPs("DB1", "Database-Backup.ps1");
        ps.Should().Contain("Database-Backup.ps1");
    }

    [Fact]
    public void BuildRunPs_contains_unc_path()
    {
        var ps = Odbc().BuildRunPs("DB1", "Database-Backup.ps1");
        ps.Should().Contain(@"\\samba01\542$");
    }

    [Fact]
    public void BuildRunPs_uses_PSSession_and_Invoke_Command()
    {
        var ps = Odbc().BuildRunPs("DB1", "Database-Backup.ps1");
        ps.Should().Contain("New-PSSession");
        ps.Should().Contain("Invoke-Command");
    }

    [Theory]
    [InlineData("Database-Backup.ps1")]
    [InlineData("Database-Clone.ps1")]
    public void BuildRunPs_works_for_both_scripts(string script)
    {
        var ps = Odbc().BuildRunPs("DB1", script);
        ps.Should().Contain(script);
    }

    // --- BuildSchedulePs ---

    [Fact]
    public void BuildSchedulePs_contains_Register_ScheduledTask()
    {
        var at = new DateTime(2030, 6, 15, 10, 30, 0);
        var ps = Odbc().BuildSchedulePs("DB1", "Database-Backup.ps1", at);
        ps.Should().Contain("Register-ScheduledTask");
    }

    [Fact]
    public void BuildSchedulePs_contains_schedule_time()
    {
        var at = new DateTime(2030, 6, 15, 10, 30, 0);
        var ps = Odbc().BuildSchedulePs("DB1", "Database-Backup.ps1", at);
        ps.Should().Contain("2030-06-15 10:30");
    }

    [Fact]
    public void BuildSchedulePs_task_name_contains_database_script_and_timestamp()
    {
        var at = new DateTime(2030, 6, 15, 10, 30, 0);
        var ps = Odbc().BuildSchedulePs("MyDB", "Database-Backup.ps1", at);
        ps.Should().Contain("DTM_Database-Backup_ps1_MyDB_203006151030");
    }

    [Fact]
    public void BuildSchedulePs_task_name_escapes_single_quotes_in_database()
    {
        var at = new DateTime(2030, 1, 1, 0, 0, 0);
        var ps = Odbc().BuildSchedulePs("O'DB", "Database-Backup.ps1", at);
        ps.Should().Contain("O''DB");
    }

    [Fact]
    public void BuildSchedulePs_contains_encoded_inner_script()
    {
        var at = new DateTime(2030, 6, 15, 10, 30, 0);
        var ps = Odbc().BuildSchedulePs("DB1", "Database-Backup.ps1", at);
        ps.Should().Contain("-EncodedCommand");
        ps.Should().Contain("New-PSSession");
    }

    [Fact]
    public void BuildSchedulePs_uses_RunLevel_Highest()
    {
        var at = new DateTime(2030, 1, 1, 0, 0, 0);
        var ps = Odbc().BuildSchedulePs("DB1", "Database-Backup.ps1", at);
        ps.Should().Contain("RunLevel Highest");
    }
}
