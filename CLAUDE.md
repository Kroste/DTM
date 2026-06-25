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

### Offene Migrationen (TODO, in dieser Reihenfolge)

1. **Repo-Baseline**
   - `Directory.Build.props` im Root anlegen (Inhalt wie Tech-Stack-Block oben).
   - `.editorconfig` anlegen (file-scoped Namespaces, Accessibility-Modifier erzwingen).
   - `LICENSE` anlegen.
   - **MinVer** einbinden; manuelle `<Version>` / `<AssemblyVersion>` aus `DTM.csproj`
     entfernen. Tag-Schema `vX.Y.Z` ist schon vorhanden.
   - `README.md` um Screenshot ergänzen.

2. **CI/CD**
   - `.github/workflows/ci.yml` für `dotnet build` + `dotnet test` bei jedem Push/PR.
   - `.github/workflows/release.yml` um AppImage-Job erweitern; Node-Version auf 24.

3. **Komposition & Robustheit**
   - `Microsoft.Extensions.DependencyInjection` + `Microsoft.Extensions.Hosting`
     einziehen; manuelle Instanziierung in `App.axaml.cs` (`BuildDataLayer`) durch
     DI ersetzen — ViewModels und Services über Container.
   - Globaler Exception-Handler in `Program.cs`
     (`AppDomain.CurrentDomain.UnhandledException` +
     `TaskScheduler.UnobservedTaskException`) → NLog Fatal + freundlicher Dialog.

4. **UI-Feinheiten**
   - `AboutWindow` ergänzen: GitHub-Link auf `https://github.com/Kroste/DTM`
     und „Buy me a coffee"-Button (`buymeacoffee.com`).
   - `.vscode/tasks.json` ergänzen: Hard-Clean-Task (rekursives Löschen von
     `bin/` und `obj/`) sowie Task „Aktuelles Logfile öffnen"
     (`logs/info.log` bzw. `logs/error.log`).

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
