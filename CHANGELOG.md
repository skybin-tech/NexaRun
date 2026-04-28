# Changelog

## [2026-04-28]

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
