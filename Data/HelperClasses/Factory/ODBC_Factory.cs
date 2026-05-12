namespace DTM
{
    public static class ODBC_Factory
    {
        static private ODBC.IDTM_ODBC? _mssql_odbc;
        static private ODBC.IDTM_ODBC? _oracle_odbc;
        public static ODBC.IDTM_ODBC? Get_DATA(string Name, ServerCredential credential)
        {
            switch (Name)
            {
                case "MSSQL":
                    if (_mssql_odbc == null)
                    {
                        _mssql_odbc = new MSSQL.MSSQL_ODBC(credential);
                    }
                    return _mssql_odbc;
                case "ORACLE":
                    if (_oracle_odbc == null)
                    {
                        _oracle_odbc = new ORACLE.ORACLE_ODBC(credential);
                    }
                    return _oracle_odbc;
                default: return null;
            }
        }
    }
}