# Changelog

## [2026-05-21] — v1.0.5

### Added
- JSON import/export (`nexarun import`, tray, Processes window, **Import JSON…** on Add Process)
- PM2-style CLI (`start`, `stop`, `restart`, `delete`, `list`, `logs`, `daemon`)
- `nexarun-processes.json` for all 14 monorepo apps (`C:\repo\SSC.Nebula` paths)
- Dashboard **Logs** tab in the same window (optional **Live update**)
- Tray crash logging to Event Viewer (`NexaRun-Tray`) and `logs\tray-crash.log`
- `ProcessLaunchHelper` for windowless process launches on Windows

### Changed
- Tray app `WinExe` (no console window); simplified tray import menu
- Dashboard background refresh reduced (30s stats only; history/logs on demand)
- Installer: Inno Setup 7 detection, Release publish guard, event log source registration

## [2026-05-08]

### Changed
- Upgraded System.CommandLine from 2.0.0 to 2.0.7 for compatibility with latest API
- Removed SetHandler calls from all CLI commands; simplified command definitions to structure only (handlers to be wired separately)

## [2026-04-28] — v1.0.1

### Changed
- All four assemblies strong-name signed with `skybin.snk` (Skybin Technology Private Limited)
- Assembly names cleaned up: Tray → `NexaRun.exe`, Daemon → `NexaRun.Daemon.exe`, CLI → `NexaRun.Cli.exe` (installed to PATH as `nexarun.exe`)
- Added `Product`, `Company`, and `Copyright` metadata to all exe projects — visible in Windows file properties
- Installer updated: copyright set to "Skybin Technology Private Limited", CLI bin copy renamed to `nexarun.exe`
- README rewritten as a user-facing guide — covers installation, tray app, CLI, examples, and auto-restart behaviour; removed developer/project structure content

## [2026-04-28] — v1.0.0

### Added
- `NexaRun.Daemon` — background Worker Service with named pipe IPC server, ProcessManager singleton, and DaemonWorker monitor loop
- `NexaRun.Cli` — CLI with `start`, `stop`, `restart`, `delete`, `list`, `logs`, and `daemon` commands using System.CommandLine and Spectre.Console
- `NexaRun.Tray` — cross-platform Avalonia 11.3 system tray app with Processes window, Dashboard window, Add/Edit Process form, Logs viewer, and Confirm dialog
- `NexaRun.Shared` — shared models (`NexaProcess`, `ProcessRun`, `StartOptions`, `IpcRequest`, `IpcResponse`), `IpcClient`, and `PipeConstants`
- Process list persisted to `~/.nexarun/processes.json`; run history (last 7 days) persisted to `~/.nexarun/history.json`
- stdout/stderr capture to `~/.nexarun/logs/<name>.log` with `FileShare.ReadWrite` to allow concurrent reading
- Auto-restart on crash with exponential backoff (1 s → 2 s → 4 s → 8 s → 30 s max)
- Resource-limit restarts: configurable Max CPU % and Max Memory MB per process; breach reason written to process log
- CPU usage calculated from `TotalProcessorTime` deltas across the 5-second monitor interval
- Executable resolved from PATH via `cmd /c` on Windows — no need for full paths (`npm`, `dotnet`, `node`, etc.)
- Dashboard with 7-day uptime bar chart (green ≥ 95 %, yellow ≥ 50 %, red < 50 %) and run history grid
- Edit Process form pre-filled from current process settings; running processes stopped and restarted on save
- `update` IPC command for editing a running process in place
- `history` IPC command returning last 7 days of `ProcessRun` records for a named process
- README.md with full usage guide, CLI reference, tray app guide, and changelog
