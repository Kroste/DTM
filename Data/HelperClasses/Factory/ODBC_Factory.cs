namespace DTM
{
    public interface IODBC_Factory
    {
        ODBC.IDTM_ODBC? Get_DATA(string Name, ServerCredential credential);
    }

    public class ODBC_Factory : IODBC_Factory
    {
        private ODBC.IDTM_ODBC? _mssql_odbc;
        private ODBC.IDTM_ODBC? _oracle_odbc;

        public ODBC.IDTM_ODBC? Get_DATA(string Name, ServerCredential credential)
        {
            switch (Name)
            {
                case "MSSQL":
                    if (null == _mssql_odbc)
                    {
                        _mssql_odbc = new MSSQL.MSSQL_ODBC(credential);
                    }
                    return _mssql_odbc;
                case "ORACLE":
                    if (null == _oracle_odbc)
                    {
                        _oracle_odbc = new ORACLE.ORACLE_ODBC(credential);
                    }
                    return _oracle_odbc;
                default: return null;
            }
        }
    }
}
