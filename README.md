# NexaRun

A PM2-inspired process manager for .NET. Start, stop, restart, and monitor long-running processes from the CLI or a system tray GUI — with a persistent background daemon keeping them alive.

---

## Projects

| Project | Type | Purpose |
|---|---|---|
| `NexaRun.Daemon` | Worker Service | Background engine — manages processes, IPC server, auto-restart |
| `NexaRun.Cli` | Console App | CLI tool (`nexarun list`, `nexarun start …`) |
| `NexaRun.Tray` | Avalonia Desktop | System tray app with GUI dashboard |
| `NexaRun.Shared` | Class Library | Shared models and IPC client |

---

## Getting Started

### 1. Build

```bash
dotnet build
```

### 2. Start the Daemon

The daemon must be running before the CLI or Tray can connect to it.

```bash
dotnet run --project NexaRun.Daemon
```

Leave this running in the background. It listens on a named pipe (`nexarun-daemon`) and manages all processes.

### 3. Use the CLI or Tray

**CLI** (in a second terminal):
```bash
dotnet run --project NexaRun.Cli -- <command>
```

**Tray app:**
```bash
dotnet run --project NexaRun.Tray
```

---

## CLI Usage

```
nexarun start <executable> [options]
nexarun stop <name>
nexarun restart <name>
nexarun delete <name>
nexarun list
nexarun logs <name> [--lines 50]
nexarun daemon start
nexarun daemon stop
```

### Examples

```bash
# Start a .NET API
nexarun start dotnet --name myapi --args "run --project ./MyApi" --cwd "C:/Projects/MyApi"

# Start a Next.js site
nexarun start npm.cmd --name mysite --args "run start" --cwd "C:/Projects/my-nextjs-app"

# Start any command (resolved from PATH like a terminal)
nexarun start node --name worker --args "server.js" --cwd "C:/Projects/worker"

# List all processes
nexarun list

# View logs
nexarun logs myapi
nexarun logs mysite --lines 100

# Stop and restart
nexarun stop myapi
nexarun restart myapi

# Remove from list
nexarun delete myapi
```

### Start Options

| Option | Description |
|---|---|
| `--name <name>` | Unique name for the process (required) |
| `--args <args>` | Arguments to pass to the executable |
| `--cwd <path>` | Working directory (defaults to current directory) |
| `--no-autorestart` | Disable automatic restart on crash |

---

## Tray App Usage

Launch the tray app with `dotnet run --project NexaRun.Tray`. A green circle appears in the system tray.

**Tray menu:**
- **Processes** — open the process list window
- **Dashboard** — open the dashboard with 7-day run history
- **Add Process** — add a new process
- **Exit NexaRun** — shut down the tray app (daemon keeps running)

### Processes Window

Lists all managed processes with status, PID, memory, restarts, and uptime. Select a process to enable the action buttons:

| Button | Action |
|---|---|
| Stop | Gracefully stop the process (SIGTERM → kill after 5s) |
| Restart | Stop and start the process |
| Logs | Open a live log viewer (auto-refreshes every 2s) |
| Edit | Edit process settings (see below) |
| Delete | Remove the process from the list |

### Add / Edit Process

| Field | Description |
|---|---|
| Name | Unique identifier for the process |
| Executable | Command to run — resolved from PATH just like a terminal (`npm`, `dotnet`, `node`, etc.) |
| Arguments | Arguments passed to the executable |
| Working Directory | Directory to run the process in |
| Auto-restart on crash | Restart automatically if the process exits unexpectedly |
| Max CPU % | Restart the process if CPU usage exceeds this threshold (e.g. `80`) |
| Max Memory MB | Restart the process if memory exceeds this threshold (e.g. `512`) |

**Edit** opens the same form pre-filled. Name cannot be changed (it is the identifier). If the process is running when you save changes, it is stopped and restarted with the new settings.

### Dashboard Window

Shows all processes in a left panel. Selecting a process shows:
- A stats bar with current status, PID, memory, and restart count
- A 7-day uptime bar chart (green ≥ 95%, yellow ≥ 50%, red < 50%)
- A run history grid with start time, duration, outcome (Running / Clean / Crashed / Stopped), and exit code

---

## Runtime Files

All data is stored in `~/.nexarun/`:

```
~/.nexarun/
├── processes.json        ← persisted process list (survives daemon restart)
├── history.json          ← last 7 days of run history
└── logs/
    ├── <name>.log        ← stdout + stderr for each process (timestamped)
    └── nexarun-daemon.log ← daemon's own log output
```

Logs are written with `FileShare.ReadWrite` so the log viewer and the daemon can access them simultaneously.

---

## Adding a Next.js Site

| Field | Value |
|---|---|
| Name | `my-site` |
| Executable | `npm` (resolved from PATH automatically) |
| Arguments | `run start` |
| Working Directory | path to your Next.js project |

For `next dev` use `run dev` as the arguments.

---

## Resource Limit Restarts

Set **Max CPU %** or **Max Memory MB** when adding or editing a process. The daemon checks every 5 seconds and restarts the process if either limit is exceeded. A `NEXARUN: Restarting — <reason>` line is written to the process log so you can see why it was restarted.

---

## Auto-Restart Backoff

Crashed processes are restarted with exponential backoff:

| Restart # | Delay |
|---|---|
| 1 | 1s |
| 2 | 2s |
| 3 | 4s |
| 4 | 8s |
| 5+ | 30s (max) |

---

## Changelog

### v0.4.0
- Added **Edit Process** — select any process and click Edit to update executable, arguments, working directory, auto-restart, and resource limits; running processes are stopped and restarted automatically
- Added **Max CPU %** and **Max Memory MB** limits per process — daemon restarts the process when either limit is exceeded
- Fixed CPU usage reporting — now calculated from `TotalProcessorTime` deltas across the 5-second monitor interval instead of always showing 0
- Limit breach reason written to the process log file

### v0.3.0
- Added **Dashboard window** with 7-day run history per process
- 7-day uptime bar chart (per calendar day, color-coded green/yellow/red)
- Run history grid showing start time, duration, outcome, and exit code
- Run history persisted to `~/.nexarun/history.json` and loaded on daemon startup
- New `history` IPC command

### v0.2.0
- Added **Avalonia cross-platform tray app** (replaces WinForms prototype)
- Tray icon with native menu: Processes, Dashboard, Add Process, Exit
- Processes window with DataGrid, action buttons (Stop / Restart / Logs / Delete)
- Add Process window with file/folder picker
- Logs window with auto-scroll and 2-second live refresh
- Confirm dialog for destructive actions
- `IpcClient` moved to `NexaRun.Shared` so CLI and Tray share it
- Executable resolved from PATH using `cmd /c` on Windows — no need to specify full paths
- Log file opened with `FileShare.ReadWrite` — no lock conflicts when viewing logs during restart

### v0.1.0
- Initial release
- `NexaRun.Daemon` — background worker service with named pipe IPC server
- `NexaRun.Cli` — CLI with `start`, `stop`, `restart`, `delete`, `list`, `logs`, `daemon` commands
- Process list persisted to `~/.nexarun/processes.json`
- stdout/stderr capture to `~/.nexarun/logs/<name>.log`
- Auto-restart on crash with exponential backoff (1s → 30s max)
- Stats monitor loop every 5 seconds (memory, uptime)
- Windows Service (`AddWindowsService`) and Linux systemd (`AddSystemd`) support
