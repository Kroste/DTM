# CLAUDE.md

> Diese Datei wird von Claude Code / Copilot beim Session-Start als Kontext geladen.
> Sie hält den **projektübergreifenden Standard-Kanon** fest, damit die Konventionen
> unabhängig vom Chat-Memory im Repo verfügbar sind.
>
> **Master-Vorlage** – pro Projekt nur den Abschnitt *„Projekt“* ausfüllen, der Rest bleibt fix.

---

## Arbeitsweise

**Deal:** Lars liefert die Ideen, Claude setzt um.

- Bei **jedem neuen Projekt** wird diese `CLAUDE.md` automatisch im Repo-Root angelegt.
- Sprache: **Deutsch**, immer **„du“**, nie „Sie“.
- Antwortstil: direkt, technisch tief, klare Single-Path-Empfehlung mit Begründung,
  sinnvolle Code-Erklärungen (keine Basics), Folgefragen vorausschauend mitdenken.

---

## Projekt

- **Name:** `DTM`
- **Kurzbeschreibung:** Avalonia-Desktop-App zur PowerShell-gestützten Administration von MSSQL- und Oracle-Datenbanken (Backup, Clone, Snapshot, Archive-Log, Samba-Copy) über das Modul `FOC-SQL.psm1` in einer in-process PowerShell-Session.
- **Repository:** `https://github.com/Kroste/DTM`
- **Lokaler Pfad:** `~/Entwicklung/DTM` (Linux) bzw. `D:\Entwicklung\DTM` (Windows)
- **Projektspezifische Besonderheiten:** Embedded PowerShell-Runspace via `Microsoft.PowerShell.SDK`; externes Update-Skript `dtm_update.ps1`; keine KI-Integration; Logo der Landeshauptstadt Potsdam (`Assets/lhp_logo.png`).

---

## Tech-Stack (Baseline)

- **.NET 10** / **C#** (LangVersion `latest`, `ImplicitUsings`, `Nullable enable`)
- Desktop-UI: **Avalonia ≥ 12.0.4** (Mindestversion, niemals darunter)
- MVVM: **CommunityToolkit.Mvvm**
- DI/Hosting: **Microsoft.Extensions.DependencyInjection** + Hosting
- Logging: **NLog**
- GitHub-Account: **Kroste** (`lars-oste@gmx.de`)
- **Referenz-Vorlage für KI-Apps: Allpaca** (Provider-Abstraktion, Settings-UI,
  Ollama-Modell-Download, AppImage-Packaging).

---

## Repo-Struktur & Tooling (Pflicht bei jedem Projekt)

### `Directory.Build.props` (Repo-Root)

Zentrale Metadaten, damit nichts pro csproj wiederholt wird:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Authors>Lars Oste</Authors>
    <RepositoryUrl>https://github.com/Kroste/$(MSBuildProjectName)</RepositoryUrl>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

### Versionierung via MinVer

- Version kommt aus dem **Git-Tag** (`v1.4.0` → Assembly `1.4.0`), **kein** manuelles
  Hochzählen von `<Version>` in der csproj.
- Tag `vX.Y.Z` koppelt direkt an die Release-Action.

### `.editorconfig` + Analyzer

- File-scoped Namespaces, Accessibility-Modifier erzwingen, konsistenter Stil.
- Zusammen mit `TreatWarningsAsErrors`: Fehler am Compile statt erst im Log.

### `.vscode/`

- `launch.json` + `tasks.json` beilegen.
- **Hard-Clean-Task** (löscht `bin/` und `obj/` rekursiv).
- Task zum **Öffnen des aktuellen Logfiles** (Logs gehören zum Workflow).

### Tests

- **Eigenes Testprojekt** ist Pflicht – kein Projekt gilt ohne als „aufgesetzt“.

### Repo-Hygiene

- `README.md` (Build/Run + Screenshot), `LICENSE`, dotnet-`.gitignore`.
- Einheitliches **App-Icon** für Fenster + Exe + AppImage.

---

## GitHub Actions (Pflicht)

### CI – bei jedem Push/PR

- `dotnet build` + `dotnet test`. Macht die Test-Pflicht durchsetzbar.

### Release – bei Tag `vX.Y.Z`

- Fertige Pakete für **Windows (win-x64 ZIP)**, **Linux (tar.gz)** und **AppImage**.
- **Node 24** verwenden.

---

## UI / Fenster (Avalonia)

- Alle Fenster erben von der **`ChromeWindow`**-Basisklasse (Custom-Chrome:
  eigene Titelleiste mit Drag, Min/Max/Close), **sauberes Beenden**.
- **Alle Fenster sind resizable** (`CanResize = true`, in `ChromeWindow` gesetzt) –
  inkl. Dialoge und Einstellungen. Keine fix dimensionierten Fenster; sinnvolle
  `MinWidth`/`MinHeight` setzen statt das Resizing zu sperren.
- **Info-/About-Fenster (InfoBox)** ist Pflicht:
  App-Name, Version (aus Assembly), Kurzbeschreibung,
  GitHub-Link (Kroste) und **„Buy me a coffee“-Button** (buymeacoffee.com).
- **Einstellungen-Fenster** ist Pflicht, sobald die App KI nutzt: hier liegen
  Provider-, Endpoint-, Modell- und API-Key-Auswahl **sowie der Modell-Download**
  (Vorbild: **Allpaca**, siehe KI-Integration).

### Avalonia-12-Konventionen (Breaking Changes ggü. v11)

- **Diagnostics:** `Avalonia.Diagnostics` ist entfernt → `AvaloniaUI.DiagnosticsSupport`
  (Debug-only, z. B. 2.2.1):

  ```xml
  <ItemGroup Condition="'$(Configuration)' == 'Debug'">
    <PackageReference Include="AvaloniaUI.DiagnosticsSupport" Version="2.2.1" />
  </ItemGroup>
  ```

- **Custom-Chrome:** `ExtendClientAreaChromeHints` (inkl. `NoChrome`) ist entfernt.
  Stattdessen:

  ```csharp
  WindowDecorations = WindowDecorations.BorderOnly;   // NICHT .None (killt Resize-Griffe)
  ExtendClientAreaToDecorationsHint = true;
  CanResize = true;
  ```

- **APIs:** `TextBox.PlaceholderText` statt `Watermark`.

---

## Architektur & Runtime

- **MVVM** via CommunityToolkit.Mvvm (`ObservableObject`, `[ObservableProperty]`,
  `[RelayCommand]`, Source-Generator-basiert).
- **DI/Komposition** via Microsoft.Extensions.DependencyInjection + Hosting –
  Logger, KI-Provider und Services werden eingehängt (testbar/austauschbar).
- **Globaler Exception-Handler:**

  ```csharp
  AppDomain.CurrentDomain.UnhandledException += (_, e) =>
      _logger.Fatal(e.ExceptionObject as Exception, "Unbehandelte Exception");
  TaskScheduler.UnobservedTaskException += (_, e) =>
  {
      _logger.Fatal(e.Exception, "Unbeobachtete Task-Exception");
      e.SetObserved();
  };
  ```

  → NLog `Fatal` + freundlicher Dialog statt stillem Absturz.

---

## Logging (NLog)

- **Grundsätzlich alles loggen** (Trace/Debug für Abläufe, Info für Aktionen,
  Warn/Error für Probleme).
- **Passwörter/Secrets dürfen NIEMALS geloggt werden** – vor dem Logging
  entfernen/maskieren. Connection-Strings über `SqlConnectionStringBuilder`
  mit geleertem `Password` loggen.
- Logs gehören zum Workflow: **nach Änderungen immer mitanschauen** und gezielt
  auf `Warn`/`Error`/Exceptions prüfen → Teil der Definition of Done.

---

## Secrets & Konfiguration

- Config plattformkonform unter **`%APPDATA%`** bzw. **`$XDG_CONFIG_HOME`** ablegen
  (**nicht** neben die Exe).
- API-Keys **nie im Klartext**: Windows **DPAPI** (`ProtectedData`),
  Linux **libsecret/SecretService**.
- Secrets **nie committen** (`.gitignore`). Logische Erweiterung von
  „Passwörter nie loggen“.

---

## KI-Integration

**Für DTM nicht relevant.** DTM ist ein reines Datenbank-Administrationswerkzeug ohne KI-Funktionen — die Provider-/Modell-/Ollama-Konventionen des Master-Kanons sind hier nicht anzuwenden. Sollte sich das ändern, gilt wieder der Master-Kanon (Allpaca als Referenz).

---

## DTM-Konventionen

- **`SystemFile`-Alias Pflicht:** Im Namespace `DTM` existiert ein eigenes `record File`
  (in `Data/Database_Stats.cs`), das `System.IO.File` schattiert. In jeder Datei, die
  `System.IO.File` braucht, deshalb verpflichtend:

  ```csharp
  using SystemFile = System.IO.File;
  ```

  und konsequent `SystemFile.ReadAllText(...)` etc. verwenden. Ohne diesen Alias greift
  der Compiler auf `DTM.File` zu und die Aufrufe schlagen mit unverständlichen
  Fehlern fehl.

- **PowerShell-Lifecycle:** PowerShell läuft als **in-process Runspace** via
  `Microsoft.PowerShell.SDK` (kein `Process.Start`). Beim App-Ende wird der
  Hauptprozess via `Process.GetCurrentProcess().Kill()` beendet **statt**
  `Environment.Exit(...)`, um Finalizer-Hänger im PowerShell-SDK zu vermeiden
  (siehe `UpdateService`). Diese Sonderbehandlung darf nicht „aufgeräumt" werden.

- **Update-Mechanismus:** Der Updater kopiert nach `%TEMP%`, startet
  `dtm_update.ps1` und beendet sich danach. Änderungen am Update-Pfad müssen
  den Skript-Vertrag (Argumente, erwartete Pfade) wahren.

- **Connection-Strings:** Vor jedem Logging über `OdbcConnectionStringBuilder` /
  `SqlConnectionStringBuilder` `Password`/`PWD` leeren – nie als Rohstring loggen.
  Logische Spezialisierung der allgemeinen Secrets-Regel.

- **FOC-SQL als Dev-Submodul:** Das PowerShell-Modul `FOC-SQL.psm1` ist unter
  `external/FOC-SQL/` als Git-Submodul (`https://github.com/Kroste/FOC-SQL.git`)
  eingebunden — **ausschließlich als Code-Referenz für die Entwicklung** (Aufrufe
  und Verhalten der Modulfunktionen wie `Backup-Database`, `Set-Snapshot`,
  `Restore-Snapshot` im Original nachschlagen). Die DTM-Runtime lädt das Modul
  **nicht** aus diesem Pfad — die produktive Auflösung läuft weiterhin über
  `SambaSource`/`ModulePath` (siehe `Data/Terminal/FocSqlRuntime` und die
  Einstellungen in `ConnectionManagerWindow`). Submodul-Inhalt aktualisieren bei
  Bedarf: `git submodule update --remote external/FOC-SQL`.

---

## Projektspezifische Realität & offene Migrationen

### Akzeptierte Abweichungen (kein TODO)

- **Keine KI-Integration** – DTM ist Daten-Admin-Tool, nicht KI-Produkt
  (siehe Abschnitt „KI-Integration").

- **„Log An/Aus"-Buttons mit semantischer Doppelnutzung** – die Buttons rufen
  einheitlich `Set-Archive-Log` auf, das Modul dispatched MSSQL→Recovery-Mode-
  Toggle (FULL/SIMPLE), Oracle→echter Archivelog-Toggle. Die Labels sind
  Oracle-zentriert, funktional klappt es für beide. Eine sauberere MSSQL-
  Alternative kommt mit Phase 3.4 (Recovery-Mode-Dropdown).

### Erledigte Migrationen

1. **Avalonia 11.2.3 → 12.x (inkl. `ChromeWindow`-Basisklasse)** — erledigt
   - Core-Pakete auf `12.0.5`, `Avalonia.Controls.DataGrid` + `Avalonia.Fonts.Inter`
     auf `12.0.1` (höher gibt es nicht).
   - `Avalonia.Diagnostics` ersetzt durch `AvaloniaUI.DiagnosticsSupport 2.2.1`
     (Debug-only).
   - `Tmds.DBus.Protocol`-Pin entfernt.
   - `Watermark` → `PlaceholderText` in `ConnectionManagerWindow.axaml` und
     `Views/Controls/ConsoleControl.axaml`.
   - `ChromeWindow`-Basisklasse in `Views/ChromeWindow.cs` (setzt
     `WindowDecorations.BorderOnly`, `ExtendClientAreaToDecorationsHint = true`,
     `CanResize = true`; stellt gemeinsame `OnTitleBarPointerPressed` und
     `OnTitleBarDoubleTapped` bereit).
   - Alle 7 Windows umgestellt; `SystemDecorations`-Attribute entfernt;
     `CanResize="False"` in den 4 Dialogen ersetzt durch `MinWidth`/`MinHeight`.
   - Folge-Anpassungen wegen Avalonia-12-API-Änderungen:
     - `IClipboard.SetTextAsync` → `SetDataAsync(new DataTransfer { … })`
       in `Views/Controls/AnsiConsole.cs`.
     - `VisualTreeAttachmentEventArgs.Root` → `TopLevel.GetTopLevel(this)`
       in `Views/Controls/ConsoleControl.axaml.cs`.
   - Build + 269 Tests grün auf Linux. **Smoke-Test der UI auf Windows + Linux
     steht noch aus** (Drag/Resize/Min/Max in jedem Fenster prüfen).

### Offene Roadmap (Phasen, in dieser Reihenfolge)

> **Legende:** `S` = klein (1–3 h, 1 Commit) · `M` = mittel (halber Tag, 2–4 Commits) ·
> `L` = groß (mehrere Tage). 📦 = FOC-SQL-Submodul muss erweitert werden
> (eigener PR in `Kroste/FOC-SQL` + manueller Samba-Rollout durch Lars,
> danach DTM ankoppeln). 🛡 = destruktive Aktion, braucht Bestätigungs-Dialog
> + Test-DB. 🔁 = setzt vorheriges Item voraus.

#### Phase 0 — Fundament (Schutz vor späteren Regressionen)

- [x] **0.1** CI-Workflow `.github/workflows/ci.yml`: `dotnet build` + `dotnet test`
      auf Push/PR. — `S` _(erledigt: `91991c7`, Actions nachgezogen auf
      Node-24-native Major-Versionen mit `1c7acde`)_
- [x] **0.2** Globaler Exception-Handler in `Program.cs`
      (`AppDomain.CurrentDomain.UnhandledException` +
      `TaskScheduler.UnobservedTaskException` → NLog Fatal + Dialog). — `S`
      _(erledigt: `0d0b48a`; zusätzlich `Dispatcher.UIThread.UnhandledException`
      für UI-Thread, `FatalErrorWindow` als ChromeWindow-Dialog)_
- [x] **0.3** `Microsoft.Extensions.DependencyInjection` einziehen; manuelle
      Instanziierung in `App.axaml.cs` (`BuildDataLayer`) durch DI ersetzen
      — ViewModels/Services über Container. — `M`
      _(erledigt: `a9b98be`; `Composition/ServiceRegistrations.cs` als Composition-Root,
      `App.Services` als statischer `IServiceProvider`. `Microsoft.Extensions.Hosting`
      bewusst weggelassen — IHost/IConfiguration/ILogger werden nicht gebraucht,
      NLog konfiguriert sich selbst, JSON-Stores haben ihr eigenes Schema. Lässt
      sich nachziehen, wenn Config/Logging via DI später nötig wird.)_
- [x] **0.4** `Directory.Build.props` (Inhalt wie Tech-Stack-Block oben) +
      `.editorconfig` (file-scoped Namespaces, Accessibility-Modifier erzwingen) +
      `LICENSE`. — `S`
      _(erledigt: `f9e1236` für `Directory.Build.props` + `.editorconfig` +
      csproj-Aufräumung, MIT-LICENSE © 2025-2026 Lars Oste separat. Bestehender
      Code ist mit den neuen Regeln konform — keine Quellcode-Anpassung nötig.)_

#### Phase 1 — Quick Wins (keine Submodul-Änderung nötig)

- [x] **1.1** `Set-Archive-Log`-Inkonsistenz geklärt — entschieden: Status quo.
      Code-Realität (entgegen ursprünglicher Roadmap-Annahme): die „Log An/Aus"-
      Buttons sind in `MainWindowViewModel.ApplyStats` für **beide** DB-Typen
      aktiv. `Set-Archive-Log` dispatched im Modul nach DB-Typ: MSSQL togglet
      `Recovery FULL/SIMPLE`, Oracle togglet echten `ARCHIVELOG ON/OFF`. Die
      semantische Doppelnutzung der gleichen Buttons bleibt absichtlich — Phase
      3.4 bringt für MSSQL einen dedizierten Recovery-Mode-Dropdown
      (FULL/SIMPLE/BULK_LOGGED), der die Mehrdeutigkeit für den MSSQL-Pfad
      auflöst. — `S` _(siehe „Akzeptierte Abweichungen" oben)_
- [ ] **1.2** Snapshot-Buttons: Multi-PDB-Warnung für Oracle vor `Restore-Snapshot`
      (`⚠ CDB wird heruntergefahren, betrifft alle PDBs`). — `S` 🛡
- [ ] **1.3** Cluster-Health-Indicator (`Get-ClusterHealthStatus`) in Info-Card oder
      als Status-Punkt. MSSQL-only, read-only. — `S`
- [ ] **1.4** Oracle-Restore-Vorschau (`Get-OracleRestoreInfo`) — neuer Dialog
      `OracleRestoreSelectWindow` mit Liste der Restore Points + PDBs vor
      `Restore-Snapshot`. Macht 1.2 obsolet, wenn richtig gebaut. — `M` 🛡

#### Phase 2 — Sessions & Backup-Workflow

- [ ] **2.1** 📦 FOC-SQL erweitern: `Close-DbSessions` als Wrapper
      (MSSQL: `Database-Close-Connections`; Oracle: `ALTER SYSTEM KILL SESSION`
      per SSH). — `M`
- [ ] **2.2** 🔁 Kill-Session-Button im `SessionsWindow` (per Row + „Alle beenden"),
      mit Bestätigung. Nutzt `Close-DbSessions`. — `M` 🛡
- [ ] **2.3** 📦 FOC-SQL erweitern: `Get-DbBackups` + `Invoke-DbRestore`
      (MSSQL: `Get-All-Backups`/`Database-Backup-Restore`; Oracle: passendes
      RMAN-Listing — optional, in v1 evtl. nur MSSQL). — `L`
- [ ] **2.4** 🔁 Backup-Browser in DTM — neuer Tab oder Dialog mit DataGrid
      (Datei, Datum, Größe) + Restore-Knopf. Pre-Check Sessions schließen via 2.1. — `L` 🛡

#### Phase 3 — Wartungs-Tooling

- [ ] **3.1** 📦 FOC-SQL erweitern: `Invoke-DbMaintenance` mit Switches
      (`-CheckDb`, `-IndexRebuild`, `-ShrinkLog`) — Wrapper um die drei
      MSSQL-Funktionen, Oracle vorerst Pass-Through. — `M`
- [ ] **3.2** 🔁 Neue Action-Gruppe „WARTUNG" im MainWindow (drei Buttons im
      Stil der bestehenden Gruppen, MSSQL-only-Filter). — `S`
- [ ] **3.3** 📦 FOC-SQL erweitern: `Set-DbRecoveryMode` (Wrapper um
      `Database-Set-Recovery-Mode`). — `S`
- [ ] **3.4** 🔁 Recovery-Mode-Dropdown im Info-Card (FULL/SIMPLE/BULK_LOGGED)
      für MSSQL, mit Bestätigung — Wechsel zu SIMPLE bricht Log-Chain. — `S`

#### Phase 4 — Polish & Komfort

- [ ] **4.1** Snapshot-Cleanup mit Altersfilter (`Database-Snapshot-Delete -Day n`)
      als Option im Remove-Snapshot-Dialog. — `S` 📦 (optional)
- [ ] **4.2** `AboutWindow` ergänzen: GitHub-Link auf `https://github.com/Kroste/DTM`
      + „Buy me a coffee"-Button (`buymeacoffee.com`). — `S`
- [ ] **4.3** `.vscode/tasks.json` ergänzen: Hard-Clean-Task (rekursives Löschen
      `bin/`/`obj/`) + Task „Aktuelles Logfile öffnen" (`logs/info.log`/`error.log`). — `S`
- [ ] **4.4** **MinVer** einbinden; manuelle `<Version>`/`<AssemblyVersion>` aus
      `DTM.csproj` entfernen. Tag-Schema `vX.Y.Z` ist schon vorhanden. — `S`
- [ ] **4.5** `.github/workflows/release.yml` um AppImage-Job erweitern
      (Node 24 ist bereits gesetzt). — `M`
- [ ] **4.6** `README.md` um Screenshot ergänzen. — `S`

#### Phase 5 — Optional / Niedrige Priorität

- [ ] **5.1** Query-Store-Toggle (MSSQL). — `S` 📦
- [ ] **5.2** SQL-Script-Runner-Dialog (`Database-Execute-SQL`/`-GetSQL-File`). — `M` 📦
- [ ] **5.3** `Database-Set-Page-Verify` / `Database-Set-Compatibility`. — `S` 📦
- [ ] **5.4** Stats-Konsolidierung: ODBC-Stats durch `Get-DatabaseStats`
      ablösen (Architektur-Refactor, beseitigt MSSQL-/Oracle-Logik-Duplikat
      zwischen `Data/MSSQL_ODBC.cs`/`ORACLE_ODBC.cs` und dem PS-Modul). — `L`

---

## Definition of Done (Checkliste)

- [ ] `Directory.Build.props`, `.editorconfig`, `.gitignore`, `README`, `LICENSE` vorhanden
- [ ] `.vscode/` mit launch/tasks inkl. Hard-Clean + Log-Öffnen-Task
- [x] Testprojekt vorhanden, `dotnet test` grün
- [ ] CI-Action (build+test) und Release-Action (Win/Linux/AppImage, Node 24) eingerichtet
- [ ] MinVer aktiv, Release an Tag `vX.Y.Z` gekoppelt
- [ ] Alle Fenster über `ChromeWindow`, **resizable** ✓ — InfoBox mit BMC-Button noch offen
- [x] Avalonia ≥ 12.0.4, v12-Konventionen eingehalten
- [ ] Globaler Exception-Handler greift → NLog Fatal + Dialog
- [x] NLog loggt umfassend, **keine Secrets** im Log; Logs nach Änderung geprüft
- [x] Secrets sicher abgelegt (DPAPI/libsecret), nichts im Klartext committet
- [x] App-Icon einheitlich (Fenster + Exe + AppImage)
