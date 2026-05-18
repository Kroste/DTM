using System.Data;
using System.Data.Odbc;
using System.Diagnostics;
using System.Text;
using DTM.ODBC;
using NLog;

namespace DTM.MSSQL
{
    public class MSSQL_ODBC(ServerCredential credential) : IDisposable, IDTM_ODBC
    {
        private ServerCredential Credential { get; set; } = credential;
        private OdbcConnection Connection { get; set; } = new OdbcConnection();
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private bool conn_open()
        {
            switch (Connection.State)
            {
                case ConnectionState.Open:
                    {
                        return true;
                    }
                case ConnectionState.Closed:

                    string driverFragment = OperatingSystem.IsWindows()
                        ? "Driver=SQL Server"
                        : "Driver={ODBC Driver 18 for SQL Server};TrustServerCertificate=yes";
                    string con = $"{driverFragment};Server={Credential.Server};Database={Credential.Datenbank};UID={Credential.User};PWD={Credential.Password};";
                    Connection.ConnectionString = con;
                    try
                    {
                        Connection.Open();
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
                DataTable dt = new();


                using (OdbcDataAdapter ad = new(SQL, Connection))
                {
                    ad.Fill(dt);
                }

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
            if (Connection.State == ConnectionState.Open)
            {
                Connection.Close();
            }
            Connection.Dispose();
        }

        public List<Database_Info> get_Datenbank_Names()
        {
            return get_Rows(
           "SELECT [name], database_id, state_desc, '' AS [FQDN] FROM sys.databases WHERE database_id > 4")
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
       }).OrderBy(db => db.Name)
       .ToList();
        }

        public Database_Stats GetDatabase_Stats(Database_Info database)
        {
            Database_Stats_MSSQL stats = new()
            {
                DatabaseTyp = "MSSQL",
                Server = Credential.Server
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
            stats.Files = new List<File>();
            DataTable dtFiles = get_Select_From_Master_Files(database.Name);
            foreach (DataRow row in dtFiles.Rows)
            {
                stats.Files.Add(new File
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
        internal static double ToDouble(object value)
            => value == null || value == DBNull.Value ? 0d : Convert.ToDouble(value);

        internal static int ToInt(object value)
            => value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);

        internal static bool ToBool(object value)
            => value != null && value != DBNull.Value && Convert.ToBoolean(value);
        private DataTable get_Select_From_sysDatabase(string Database)
        {
            StringBuilder sql = new();

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
            StringBuilder sql = new();

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
            StringBuilder sql = new();

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
            StringBuilder sql = new();

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

        private const string UncBase = @"\\samba01\542$\5422_IT-Basis-Infrastruktur\MS-SQL\Powershell\Datenbanken";

        public bool Backup_Database(Database_Info database, DateTime backupTime)
        {
            string psCmd = backupTime > DateTime.Now
                ? BuildSchedulePs(database.Name, "Database-Backup.ps1", backupTime)
                : BuildRunPs(database.Name, "Database-Backup.ps1");
            ExecuteLocalPs(psCmd);
            return true;
        }

        public bool Clone_Database(Database_Info database, DateTime cloneTime)
        {
            string psCmd = cloneTime > DateTime.Now
                ? BuildSchedulePs(database.Name, "Database-Clone.ps1", cloneTime)
                : BuildRunPs(database.Name, "Database-Clone.ps1");
            ExecuteLocalPs(psCmd);
            return true;
        }

        internal string BuildRunPs(string database, string script)
        {
            string sv  = EscPs(Credential.Server);
            string u   = EscPs(Credential.User);
            string pw  = EscPs(Credential.Password);
            string unc = EscPs(UncBase);
            string db  = EscPs(database);

            // $$""" = double-dollar raw string: {{expr}} is C# interpolation,
            // bare $ { } are literal characters (no doubling needed for PS script blocks).
            return $$"""
                $pw   = ConvertTo-SecureString '{{pw}}' -AsPlainText -Force
                $cred = New-Object PSCredential('{{u}}', $pw)
                $sess = New-PSSession -ComputerName '{{sv}}' -Credential $cred -ErrorAction Stop
                try {
                    Invoke-Command -Session $sess -ArgumentList '{{unc}}','{{db}}','{{u}}','{{pw}}' -ScriptBlock {
                        param($unc,$db,$user,$pwStr)
                        $c = New-Object PSCredential($user,(ConvertTo-SecureString $pwStr -AsPlainText -Force))
                        New-PSDrive -Name DTMz -PSProvider FileSystem -Root $unc -Credential $c -ErrorAction Stop | Out-Null
                        try   { Set-Location "DTMz:\$db"; & ".\{{script}}" }
                        finally { Remove-PSDrive -Name DTMz -Force -ErrorAction SilentlyContinue }
                    }
                } finally { Remove-PSSession $sess }
                """;
        }

        internal string BuildSchedulePs(string database, string script, DateTime at)
        {
            string sv    = EscPs(Credential.Server);
            string u     = EscPs(Credential.User);
            string pw    = EscPs(Credential.Password);
            string unc   = EscPs(UncBase);
            string db    = EscPs(database);
            string task  = $"DTM_{script.Replace('.', '_')}_{EscPs(database)}_{at:yyyyMMddHHmm}";
            string atStr = at.ToString("yyyy-MM-dd HH:mm");

            // Inner script runs on FOC-SQL01 as scheduled task action.
            // Base64-encoded (UTF-16LE) so -EncodedCommand bypasses all quoting issues.
            string inner = $$"""
                $pw = ConvertTo-SecureString '{{pw}}' -AsPlainText -Force
                $c  = New-Object PSCredential('{{u}}', $pw)
                New-PSDrive -Name DTMz -PSProvider FileSystem -Root '{{unc}}' -Credential $c -ErrorAction Stop | Out-Null
                try   { Set-Location "DTMz:\{{db}}"; & ".\{{script}}" }
                finally { Remove-PSDrive -Name DTMz -Force -ErrorAction SilentlyContinue }
                """;
            string enc = Convert.ToBase64String(Encoding.Unicode.GetBytes(inner));

            return $$"""
                $pw   = ConvertTo-SecureString '{{pw}}' -AsPlainText -Force
                $cred = New-Object PSCredential('{{u}}', $pw)
                $sess = New-PSSession -ComputerName '{{sv}}' -Credential $cred -ErrorAction Stop
                try {
                    Invoke-Command -Session $sess -ArgumentList '{{task}}','{{atStr}}','{{enc}}','{{u}}','{{pw}}' -ScriptBlock {
                        param($taskName,$at,$enc,$taskUser,$taskPw)
                        $action  = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument "-NonInteractive -EncodedCommand $enc"
                        $trigger = New-ScheduledTaskTrigger -Once -At $at
                        Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -User $taskUser -Password $taskPw -RunLevel Highest -Force | Out-Null
                        Write-Host "Aufgabe '$taskName' fuer $at registriert"
                    }
                } finally { Remove-PSSession $sess }
                """;
        }

        private string[] ExecuteLocalPs(string script)
        {
            string psExe = OperatingSystem.IsWindows() ? "powershell.exe" : "pwsh";
            string enc = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

            var psi = new ProcessStartInfo
            {
                FileName = psExe,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            psi.ArgumentList.Add("-NonInteractive");
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-EncodedCommand");
            psi.ArgumentList.Add(enc);

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            try
            {
                using var proc = new Process { StartInfo = psi };
                proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
                proc.ErrorDataReceived  += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
                proc.Start();
                proc.StandardInput.Close();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                if (!proc.WaitForExit(120_000))
                {
                    try { proc.Kill(entireProcessTree: true); } catch { }
                    _logger.Warn("PowerShell-Befehl: Timeout nach 120s");
                    return Array.Empty<string>();
                }
                proc.WaitForExit();

                if (proc.ExitCode != 0)
                    _logger.Warn($"PowerShell fehlgeschlagen (Exit {proc.ExitCode}): {stderr.ToString().Trim()}");

                return stdout.ToString().Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            }
            catch (Exception ex)
            {
                _logger.Warn($"Fehler beim Ausführen von PowerShell: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        internal static string EscPs(string s) => s.Replace("'", "''");
    }
}