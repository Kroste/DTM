using System.Data;
using System.Data.Odbc;
using System.Text;
using DTM.ODBC;
using NLog;

namespace DTM.MSSQL
{
    public class MSSQL_ODBC : IDisposable, IDTM_ODBC
    {
        private ServerCredential cred { get; set; }
        private OdbcConnection conn { get; set; }
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public MSSQL_ODBC(ServerCredential credential)
        {
            this.cred = credential;
            conn = new OdbcConnection();
        }

        private bool conn_open()
        {
            switch (conn.State)
            {
                case ConnectionState.Open:
                    {
                        return true;
                    }
                case ConnectionState.Closed:

                    string con = $"Driver=SQL Server;Server={cred.Server};Database={cred.Datenbank};UID={cred.User};PWD={cred.Password};";
                    conn.ConnectionString = con;
                    try
                    {
                        conn.Open();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex.Message);
                        return false;
                    }
                default: return false;
            }
        }

        private DataTable get_Rows(string SQL)
        {
            if (string.IsNullOrWhiteSpace(SQL))
            {
                _logger.Error("SQL Leer");
                throw new Exception("SQL Leer");
            }

            _logger.Debug($"SQL: {SQL}");
            if (conn_open())
            {
                DataTable dt = new DataTable();


                OdbcDataAdapter ad = new OdbcDataAdapter(SQL, conn);
                ad.Fill(dt);
                return dt;

            }
            else
            {
                _logger.Error("Keine Verbindung zur Datenbank");
                throw new Exception("Keine Verbindung zur Datenbank");
            }
        }

        public void Dispose()
        {
            if (conn.State == ConnectionState.Open)
            {
                conn.Close();
            }
            conn.Dispose();
        }

        public List<Database_Info> get_Datenbank_Names()
        {
            return get_Rows(
           "SELECT [name], database_id, state_desc, '' AS [FQDN] FROM sys.databases")
       .AsEnumerable()
       .Select(r => new Database_Info
       {
           Name = r.Field<string>("name")!,
           Id = Convert.ToString(r.Field<int>("database_id")),
           FQDN = r.Field<string>("FQDN") ?? string.Empty,
           Status = string.Equals(
                        r.Field<string>("state_desc"),
                        "ONLINE",
                        StringComparison.OrdinalIgnoreCase)
                    ? Database_Status.up
                    : Database_Status.down
       })
       .ToList();
        }

        public Database_Stats GetDatabase_Stats(Database_Info database)
        {
            Database_Stats_MSSQL stats = new Database_Stats_MSSQL
            {
                DatabaseTyp = "MSSQL",
                Server = cred.Server
            };

            // ---------- 1. Hauptdaten aus sys.databases ----------
            DataTable dtMain = get_Select_From_sysDatabase(database.Name);
            if (dtMain.Rows.Count > 0)
            {
                DataRow row = dtMain.Rows[0];

                // Basisklasse
                stats.Name = row["DatabaseName"] as string;
                stats.State = row["State"] as string;
                stats.DataSizeMB = ToDouble(row["DataSizeMB"]);

                // MSSQL-spezifisch
                stats.RecorveryModel = row["RecoveryModel"] as string;
                stats.CompatibllityLevel = ToInt(row["CompatibilityLevel"]);
                stats.Collation = row["Collation"] as string;
                stats.UserAccess = row["UserAccess"] as string;
                stats.IsReadOnly = ToBool(row["IsReadOnly"]);
                stats.PageVerify = row["PageVerify"] as string;
                stats.CreationTime = row["CreateDate"] as DateTime?;
                stats.LogSizeMB = ToDouble(row["LogSizeMB"]);
                stats.TotalSizeMB = ToDouble(row["TotalSizeMB"]);
                stats.LastFullBackup = row["LastFullBackup"] as DateTime?;
                stats.LastDiffBackup = row["LastDiffBackup"] as DateTime?;
                stats.LastLogBackup = row["LastLogBackup"] as DateTime?;
            }

            // ---------- 2. Files aus sys.master_files ----------
            stats.Files = new List<DTM.File>();
            DataTable dtFiles = get_Select_From_Master_Files(database.Name);
            foreach (DataRow row in dtFiles.Rows)
            {
                stats.Files.Add(new DTM.File
                {
                    FileLogicalName = row["FileLogicalName"] as string,
                    Type = row["FileType"] as string,
                    FileSizeMB = ToDouble(row["FileSizeMB"]),
                    FileMaxSizeMB = ToDouble(row["FileMaxSizeMB"]),
                    Growth = ToDouble(row["growth"]),
                    IsPercentigGrowth = ToBool(row["is_percent_growth"]),
                    PysicalName = row["physical_name"] as string
                });
            }

            // ---------- 3. Sessions aus sys.dm_exec_sessions ----------
            stats.Sessions = new List<Session>();
            DataTable dtSessions = get_Select_From_dm_exec_sessions(database.Name);
            foreach (DataRow row in dtSessions.Rows)
            {
                stats.Sessions.Add(new Session
                {
                    Username = row["LoginName"] as string,
                    Maschine = row["HostName"] as string,
                    Program = row["ProgramName"] as string,
                    Status = row["Status"] as string
                });
            }
            stats.ActiveConnections = stats.Sessions.Count;

            // ---------- 4. Buffer Pool ----------
            DataTable dtBuffer = get_Select_From_BufferPoolMB(database.Name);
            if (dtBuffer.Rows.Count > 0)
            {
                stats.BufferSizeMB = ToDouble(dtBuffer.Rows[0]["BufferPoolMB"]);
            }

            return stats;
        }

        // ---------- kleine Helper für robuste Konvertierungen ----------
        private static double ToDouble(object value)
            => value == null || value == DBNull.Value ? 0d : Convert.ToDouble(value);

        private static int ToInt(object value)
            => value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);

        private static bool ToBool(object value)
            => value != null && value != DBNull.Value && Convert.ToBoolean(value);
        private DataTable get_Select_From_sysDatabase(string Database)
        {
            StringBuilder sql = new StringBuilder();

            sql.Append($"SELECT ");
            sql.Append($"d.name                       AS DatabaseName,");
            sql.Append($"d.database_id                AS DatabaseId,");
            sql.Append($"d.state_desc                 AS State,");
            sql.Append($"d.recovery_model_desc        AS RecoveryModel,");
            sql.Append($"d.compatibility_level        AS CompatibilityLevel,");
            sql.Append($"d.collation_name             AS Collation,");
            sql.Append($"d.user_access_desc           AS UserAccess,");
            sql.Append($"d.is_read_only               AS IsReadOnly,");
            sql.Append($"d.page_verify_option_desc    AS PageVerify,");
            sql.Append($"d.create_date                AS CreateDate,");

            sql.Append($"(SELECT ROUND(SUM(CASE WHEN mf.type = 0 THEN mf.size ELSE 0 END) * 8.0 / 1024, 2)");
            sql.Append($"FROM sys.master_files mf WHERE mf.database_id = d.database_id)   AS DataSizeMB,");

            sql.Append($"(SELECT ROUND(SUM(CASE WHEN mf.type = 1 THEN mf.size ELSE 0 END) * 8.0 / 1024, 2)");
            sql.Append($"FROM sys.master_files mf WHERE mf.database_id = d.database_id)   AS LogSizeMB,");

            sql.Append($"(SELECT ROUND(SUM(mf.size) * 8.0 / 1024, 2)");
            sql.Append($"FROM sys.master_files mf WHERE mf.database_id = d.database_id)   AS TotalSizeMB,");

            sql.Append($"(SELECT MAX(bs.backup_finish_date) FROM msdb.dbo.backupset bs ");
            sql.Append($"WHERE bs.database_name = '{Database}' AND bs.type = 'D')                   AS LastFullBackup,");

            sql.Append($"(SELECT MAX(bs.backup_finish_date) FROM msdb.dbo.backupset bs ");
            sql.Append($"WHERE bs.database_name = '{Database}' AND bs.type = 'I')                   AS LastDiffBackup,");

            sql.Append($"(SELECT MAX(bs.backup_finish_date) FROM msdb.dbo.backupset bs ");
            sql.Append($"WHERE bs.database_name = '{Database}' AND bs.type = 'L')                   AS LastLogBackup ");

            sql.Append($"FROM sys.databases d ");
            sql.Append($"WHERE d.name = '{Database}';");

            return get_Rows(sql.ToString());
        }
        private DataTable get_Select_From_Master_Files(string Database)
        {
            StringBuilder sql = new StringBuilder();

            sql.Append($"SELECT ");
            sql.Append($"mf.name             AS FileLogicalName, ");
            sql.Append($"mf.type_desc        AS FileType, ");
            sql.Append($"ROUND(mf.size * 8.0 / 1024, 2) AS FileSizeMB, ");
            sql.Append($"CASE mf.max_size WHEN -1 THEN -1 ELSE ROUND(mf.max_size * 8.0 / 1024, 2) END AS FileMaxSizeMB, ");
            sql.Append($"mf.growth, ");
            sql.Append($"mf.is_percent_growth, ");
            sql.Append($"mf.physical_name ");
            sql.Append($"FROM sys.master_files mf ");
            sql.Append($"WHERE mf.database_id = DB_ID(N'{Database}') ");
            sql.Append($"ORDER BY mf.type_desc, mf.name; ");

            return get_Rows(sql.ToString());
        }

        private DataTable get_Select_From_dm_exec_sessions(string Database)
        {
            StringBuilder sql = new StringBuilder();

            sql.Append($"SELECT ");
            sql.Append($"s.session_id        AS SessionId,");
            sql.Append($"s.host_name         AS HostName,");
            sql.Append($"s.login_name        AS LoginName,");
            sql.Append($"s.program_name      AS ProgramName,");
            sql.Append($"s.status            AS Status,");
            sql.Append($"s.login_time        AS LoginTime,");
            sql.Append($"DB_NAME(s.database_id) AS CurrentDatabase ");
            sql.Append($"FROM sys.dm_exec_sessions s ");
            sql.Append($"WHERE s.database_id = DB_ID(N'{Database}') ");
            sql.Append($"AND s.is_user_process = 1 ");
            sql.Append($"ORDER BY s.host_name, s.login_name; ");

            return get_Rows(sql.ToString());
        }

        private DataTable get_Select_From_BufferPoolMB(string Database)
        {
            StringBuilder sql = new StringBuilder();

            sql.Append($"SELECT ISNULL( ");
            sql.Append($"CAST( ");
            sql.Append($"(SELECT TOP(1) cntr_value FROM sys.dm_os_performance_counters ");
            sql.Append($"WHERE counter_name = 'Database pages' AND object_name LIKE '%Buffer Manager%') ");
            sql.Append($"* 1.0 ");
            sql.Append($"* (SELECT SUM(size) FROM sys.master_files WHERE database_id = DB_ID(N'{Database}') AND type = 0) ");
            sql.Append($"/ NULLIF((SELECT SUM(size) FROM sys.master_files WHERE type = 0), 0) ");
            sql.Append($"* 8.0 / 1024 ");
            sql.Append($"AS decimal(18,2)) ");
            sql.Append($", 0) AS BufferPoolMB; ");

            return get_Rows(sql.ToString());
        }
    }
}