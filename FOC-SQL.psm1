<#
Autor:  Lars Oste 27.05.2025
Fachbereich: FB 54 LH Potsdam
Version: 1.0
#>


$global:Server = "FOC-SQL01"
$global:uncPath = "\\samba01\542$\5422_IT-Basis-Infrastruktur\MS-SQL\Powershell\Datenbanken"
$global:Oracle = @("DBADVIS01", "DBARCHIV01", "DBAVVISO01", "DBCALIFORNIA01", "DBCM601", "DBCMHELPDESK01",
    "DBCONFLUENCE01", "DBD301", "DBELINA01", "DBGEDAS01", "DBGEODIN01", "DBGIS01", "DBGIS02",
    "DBHESSMPA01", "DBHHFIN01", "DBIKOLFS01", "DBIKOLKFZ01", "DBIMDAS01", "DBISGA01", "DBJIRA01",
    "DBJIRA03", "DBJPAX01", "DBKUFER501", "DBMESO01", "DBMIGEWA01", "DBOCTOWARE01", "DBOPENPROSOZ01",
    "DBPROSOZ1401", "DBPDM3D01", "DBPOLITESS01", "DBQSR01", "DBQSRKIS01", "DBSASKIA01", "DBSIBBW01",
    "DBSMARTFINDER01", "DBSOPART01", "DBVMS01", "DBVOISMESO01", "DBZASYS01", "DBZENWORKS01",
    "DBADVISTEST01", "DBAVVISOTEST01", "DBCALIFORNIATEST01", "DBCMHELPDESKTEST01", "DBD3TEST01",
    "DBELINATEST01", "DBGEODINTEST01", "DBHHFINTEST01", "DBIKOLFSTEST01", "DBIKOLKFZTEST01",
    "DBIMDASTEST01", "DBISGATEST01", "DBJIRATEST01", "DBJPAXTEST01", "DBKUFER5TEST01", "DBMIGEWATEST01",
    "DBOPENPROSOZTEST01", "DBPROSOZ14TEST01", "DBQSRTEST01", "DBQSRKISTEST01", "DBSASKIATEST01",
    "DBSMARTFINDERTEST01", "DBSOPARTTEST01", "DBTEST01", "DBVMSTEST01", "DBVOISMESOTEST01",
    "DBHHFINPRUEF01", "DBOPENPROSOZSCHULUNG01", "DBPROSOZ14SCHULUNG01")

function Backup-Database {
    [CmdletBinding()]
    Param(
        [parameter()][string]$Server = $global:Server,
        [parameter()][string]$Time,
        [parameter()][string]$Date
    )

    DynamicParam {
        $parameterAttribute = New-Object System.Management.Automation.ParameterAttribute
        $parameterAttribute.Mandatory = $true
        $parameterAttribute.ValueFromPipeline = $true
        $parameterAttribute.Position = 0
        $parameterAttribute.HelpMessage = "Bitte Datenbank angeben:"
        $parameterAttribute.ParameterSetName = 'Database'

        $attributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
        $attributeCollection.Add($parameterAttribute)

        $result = Get-Databases
        $attributeCollection.Add((New-Object System.Management.Automation.ValidateSetAttribute($result)))

        $RuntimeParam = New-Object System.Management.Automation.RuntimeDefinedParameter('Database', [string], $attributeCollection)
        $RuntimeParamDic = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary
        $RuntimeParamDic.Add('Database', $RuntimeParam)

        return $RuntimeParamDic
    }

    process {
        $Database = $PSBoundParameters['Database']

        if ($Database -in $global:Oracle) {
            Enter-SQLServer -Server $Server -Database $Database -Script "Database-Backup.ps1" -ScriptLocation "Samba01" -Time $Time -Date $Date
        }
        else {
            $schedTime = Get-SchedTimeOrPrompt -Time $Time -Date $Date -TaskDescription "Backup fuer '$Database'"
            if ($schedTime -eq 'CANCEL') { return }
            Set-ScheduledTaskTime -Server $Server -Database $Database -TaskName "Backup" -ScheduledTime $schedTime
        }
    }
}
function Set-Snapshot {
    [CmdletBinding()]
    Param(
        [parameter()][string]$Server = $global:Server,
        [parameter()][string]$Time,
        [parameter()][string]$Date
    )

    DynamicParam {
        $parameterAttribute = New-Object System.Management.Automation.ParameterAttribute
        $parameterAttribute.Mandatory = $true
        $parameterAttribute.ValueFromPipeline = $true
        $parameterAttribute.Position = 0
        $parameterAttribute.HelpMessage = "Bitte Datenbank angeben:"
        $parameterAttribute.ParameterSetName = 'Database'

        $attributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
        $attributeCollection.Add($parameterAttribute)

        $result = Get-Databases
        $attributeCollection.Add((New-Object System.Management.Automation.ValidateSetAttribute($result)))

        $RuntimeParam = New-Object System.Management.Automation.RuntimeDefinedParameter('Database', [string], $attributeCollection)
        $RuntimeParamDic = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary
        $RuntimeParamDic.Add('Database', $RuntimeParam)

        return $RuntimeParamDic
    }

    process {
        $Database = $PSBoundParameters['Database']

        if ($Database -in $global:Oracle) {
            Enter-SQLServer -Server $Server -Database $Database -Script "Database-Snapshot.ps1" -ScriptLocation "Samba01" -Time $Time -Date $Date
        }
        else {
            $schedTime = Get-SchedTimeOrPrompt -Time $Time -Date $Date -TaskDescription "Snapshot fuer '$Database'"
            if ($schedTime -eq 'CANCEL') { return }
            Set-ScheduledTaskTime -Server $Server -Database $Database -TaskName "Snapshot" -ScheduledTime $schedTime
        }
    }
}
function Restore-Snapshot {
    [CmdletBinding()]
    Param(
        [parameter()][string]$Server = $global:Server
    )

    DynamicParam {
        $parameterAttribute = New-Object System.Management.Automation.ParameterAttribute
        $parameterAttribute.Mandatory = $true
        $parameterAttribute.ValueFromPipeline = $true
        $parameterAttribute.Position = 0
        $parameterAttribute.HelpMessage = "Bitte Datenbank angeben:"
        $parameterAttribute.ParameterSetName = 'Database'

        $attributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
        $attributeCollection.Add($parameterAttribute)

        $result = Get-Databases
        $attributeCollection.Add((New-Object System.Management.Automation.ValidateSetAttribute($result)))

        $RuntimeParam = New-Object System.Management.Automation.RuntimeDefinedParameter('Database', [string], $attributeCollection)
        $RuntimeParamDic = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary
        $RuntimeParamDic.Add('Database', $RuntimeParam)

        return $RuntimeParamDic
    }

    process {
        $Database = $PSBoundParameters['Database']

        if ($Database -in $global:Oracle) {
            Restore-Snapshot-Oracle -Database $Database
        }
        else {
            Restore-Snapshot-MSSQL -Server $Server -Database $Database
        }
    }
}
function Remove-Snapshot {
    [CmdletBinding()]
    Param(
        [parameter()][string]$Server = $global:Server
    )

    DynamicParam {
        $parameterAttribute = New-Object System.Management.Automation.ParameterAttribute
        $parameterAttribute.Mandatory = $true
        $parameterAttribute.ValueFromPipeline = $true
        $parameterAttribute.Position = 0
        $parameterAttribute.HelpMessage = "Bitte Datenbank angeben:"
        $parameterAttribute.ParameterSetName = 'Database'

        $attributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
        $attributeCollection.Add($parameterAttribute)

        $result = Get-Databases
        $attributeCollection.Add((New-Object System.Management.Automation.ValidateSetAttribute($result)))

        $RuntimeParam = New-Object System.Management.Automation.RuntimeDefinedParameter('Database', [string], $attributeCollection)
        $RuntimeParamDic = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary
        $RuntimeParamDic.Add('Database', $RuntimeParam)

        return $RuntimeParamDic
    }

    process {
        $Database = $PSBoundParameters['Database']

        if ($Database -in $global:Oracle) {
            Remove-Snapshot-Oracle -Database $Database
        }
        else {
            Remove-Snapshot-MSSQL -Server $Server -Database $Database
        }
    }
}
function Sync-Database-ToTest {
    [CmdletBinding()]
    Param(
        [parameter()][string]$Server = $global:Server,
        [parameter()][string]$Time,
        [parameter()][string]$Date
    )

    DynamicParam {
        $parameterAttribute = New-Object System.Management.Automation.ParameterAttribute
        $parameterAttribute.Mandatory = $true
        $parameterAttribute.ValueFromPipeline = $true
        $parameterAttribute.Position = 0
        $parameterAttribute.HelpMessage = "Bitte Datenbank angeben:"
        $parameterAttribute.ParameterSetName = 'Database'

        $attributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
        $attributeCollection.Add($parameterAttribute)

        $result = Get-Databases
        $attributeCollection.Add((New-Object System.Management.Automation.ValidateSetAttribute($result)))

        $RuntimeParam = New-Object System.Management.Automation.RuntimeDefinedParameter('Database', [string], $attributeCollection)
        $RuntimeParamDic = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary
        $RuntimeParamDic.Add('Database', $RuntimeParam)

        return $RuntimeParamDic
    }

    process {
        $Database = $PSBoundParameters['Database']

        if ($Database -in $global:Oracle) {
            Enter-SQLServer -Server $Server -Database $Database -Script "Database-Clone.ps1" -ScriptLocation "Samba01" -Time $Time -Date $Date
        }
        else {
            $schedTime = Get-SchedTimeOrPrompt -Time $Time -Date $Date -TaskDescription "Clone fuer '$Database'"
            if ($schedTime -eq 'CANCEL') { return }
            Set-ScheduledTaskTime -Server $Server -Database $Database -TaskName "Clone" -ScheduledTime $schedTime
        }
    }
}
function Copy-Database-ToSamba {
    [CmdletBinding()]
    Param(
        [parameter()][string]$Server = $global:Server
    )
    DynamicParam {
        $parameterAttribute = New-Object System.Management.Automation.ParameterAttribute
        $parameterAttribute.Mandatory = $true
        $parameterAttribute.ValueFromPipeline = $true
        $parameterAttribute.Position = 0
        $parameterAttribute.HelpMessage = "Bitte Datenbank angeben:"
        $parameterAttribute.ParameterSetName = 'Database'

        $attributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
        $attributeCollection.Add($parameterAttribute)

        $result = Get-Databases
        $attributeCollection.Add((New-Object System.Management.Automation.ValidateSetAttribute($result)))

        $RuntimeParam = New-Object System.Management.Automation.RuntimeDefinedParameter('Database', [string], $attributeCollection)
        $RuntimeParamDic = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary
        $RuntimeParamDic.Add('Database', $RuntimeParam)

        return $RuntimeParamDic
    }

    process {
        $Database = $PSBoundParameters['Database']

        if ( $Database -in $global:Oracle) {        
            Copy-Database-ToSamba-Oracle -Database $Database
        }
        else {        
            Copy-Database-ToSamba-MSSQL -Server $Server -Database $Database
        }    
    }  
}
function Set-Archive-Log {
    [CmdletBinding()]
    Param(
        [parameter()][string]$Server = $global:Server,
        [switch]$Off
    )

    DynamicParam {
        $parameterAttribute = New-Object System.Management.Automation.ParameterAttribute
        $parameterAttribute.Mandatory = $true
        $parameterAttribute.ValueFromPipeline = $true
        $parameterAttribute.Position = 0
        $parameterAttribute.HelpMessage = "Bitte Datenbank angeben:"
        $parameterAttribute.ParameterSetName = 'Database'

        $attributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
        $attributeCollection.Add($parameterAttribute)

        $result = Get-Databases
        $attributeCollection.Add((New-Object System.Management.Automation.ValidateSetAttribute($result)))

        $RuntimeParam = New-Object System.Management.Automation.RuntimeDefinedParameter('Database', [string], $attributeCollection)
        $RuntimeParamDic = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary
        $RuntimeParamDic.Add('Database', $RuntimeParam)

        return $RuntimeParamDic
    }

    process {
        
        $Database = $PSBoundParameters['Database']
        
        if ($Off) {
            Enter-SQLServer -Server $Server -Database $Database -Script Archive-Log -ScriptLocation Powershell -Off
        }
        else {        
            Enter-SQLServer -Server $Server -Database $Database -Script Archive-Log -ScriptLocation Powershell
        }       
    }
}
function Get-ClusterHealthStatus {
    [CmdletBinding()]
    Param(
        [parameter()][string]$Server = $global:Server
    )
    process {
        Enter-SQLServer -Server $Server -Script "ClusterHealthCheck.ps1" -ScriptLocation "Local"
    }
}
#<----------------------------------AUFGABENPLANER FUNKTIONEN--------------------------------->#

<#
.SYNOPSIS
Loest -Time/-Date Parameter zu einem [datetime] auf (oder $null = sofort).

.DESCRIPTION
Nicht-interaktiver Ersatz fuer Read-ScheduledDateTime, gedacht fuer
programmatische Aufrufe (z.B. aus DTM). Regeln:
 - Beide leer            -> $null  (= sofort ausfuehren)
 - Nur -Time '19:00'     -> heute 19:00 (wenn schon vorbei: trotzdem heute,
                            der Aufrufer entscheidet ueber Vergangenheit)
 - -Date '20.05.2026'    -> dieser Tag; ohne -Time -> 00:00
 - -Time + -Date         -> exakt dieser Zeitpunkt

Gibt zurueck:
 - [datetime]  bei gueltiger Eingabe
 - $null       wenn beide Parameter leer (= sofort)
 - 'CANCEL'    bei ungueltigem Format

.PARAMETER Time
Uhrzeit als 'HH:mm' (z.B. '19:00'). Optional.

.PARAMETER Date
Datum als 'dd.MM.yyyy' (z.B. '20.05.2026'). Optional.

.EXAMPLE
Resolve-ScheduledDateTime -Time '19:00' -Date '20.05.2026'
Resolve-ScheduledDateTime -Time '22:30'          # heute 22:30
Resolve-ScheduledDateTime                          # $null -> sofort
#>
function Resolve-ScheduledDateTime {
    [CmdletBinding()]
    param(
        [string]$Time,
        [string]$Date
    )

    $culture = [System.Globalization.CultureInfo]::GetCultureInfo("de-DE")
    $styles  = [System.Globalization.DateTimeStyles]::None

    $hasTime = -not [string]::IsNullOrWhiteSpace($Time)
    $hasDate = -not [string]::IsNullOrWhiteSpace($Date)

    # Beide leer -> sofort
    if (-not $hasTime -and -not $hasDate) {
        return $null
    }

    # Datum bestimmen: angegeben oder heute
    $datePart = $null
    if ($hasDate) {
        $parsedDate = [datetime]::MinValue
        if (-not [datetime]::TryParseExact($Date.Trim(), 'dd.MM.yyyy', $culture, $styles, [ref]$parsedDate)) {
            Write-Error "Ungueltiges Datumsformat '$Date'. Erwartet: dd.MM.yyyy"
            return 'CANCEL'
        }
        $datePart = $parsedDate.Date
    }
    else {
        $datePart = (Get-Date).Date
    }

    # Uhrzeit bestimmen: angegeben oder 00:00
    $timeSpan = [TimeSpan]::Zero
    if ($hasTime) {
        $parsedTime = [datetime]::MinValue
        # [string[]]-Cast wichtig: ohne ihn waehlt PowerShell die falsche
        # TryParseExact-Overload (object[] statt string[]) und das Parsen schlaegt fehl.
        $timeFormats = [string[]]@('HH:mm', 'H:mm')
        if (-not [datetime]::TryParseExact($Time.Trim(), $timeFormats, $culture, $styles, [ref]$parsedTime)) {
            Write-Error "Ungueltiges Zeitformat '$Time'. Erwartet: HH:mm"
            return 'CANCEL'
        }
        $timeSpan = $parsedTime.TimeOfDay
    }

    return $datePart.Add($timeSpan)
}

<#
.SYNOPSIS
Entscheidet zwischen Parameter-basierter und interaktiver Zeitermittlung.

.DESCRIPTION
Wenn -Time ODER -Date gesetzt sind, wird Resolve-ScheduledDateTime genutzt
(nicht-interaktiv, fuer DTM/Automatisierung). Sonst faellt die Funktion auf
Read-ScheduledDateTime zurueck (interaktives Read-Host, wie bisher fuer
manuelle PowerShell-Nutzung). Damit bleiben bestehende Aufrufe ohne -Time/-Date
identisch im Verhalten.

Rueckgabe: [datetime] | $null (sofort) | 'CANCEL'.
#>
function Get-SchedTimeOrPrompt {
    [CmdletBinding()]
    param(
        [string]$Time,
        [string]$Date,
        [string]$TaskDescription = "Aufgabe"
    )

    $hasParams = (-not [string]::IsNullOrWhiteSpace($Time)) -or (-not [string]::IsNullOrWhiteSpace($Date))

    if ($hasParams) {
        return Resolve-ScheduledDateTime -Time $Time -Date $Date
    }

    # Kein Parameter -> interaktiv (unveraendertes Verhalten).
    return Read-ScheduledDateTime -TaskDescription $TaskDescription
}

<#
.SYNOPSIS
Fragt den Benutzer nach Datum und Uhrzeit im Format 'dd.MM.yyyy HH:mm'.

.DESCRIPTION
Gibt $null zurueck wenn leer (= sofort ausfuehren),
gibt 'CANCEL' zurueck bei Abbruch oder ungueltigem Format,
gibt ein [datetime]-Objekt zurueck bei gueltigem Datum.

.PARAMETER TaskDescription
Beschreibung der Aufgabe fuer die Rueckmeldung.

.EXAMPLE
$schedTime = Read-ScheduledDateTime -TaskDescription "Snapshot fuer 'MANSYS'"
#>
function Read-ScheduledDateTime {
    [CmdletBinding()]
    param(
        [string]$TaskDescription = "Aufgabe"
    )

    $format  = "dd.MM.yyyy HH:mm"
    $culture = [System.Globalization.CultureInfo]::GetCultureInfo("de-DE")
    $styles  = [System.Globalization.DateTimeStyles]::None

    $dateInput = Read-Host "Bitte Datum und Uhrzeit im Format '$format' eingeben (leer = sofort ausfuehren)"

    if ([string]::IsNullOrWhiteSpace($dateInput)) {
        Write-Host "$TaskDescription wird sofort ausgefuehrt." -ForegroundColor Cyan
        return $null
    }

    $datetime = [datetime]::MinValue
    $success  = [datetime]::TryParseExact($dateInput, $format, $culture, $styles, [ref]$datetime)

    if (-not $success) {
        Write-Error "Ungueltiges Format. Erwartet: $format"
        return 'CANCEL'
    }

    if ($datetime -lt (Get-Date)) {
        Write-Warning "Der angegebene Zeitpunkt liegt in der Vergangenheit."
        if (-not (Confirm-Action "Trotzdem fortfahren? (ja/j):")) {
            return 'CANCEL'
        }
    }

    Write-Host "$TaskDescription geplant fuer: $($datetime.ToString($format))" -ForegroundColor Cyan
    return $datetime
}

<#
.SYNOPSIS
Aktualisiert den Trigger eines Scheduled Tasks auf dem SQL-Server oder startet ihn sofort.

.DESCRIPTION
Verbindet sich per PSSession zum SQL-Server und aktualisiert den Trigger
des angegebenen Scheduled Tasks unter \MSSQL\<Database>\.
Bei $ScheduledTime = $null wird der Task sofort per Start-ScheduledTask gestartet.

.PARAMETER Server
SQL-Server-Instanzname (Standard: $global:Server).

.PARAMETER Database
Name der Datenbank (= Unterordner im Aufgabenplaner).

.PARAMETER TaskName
Name der Aufgabe im Aufgabenplaner (Snapshot, Backup, Clone).

.PARAMETER ScheduledTime
Geplanter Zeitpunkt als [datetime] oder $null fuer sofortige Ausfuehrung.

.EXAMPLE
# Task sofort starten
Set-ScheduledTaskTime -Server FOC-SQL01 -Database MANSYS -TaskName Snapshot

.EXAMPLE
# Task auf bestimmten Zeitpunkt planen
Set-ScheduledTaskTime -Server FOC-SQL01 -Database MANSYS -TaskName Backup -ScheduledTime (Get-Date "28.04.2026 22:00")
#>
function Set-ScheduledTaskTime {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Server,
        [Parameter(Mandatory)][string]$Database,
        [Parameter(Mandatory)][ValidateSet("Snapshot", "Backup", "Clone")][string]$TaskName,
        [Parameter()]$ScheduledTime = $null
    )

    # --- Credential laden (gleicher Mechanismus wie Enter-FOCSQL) ---
    $cred_file = "$env:USERPROFILE\credential.xml"

    if (Test-Path $cred_file) {
        Write-Verbose "Credential-Datei gefunden: $cred_file"
        $credential = Import-Clixml -Path $cred_file
    }
    else {
        Write-Host "Keine gespeicherte Credential-Datei gefunden. Bitte Anmeldedaten eingeben:"
        $credential = Get-Credential
        $credential | Export-Clixml -Path $cred_file
        Write-Host "Credentials gespeichert in: $cred_file"
    }

    Write-Host "Baue Verbindung zum Server $Server auf" -ForegroundColor DarkYellow
    $session = New-PSSession -ComputerName $Server -Credential $credential

    try {
        $result = Invoke-Command -Session $session -ArgumentList $Database, $TaskName, $ScheduledTime, $credential -ScriptBlock {
            param([string]$db, [string]$task, $schedTime, [PSCredential]$cred)

            $taskPath = "\MSSQL\$db\"

            # --- Task pruefen ---
            $existingTask = Get-ScheduledTask -TaskPath $taskPath -TaskName $task -ErrorAction SilentlyContinue

            if ($null -eq $existingTask) {
                return [PSCustomObject]@{
                    Success = $false
                    Message = "Task '$task' fuer Datenbank '$db' nicht gefunden unter Pfad '$taskPath'."
                    Action  = 'NotFound'
                }
            }

            # Pruefen ob Task bereits laeuft
            if ($existingTask.State -eq 'Running') {
                return [PSCustomObject]@{
                    Success = $false
                    Message = "Task '$task' fuer '$db' laeuft bereits. Bitte warten bis die aktuelle Ausfuehrung beendet ist."
                    Action  = 'AlreadyRunning'
                }
            }

            try {
                $userName = $cred.UserName
                $password = $cred.GetNetworkCredential().Password

                # Sofort = Trigger auf jetzt, Geplant = Trigger auf Wunschzeit
                $triggerTime = if ($null -eq $schedTime) { Get-Date } else { $schedTime }
                $trigger = New-ScheduledTaskTrigger -Once -At $triggerTime

                # -User + -Password setzt LogonType auf Password (Kennwort gespeichert, Netzwerkzugriff moeglich)
                Set-ScheduledTask -TaskPath $taskPath -TaskName $task -Trigger $trigger -User $userName -Password $password -ErrorAction Stop | Out-Null

                if ($null -eq $schedTime) {
                    # Sofort: Task zusaetzlich manuell starten (Trigger-Zeit koennte bereits vergangen sein)
                    Start-ScheduledTask -TaskPath $taskPath -TaskName $task -ErrorAction Stop
                    return [PSCustomObject]@{
                        Success = $true
                        Message = "Task '$task' fuer Datenbank '$db' wurde sofort gestartet (laeuft im Hintergrund auf dem Server)."
                        Action  = 'StartedNow'
                    }
                }
                else {
                    return [PSCustomObject]@{
                        Success       = $true
                        Message       = "Task '$task' fuer Datenbank '$db' geplant am $($schedTime.ToString('dd.MM.yyyy HH:mm'))."
                        Action        = 'Scheduled'
                        ScheduledTime = $schedTime.ToString('dd.MM.yyyy HH:mm')
                    }
                }
            }
            catch {
                return [PSCustomObject]@{
                    Success = $false
                    Message = "Fehler bei '$task': $($_.Exception.Message)"
                    Action  = 'Error'
                }
            }
        }

        # --- Rueckmeldung (lokal) ---
        Write-Host ""
        if ($result.Success) {
            Write-Host $result.Message -ForegroundColor Green
        }
        else {
            Write-Error $result.Message
        }

        return $result
    }
    finally {
        Remove-PSSession $session
    }
}

#<----------------------------------HILFS FUNKTIONEN--------------------------------->#

function Enter-SQLServer {
    [CmdletBinding()]
    Param(
        [parameter()][string]$Server,
        [parameter()][string]$Database,
        [ValidateSet("Database-Backup.ps1", "Database-Snapshot.ps1", "Database-Clone.ps1", "Database-Restore-Snapshot.ps1", "Database-Delete-Snapshot.ps1", "ClusterHealthCheck.ps1", "Archive-Log")]
        [parameter()][string]$Script,
        [ValidateSet("Local", "Samba01", "Powershell")]
        [parameter()][string]$ScriptLocation = "Samba01",
        [switch]$Off,
        [parameter()][string]$Time,
        [parameter()][string]$Date
    )

    if ( $Database -in $global:Oracle) {
        if ($Off) { Enter-OracleSQL -Database $Database -Script $Script -Off -Time $Time -Date $Date }
        else { Enter-OracleSQL -Database $Database -Script $Script -Time $Time -Date $Date }
    }
    else {
        if ($Off) { Enter-FOCSQL -Server $Server -Database $Database -Script $Script -ScriptLocation $ScriptLocation -Off }
        else { Enter-FOCSQL -Server $Server -Database $Database -Script $Script -ScriptLocation $ScriptLocation }
    }
}
function Enter-FOCSQL {
    [CmdletBinding()]
    Param(
        [parameter()][string]$Server,
        [parameter()][string]$Database,
        [ValidateSet("Database-Backup.ps1", "Database-Snapshot.ps1", "Database-Clone.ps1", "Database-Restore-Snapshot.ps1", "Database-Delete-Snapshot.ps1", "ClusterHealthCheck.ps1", "Archive-Log")]
        [parameter()][string]$Script,
        [ValidateSet("Local", "Samba01", "Powershell")]
        [parameter()][string]$ScriptLocation = "Samba01",
        [switch]$Off
    )

   
    process {
        $cred_file = "$env:USERPROFILE\credential.xml"

        if (Test-Path $cred_file) {
            Write-Host "Credential-Datei gefunden: $cred_file"
            $credential = Import-Clixml -Path $cred_file
        }
        else {
            Write-Host "Keine gespeicherte Credential-Datei gefunden. Bitte Anmeldedaten eingeben:"
            $credential = Get-Credential
            $credential | Export-Clixml -Path $cred_file
            Write-Host "Credentials gespeichert in: $cred_file"
        }

        Write-Host "Baue Verbindung zum Server $Server auf" -ForegroundColor DarkYellow
        $session = New-PSSession -ComputerName $Server -Credential $credential

        try {        

            Invoke-Command -Session $session -ArgumentList $global:uncPath, $Database, $Script, $credential, $ScriptLocation, $Off -ScriptBlock {
                param($uncPath, $Database, $Script, [PSCredential] $cred, $ScriptLocation = "Samba01", $Off)


                $cases = @{
                    "Samba01"    = {            
                        try {
                            # PSDrive einbinden
                            Write-Host "Richte temporaeres Laufwerk Z: ein fuer $uncPath"
                            New-PSDrive -Name "z" -PSProvider FileSystem -Root $uncPath -Credential $cred -ErrorAction Stop | Out-Null
                   
                            Write-Host "Wechsle nach z:\$Database"
                            Set-Location -Path "z:\$Database"

                            Write-Host "Starte Script $Script"
                            & ".\$Script"
  
                            Write-Host "Entferne temporaeres Laufwerk z:"
                            Remove-PSDrive -Name "z:" -Force -ErrorAction SilentlyContinue
                        }
                        catch {
                            Write-Error "Fehler beim Einbinden oder Ausfuehren: $_"
                        } 
                    }
                    "Local"      = {
                        try {
                            $scriptFile = "C:\MSSQL_TEMP\$Script"
                            Write-Host "Wechsle nach C:\MSSQL_TEMP"
                            Set-Location -Path "C:\MSSQL_TEMP"

                            if (-not (Test-Path $scriptFile)) {
                                Write-Warning "Datei '$scriptFile' nicht gefunden. Verzeichnisinhalt:"
                                Get-ChildItem -Path "C:\MSSQL_TEMP" | ForEach-Object { Write-Host "  $($_.Name)" }
                                throw "Script '$scriptFile' existiert nicht."
                            }

                            Write-Host "Starte Script $Script"
                            $scriptContent = Get-Content -Path $scriptFile -Raw -Encoding UTF8
                            Invoke-Expression $scriptContent
                        }
                        catch {
                            Write-Error "Fehler beim Einbinden oder Ausfuehren: $_"
                        } 
                    }
                    "Powershell" = {
                        switch ($Script) {
                            "Archive-Log" {

                                
                                if ($Off) {
                                    Database-Set-Recovery-Mode -Database $Database -Recovery SIMPLE -Verbose
                                }
                                else {
                                    Database-Set-Recovery-Mode -Database $Database -Recovery FULL -Verbose
                                }
                            }
                            Default {
                                Write-Error "$Script wird im Powershell-Modus nicht unterstuetzt"
                                return
                            }
                        }
                    }
                }#Ende @
                if ($cases.ContainsKey($ScriptLocation)) {
                    & $cases[$ScriptLocation]
                }
                else {
                    Write-Error "$ScriptLocation ist unbekannt."
                }

                Get-Date
            }
        }
        finally {
            Remove-PSSession $session
        }
    }
}
function Enter-OracleSQL {
    [CmdletBinding()]
    Param( 
        [parameter()][string]$Database,
        [ValidateSet("Database-Backup.ps1", "Database-Snapshot.ps1", "Database-Clone.ps1", "Database-Restore-Snapshot.ps1", "Database-Delete-Snapshot.ps1", "ClusterHealthCheck.ps1", "Archive-Log")]
        [parameter()][string]$Script,
        [switch]$Off,
        [parameter()][string]$commandExpand = "",
        [parameter()][string]$Time,
        [parameter()][string]$Date
    )

    $server = "oracle@$Database"
    $remoteCommand = ""
    $schedulerCommand = ""

    # Zeit aufloesen: -Time/-Date (nicht-interaktiv) ODER interaktiv (Read-Host),
    # konsistent mit dem MSSQL-Pfad. $resolved ist [datetime] | $null (sofort) | 'CANCEL'.
    # Fuer alle Scripts mit Zeitplanung (Backup, Snapshot, Clone).
    $resolved = $null
    if ($Script -in @("Database-Backup.ps1", "Database-Snapshot.ps1", "Database-Clone.ps1")) {
        $resolved = Get-SchedTimeOrPrompt -Time $Time -Date $Date -TaskDescription "$($Script.Replace('.ps1','')) fuer '$Database'"
        if ($resolved -eq 'CANCEL') { return }
    }

    switch ($Script) {
        "Database-Snapshot.ps1" { 
            $remoteCommand = "/mnt/dbmgmt/scripts/generic_restorepoint.sh -N Update -G NO" 
            if ($null -eq $resolved) {
                # Sofort ausfuehren (kein at-Scheduling)
                $schedulerCommand = $remoteCommand
            }
            else {
                $atTime           = $resolved.ToString('HH:mm dd.MM.yyyy')
                $schedulerCommand = "echo '$remoteCommand' | at $atTime"
            }
        }
        "Database-Backup.ps1" {
            $remoteCommand = "/mnt/dbmgmt/scripts/run-generic-export.sh"
            if ($null -eq $resolved) {
                # Sofort ausfuehren (kein at-Scheduling)
                $schedulerCommand = $remoteCommand
            }
            else {
                $atTime           = $resolved.ToString('HH:mm dd.MM.yyyy')
                $schedulerCommand = "echo '$remoteCommand' | at $atTime"
            }
        }
        "Archive-Log" {
            if ($Off) { $schedulerCommand = "/mnt/dbmgmt/scripts/archivelog-off.sh" }
            else { $schedulerCommand = "/mnt/dbmgmt/scripts/archivelog-on.sh" }
        }
        "Database-Clone.ps1" {
            if ($null -eq $resolved) {
                # Sofort: Clone-Script direkt ausfuehren (ohne at)
                $schedulerCommand  = 'clone=$(ls -t /home/oracle/scripts/clone* | head -n1) && '
                $schedulerCommand += 'if [[ -z $clone ]]; then echo "kein Clone Script gefunden"; else echo $clone; fi'
            }
            else {
                $atTime            = $resolved.ToString('HH:mm dd.MM.yyyy')
                $schedulerCommand  = 'clone=$(ls -t /home/oracle/scripts/clone* | head -n1) && '
                $schedulerCommand += 'if [[ -z $clone ]]; then echo "kein Clone Script gefunden"; else echo $clone | at ' + $atTime + '; fi'
            }                        
        }    
    
        Default { 
            Write-Host "$($Script.Replace('.ps1', '')) wird fuer Oracle noch nicht unterstuetzt." -ForegroundColor Red 
            return 
        }
    }
    # --> Remote-Befehl zusammenbauen
    if ( "" -ne $commandExpand) {
        $remoteCmd = $schedulerCommand + ' && ' + $commandExpand + ' ; exit'
    }
    else {
        $remoteCmd = $schedulerCommand + ' ; exit'
    }

    Write-Debug $remoteCmd
    Write-Host "Baue Verbindung zu $server auf"
    #Write-Debug $remoteCmd

    # Argumente als Array - dadurch musst du nichts escapen
    $sshArgs = @(
        '-tt', # zwei TTY-Layer fuer 'sudo' usw.
        $server, # oracle@<DB-Name>
        '--', # trennt lokale von entfernten Argumenten     
        $remoteCmd  # dein Scheduler-Befehl
    )
    # --> Ausfuehrung im selben Fenster
    & ssh @sshArgs

    Get-Date
}

function Get-Databases {
    [CmdletBinding()]
    param ( )

    $query = "SELECT name FROM sys.databases WHERE name NOT IN ('tempdb', 'TPL_TEMP') AND source_database_id IS NULL AND state_desc = 'ONLINE' ORDER BY name; "

    try {
        $result = Invoke-Sqlcmd -ServerInstance $global:Server -Database "master" -Query $query -ReturnDataTable
        $sqlDatabases = $result | ForEach-Object { $_.name }

        # Kombinieren und Duplikate entfernen

        return ($sqlDatabases + $global:Oracle) | Sort-Object -Unique
    }
    catch {
        Write-Warning "Fehler beim Abrufen der Datenbanken: $_"
        return $global:Oracle  # Fallback nur auf statisches Array
    }
}
function Confirm-Action {
    param (
        [string]$Message = "Moechten Sie fortfahren? (ja/j):"
    )

    $answer = Read-Host $Message
    if ($answer -notin @("ja", "j")) {
        
        Write-Host "Aktion abgebrochen." -ForegroundColor Yellow
        return $false
    }
    Write-Host "Aktion bestaetigt." -ForegroundColor Green
    return $true
}
# HINWEIS: Diese Funktion überdeckt das PowerShell-Builtin Invoke-Sqlcmd.
# MSSQL.psm1 nutzt das Builtin mit -InputFile-Parameter, der hier fehlt.
# Beide Module nicht gleichzeitig in derselben Session importieren.
function Invoke-Sqlcmd {
    [CmdletBinding()]
    param (
        [Parameter()][string]$ServerInstance = $global:Server,
        [Parameter()][string]$Database = "master",
        [Parameter(Mandatory = $true)][ValidateNotNullOrEmpty()][string]$Query,
        [Parameter()][int]$Timeout = 1800,
        [Parameter()][switch]$ReturnDataTable  # optional: nur erste Tabelle zurückgeben
    )

    Write-Verbose "Connecting to SQL Server: $ServerInstance, Database: $Database"
    Write-Verbose "Executing query:`n$Query"

    $Conn = $null
    try {
        $Conn = New-Object System.Data.SqlClient.SqlConnection
        $Conn.ConnectionString = "Server=$ServerInstance; Integrated Security=True; Initial Catalog=$Database; Connection Timeout=$Timeout"
        $Conn.Open()

        $Cmd = $Conn.CreateCommand()
        $Cmd.CommandText = $Query
        $Cmd.CommandTimeout = $Timeout

        $Adapter = New-Object System.Data.SqlClient.SqlDataAdapter $Cmd
        $DataSet = New-Object System.Data.DataSet
        [void]$Adapter.Fill($DataSet)

        if ($ReturnDataTable -and $DataSet.Tables.Count -gt 0) {
            return $DataSet.Tables[0]
        }
        return $DataSet.Tables
    }
    catch {
        Write-Error "Fehler beim Ausfuehren von SQL: $_"
        return $null
    }
    finally {
        if ($Conn) {
            $Conn.Close()
            $Conn.Dispose()
        }
    }
}
function Copy-Database-ToSamba-MSSQL {
    [CmdletBinding()]
    Param(
        [parameter()][string]$Server = $global:Server,
        [parameter()][string]$Database
    )

    process {
        $cred_file = "$env:USERPROFILE\credential.xml"

        if (Test-Path $cred_file) {
            Write-Debug "Credential-Datei gefunden: $cred_file"
            $credential = Import-Clixml -Path $cred_file
        }
        else {
            Write-Host "Keine gespeicherte Credential-Datei gefunden. Bitte Anmeldedaten eingeben:"
            $credential = Get-Credential
            $credential | Export-Clixml -Path $cred_file
            Write-Host "Credentials gespeichert in: $cred_file"
        }
    
        Enter-SQLServer -Server $Server -Database $Database -Script "Database-Backup.ps1" -ScriptLocation "Samba01"
    
        $uncPath = "\\samba01\Temp"

        $session = New-PSSession -ComputerName $Server -Credential $credential
        try {
            Invoke-Command -Session $session -ArgumentList $uncPath, $Database, $credential -ScriptBlock {
                param($uncPath, $Database, [PSCredential] $cred)

                Write-Host "Richte temporaeres Laufwerk Z: ein fuer $uncPath"
                New-PSDrive -Name "z" -PSProvider FileSystem -Root $uncPath -Credential $cred -ErrorAction Stop | Out-Null

                If (-not (Test-Path -Path "z:\Datenbanken")) {
                    Write-Host "Erstelle \samba01\Temp\Datenbanken"
                    New-Item -Name "Datenbanken" -ItemType Directory -Path "z:"
                }

                If (-not (Test-Path -Path "z:\Datenbanken\$Database")) {
                    Write-Host "Erstelle \\samba01\Temp\Datenbanken\$Database"
                    New-Item -Name "$Database" -ItemType Directory -Path "z:\Datenbanken"
                }

                $file = (Get-ChildItem "D:\MSSQL13.MSSQLSERVER\MSSQL\Backup\$Database\01 T�glich" -File |
                    Sort-Object LastWriteTime -Descending |
                    Select-Object -First 1)

                Write-Host "Kopiere $file.FullName nach \\samba01\Temp\Datenbanken\$Database\$($file.Name)"
                Copy-Item -Path $file.FullName -Destination "z:\Datenbanken\$Database\$($file.Name)"

                Write-Host "$uncPath\Datenbanken\$Database\$($file.Name)"
                Write-Host "Entferne temporaeres Laufwerk z:"
                Remove-PSDrive -Name "z:" -Force -ErrorAction SilentlyContinue

                Get-Date
            }        
        }
        finally {
            Remove-PSSession $session
        }
    }
}
function Copy-Database-ToSamba-Oracle {
    [CmdletBinding()]
    Param( 
        [parameter()][string]$Database
    )

    process {
        $cred_file = "$env:USERPROFILE\credential.xml"

        if (Test-Path $cred_file) {
            Write-Debug "Credential-Datei gefunden: $cred_file"
            $credential = Import-Clixml -Path $cred_file
        }
        else {
            Write-Host "Keine gespeicherte Credential-Datei gefunden. Bitte Anmeldedaten eingeben:"
            $credential = Get-Credential
            $credential | Export-Clixml -Path $cred_file
            Write-Host "Credentials gespeichert in: $cred_file"
        }
        $dbase = "$($Database.ToUpper().Substring(2,$Database.Length-2))-$(Get-Date -Format "yyyyMMdd")"
        # SICHERHEITSHINWEIS: Das Passwort wird im Klartext an den SSH-Befehl übergeben.
        # Empfehlung: SSH-Key-Authentifizierung einrichten (ssh-copy-id oracle@<DB>).

        $userName = if ($credential.UserName -match '\\') { $credential.UserName.Split('\')[1] } else { $credential.UserName }

        # Credential-Inhalt als Base64 kodieren — umgeht alle Shell-Quoting-Probleme
        # bei Sonderzeichen im Passwort (", &, ', \, etc.) über den SSH-Argument-Pfad
        $credContent = "username=$userName`npassword=$($credential.GetNetworkCredential().Password)`ndomain=lhp.intern"
        $credB64     = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($credContent))

        # [Fix #6] Bedingte Installation statt immer dnf aufrufen
        $commandExpand  = 'rpm -q samba-client &>/dev/null || sudo dnf install -y samba-client && '

        $commandExpand += "echo 'erzeuge Credential' && "

        # Base64-Blob enthält nur [A-Za-z0-9+/=] — kein Quoting nötig
        $commandExpand += "( umask 0077 && echo '$credB64' | base64 -d > ~/.smbcred-ad ) && "

        $commandExpand += 'latest=$(ls -t /mnt/back02/exports/*.DMP | head -n1) && '

        # [Fix #5] Abbruch falls keine DMP-Datei gefunden
        $commandExpand += '[ -z "$latest" ] && echo "FEHLER: Keine DMP-Datei gefunden" && exit 1 || true && '

        # Logfile unabhaengig vom DMP-Namen direkt suchen
        $commandExpand += 'latest_log=$(ls -t /mnt/back02/exports/*.LOG 2>/dev/null | head -n1) && '

        $commandExpand += 'echo "kopiere $latest und $latest_log" && '

        # [Fix #3] Sicheres Tempfile statt festem /tmp/commands.smb
        $commandExpand += 'tmpfile=$(mktemp /tmp/commands.XXXXXX.smb) && '

        # put mit explizitem Remote-Namen via ${var##*/} (bash-basename ohne Subshell)
        # Logfile nur wenn vorhanden ([ -f ] — kein Abbruch falls nicht existent)
        # { ... } mit ; getrennt: jede Zeile läuft unabhängig, Exit-Code = echo quit = 0
        $commandExpand += "( umask 0077 && { printf 'mkdir Datenbanken\nmkdir Datenbanken\%s\ncd Datenbanken\%s\nput %s %s\n' '$($Database)' '$($Database)' `"`$latest`" `"`${latest##*/}`"; [ -n `"`$latest_log`" ] && printf 'put %s %s\n' `"`$latest_log`" `"`${latest_log##*/}`"; echo quit; } > `$tmpfile ) && "

        $commandExpand += "smbclient //samba01/temp -A ~/.smbcred-ad < `$tmpfile && "

        $commandExpand += "echo 'entferne Credential' && "

        # [Fix #3] tmpfile-Variable statt festem Pfad
        $commandExpand += "rm -f `$tmpfile && "

        # [Fix #2] ~/.smbcred-ad statt .smbcred-ad (relativer Pfad war falsch)
        $commandExpand += "rm -f ~/.smbcred-ad"
        
        Enter-OracleSQL -Database $Database -Script "Database-Backup.ps1"  -commandExpand $commandExpand
    }
}

#<----------------------------------STATISTIK-FUNKTIONEN--------------------------------->#

<#
.SYNOPSIS
Gibt statistische Kennzahlen einer Datenbank zurueck (MSSQL oder Oracle).

.DESCRIPTION
Liefert ein PSCustomObject mit Speicherbelegung, aktiven Verbindungen/Sessions,
Recovery-/Archivelog-Modus und weiteren Kennzahlen.
Bei MSSQL-Datenbanken wird eine T-SQL-Abfrage gegen sys-Katalog-Views ausgefuehrt.
Bei Oracle-Datenbanken wird per SSH ein SQL*Plus-Skript auf dem DB-Host gestartet.

.PARAMETER Server
SQL-Server-Instanzname (nur MSSQL, Standard: $global:Server).

.PARAMETER Database
Name der Datenbank (DynamicParam mit Tab-Vervollstaendigung).

.EXAMPLE
Get-DatabaseStats -Database MANSYS

.EXAMPLE
Get-DatabaseStats -Database DBTEST01
#>
function Get-DatabaseStats {
    [CmdletBinding()]
    Param(
        [parameter()][string]$Server = $global:Server
    )

    DynamicParam {
        $parameterAttribute = New-Object System.Management.Automation.ParameterAttribute
        $parameterAttribute.Mandatory = $true
        $parameterAttribute.ValueFromPipeline = $true
        $parameterAttribute.Position = 0
        $parameterAttribute.HelpMessage = "Bitte Datenbank angeben:"
        $parameterAttribute.ParameterSetName = 'Database'

        $attributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
        $attributeCollection.Add($parameterAttribute)

        $result = Get-Databases
        $attributeCollection.Add((New-Object System.Management.Automation.ValidateSetAttribute($result)))

        $RuntimeParam = New-Object System.Management.Automation.RuntimeDefinedParameter('Database', [string], $attributeCollection)
        $RuntimeParamDic = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary
        $RuntimeParamDic.Add('Database', $RuntimeParam)

        return $RuntimeParamDic
    }

    process {
        $Database = $PSBoundParameters['Database']

        if ($Database -in $global:Oracle) {
            Get-DatabaseStats-Oracle -Database $Database
        }
        else {
            Get-DatabaseStats-MSSQL -Server $Server -Database $Database
        }
    }
}

function Get-DatabaseStats-MSSQL {
    [CmdletBinding()]
    param(
        [string]$Server,
        [string]$Database
    )

    # --- Credential laden (gleicher Mechanismus wie Enter-FOCSQL) ---
    $cred_file = "$env:USERPROFILE\credential.xml"

    if (Test-Path $cred_file) {
        Write-Verbose "Credential-Datei gefunden: $cred_file"
        $credential = Import-Clixml -Path $cred_file
    }
    else {
        Write-Host "Keine gespeicherte Credential-Datei gefunden. Bitte Anmeldedaten eingeben:"
        $credential = Get-Credential
        $credential | Export-Clixml -Path $cred_file
        Write-Host "Credentials gespeichert in: $cred_file"
    }

    Write-Verbose "Baue Verbindung zum Server $Server auf"
    $session = New-PSSession -ComputerName $Server -Credential $credential

    try {
        $result = Invoke-Command -Session $session -ArgumentList $Server, $Database -ScriptBlock {
            param([string]$ServerInstance, [string]$db)

            # --- Hauptabfrage (sys.databases / sys.master_files / msdb) ---
            $query = @"
DECLARE @db sysname = N'$db';

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = @db)
BEGIN
    RAISERROR('Datenbank %s nicht gefunden.', 16, 1, @db);
    RETURN;
END;

SELECT
    d.name                       AS DatabaseName,
    d.database_id                AS DatabaseId,
    d.state_desc                 AS State,
    d.recovery_model_desc        AS RecoveryModel,
    d.compatibility_level        AS CompatibilityLevel,
    d.collation_name             AS Collation,
    d.user_access_desc           AS UserAccess,
    d.is_read_only               AS IsReadOnly,
    d.page_verify_option_desc    AS PageVerify,
    d.create_date                AS CreateDate,

    (SELECT ROUND(SUM(CASE WHEN mf.type = 0 THEN mf.size ELSE 0 END) * 8.0 / 1024, 2)
     FROM sys.master_files mf WHERE mf.database_id = d.database_id)   AS DataSizeMB,

    (SELECT ROUND(SUM(CASE WHEN mf.type = 1 THEN mf.size ELSE 0 END) * 8.0 / 1024, 2)
     FROM sys.master_files mf WHERE mf.database_id = d.database_id)   AS LogSizeMB,

    (SELECT ROUND(SUM(mf.size) * 8.0 / 1024, 2)
     FROM sys.master_files mf WHERE mf.database_id = d.database_id)   AS TotalSizeMB,

    (SELECT MAX(bs.backup_finish_date) FROM msdb.dbo.backupset bs
     WHERE bs.database_name = @db AND bs.type = 'D')                   AS LastFullBackup,

    (SELECT MAX(bs.backup_finish_date) FROM msdb.dbo.backupset bs
     WHERE bs.database_name = @db AND bs.type = 'I')                   AS LastDiffBackup,

    (SELECT MAX(bs.backup_finish_date) FROM msdb.dbo.backupset bs
     WHERE bs.database_name = @db AND bs.type = 'L')                   AS LastLogBackup

FROM sys.databases d
WHERE d.name = @db;
"@

            # --- Datenbankdateien ---
            $fileQuery = @"
SELECT
    mf.name             AS FileLogicalName,
    mf.type_desc        AS FileType,
    ROUND(mf.size * 8.0 / 1024, 2) AS FileSizeMB,
    CASE mf.max_size WHEN -1 THEN -1 ELSE ROUND(mf.max_size * 8.0 / 1024, 2) END AS FileMaxSizeMB,
    mf.growth,
    mf.is_percent_growth,
    mf.physical_name
FROM sys.master_files mf
WHERE mf.database_id = DB_ID(N'$db')
ORDER BY mf.type_desc, mf.name;
"@

            # --- Session-Details (VIEW SERVER STATE) ---
            $sessionQuery = @"
SELECT
    s.session_id        AS SessionId,
    s.host_name         AS HostName,
    s.login_name        AS LoginName,
    s.program_name      AS ProgramName,
    s.status            AS Status,
    s.login_time        AS LoginTime,
    DB_NAME(s.database_id) AS CurrentDatabase
FROM sys.dm_exec_sessions s
WHERE s.database_id = DB_ID(N'$db')
  AND s.is_user_process = 1
ORDER BY s.host_name, s.login_name;
"@

            # --- Buffer-Pool-Schaetzung (VIEW SERVER STATE) ---
            $bufferQuery = @"
SELECT ISNULL(
    CAST(
        (SELECT TOP(1) cntr_value FROM sys.dm_os_performance_counters
         WHERE counter_name = 'Database pages' AND object_name LIKE '%Buffer Manager%')
        * 1.0
        * (SELECT SUM(size) FROM sys.master_files WHERE database_id = DB_ID(N'$db') AND type = 0)
        / NULLIF((SELECT SUM(size) FROM sys.master_files WHERE type = 0), 0)
        * 8.0 / 1024
    AS decimal(18,2))
, 0) AS BufferPoolMB;
"@

            try {
                $row = Invoke-Sqlcmd -ServerInstance $ServerInstance -Database "master" -Query $query -ErrorAction Stop

                if (-not $row) {
                    Write-Warning "Keine Daten fuer Datenbank '$db' erhalten."
                    return $null
                }

                $r = if ($row -is [System.Array]) { $row[0] } else { $row }

                $filesRaw = Invoke-Sqlcmd -ServerInstance $ServerInstance -Database "master" -Query $fileQuery -ErrorAction SilentlyContinue
                $files = @()
                if ($filesRaw) {
                    foreach ($f in $filesRaw) {
                        $files += [PSCustomObject]@{
                            FileLogicalName  = $f.FileLogicalName
                            FileType         = $f.FileType
                            FileSizeMB       = [math]::Round($f.FileSizeMB, 2)
                            FileMaxSizeMB    = if ($f.FileMaxSizeMB -eq -1) { -1 } else { [math]::Round($f.FileMaxSizeMB, 2) }
                            Growth           = $f.growth
                            IsPercentGrowth  = [bool]$f.is_percent_growth
                            PhysicalName     = $f.physical_name
                        }
                    }
                }

                # --- Optionale DMV-Abfragen ---
                $sessions  = @()
                $bufferMB  = 0

                try {
                    $sessRaw = Invoke-Sqlcmd -ServerInstance $ServerInstance -Database "master" -Query $sessionQuery -ErrorAction Stop
                    if ($sessRaw) {
                        foreach ($s in $sessRaw) {
                            $sessions += [PSCustomObject]@{
                                SessionId       = $s.SessionId
                                HostName        = $s.HostName
                                LoginName       = $s.LoginName
                                ProgramName     = $s.ProgramName
                                Status          = $s.Status
                                LoginTime       = $s.LoginTime
                                CurrentDatabase = $s.CurrentDatabase
                            }
                        }
                    }
                }
                catch {
                    Write-Warning "Session-Details nicht verfuegbar: $($_.Exception.Message)"
                }

                try {
                    $bufferResult = Invoke-Sqlcmd -ServerInstance $ServerInstance -Database "master" -Query $bufferQuery -ErrorAction Stop
                    if ($bufferResult) {
                        $br = if ($bufferResult -is [System.Array]) { $bufferResult[0] } else { $bufferResult }
                        $bufferMB = $br.BufferPoolMB
                    }
                }
                catch {
                    Write-Warning "Buffer-Pool-Schaetzung nicht verfuegbar: $($_.Exception.Message)"
                }

                [PSCustomObject]@{
                    DatabaseName       = $r.DatabaseName
                    DatabaseType       = 'MSSQL'
                    DatabaseId         = $r.DatabaseId
                    State              = $r.State
                    RecoveryModel      = $r.RecoveryModel
                    CompatibilityLevel = $r.CompatibilityLevel
                    Collation          = $r.Collation
                    UserAccess         = $r.UserAccess
                    IsReadOnly         = [bool]$r.IsReadOnly
                    PageVerify         = $r.PageVerify
                    CreateDate         = $r.CreateDate
                    DataSizeMB         = [math]::Round($r.DataSizeMB, 2)
                    LogSizeMB          = [math]::Round($r.LogSizeMB, 2)
                    TotalSizeMB        = [math]::Round($r.TotalSizeMB, 2)
                    BufferPoolMB       = [math]::Round($bufferMB, 2)
                    ActiveConnections  = $sessions.Count
                    Sessions           = $sessions
                    LastFullBackup     = $r.LastFullBackup
                    LastDiffBackup     = $r.LastDiffBackup
                    LastLogBackup      = $r.LastLogBackup
                    Files              = $files
                }
            }
            catch {
                Write-Warning "Fehler beim Abrufen der Statistiken fuer '$db': $($_.Exception.Message)"
                return $null
            }
        }

        return $result
    }
    finally {
        Remove-PSSession $session
    }
}

function Get-DatabaseStats-Oracle {
    [CmdletBinding()]
    param(
        [string]$Database
    )

    # --- SQL*Plus-Skript mit Trennzeichen-basierter Ausgabe ---
    $sqlScript = @'
SET PAGESIZE 0 FEEDBACK OFF HEADING OFF LINESIZE 500 TRIMSPOOL ON TAB OFF

ALTER SESSION SET NLS_NUMERIC_CHARACTERS = '.,';

SELECT 'DBSIZE|' || ROUND(SUM(bytes)/1024/1024, 2)
FROM dba_data_files;

SELECT 'SESSIONS|' || COUNT(*)
FROM v$session
WHERE type = 'USER';

SELECT 'SESS|' || NVL(username, 'N/A') || '|' || NVL(machine, 'N/A') || '|' || NVL(program, 'N/A') || '|' || status
FROM v$session
WHERE type = 'USER'
ORDER BY machine, username;

SELECT 'SGA|' || ROUND(SUM(value)/1024/1024, 2)
FROM v$sga;

SELECT 'PGA|' || ROUND(value/1024/1024, 2)
FROM v$pgastat
WHERE name = 'total PGA allocated';

SELECT 'ARCHIVELOG|' || log_mode
FROM v$database;

SELECT 'INSTANCE|' || status || '|' || instance_name || '|' || host_name || '|' || version
FROM v$instance;

SELECT 'TS|' ||
       df.tablespace_name || '|' ||
       ROUND(df.total_mb, 2) || '|' ||
       ROUND(df.total_mb - NVL(fs.free_mb, 0), 2) || '|' ||
       ROUND(NVL(fs.free_mb, 0), 2) || '|' ||
       ROUND((df.total_mb - NVL(fs.free_mb, 0)) * 100 / NULLIF(df.total_mb, 0), 1)
FROM   (SELECT tablespace_name, SUM(bytes)/1024/1024 AS total_mb
        FROM   dba_data_files
        GROUP BY tablespace_name) df
LEFT JOIN
       (SELECT tablespace_name, SUM(bytes)/1024/1024 AS free_mb
        FROM   dba_free_space
        GROUP BY tablespace_name) fs
ON     df.tablespace_name = fs.tablespace_name
ORDER BY df.tablespace_name;

EXIT;
'@

    $server = "oracle@$Database"

    try {
        # Base64-Kodierung umgeht Shell-Quoting-Probleme (bewaehrtes Muster)
        $sqlB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($sqlScript))
        $remoteCmd = "echo '$sqlB64' | base64 -d | sqlplus -s / as sysdba"

        Write-Verbose "Verbinde mit $server fuer Statistikabfrage"
        $output = & ssh $server "--" $remoteCmd 2>&1

        if ($LASTEXITCODE -ne 0) {
            Write-Warning "SSH-Verbindung zu $server fehlgeschlagen (Exit-Code: $LASTEXITCODE)"
            return
        }

        # --- Ausgabe zeilenweise parsen ---
        $dbSizeMB       = 0
        $activeSessions = 0
        $sgaSizeMB      = 0
        $pgaAllocatedMB = 0
        $archiveLogMode = 'UNBEKANNT'
        $instanceStatus = 'UNBEKANNT'
        $instanceName   = ''
        $hostName       = ''
        $oracleVersion  = ''
        $tablespaces    = @()
        $sessions       = @()

        foreach ($line in $output) {
            $l = "$line".Trim()
            if ([string]::IsNullOrWhiteSpace($l)) { continue }

            $parts = $l -split '\|'
            $prefix = $parts[0].Trim()

            switch ($prefix) {
                'DBSIZE' {
                    $dbSizeMB = [double]($parts[1].Trim().Replace(',', '.'))
                }
                'SESSIONS' {
                    $activeSessions = [int]($parts[1].Trim())
                }
                'SGA' {
                    $sgaSizeMB = [double]($parts[1].Trim().Replace(',', '.'))
                }
                'PGA' {
                    $pgaAllocatedMB = [double]($parts[1].Trim().Replace(',', '.'))
                }
                'ARCHIVELOG' {
                    $archiveLogMode = $parts[1].Trim()
                }
                'INSTANCE' {
                    $instanceStatus = $parts[1].Trim()
                    if ($parts.Count -ge 3) { $instanceName = $parts[2].Trim() }
                    if ($parts.Count -ge 4) { $hostName      = $parts[3].Trim() }
                    if ($parts.Count -ge 5) { $oracleVersion = $parts[4].Trim() }
                }
                'TS' {
                    if ($parts.Count -ge 6) {
                        $tablespaces += [PSCustomObject]@{
                            TablespaceName = $parts[1].Trim()
                            TotalMB        = [math]::Round([double]($parts[2].Trim().Replace(',', '.')), 2)
                            UsedMB         = [math]::Round([double]($parts[3].Trim().Replace(',', '.')), 2)
                            FreeMB         = [math]::Round([double]($parts[4].Trim().Replace(',', '.')), 2)
                            UsedPercent    = [math]::Round([double]($parts[5].Trim().Replace(',', '.')), 1)
                        }
                    }
                }
                'SESS' {
                    if ($parts.Count -ge 5) {
                        $sessions += [PSCustomObject]@{
                            Username    = $parts[1].Trim()
                            Machine     = $parts[2].Trim()
                            Program     = $parts[3].Trim()
                            Status      = $parts[4].Trim()
                        }
                    }
                }
            }
        }

        [PSCustomObject]@{
            DatabaseName    = $Database
            DatabaseType    = 'Oracle'
            InstanceName    = $instanceName
            HostName        = $hostName
            OracleVersion   = $oracleVersion
            InstanceStatus  = $instanceStatus
            ArchiveLogMode  = $archiveLogMode
            DatabaseSizeMB  = [math]::Round($dbSizeMB, 2)
            SGASizeMB       = [math]::Round($sgaSizeMB, 2)
            PGAAllocatedMB  = [math]::Round($pgaAllocatedMB, 2)
            ActiveSessions  = $activeSessions
            Sessions        = $sessions
            Tablespaces     = $tablespaces
        }
    }
    catch {
        Write-Warning "Fehler beim Abrufen der Oracle-Statistiken fuer '$Database': $($_.Exception.Message)"
    }
}



#<----------------------------------MSSQL SNAPSHOT FUNKTIONEN--------------------------------->#

<#
.SYNOPSIS
Stellt eine MSSQL-Datenbank aus einem ausgewaehlten Snapshot wieder her.

.DESCRIPTION
1. Fragt alle vorhandenen Snapshots der Datenbank per PSSession ab
2. Zeigt tabellarische Auswahl an
3. Nach Bestaetigung: SINGLE_USER -> RESTORE FROM SNAPSHOT -> MULTI_USER
4. Fehlerbehandlung mit Rollback auf MULTI_USER

.PARAMETER Database
Name der Quelldatenbank.

.PARAMETER Server
SQL-Server-Instanz (Standard: $global:Server).

.EXAMPLE
Restore-Snapshot-MSSQL -Server FOC-SQL01 -Database MANSYS
#>
function Restore-Snapshot-MSSQL {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Database,
        [string]$Server = $global:Server
    )

    # --- Credential laden ---
    $cred_file = "$env:USERPROFILE\credential.xml"

    if (Test-Path $cred_file) {
        Write-Verbose "Credential-Datei gefunden: $cred_file"
        $credential = Import-Clixml -Path $cred_file
    }
    else {
        Write-Host "Keine gespeicherte Credential-Datei gefunden. Bitte Anmeldedaten eingeben:"
        $credential = Get-Credential
        $credential | Export-Clixml -Path $cred_file
        Write-Host "Credentials gespeichert in: $cred_file"
    }

    Write-Host "Baue Verbindung zum Server $Server auf" -ForegroundColor DarkYellow
    $session = New-PSSession -ComputerName $Server -Credential $credential

    try {
        # --- Schritt 1: Snapshots abfragen (remote) ---
        Write-Host "Frage Snapshots fuer $Database ab..." -ForegroundColor DarkYellow

        $snapshots = @(Invoke-Command -Session $session -ArgumentList $Server, $Database -ScriptBlock {
            param([string]$ServerInstance, [string]$db)

            $query = @"
SELECT snap.name        AS Name,
       snap.create_date AS CreateDate
FROM   sys.databases snap
JOIN   sys.databases db ON snap.source_database_id = db.database_id
WHERE  db.name = N'$db'
ORDER BY snap.create_date DESC;
"@
            try {
                $rows = Invoke-Sqlcmd -ServerInstance $ServerInstance -Database "master" -Query $query -ErrorAction Stop
                if (-not $rows) { return @() }

                $result = @()
                foreach ($r in $rows) {
                    $result += [PSCustomObject]@{
                        Name       = $r.Name
                        CreateDate = $r.CreateDate.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                }
                return $result
            }
            catch {
                Write-Warning "Fehler beim Abfragen der Snapshots: $($_.Exception.Message)"
                return @()
            }
        })

        if ($snapshots.Count -eq 0) {
            Write-Warning "Keine Snapshots fuer die Datenbank $Database gefunden."
            return
        }

        # --- Schritt 2: Snapshots anzeigen (lokal) ---
        Write-Host ""
        Write-Host "Verfuegbare Snapshots fuer '$Database':" -ForegroundColor Cyan
        Write-Host ("-" * 75)
        Write-Host ("{0,4}  {1,-45} {2}" -f "Nr.", "Snapshot-Name", "Erstellt am")
        Write-Host ("-" * 75)

        for ($i = 0; $i -lt $snapshots.Count; $i++) {
            $snap = $snapshots[$i]
            Write-Host ("{0,4}  {1,-45} {2}" -f "[$($i + 1)]", $snap.Name, $snap.CreateDate)
        }
        Write-Host ""

        # --- Schritt 3: Benutzer waehlt (lokal) ---
        $selection = Read-Host "Bitte Nummer des Snapshots eingeben (1-$($snapshots.Count))"

        $idx = -1
        if (-not [int]::TryParse($selection, [ref]$idx)) {
            Write-Error "Ungueltige Eingabe. Bitte eine Zahl eingeben."
            return
        }
        $idx = $idx - 1

        if ($idx -lt 0 -or $idx -ge $snapshots.Count) {
            Write-Error "Ungueltige Auswahl. Bitte eine Nummer zwischen 1 und $($snapshots.Count) eingeben."
            return
        }

        $selectedSnap = $snapshots[$idx].Name

        # --- Schritt 4: Bestaetigung (lokal) ---
        if (-not (Confirm-Action "ACHTUNG! Die Datenbank '$Database' wird auf den Snapshot '$selectedSnap' zurueckgesetzt. Alle Aenderungen nach dem Snapshot gehen verloren. Sicher fortfahren? (ja/j):")) {
            return
        }

        # --- Schritt 5: Restore ausfuehren (remote) ---
        Write-Host ""
        Write-Host "Stelle Datenbank '$Database' aus Snapshot '$selectedSnap' wieder her..." -ForegroundColor DarkYellow

        $restoreResult = Invoke-Command -Session $session -ArgumentList $Server, $Database, $selectedSnap -ScriptBlock {
            param([string]$ServerInstance, [string]$db, [string]$snapName)

            $errors = @()

            try {
                # Aktive Verbindungen schliessen
                Write-Host "  Schliesse aktive Verbindungen..." -ForegroundColor DarkYellow
                $killQuery = @"
DECLARE @kill varchar(max) = '';
SELECT @kill = @kill + 'KILL ' + CONVERT(varchar(5), session_id) + ';'
FROM sys.dm_exec_sessions
WHERE database_id = DB_ID(N'$db')
  AND is_user_process = 1;
EXEC(@kill);
"@
                Invoke-Sqlcmd -ServerInstance $ServerInstance -Database "master" -Query $killQuery -ErrorAction Stop

                # SINGLE_USER
                Write-Host "  Setze SINGLE_USER Modus..." -ForegroundColor DarkYellow
                Invoke-Sqlcmd -ServerInstance $ServerInstance -Database "master" -Query "ALTER DATABASE [$db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;" -ErrorAction Stop

                # Restore
                Write-Host "  Restore aus Snapshot '$snapName'..." -ForegroundColor DarkYellow
                Invoke-Sqlcmd -ServerInstance $ServerInstance -Database "master" -Query "RESTORE DATABASE [$db] FROM DATABASE_SNAPSHOT = '$snapName';" -ErrorAction Stop

                # MULTI_USER
                Invoke-Sqlcmd -ServerInstance $ServerInstance -Database "master" -Query "ALTER DATABASE [$db] SET MULTI_USER WITH ROLLBACK IMMEDIATE;" -ErrorAction Stop
            }
            catch {
                $errors += $_.Exception.Message

                # Sicherheitshalber MULTI_USER zuruecksetzen
                try {
                    Invoke-Sqlcmd -ServerInstance $ServerInstance -Database "master" -Query "ALTER DATABASE [$db] SET MULTI_USER WITH ROLLBACK IMMEDIATE;" -ErrorAction SilentlyContinue
                }
                catch { }
            }

            return [PSCustomObject]@{
                Success = ($errors.Count -eq 0)
                Errors  = $errors
            }
        }

        # --- Schritt 6: Fehlerauswertung (lokal) ---
        if ($restoreResult.Success) {
            Write-Host ""
            Write-Host "Datenbank '$Database' erfolgreich aus Snapshot '$selectedSnap' wiederhergestellt." -ForegroundColor Green
        }
        else {
            Write-Host ""
            Write-Error "Restore fehlgeschlagen!"
            foreach ($err in $restoreResult.Errors) {
                Write-Host "  $err" -ForegroundColor Red
            }
            Write-Warning "MULTI_USER Modus wurde automatisch zurueckgesetzt."
        }
    }
    finally {
        Remove-PSSession $session
    }

    Get-Date
}

<#
.SYNOPSIS
Loescht ausgewaehlte Snapshots einer MSSQL-Datenbank.

.DESCRIPTION
1. Fragt alle vorhandenen Snapshots der Datenbank per PSSession ab
2. Zeigt tabellarische Auswahl an (einzeln oder alle)
3. Fuehrt nach Bestaetigung DROP DATABASE fuer die gewaehlten Snapshots aus

.PARAMETER Database
Name der Quelldatenbank.

.PARAMETER Server
SQL-Server-Instanz (Standard: $global:Server).

.EXAMPLE
Remove-Snapshot-MSSQL -Server FOC-SQL01 -Database MANSYS
#>
function Remove-Snapshot-MSSQL {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Database,
        [string]$Server = $global:Server
    )

    # --- Credential laden ---
    $cred_file = "$env:USERPROFILE\credential.xml"

    if (Test-Path $cred_file) {
        Write-Verbose "Credential-Datei gefunden: $cred_file"
        $credential = Import-Clixml -Path $cred_file
    }
    else {
        Write-Host "Keine gespeicherte Credential-Datei gefunden. Bitte Anmeldedaten eingeben:"
        $credential = Get-Credential
        $credential | Export-Clixml -Path $cred_file
        Write-Host "Credentials gespeichert in: $cred_file"
    }

    Write-Host "Baue Verbindung zum Server $Server auf" -ForegroundColor DarkYellow
    $session = New-PSSession -ComputerName $Server -Credential $credential

    try {
        # --- Schritt 1: Snapshots abfragen (remote) ---
        Write-Host "Frage Snapshots fuer $Database ab..." -ForegroundColor DarkYellow

        $snapshots = @(Invoke-Command -Session $session -ArgumentList $Server, $Database -ScriptBlock {
            param([string]$ServerInstance, [string]$db)

            $query = @"
SELECT snap.name        AS Name,
       snap.create_date AS CreateDate
FROM   sys.databases snap
JOIN   sys.databases db ON snap.source_database_id = db.database_id
WHERE  db.name = N'$db'
ORDER BY snap.create_date DESC;
"@
            try {
                $rows = Invoke-Sqlcmd -ServerInstance $ServerInstance -Database "master" -Query $query -ErrorAction Stop
                if (-not $rows) { return @() }

                $result = @()
                foreach ($r in $rows) {
                    $result += [PSCustomObject]@{
                        Name       = $r.Name
                        CreateDate = $r.CreateDate.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                }
                return $result
            }
            catch {
                Write-Warning "Fehler beim Abfragen der Snapshots: $($_.Exception.Message)"
                return @()
            }
        })

        if ($snapshots.Count -eq 0) {
            Write-Warning "Keine Snapshots fuer die Datenbank $Database gefunden."
            return
        }

        # --- Schritt 2: Snapshots anzeigen (lokal) ---
        Write-Host ""
        Write-Host "Verfuegbare Snapshots fuer '$Database':" -ForegroundColor Cyan
        Write-Host ("-" * 75)
        Write-Host ("{0,4}  {1,-45} {2}" -f "Nr.", "Snapshot-Name", "Erstellt am")
        Write-Host ("-" * 75)

        for ($i = 0; $i -lt $snapshots.Count; $i++) {
            $snap = $snapshots[$i]
            Write-Host ("{0,4}  {1,-45} {2}" -f "[$($i + 1)]", $snap.Name, $snap.CreateDate)
        }

        Write-Host ""
        Write-Host "  [A]  Alle Snapshots loeschen" -ForegroundColor Yellow
        Write-Host ""

        # --- Schritt 3: Benutzer waehlt (lokal) ---
        $selection = Read-Host "Bitte Nummer eingeben (1-$($snapshots.Count)) oder 'A' fuer alle"

        $snapsToDelete = @()

        if ($selection -ieq 'A') {
            $snapsToDelete = $snapshots
            $confirmMsg = "ACHTUNG! Alle $($snapsToDelete.Count) Snapshots der Datenbank '$Database' werden geloescht. Sicher fortfahren? (ja/j):"
        }
        else {
            $idx = -1
            if (-not [int]::TryParse($selection, [ref]$idx)) {
                Write-Error "Ungueltige Eingabe. Bitte eine Zahl oder 'A' eingeben."
                return
            }
            $idx = $idx - 1

            if ($idx -lt 0 -or $idx -ge $snapshots.Count) {
                Write-Error "Ungueltige Auswahl. Bitte eine Nummer zwischen 1 und $($snapshots.Count) eingeben."
                return
            }

            $snapsToDelete = @($snapshots[$idx])
            $confirmMsg = "ACHTUNG! Der Snapshot '$($snapsToDelete[0].Name)' wird geloescht. Sicher fortfahren? (ja/j):"
        }

        # --- Schritt 4: Bestaetigung (lokal) ---
        if (-not (Confirm-Action $confirmMsg)) {
            return
        }

        # --- Schritt 5: DROP DATABASE ausfuehren (remote) ---
        $snapNames = $snapsToDelete | ForEach-Object { $_.Name }

        Write-Host ""
        Write-Host "Loesche $($snapNames.Count) Snapshot(s)..." -ForegroundColor DarkYellow

        $deleteResult = Invoke-Command -Session $session -ArgumentList $Server, (,$snapNames) -ScriptBlock {
            param([string]$ServerInstance, [string[]]$names)

            $deleted = @()
            $errors  = @()

            foreach ($snapName in $names) {
                try {
                    Write-Host "  Loesche Snapshot '$snapName'..." -ForegroundColor DarkYellow
                    Invoke-Sqlcmd -ServerInstance $ServerInstance -Database "master" -Query "DROP DATABASE [$snapName];" -ErrorAction Stop
                    $deleted += $snapName
                    Write-Host "  Snapshot '$snapName' geloescht." -ForegroundColor Green
                }
                catch {
                    $errors += "Fehler beim Loeschen von '$snapName': $($_.Exception.Message)"
                }
            }

            return [PSCustomObject]@{
                Deleted = $deleted
                Errors  = $errors
            }
        }

        # --- Schritt 6: Zusammenfassung (lokal) ---
        Write-Host ""
        if ($deleteResult.Errors.Count -gt 0) {
            Write-Error "Beim Loeschen sind $($deleteResult.Errors.Count) Fehler aufgetreten:"
            foreach ($err in $deleteResult.Errors) {
                Write-Host "  $err" -ForegroundColor Red
            }
        }

        if ($deleteResult.Deleted.Count -gt 0) {
            $deletedNames = $deleteResult.Deleted -join ', '
            Write-Host "Erfolgreich geloescht: $deletedNames" -ForegroundColor Green
        }
    }
    finally {
        Remove-PSSession $session
    }

    Get-Date
}

#<----------------------------------ORACLE RESTORE-POINT FUNKTIONEN--------------------------------->#

<#
.SYNOPSIS
Fragt PDB-Namen und Restore Points einer Oracle-CDB/PDB-Instanz per SSH ab.

.DESCRIPTION
Verbindet sich per SSH zum Oracle-Host und fuehrt ein SQL*Plus-Skript aus,
das alle PDB-Namen (ohne PDB$SEED) sowie alle Restore Points mit Zeitstempel
und Container-ID zurueckliefert.

.PARAMETER Database
Hostname der Oracle-Datenbank (z.B. DBTEST01).

.EXAMPLE
$info = Get-OracleRestoreInfo -Database DBTEST01
$info.PdbNames        # z.B. @("TEST", "PROD")
$info.RestorePoints   # Array mit Name, Time, Guaranteed, ConId
#>
function Get-OracleRestoreInfo {
    [CmdletBinding()]
    param( )
     DynamicParam {
        $parameterAttribute = New-Object System.Management.Automation.ParameterAttribute
        $parameterAttribute.Mandatory = $true
        $parameterAttribute.ValueFromPipeline = $true
        $parameterAttribute.Position = 0
        $parameterAttribute.HelpMessage = "Bitte Datenbank angeben:"
        $parameterAttribute.ParameterSetName = 'Database'

        $attributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
        $attributeCollection.Add($parameterAttribute)

        $result = $global:Oracle
        $attributeCollection.Add((New-Object System.Management.Automation.ValidateSetAttribute($result)))

        $RuntimeParam = New-Object System.Management.Automation.RuntimeDefinedParameter('Database', [string], $attributeCollection)
        $RuntimeParamDic = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary
        $RuntimeParamDic.Add('Database', $RuntimeParam)

        return $RuntimeParamDic
    }

    process {
     $Database = $PSBoundParameters['Database']

    $server = "oracle@$Database"

    # Single-Quoted Here-String: $-Zeichen bleiben literal (CDB$ROOT, PDB$SEED)
    $sqlQuery = @'
SET PAGESIZE 0 FEEDBACK OFF HEADING OFF LINESIZE 500 TRIMSPOOL ON TAB OFF
ALTER SESSION SET CONTAINER = CDB$ROOT;
SELECT 'PDB|' || pdb_name || '|' || con_id FROM dba_pdbs WHERE pdb_name <> 'PDB$SEED' AND status = 'NORMAL' ORDER BY con_id;
SELECT 'RP|' || name || '|' || TO_CHAR(time, 'YYYY-MM-DD HH24:MI:SS') || '|' || guarantee_flashback_database || '|' || con_id || '|' || scn FROM v$restore_point ORDER BY time DESC;
EXIT;
'@

    $sqlB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($sqlQuery))
    $remoteCmd = "echo '$sqlB64' | base64 -d | sqlplus -s / as sysdba"

    Write-Verbose "Frage PDB-Namen und Restore Points von $server ab"
    $output = & ssh $server "--" $remoteCmd 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Error "SSH-Verbindung zu $server fehlgeschlagen (Exit-Code: $LASTEXITCODE)"
        return $null
    }

    $pdbNames = @()
    $restorePoints = @()

    foreach ($line in $output) {
        $l = "$line".Trim()
        if ([string]::IsNullOrWhiteSpace($l)) { continue }

        $parts = $l -split '\|'
        $prefix = $parts[0].Trim()

        switch ($prefix) {
            'PDB' {
                if ($parts.Count -ge 3) {
                    $pdbNames += [PSCustomObject]@{
                        Name  = $parts[1].Trim()
                        ConId = $parts[2].Trim()
                    }
                }
            }
            'RP' {
                if ($parts.Count -ge 6) {
                    $restorePoints += [PSCustomObject]@{
                        Name       = $parts[1].Trim()
                        Time       = $parts[2].Trim()
                        Guaranteed = $parts[3].Trim()
                        ConId      = $parts[4].Trim()
                        SCN        = $parts[5].Trim()
                    }
                }
            }
        }
    }

    return [PSCustomObject]@{
        PdbNames      = $pdbNames
        RestorePoints = $restorePoints
    }
}
}

<#
.SYNOPSIS
Prueft die Ausgabe eines Remote-Befehls auf Oracle- bzw. RMAN-Fehler.

.DESCRIPTION
Durchsucht ein String-Array nach Zeilen die mit ORA-, RMAN- oder SP2-
beginnen und gibt diese als Fehler-Array zurueck. Wird von
Restore-Snapshot-Oracle und Remove-Snapshot-Oracle verwendet.

.PARAMETER Output
Array von Ausgabezeilen (stdout+stderr) des SSH-Befehls.

.OUTPUTS
String-Array mit gefundenen Fehlermeldungen. Leer = keine Fehler.
#>
function Find-OracleErrors {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][array]$Output
    )

    $errors = @()
    foreach ($line in $Output) {
        $l = "$line".Trim()
        if ($l -match '^(ORA-|RMAN-|SP2-)') {
            $errors += $l
        }
    }
    return $errors
}

<#
.SYNOPSIS
Fuehrt ein RMAN Point-in-Time Recovery einer Oracle CDB auf einen
ausgewaehlten Restore Point durch.

.DESCRIPTION
1. Fragt alle PDB-Namen und verfuegbare Restore Points per SSH/SQL*Plus ab
2. Zeigt dem Benutzer die Auswahl an
3. Bei mehreren PDBs: Warnung dass ALLE PDBs zurueckgesetzt werden
4. Fuehrt nach Bestaetigung die RMAN-Recovery-Sequenz aus:
   - SHUTDOWN IMMEDIATE (gesamte CDB)
   - STARTUP MOUNT
   - RESTORE DATABASE UNTIL RESTORE POINT <RP>
   - RECOVER DATABASE UNTIL RESTORE POINT <RP>
   - ALTER DATABASE OPEN RESETLOGS
   - Alle PDBs oeffnen + State speichern
5. Empfiehlt anschliessend ein neues RMAN-Backup

ACHTUNG: Faehrt die gesamte CDB herunter und setzt ALLE PDBs zurueck!
Nach OPEN RESETLOGS sind alte Archivelogs/Backups fuer kuenftige
Recovery-Operationen nicht mehr verwendbar.

.PARAMETER Database
Hostname der Oracle-Datenbank (z.B. DBTEST01).

.EXAMPLE
Restore-Snapshot-Oracle -Database DBTEST01
#>
function Restore-Snapshot-Oracle {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Database
    )

    $server = "oracle@$Database"

    # --- Schritt 1: PDBs und Restore Points abfragen ---
    Write-Host "Frage Restore Points von $Database ab..." -ForegroundColor DarkYellow
    $info = Get-OracleRestoreInfo -Database $Database

    if ($null -eq $info) { return }

    if ($info.PdbNames.Count -eq 0) {
        Write-Error "Konnte keine PDBs fuer $Database ermitteln."
        return
    }

    if ($info.RestorePoints.Count -eq 0) {
        Write-Warning "Keine Restore Points fuer $Database gefunden."
        return
    }

    # --- Schritt 2: PDB-Uebersicht ---
    Write-Host ""
    if ($info.PdbNames.Count -gt 1) {
        Write-Host "WARNUNG: Diese CDB enthaelt $($info.PdbNames.Count) PDBs!" -ForegroundColor Red
        Write-Host "Ein RMAN Recovery setzt die GESAMTE CDB zurueck," -ForegroundColor Red
        Write-Host "d.h. ALLE folgenden PDBs sind betroffen:" -ForegroundColor Red
    }
    else {
        Write-Host "PDB in dieser CDB:" -ForegroundColor Cyan
    }
    Write-Host ""
    foreach ($pdb in $info.PdbNames) {
        Write-Host "  - $($pdb.Name) (CON_ID: $($pdb.ConId))" -ForegroundColor Cyan
    }

    # --- Schritt 3: Restore Points anzeigen ---
    Write-Host ""
    Write-Host "Verfuegbare Restore Points:" -ForegroundColor Cyan
    Write-Host ("-" * 95)
    Write-Host ("{0,4}  {1,-35} {2,-22} {3,-5} {4,-5} {5}" -f "Nr.", "Name", "Zeitpunkt", "Guar.", "ConId", "SCN")
    Write-Host ("-" * 95)

    for ($i = 0; $i -lt $info.RestorePoints.Count; $i++) {
        $rp = $info.RestorePoints[$i]
        Write-Host ("{0,4}  {1,-35} {2,-22} {3,-5} {4,-5} {5}" -f "[$($i + 1)]", $rp.Name, $rp.Time, $rp.Guaranteed, $rp.ConId, $rp.SCN)
    }
    Write-Host ""

    # --- Schritt 4: Benutzer waehlt ---
    $selection = Read-Host "Bitte Nummer des Restore Points eingeben (1-$($info.RestorePoints.Count))"

    $idx = -1
    if (-not [int]::TryParse($selection, [ref]$idx)) {
        Write-Error "Ungueltige Eingabe. Bitte eine Zahl eingeben."
        return
    }
    $idx = $idx - 1

    if ($idx -lt 0 -or $idx -ge $info.RestorePoints.Count) {
        Write-Error "Ungueltige Auswahl. Bitte eine Nummer zwischen 1 und $($info.RestorePoints.Count) eingeben."
        return
    }

    $selectedRP   = $info.RestorePoints[$idx]
    $rpName       = $selectedRP.Name
    $rpSCN        = $selectedRP.SCN
    $rpConId      = [int]$selectedRP.ConId

    # --- Schritt 5: RMAN-Methode waehlen (ConId-basiert) ---
    # ConId 0 oder 1 = CDB-level Restore Point -> UNTIL RESTORE POINT (Name)
    # ConId > 1      = PDB-level Restore Point  -> UNTIL SCN (Fallback)
    if ($rpConId -le 1) {
        $rmanClause = "RESTORE POINT $rpName"
        $methodInfo = "UNTIL RESTORE POINT '$rpName' (CDB-level, ConId=$rpConId)"
    }
    else {
        $rmanClause = "SCN $rpSCN"
        $methodInfo = "UNTIL SCN $rpSCN (PDB-level RP '$rpName', ConId=$rpConId -> SCN-Fallback)"
    }

    Write-Host ""
    Write-Host "Recovery-Methode: $methodInfo" -ForegroundColor Cyan

    # --- Schritt 6: Bestaetigung (verschaerft bei Multi-PDB) ---
    $pdbList = ($info.PdbNames | ForEach-Object { $_.Name }) -join ', '

    if ($info.PdbNames.Count -gt 1) {
        $confirmMsg = "ACHTUNG! Die GESAMTE CDB wird zurueckgesetzt ($methodInfo). " +
                      "Alle $($info.PdbNames.Count) PDBs ($pdbList) verlieren saemtliche Aenderungen nach diesem Zeitpunkt. " +
                      "Nach OPEN RESETLOGS sind alte Backups/Archivelogs nicht mehr verwendbar. " +
                      "Sicher fortfahren? (ja/j):"
    }
    else {
        $confirmMsg = "ACHTUNG! Die CDB wird zurueckgesetzt ($methodInfo). " +
                      "PDB '$pdbList' verliert alle Aenderungen nach diesem Zeitpunkt. " +
                      "Nach OPEN RESETLOGS sind alte Backups/Archivelogs nicht mehr verwendbar. " +
                      "Sicher fortfahren? (ja/j):"
    }

    if (-not (Confirm-Action $confirmMsg)) {
        return
    }

    # --- Schritt 7: RMAN-Backup Pre-Check (VOR dem Shutdown!) ---
    Write-Host ""
    Write-Host "Pruefe ob RMAN-Backups vorhanden sind..." -ForegroundColor DarkYellow

    $checkScript = "LIST BACKUP OF DATABASE SUMMARY;`nEXIT;"
    $checkB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($checkScript))
    $checkCmd = "echo '$checkB64' | base64 -d | rman target /"

    $checkOutput = & ssh $server '--' $checkCmd 2>&1

    # Pruefen ob "specification does not match any backup" in der Ausgabe vorkommt
    $noBackup = $checkOutput | Where-Object { "$_" -match 'specification does not match any backup' }

    if ($noBackup) {
        Write-Host ""
        Write-Error "ABBRUCH: Keine RMAN-Backups fuer $Database gefunden!"
        Write-Warning "Ohne vorhandene RMAN-Backups ist kein Recovery moeglich."
        Write-Warning "Bitte zuerst ein RMAN-Backup erstellen:"
        Write-Host "  ssh $server" -ForegroundColor Yellow
        Write-Host "  rman target /" -ForegroundColor Yellow
        Write-Host "  BACKUP DATABASE PLUS ARCHIVELOG;" -ForegroundColor Yellow
        return
    }

    # Letztes Backup-Datum anzeigen
    $lastBackupLine = $checkOutput | Where-Object { "$_" -match '^\s*\d+\s+B\s+F' } | Select-Object -Last 1
    if ($lastBackupLine) {
        Write-Host "RMAN-Backups vorhanden. Letztes Full-Backup: $("$lastBackupLine".Trim())" -ForegroundColor Green
    }
    else {
        Write-Host "RMAN-Backups vorhanden." -ForegroundColor Green
    }

    # --- Schritt 8: RMAN Recovery ausfuehren ---
    $rmanScript = @"
SHUTDOWN IMMEDIATE;
STARTUP MOUNT;
RUN {
  RESTORE DATABASE UNTIL $rmanClause;
  RECOVER DATABASE UNTIL $rmanClause;
  ALTER DATABASE OPEN RESETLOGS;
}
SQL "ALTER PLUGGABLE DATABASE ALL OPEN";
SQL "ALTER PLUGGABLE DATABASE ALL SAVE STATE";
EXIT;
"@

    $rmanB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($rmanScript))
    $remoteCmd = "echo '$rmanB64' | base64 -d | rman target /"

    Write-Host ""
    Write-Host "Fuehre RMAN Recovery aus: $methodInfo" -ForegroundColor DarkYellow
    Write-Host "Betroffene PDBs: $pdbList" -ForegroundColor DarkYellow
    Write-Host "Die CDB wird heruntergefahren und im Mount-Modus neu gestartet..." -ForegroundColor DarkYellow
    Write-Host ""

    # -tt fuer TTY (shutdown/startup benoetigen interaktive Session)
    $rmanOutput = & ssh '-tt' $server '--' $remoteCmd 2>&1

    # Ausgabe anzeigen
    foreach ($line in $rmanOutput) {
        Write-Host $line
    }

    # --- Schritt 9: Fehlerauswertung aus Ausgabe ---
    $oraErrors = Find-OracleErrors -Output $rmanOutput

    if ($oraErrors.Count -gt 0) {
        Write-Host ""
        Write-Error "RMAN Recovery fehlgeschlagen! Folgende Fehler wurden erkannt:"
        foreach ($err in $oraErrors) {
            Write-Host "  $err" -ForegroundColor Red
        }
        Write-Host ""
        Write-Warning "Bitte den Status der Datenbank manuell pruefen:"
        Write-Host "  ssh $server" -ForegroundColor Yellow
        Write-Host "  sqlplus / as sysdba" -ForegroundColor Yellow
        Write-Host "  SELECT STATUS FROM V`$INSTANCE;" -ForegroundColor Yellow
        Write-Host "  SELECT NAME, OPEN_MODE FROM V`$DATABASE;" -ForegroundColor Yellow
        Write-Host "  -- Falls MOUNTED: ALTER DATABASE OPEN;" -ForegroundColor Yellow
    }
    else {
        Write-Host ""
        Write-Host "RMAN Recovery erfolgreich abgeschlossen." -ForegroundColor Green
        Write-Host "Alle PDBs ($pdbList) zurueckgesetzt: $methodInfo" -ForegroundColor Green
        Write-Host ""
        Write-Warning "WICHTIG: Nach OPEN RESETLOGS sind alte Backups/Archivelogs ungueltig."
        Write-Warning "Bitte zeitnah ein neues RMAN-Backup erstellen!"
        Write-Host "  Empfehlung: Backup-Database -Database $Database" -ForegroundColor Yellow
    }

    Get-Date
}

<#
.SYNOPSIS
Loescht ausgewaehlte Restore Points einer Oracle PDB.

.DESCRIPTION
1. Fragt verfuegbare Restore Points per SSH/SQL*Plus ab
2. Zeigt dem Benutzer die Auswahl an (einzeln oder alle)
3. Fuehrt nach Bestaetigung DROP RESTORE POINT aus
4. Prueft die Ausgabe auf ORA-Fehler

.PARAMETER Database
Hostname der Oracle-Datenbank (z.B. DBTEST01).

.EXAMPLE
Remove-Snapshot-Oracle -Database DBTEST01
#>
function Remove-Snapshot-Oracle {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Database
    )

    $server = "oracle@$Database"

    # --- Schritt 1: Restore Points abfragen ---
    Write-Host "Frage Restore Points von $Database ab..." -ForegroundColor DarkYellow
    $info = Get-OracleRestoreInfo -Database $Database

    if ($null -eq $info) { return }

    if ($info.PdbNames.Count -eq 0) {
        Write-Error "Konnte keine PDBs fuer $Database ermitteln."
        return
    }

    if ($info.RestorePoints.Count -eq 0) {
        Write-Warning "Keine Restore Points fuer $Database gefunden."
        return
    }

    # --- Schritt 2: Restore Points anzeigen ---
    Write-Host ""
    Write-Host "Verfuegbare Restore Points:" -ForegroundColor Cyan
    Write-Host ("-" * 95)
    Write-Host ("{0,4}  {1,-35} {2,-22} {3,-5} {4,-5} {5}" -f "Nr.", "Name", "Zeitpunkt", "Guar.", "ConId", "SCN")
    Write-Host ("-" * 95)

    for ($i = 0; $i -lt $info.RestorePoints.Count; $i++) {
        $rp = $info.RestorePoints[$i]
        Write-Host ("{0,4}  {1,-35} {2,-22} {3,-5} {4,-5} {5}" -f "[$($i + 1)]", $rp.Name, $rp.Time, $rp.Guaranteed, $rp.ConId, $rp.SCN)
    }

    Write-Host ""
    Write-Host "  [A]  Alle Restore Points loeschen" -ForegroundColor Yellow
    Write-Host ""

    # --- Schritt 3: Benutzer waehlt ---
    $selection = Read-Host "Bitte Nummer eingeben (1-$($info.RestorePoints.Count)) oder 'A' fuer alle"

    $rpToDelete = @()

    if ($selection -ieq 'A') {
        $rpToDelete = $info.RestorePoints
        $confirmMsg = "ACHTUNG! Alle $($rpToDelete.Count) Restore Points werden geloescht. Sicher fortfahren? (ja/j):"
    }
    else {
        $idx = -1
        if (-not [int]::TryParse($selection, [ref]$idx)) {
            Write-Error "Ungueltige Eingabe. Bitte eine Zahl oder 'A' eingeben."
            return
        }
        $idx = $idx - 1

        if ($idx -lt 0 -or $idx -ge $info.RestorePoints.Count) {
            Write-Error "Ungueltige Auswahl. Bitte eine Nummer zwischen 1 und $($info.RestorePoints.Count) eingeben."
            return
        }

        $rpToDelete = @($info.RestorePoints[$idx])
        $confirmMsg = "ACHTUNG! Der Restore Point '$($rpToDelete[0].Name)' wird geloescht. Sicher fortfahren? (ja/j):"
    }

    # --- Schritt 4: Bestaetigung ---
    if (-not (Confirm-Action $confirmMsg)) {
        return
    }

    # --- Schritt 5: DROP RESTORE POINT ausfuehren ---
    # Restore Points nach Container gruppieren:
    # ConId 0/1 = CDB-level -> aus CDB$ROOT loeschen
    # ConId > 1 = PDB-level -> in die jeweilige PDB wechseln
    $dropStatements = "SET PAGESIZE 0 FEEDBACK ON ECHO ON`n"

    # Nach ConId gruppieren, damit nur einmal pro Container gewechselt wird
    $grouped = $rpToDelete | Group-Object -Property ConId

    foreach ($group in $grouped) {
        $conId = [int]$group.Name

        if ($conId -le 1) {
            $dropStatements += "ALTER SESSION SET CONTAINER = CDB`$ROOT;`n"
            Write-Verbose "Loesche CDB-level Restore Points (ConId=$conId) aus CDB`$ROOT"
        }
        else {
            # PDB-Name anhand der ConId ermitteln
            $targetPdb = $info.PdbNames | Where-Object { $_.ConId -eq "$conId" } | Select-Object -First 1

            if ($null -eq $targetPdb) {
                Write-Error "Keine PDB mit ConId=$conId gefunden. Restore Points in diesem Container werden uebersprungen."
                continue
            }

            $dropStatements += "ALTER SESSION SET CONTAINER = $($targetPdb.Name);`n"
            Write-Verbose "Loesche PDB-level Restore Points (ConId=$conId) aus PDB '$($targetPdb.Name)'"
        }

        foreach ($rp in $group.Group) {
            $dropStatements += "DROP RESTORE POINT $($rp.Name);`n"
        }
    }

    $dropStatements += "EXIT;`n"

    $sqlB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($dropStatements))
    $remoteCmd = "echo '$sqlB64' | base64 -d | sqlplus / as sysdba"

    Write-Host ""
    Write-Host "Loesche $($rpToDelete.Count) Restore Point(s)..." -ForegroundColor DarkYellow

    $dropOutput = & ssh $server '--' $remoteCmd 2>&1

    # Ausgabe anzeigen
    foreach ($line in $dropOutput) {
        Write-Host $line
    }

    # --- Schritt 6: Fehlerauswertung ---
    $oraErrors = Find-OracleErrors -Output $dropOutput

    if ($oraErrors.Count -gt 0) {
        Write-Host ""
        Write-Error "Beim Loeschen sind Fehler aufgetreten:"
        foreach ($err in $oraErrors) {
            Write-Host "  $err" -ForegroundColor Red
        }
    }
    else {
        Write-Host ""
        foreach ($rp in $rpToDelete) {
            Write-Host "Restore Point '$($rp.Name)' geloescht." -ForegroundColor Green
        }
    }

    Get-Date
}

Export-ModuleMember -Function Backup-Database
Export-ModuleMember -Function Set-Snapshot
Export-ModuleMember -Function Restore-Snapshot
Export-ModuleMember -Function Remove-Snapshot
Export-ModuleMember -Function Sync-Database-ToTest
Export-ModuleMember -Function Get-ClusterHealthStatus
Export-ModuleMember -Function Copy-Database-ToSamba
Export-ModuleMember -Function Set-Archive-Log
Export-ModuleMember -Function Get-DatabaseStats
Export-ModuleMember -Function Get-OracleRestoreInfo
Export-ModuleMember -Function Set-ScheduledTaskTime
Export-ModuleMember -Function Resolve-ScheduledDateTime
Export-ModuleMember -Function Get-SchedTimeOrPrompt