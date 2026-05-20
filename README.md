# DTM — Datenbank-Manager

Avalonia-Desktop-App (.NET 10) zur Verwaltung von MSSQL- und Oracle-Datenbanken
(Backup, Clone, Snapshot, Archive-Log, Samba-Copy). Alle Datenbank-Aktionen
laufen über das PowerShell-Modul **FOC-SQL.psm1**; DTM baut kein eigenes
Remoting nach, sondern ruft die Modulfunktionen in einer eingebetteten
PowerShell-Session auf.

## Voraussetzungen

- .NET 10 SDK
- Windows (für die Modul-Aktionen; die App selbst läuft auch unter Linux/macOS,
  aber die FOC-SQL-Funktionen brauchen die Windows-/Domänen-Umgebung)
- Eine `credential.xml` im Benutzerprofil:
  ```powershell
  Get-Credential | Export-Clixml "$env:USERPROFILE\credential.xml"
  ```
- Das FOC-SQL-Modul in der Samba-Quelle, von der die Profil-Logik kopiert
  (siehe `appsettings` → `FocSql.SambaSource`).

## Einrichtung

1. `appsettings.example.json` nach `appsettings.json` kopieren und die echten
   Server/Zugangsdaten eintragen. (Die `appsettings.json` ist in `.gitignore`,
   damit keine Passwörter eingecheckt werden.)
2. Bauen und starten:
   ```
   dotnet build DTM.csproj -c Release
   dotnet run --project DTM.csproj -c Release
   ```

## Architektur (Kurzüberblick)

- **Views/** — Avalonia-UI. `MainWindow` zeigt den DB-Baum, die Info-Anzeige,
  die Aktions-Buttons und die eingebettete PowerShell-Konsole (`ConsoleControl`).
- **ViewModels/** — MVVM. `MainWindowViewModel` löst die Aktionen aus
  (Zeitwahl via `TimePickerWindow`, dann Aufruf über den `TerminalBus`).
- **Data/Terminal/** — die PowerShell-Integration:
  - `PowerShellTerminalSession` — in-process Runspace mit eigenem `DtmPSHost`,
    der interaktive Prompts (Read-Host) an die Konsolen-Eingabe delegiert.
  - `FocSqlRuntime` — baut das Modul-Lade-Snippet (Profil-Logik: von Samba
    kopieren falls nötig, dann importieren) und die Funktionsaufrufe.
  - `TerminalBus` — Mediator zwischen ViewModel-Aktionen und der Session.
  - `AnsiParser` / `AnsiPalette` / `AnsiConsole` — farbige Ausgabe.
- **Data/HelperClasses/** — ODBC-Zugriff für die DB-Liste und die
  Statistik-Anzeige (MSSQL/Oracle). Backup/Clone laufen NICHT hierüber,
  sondern über das Modul.
- **FOC-SQL.psm1** — das PowerShell-Modul mit den eigentlichen DB-Funktionen.

## Aktionen

| Button | Modulfunktion | Zeitplanung | Interaktiv |
|--------|---------------|-------------|------------|
| Backup           | Backup-Database       | ja  | – |
| Clone            | Sync-Database-ToTest  | ja  | – |
| Snapshot         | Set-Snapshot          | ja  | – |
| Restore Snapshot | Restore-Snapshot      | –   | ja (Auswahl + Bestätigung) |
| Remove Snapshot  | Remove-Snapshot       | –   | ja |
| ArchiveLog An    | Set-Archive-Log       | –   | – |
| ArchiveLog Aus   | Set-Archive-Log -Off  | –   | – |
| DB → Samba       | Copy-Database-ToSamba | –   | – |

Zeitplanung: Im Zeit-Dialog „Sofort" oder „Geplant" (Datum/Uhrzeit) wählen.
Bei Oracle wird daraus ein `at HH:mm dd.MM.yyyy`-Job, bei MSSQL ein geplanter
Task. Interaktive Aktionen (Restore/Remove) zeigen ihre Auswahl-Prompts im
pwsh-Tab; Antworten (Nummer, ja/j) in die Befehlszeile tippen.

## Modul-Deployment

Die `FOC-SQL.psm1` muss in der Samba-Quelle liegen, von der die Clients per
Profil-Logik kopieren. Wenn das PowerShell-Profil das Modul bereits vorlädt,
überspringt DTM den Import (`Get-Module`-Check) — dann muss auch die
Profil-Version diese `FOC-SQL.psm1` sein.
