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
- Das FOC-SQL-Modul in der Samba-Quelle (Pfad über die App konfigurierbar).

## Einrichtung

1. Bauen und starten:
   ```
   dotnet build DTM.csproj -c Release
   dotnet run --project DTM.csproj -c Release
   ```
2. Verbindungen über das ⚙-Symbol neben „Datenbanken" einrichten
   (siehe [Verbindungen verwalten](#verbindungen-verwalten)).
3. Im selben Dialog Samba-Quelle und optionalen Modul-Pfad eintragen und
   **Speichern** klicken.

## Verbindungen verwalten

Das ⚙-Symbol neben der „Datenbanken"-Überschrift öffnet den Dialog
**Verbindungen verwalten**.

| Feld | Bedeutung |
|------|-----------|
| Typ | Datenbanktyp (`MSSQL`, `ORACLE`, `PostgreSQL`) — DropDown |
| Server | Hostname oder IP des Datenbankservers |
| Benutzer | DB-Benutzername |
| Passwort | Wird verschlüsselt gespeichert (DPAPI unter Windows, Base64 unter Linux) |
| Datenbank | Standard-Datenbankname (MSSQL: Zieldatenbank; Oracle: SID/Service-Name) |
| ConnectionString | Optionaler ODBC-ConnectionString; wenn gesetzt, werden Server/User/Passwort ignoriert |

Aktionen: **Neu**, **Bearbeiten** (Doppelklick oder Schaltfläche), **Löschen**.
Änderungen werden sofort in `%APPDATA%\DTM\connections.json` persistiert und
beim nächsten Programmstart automatisch geladen.

Unter **FOC-SQL Modul** im gleichen Dialog:

| Feld | Bedeutung |
|------|-----------|
| Samba-Quelle | UNC-Pfad mit `FOC-SQL.psm1` (z. B. `\\server\share\Modules\FOC`) |
| Modulpfad (Override) | Absoluter lokaler Pfad; leer = Samba-Logik aktiv |

## Architektur (Kurzüberblick)

- **Views/** — Avalonia-UI.
  - `MainWindow` — DB-Baum, Info-Anzeige, Aktions-Buttons, PowerShell-Konsole.
  - `ConnectionManagerWindow` / `EditConnectionWindow` — Verbindungsverwaltung.
  - `TimePickerWindow` — Zeitplanung für Backup/Clone/Snapshot.
  - `SessionsWindow` — Anzeige aktiver DB-Sessions.
- **ViewModels/** — MVVM (CommunityToolkit.Mvvm).
  - `MainWindowViewModel` — Aktionen, Statistik-Anzeige, Baum-Aufbau.
  - `ConnectionManagerViewModel` — Verbindungsliste, FocSql-Einstellungen.
  - `EditConnectionViewModel` — Formular für eine einzelne Verbindung.
- **Data/Config/** — Persistenz ohne externe Abhängigkeit:
  - `ConnectionStore` — Lesen/Schreiben von `connections.json`; Passwörter via
    `Protect`/`Unprotect` (DPAPI/Base64).
  - `AppSettingsStore` — Lesen/Schreiben von `settings.json` (FocSql-Konfiguration).
  - `FocSqlRuntime` — Laufzeit-Zustand der FocSql-Konfiguration.
- **Data/Terminal/** — PowerShell-Integration:
  - `PowerShellTerminalSession` — in-process Runspace mit `DtmPSHost`/`DtmPSHostUI`,
    der interaktive Prompts (Read-Host, PromptForChoice) an die Konsolen-Eingabe
    delegiert.
  - `FocSqlRuntime` — baut das Modul-Lade-Snippet und die Funktionsaufrufe.
  - `TerminalBus` — Mediator zwischen ViewModel-Aktionen und der Session.
  - `AnsiParser` / `AnsiPalette` / `AnsiConsole` — farbige Ausgabe.
- **Data/HelperClasses/** — Modell-Klassen (`Database_Info`, `Database_Stats`,
  `ServerCredential`, `DB_SERVER`) sowie ODBC-Zugriff für DB-Liste und Statistik
  (MSSQL/Oracle). Backup/Clone laufen **nicht** hierüber, sondern über das Modul.

## Aktionen

| Button | Modulfunktion | Zeitplanung | Interaktiv |
|--------|---------------|-------------|------------|
| Backup           | Backup-Database       | ja  | – |
| Clone            | Sync-Database-ToTest  | ja  | – |
| DB → Samba       | Copy-Database-ToSamba | –   | – |
| Snapshot         | Set-Snapshot          | ja  | – |
| Restore          | Restore-Snapshot      | –   | ja (Auswahl + Bestätigung) |
| Remove           | Remove-Snapshot       | –   | ja |
| ArchiveLog An    | Set-Archive-Log       | –   | – |
| ArchiveLog Aus   | Set-Archive-Log -Off  | –   | – |

Zeitplanung: Im Zeit-Dialog „Sofort" oder „Geplant" (Datum/Uhrzeit) wählen.
Bei Oracle wird daraus ein `at HH:mm dd.MM.yyyy`-Job, bei MSSQL ein geplanter
Task. Interaktive Aktionen (Restore/Remove) zeigen Prompts im pwsh-Tab;
Antworten (Nummer, `ja`/`j`) in die Befehlszeile tippen.

## Datenspeicherung

| Datei | Inhalt |
|-------|--------|
| `%APPDATA%\DTM\connections.json` | Verbindungsliste (Passwörter verschlüsselt) |
| `%APPDATA%\DTM\settings.json` | FocSql-Einstellungen (SambaSource, ModulePath) |

Beide Dateien werden beim ersten Speichern automatisch angelegt.

## Modul-Deployment

Die `FOC-SQL.psm1` muss in der Samba-Quelle liegen, von der die Clients per
Profil-Logik kopieren. Wenn das PowerShell-Profil das Modul bereits vorlädt,
überspringt DTM den Import (`Get-Module`-Check) — dann muss auch die
Profil-Version diese `FOC-SQL.psm1` sein.

## Tests

```
dotnet test DTM.Tests/DTM.Tests.csproj
```

Die Test-Suite (~269 Tests, xUnit + FluentAssertions) deckt ab:

- `Data/Config/` — ConnectionStore, AppSettingsStore, ConnectionEntry
- `Data/Terminal/` — AnsiParser, AnsiPalette, FocSqlRuntime, TerminalBus, DtmPSHostUI
- `Data/HelperClasses/` — ServerCredential, DB_SERVER, Database_Info, Database_Stats-Varianten
- `Data/` — DTM_DATA (Routing via FakeFactory), AsyncUtil
- `ViewModels/` — MainWindowViewModel, ConnectionManagerViewModel, EditConnectionViewModel,
  SessionsViewModel, TimePickerViewModel, TreeNode-ViewModels

Keine Abhängigkeit auf DB-Server, Avalonia-UI-Thread oder PowerShell-Runspace.
