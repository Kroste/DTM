using DTM.ODBC;
using FluentAssertions;
using Moq;
using Xunit;

namespace DTM.Tests;

public class DTM_DATATests
{
    private static readonly ServerCredential MssqlCred = new("mssql-srv", "u", "p", "db");
    private static readonly ServerCredential OracleCred = new("oracle-srv", "u", "p", "db");

    private static Dictionary<DB_SERVER.ServerTyp, DB_SERVER> Servers()
        => new()
        {
            { DB_SERVER.ServerTyp.MSSQL, new DB_SERVER(MssqlCred) },
            { DB_SERVER.ServerTyp.ORACLE, new DB_SERVER(OracleCred) },
        };

    private static Database_Info NewInfo() => new()
    {
        Name = "DB1",
        Id = "1",
        FQDN = "host.local",
        Status = Database_Status.up
    };

    [Fact]
    public void Constructor_with_null_factory_throws()
    {
        Action act = () => new DTM_DATA(Servers(), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_stores_dictionary()
    {
        var dict = Servers();
        var factory = Mock.Of<IODBC_Factory>();

        var data = new DTM_DATA(dict, factory);

        data.db_Servers.Should().BeSameAs(dict);
    }

    [Fact]
    public void get_Database_Names_delegates_to_factory_and_odbc()
    {
        var expected = new List<Database_Info> { NewInfo() };
        var odbc = new Mock<IDTM_ODBC>();
        odbc.Setup(o => o.get_Datenbank_Names()).Returns(expected);

        var factory = new Mock<IODBC_Factory>();
        factory.Setup(f => f.Get_DATA("MSSQL", MssqlCred)).Returns(odbc.Object);

        var result = new DTM_DATA(Servers(), factory.Object)
            .get_Database_Names(DB_SERVER.ServerTyp.MSSQL);

        result.Should().BeSameAs(expected);
        factory.Verify(f => f.Get_DATA("MSSQL", MssqlCred), Times.Once);
        odbc.Verify(o => o.get_Datenbank_Names(), Times.Once);
    }

    [Fact]
    public void get_Database_Names_uses_oracle_credential_for_oracle_server()
    {
        var odbc = new Mock<IDTM_ODBC>();
        odbc.Setup(o => o.get_Datenbank_Names()).Returns(new List<Database_Info>());

        var factory = new Mock<IODBC_Factory>();
        factory.Setup(f => f.Get_DATA("ORACLE", OracleCred)).Returns(odbc.Object);

        new DTM_DATA(Servers(), factory.Object).get_Database_Names(DB_SERVER.ServerTyp.ORACLE);

        factory.Verify(f => f.Get_DATA("ORACLE", OracleCred), Times.Once);
    }

    [Fact]
    public void get_Database_Stats_delegates_to_factory_with_database()
    {
        var info = NewInfo();
        var stats = new Database_Stats_MSSQL { Name = "DB1" };

        var odbc = new Mock<IDTM_ODBC>();
        odbc.Setup(o => o.GetDatabase_Stats(info)).Returns(stats);

        var factory = new Mock<IODBC_Factory>();
        factory.Setup(f => f.Get_DATA("MSSQL", MssqlCred)).Returns(odbc.Object);

        var result = new DTM_DATA(Servers(), factory.Object)
            .get_Database_Stats(DB_SERVER.ServerTyp.MSSQL, info);

        result.Should().BeSameAs(stats);
        odbc.Verify(o => o.GetDatabase_Stats(info), Times.Once);
    }

    [Fact]
    public void Backup_Database_passes_arguments_through()
    {
        var info = NewInfo();
        var when = new DateTime(2030, 1, 2, 3, 4, 5);

        var odbc = new Mock<IDTM_ODBC>();
        odbc.Setup(o => o.Backup_Database(info, when)).Returns(true);

        var factory = new Mock<IODBC_Factory>();
        factory.Setup(f => f.Get_DATA("ORACLE", OracleCred)).Returns(odbc.Object);

        var result = new DTM_DATA(Servers(), factory.Object)
            .Backup_Database(DB_SERVER.ServerTyp.ORACLE, info, when);

        result.Should().BeTrue();
        odbc.Verify(o => o.Backup_Database(info, when), Times.Once);
    }

    [Fact]
    public void Clone_Database_passes_arguments_through()
    {
        var info = NewInfo();
        var when = new DateTime(2031, 5, 6, 7, 8, 9);

        var odbc = new Mock<IDTM_ODBC>();
        odbc.Setup(o => o.Clone_Database(info, when)).Returns(false);

        var factory = new Mock<IODBC_Factory>();
        factory.Setup(f => f.Get_DATA("MSSQL", MssqlCred)).Returns(odbc.Object);

        var result = new DTM_DATA(Servers(), factory.Object)
            .Clone_Database(DB_SERVER.ServerTyp.MSSQL, info, when);

        result.Should().BeFalse();
        odbc.Verify(o => o.Clone_Database(info, when), Times.Once);
    }
}
