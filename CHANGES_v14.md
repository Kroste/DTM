# DTM — Patch v14 (ExecutionPolicy-Fix + SSH-Tab entfernt)

Dieser Stand ist das **komplette Projekt** als Download (nicht nur ein Patch).
Inhaltlich gegenüber v13 zwei Änderungen.

## 1. Bugfix: „running scripts is disabled on this system"

Im Screenshot scheiterte der Modul-Import an der PowerShell-ExecutionPolicy.

Fix in `MainWindowViewModel.ShellInitialCommand`: ganz am Anfang des
Initial-Setups wird die Policy nur für DIESEN Prozess auf Bypass gesetzt:
```powershell
try { Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force -ErrorAction Stop } catch {}
```
- `-Scope Process` wirkt nur im laufenden Runspace, ändert nichts am System
  und braucht keine Admin-Rechte.
- Das `try/catch` schluckt den Fall, dass die Operation auf der Plattform
  nicht unterstützt ist (z.B. Linux), ohne das Setup abzubrechen
  (sandbox-verifiziert).

## 2. SSH-Tab komplett entfernt

Da alle Oracle-Befehle über die FOC-SQL-Modulfunktionen laufen (die ihr eigenes
SSH-Remoting kapseln), war der separate SSH-Tab überflüssig. Nebeneffekt: die
doppelten „FOC-SQL Modul geladen" / „Verbunden"-Zeilen im Screenshot kamen vom
Initial-Setup, das pro Tab einmal lief — mit nur noch einem Tab tritt das
nicht mehr auf.

Entfernt:
- `Views/MainWindow.axaml`: TabControl raus, nur noch das `ConsoleControl` direkt.
- `Views/Controls/ConsoleControl.axaml(.cs)`: auf PowerShell-only vereinfacht
  (278 → 163 Zeilen). `TerminalKind`-Enum, `Host`/`User`/`Port`-Properties,
  `CreateSshSession` entfernt.
- `Data/Terminal/SshTerminalSession.cs`, `SshKeyLocator.cs`,
  `SshRuntimeConfig.cs` — gelöscht.
- `Data/Config/AppSettings.cs`: `SshConfig`-Klasse + `Ssh`-Property raus.
- `App.axaml.cs`: `SshRuntimeConfig.Current`-Verdrahtung raus.
- `ViewModels/MainWindowViewModel.cs`: `SshHost`/`SshUser`-Properties + die
  Oracle-Host/User-Zuweisung raus.
- `DTM.csproj`: `SSH.NET`-PackageReference entfernt (PowerShell-SDK bleibt).
- `appsettings.example.json`: `Ssh`-Sektion raus.

## Build verifiziert

```
dotnet build -c Debug    # 0 Errors
dotnet build -c Release  # 0 Errors
```

Aus dem ausgelieferten Quellbaum frisch gebaut (Release) — läuft.

## Inhalt dieses Archivs

Das komplette, schlanke Projekt (ohne bin/obj):
- C#-Quellen, Avalonia-Views, `FOC-SQL.psm1`
- `appsettings.example.json` (Vorlage — echte Daten in `appsettings.json`)
- `README.md`, `.gitignore`

## Nach dem Auspacken

1. `appsettings.example.json` → `appsettings.json` kopieren, echte Daten rein.
2. `credential.xml` im Benutzerprofil anlegen (siehe README).
3. `FOC-SQL.psm1` in die Samba-Quelle deployen.
4. `dotnet build DTM.csproj -c Release` und starten.

## Hinweise zum Verlauf (frühere Patches v1–v13)

Der Umbau in Kürze: SSH/PowerShell von Process.Start-Pipes auf
PowerShell-SDK umgestellt → Anzeige/ANSI/Threading gefixt → kompletter
Umstieg auf das FOC-SQL-Modul (alle 8 Aktionen, -Time/-Date für MSSQL und
Oracle, interaktiver DtmPSHost für Restore/Remove) → toten Code entfernt →
ExecutionPolicy-Fix und SSH-Tab raus (dieser Stand).
