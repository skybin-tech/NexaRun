# Changelog

## [2026-06-05]

### Changed
- `NexaRun.Daemon` — upgrade `AWSSDK.SimpleEmail` to `AWSSDK.SimpleEmailV2` (v4.0.14) with API updates to use SendEmailV2 request structure
- `ProcessAlertService` — migrate from Amazon.SimpleEmail to Amazon.SimpleEmailV2; update SendEmailRequest to use V2 API (FromEmailAddress, EmailContent with nested Message structure)

## [2026-05-30] — v1.0.8

### Added
- `nexarun restart all` — PM2-style alias for restarting every managed process (same behavior as `nexarun restart-all`)
- `NexaRun.Cli/RestartAllRunner` — shared CLI orchestration for restart-all: per-app Spectre progress bar, id-based restarts, settle wait only on the last process

### Changed
- `nexarun restart` — accepts `all` as a target in addition to id or name; help text documents PM2-style usage
- `NexaRun.Tray/NexaRun.Tray.csproj` — bump Avalonia + Avalonia.Desktop + Avalonia.Themes.Fluent 12.0.3→12.0.4

## 2026-05-23
- **NexaRun.Tray:** Refactor Avalonia 12 window code-behind to typed patterns; align tray csproj packages

## [2026-05-23] — v1.0.7

### Changed
- `NexaRun.Daemon` — bump `AWSSDK.SimpleEmail` from 3.7.401 to 4.0.2.31

### Added
- `NexaRun.Shared/ProcessTarget` — PM2-style id-or-name resolution; all CLI commands and daemon handlers now accept a numeric id (from `nexarun list`) or a name string
- `NexaRun.Cli/CliCommands` — shared `TargetArgument` and `TargetRequest` helpers used by stop, restart, delete, logs, and start commands
- `NexaRun.Cli/CliIpc` — `IpcClient.Send` extension with optional Spectre.Console spinner and elapsed timer; `RunWithProgress` helper; per-command status messages
- `nexarun restart-all` — restarts every managed process one at a time with a Spectre progress bar; skips settle wait between processes, only waits after the last one
- `nexarun clear-all` — stop and remove all processes at once
- `nexarun version` — print assembly informational version
- `ProcessManager.StartExisting` — start a saved (stopped/errored) process by id or name without re-registering it
- `IpcRequest.SettleAfterStart` — lets restart-all skip the post-start wait between processes

### Changed
- `nexarun start` — unified target argument: pass an id/name to resume a saved process, or an executable with flags to register a new one
- `nexarun stop/restart/delete/logs` — all accept id or name (was name-only)
- `ProcessManager.Stop/Restart/Delete/GetLogs/GetHistory` — resolve via `ProcessTarget.TryResolve`; error messages include `[id] name`
- `ProcessManager.GetLogs` and `GetHistory` return result tuples with success flag; daemon surfaces errors instead of silently returning empty
- `IpcServer` — `"start"` dispatch splits: start-existing path when `Options.ExecutablePath` is absent; `HandleRestart` forwards `SettleAfterStart` flag

## [2026-05-22] — v1.0.6

### Added
- **Settings** window: configurable failed-process recovery interval (min 10 min), enable/disable recovery, **AWS SES** email when a process goes Online → Errored
- Daemon **`restart-all`** — restart every process one by one without selecting a row in the tray

### Changed
- Installer: `ChangesEnvironment=yes` and post-install PATH update so `nexarun` works in new terminals (`{app}\bin`)
- Import no longer auto-starts processes (use **Start** or `nexarun import --start`)
- **Restart All** uses daemon `restart-all`; no row selection required
- Recovery check interval and email alerts read from `%APPDATA%\NexaRun\settings.json`

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
