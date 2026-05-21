using NLog;

namespace DTM
{
    public interface IODBC_Factory
    {
        ODBC.IDTM_ODBC? Get_DATA(string Name, ServerCredential credential);
    }

    public class ODBC_Factory : IODBC_Factory
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private ODBC.IDTM_ODBC? _mssql_odbc;
        private ODBC.IDTM_ODBC? _oracle_odbc;

        public ODBC.IDTM_ODBC? Get_DATA(string Name, ServerCredential credential)
        {
            switch (Name)
            {
                case "MSSQL":
                    if (null == _mssql_odbc)
                    {
                        _logger.Debug("ODBC_Factory: Neue MSSQL-Instanz erstellt für Server {0}", credential.Server);
                        _mssql_odbc = new MSSQL.MSSQL_ODBC(credential);
                    }
                    else
                    {
                        _logger.Debug("ODBC_Factory: Bestehende MSSQL-Instanz zurückgegeben.");
                    }
                    return _mssql_odbc;
                case "ORACLE":
                    if (null == _oracle_odbc)
                    {
                        _logger.Debug("ODBC_Factory: Neue ORACLE-Instanz erstellt für Server {0}", credential.Server);
                        _oracle_odbc = new ORACLE.ORACLE_ODBC(credential);
                    }
                    else
                    {
                        _logger.Debug("ODBC_Factory: Bestehende ORACLE-Instanz zurückgegeben.");
                    }
                    return _oracle_odbc;
                default:
                    _logger.Warn("ODBC_Factory: Unbekannter Datenbanktyp '{0}'", Name);
                    return null;
            }
        }
    }
}
