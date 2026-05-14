using DTM.MSSQL;
using DTM.ORACLE;
using FluentAssertions;
using Xunit;

namespace DTM.Tests.Factory;

public class ODBC_FactoryTests
{
    private static readonly ServerCredential Cred = new("srv", "u", "p", "db");

    [Fact]
    public void Get_DATA_for_MSSQL_returns_MSSQL_ODBC_instance()
    {
        var factory = new ODBC_Factory();

        var instance = factory.Get_DATA("MSSQL", Cred);

        instance.Should().NotBeNull();
        instance.Should().BeOfType<MSSQL_ODBC>();
    }

    [Fact]
    public void Get_DATA_for_ORACLE_returns_ORACLE_ODBC_instance()
    {
        var factory = new ODBC_Factory();

        var instance = factory.Get_DATA("ORACLE", Cred);

        instance.Should().NotBeNull();
        instance.Should().BeOfType<ORACLE_ODBC>();
    }

    [Fact]
    public void Get_DATA_for_unknown_name_returns_null()
    {
        var factory = new ODBC_Factory();

        factory.Get_DATA("UNBEKANNT", Cred).Should().BeNull();
    }

    [Fact]
    public void Second_call_returns_same_instance_per_factory()
    {
        var factory = new ODBC_Factory();

        var first = factory.Get_DATA("MSSQL", Cred);
        var second = factory.Get_DATA("MSSQL", Cred);

        second.Should().BeSameAs(first);
    }

    [Fact]
    public void Different_factories_yield_different_instances()
    {
        var first = new ODBC_Factory().Get_DATA("MSSQL", Cred);
        var second = new ODBC_Factory().Get_DATA("MSSQL", Cred);

        second.Should().NotBeSameAs(first);
    }

    [Fact]
    public void Factory_caches_MSSQL_and_ORACLE_independently()
    {
        var factory = new ODBC_Factory();

        var mssql = factory.Get_DATA("MSSQL", Cred);
        var oracle = factory.Get_DATA("ORACLE", Cred);

        mssql.Should().NotBeSameAs(oracle);
        mssql.Should().BeOfType<MSSQL_ODBC>();
        oracle.Should().BeOfType<ORACLE_ODBC>();
    }
}
