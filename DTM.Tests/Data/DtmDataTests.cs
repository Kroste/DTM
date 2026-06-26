using FluentAssertions;
using Xunit;

namespace DTM.Tests.Data;

public class DtmDataTests
{
    private sealed class FakeOdbc : ODBC.IDTM_ODBC
    {
        public List<Database_Info> Names { get; set; } = [];
        public Database_Stats Stats { get; set; } = new Database_Stats_MSSQL();

        public List<Database_Info> get_Datenbank_Names() => Names;
        public Database_Stats GetDatabase_Stats(Database_Info db) => Stats;
    }

    private sealed class FakeFactory : IODBC_Factory
    {
        public string? LastRequested;
        public readonly FakeOdbc Odbc = new();

        public ODBC.IDTM_ODBC? Get_DATA(string name, ServerCredential cred)
        {
            LastRequested = name;
            return name is "MSSQL" or "ORACLE" ? Odbc : null;
        }
    }

    private static (DTM_DATA data, FakeFactory factory) Make(DB_SERVER.ServerTyp typ)
    {
        var factory = new FakeFactory();
        var servers = new Dictionary<DB_SERVER.ServerTyp, DB_SERVER>
        {
            [typ] = new DB_SERVER(typ, new ServerCredential("server", "user", "pass", "db", ""))
        };
        return (new DTM_DATA(servers, factory), factory);
    }

    [Fact]
    public void GetDatabaseNames_Mssql_RequestsFactoryWithMssql()
    {
        var (data, factory) = Make(DB_SERVER.ServerTyp.MSSQL);
        data.get_Database_Names(DB_SERVER.ServerTyp.MSSQL);
        factory.LastRequested.Should().Be("MSSQL");
    }

    [Fact]
    public void GetDatabaseNames_Oracle_RequestsFactoryWithOracle()
    {
        var (data, factory) = Make(DB_SERVER.ServerTyp.ORACLE);
        data.get_Database_Names(DB_SERVER.ServerTyp.ORACLE);
        factory.LastRequested.Should().Be("ORACLE");
    }

    [Fact]
    public void GetDatabaseNames_ReturnsOdbcResult()
    {
        var (data, factory) = Make(DB_SERVER.ServerTyp.MSSQL);
        factory.Odbc.Names = [new Database_Info { Name = "TestDB", Id = "1", FQDN = "", Status = Database_Status.up }];
        var result = data.get_Database_Names(DB_SERVER.ServerTyp.MSSQL);
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("TestDB");
    }

    [Fact]
    public void GetDatabaseStats_Mssql_RequestsFactoryWithMssql()
    {
        var (data, factory) = Make(DB_SERVER.ServerTyp.MSSQL);
        data.get_Database_Stats(DB_SERVER.ServerTyp.MSSQL, new Database_Info { Name = "db", Id = "1", FQDN = "", Status = Database_Status.up });
        factory.LastRequested.Should().Be("MSSQL");
    }

    [Fact]
    public void GetDatabaseStats_ReturnsOdbcResult()
    {
        var (data, factory) = Make(DB_SERVER.ServerTyp.MSSQL);
        var expected = new Database_Stats_MSSQL { Name = "MyDB" };
        factory.Odbc.Stats = expected;
        var result = data.get_Database_Stats(DB_SERVER.ServerTyp.MSSQL, new Database_Info { Name = "MyDB", Id = "1", FQDN = "", Status = Database_Status.up });
        result.Should().BeSameAs(expected);
    }

    [Fact]
    public void DbServers_ExposedCorrectly()
    {
        var (data, _) = Make(DB_SERVER.ServerTyp.MSSQL);
        data.db_Servers.Should().ContainKey(DB_SERVER.ServerTyp.MSSQL);
    }

    [Fact]
    public void GetDatabaseNames_MissingServerType_ThrowsNullRef()
    {
        var (data, _) = Make(DB_SERVER.ServerTyp.MSSQL);
        // ORACLE key missing → FirstOrDefault returns default → Value is null → throws
        Action act = () => data.get_Database_Names(DB_SERVER.ServerTyp.ORACLE);
        act.Should().Throw<Exception>();
    }
}
