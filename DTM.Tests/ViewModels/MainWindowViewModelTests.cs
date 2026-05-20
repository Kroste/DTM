using DTM.ViewModels;
using DTM.ViewModels.TreeNodes;
using FluentAssertions;
using Xunit;

namespace DTM.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private sealed class StubData : IDTM_DATA
    {
        public List<Database_Info> get_Database_Names(DB_SERVER.ServerTyp t) => [];
        public Database_Stats get_Database_Stats(DB_SERVER.ServerTyp t, Database_Info db)
            => new Database_Stats_MSSQL();
    }

    private static MainWindowViewModel MakeVm(params DB_SERVER.ServerTyp[] types)
    {
        var servers = types.ToDictionary(t => t, _ => new DB_SERVER(new ServerCredential()));
        return new MainWindowViewModel(new StubData(), servers);
    }

    private static DatabaseNodeViewModel MakeDbNode(string name, DB_SERVER.ServerTyp typ,
        string fqdn = "")
    {
        var info = new Database_Info { Name = name, FQDN = fqdn, Id = "1", Status = Database_Status.up };
        return new DatabaseNodeViewModel(info, typ);
    }

    // ------------------------------------------------------------------ Constructor

    [Fact]
    public void Constructor_EmptyServers_RootNodesEmpty()
    {
        MakeVm().RootNodes.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_TwoServers_RootNodesHasTwo()
    {
        MakeVm(DB_SERVER.ServerTyp.MSSQL, DB_SERVER.ServerTyp.ORACLE)
            .RootNodes.Should().HaveCount(2);
    }

    // ------------------------------------------------------------------ ApplyStats MSSQL

    [Fact]
    public void ApplyStats_Mssql_BackupButtonText_IsBackup()
    {
        var vm = MakeVm();
        vm.ApplyStats(new Database_Stats_MSSQL());
        vm.BackupButtonText.Should().Be("Backup");
    }

    [Fact]
    public void ApplyStats_Mssql_RecoveryLabel_IsRecovery()
    {
        var vm = MakeVm();
        vm.ApplyStats(new Database_Stats_MSSQL());
        vm.RecoveryLabel.Should().Be("Recovery");
    }

    [Fact]
    public void ApplyStats_Mssql_SetsDbName()
    {
        var vm = MakeVm();
        vm.ApplyStats(new Database_Stats_MSSQL { Name = "AdventureWorks" });
        vm.DbName.Should().Be("AdventureWorks");
    }

    [Fact]
    public void ApplyStats_Mssql_SetsDbHost()
    {
        var vm = MakeVm();
        vm.ApplyStats(new Database_Stats_MSSQL { Server = "sql01" });
        vm.DbHost.Should().Be("sql01");
    }

    [Fact]
    public void ApplyStats_Mssql_SetsDbVersion_AsCompatLevel()
    {
        var vm = MakeVm();
        vm.ApplyStats(new Database_Stats_MSSQL { CompatibllityLevel = 160 });
        vm.DbVersion.Should().Be("160");
    }

    [Fact]
    public void ApplyStats_Mssql_SetsDbSize_WithMbSuffix()
    {
        var vm = MakeVm();
        vm.ApplyStats(new Database_Stats_MSSQL { DataSizeMB = 512.5 });
        vm.DbSize.Should().Be("512.5 MB");
    }

    [Fact]
    public void ApplyStats_Mssql_NullFields_FallToDash()
    {
        var vm = MakeVm();
        vm.ApplyStats(new Database_Stats_MSSQL { Name = null, Server = null, State = null, RecorveryModel = null });
        vm.DbName.Should().Be("—");
        vm.DbHost.Should().Be("—");
        vm.DbStatus.Should().Be("—");
        vm.RecoveryOrArchiveMode.Should().Be("—");
    }

    // ------------------------------------------------------------------ ApplyStats Oracle

    [Fact]
    public void ApplyStats_Oracle_BackupButtonText_IsDump()
    {
        var vm = MakeVm();
        vm.ApplyStats(new Database_Stats_ORACLE());
        vm.BackupButtonText.Should().Be("Dump");
    }

    [Fact]
    public void ApplyStats_Oracle_RecoveryLabel_IsArchiveLog()
    {
        var vm = MakeVm();
        vm.ApplyStats(new Database_Stats_ORACLE());
        vm.RecoveryLabel.Should().Be("ArchiveLog");
    }

    [Fact]
    public void ApplyStats_Oracle_SetsAllOracleFields()
    {
        var vm = MakeVm();
        vm.ApplyStats(new Database_Stats_ORACLE
        {
            InstanceName = "ORCL",
            Server = "orasrv",
            State = "OPEN",
            OracleVersion = "19.3",
            DataSizeMB = 1024,
            ArchiveLogMode = "ARCHIVELOG"
        });
        vm.DbName.Should().Be("ORCL");
        vm.DbHost.Should().Be("orasrv");
        vm.DbStatus.Should().Be("OPEN");
        vm.DbVersion.Should().Be("19.3");
        vm.DbSize.Should().Be("1024 MB");
        vm.RecoveryOrArchiveMode.Should().Be("ARCHIVELOG");
    }

    // ------------------------------------------------------------------ ApplyStats Sessions

    [Fact]
    public void ApplyStats_SessionsNull_CountIsZero()
    {
        var vm = MakeVm();
        vm.ApplyStats(new Database_Stats_MSSQL { Sessions = null });
        vm.ActiveSessionsCount.Should().Be("0");
    }

    [Fact]
    public void ApplyStats_Sessions_SetsCountAndLabel()
    {
        var vm = MakeVm();
        vm.ApplyStats(new Database_Stats_MSSQL
        {
            Sessions = [new Session(), new Session(), new Session()]
        });
        vm.ActiveSessionsCount.Should().Be("3");
        vm.ActiveSessionsLabel.Should().Be("Aktive Sessions: 3");
    }

    // ------------------------------------------------------------------ ModuleDatabaseId

    [Fact]
    public void ModuleDatabaseId_Mssql_ReturnsName()
    {
        var node = MakeDbNode("MyDB", DB_SERVER.ServerTyp.MSSQL);
        MainWindowViewModel.ModuleDatabaseId(node).Should().Be("MyDB");
    }

    [Fact]
    public void ModuleDatabaseId_Oracle_WithFqdn_ReturnsFqdn()
    {
        var node = MakeDbNode("VM-ORACLE", DB_SERVER.ServerTyp.ORACLE, fqdn: "ora.company.local");
        MainWindowViewModel.ModuleDatabaseId(node).Should().Be("ora.company.local");
    }

    [Fact]
    public void ModuleDatabaseId_Oracle_EmptyFqdn_FallsBackToName()
    {
        var node = MakeDbNode("VM-ORACLE", DB_SERVER.ServerTyp.ORACLE, fqdn: "");
        MainWindowViewModel.ModuleDatabaseId(node).Should().Be("VM-ORACLE");
    }

    [Fact]
    public void ModuleDatabaseId_Oracle_NullFqdn_FallsBackToName()
    {
        var info = new Database_Info { Name = "VM-ORACLE", FQDN = null, Id = "1", Status = Database_Status.up };
        var node = new DatabaseNodeViewModel(info, DB_SERVER.ServerTyp.ORACLE);
        MainWindowViewModel.ModuleDatabaseId(node).Should().Be("VM-ORACLE");
    }
}
