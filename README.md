# NexaRun

NexaRun is a process manager for Windows — keep your apps running forever. Start any process (Node.js, .NET, Python, anything), and NexaRun will monitor it, restart it if it crashes, and enforce CPU and memory limits. Manage everything from a clean system tray GUI or the command line.

---

## Installation

Run `NexaRun-Setup.exe` and follow the wizard.

The installer will:
- Install the **NexaRun Daemon** as a Windows Service that starts automatically with Windows
- Add the **`nexarun` CLI** to your system PATH
- Launch the **NexaRun tray app** so you can start managing processes immediately
- Ship a sample `nexarun-processes.json` for JSON import

To build the installer from source: run `installer\generate-icon.ps1` then `installer\build.ps1` (requires Inno Setup 6).

---

## Tray App

Click the NexaRun icon in the system tray to get started. Right-click for the menu.

### Add a Process

1. Right-click the tray icon → **Add Process**
2. Fill in the form:

| Field | Description |
|---|---|
| **Name** | A unique name to identify this process |
| **Executable** | The command to run — works just like a terminal (`npm`, `dotnet`, `node`, `python`, etc.) |
| **Arguments** | Arguments passed to the executable |
| **Working Directory** | The folder to run the process in |
| **Auto-restart on crash** | Automatically restart if the process exits unexpectedly |
| **Max CPU %** | Restart if CPU usage exceeds this value (e.g. `80`) |
| **Max Memory MB** | Restart if memory exceeds this value (e.g. `512`) |

3. Click **Save & Start**

### Examples

| App | Executable | Arguments | Working Directory |
|---|---|---|---|
| Next.js site | `npm` | `run start` | `C:\Projects\my-site` |
| Next.js dev | `npm` | `run dev` | `C:\Projects\my-site` |
| .NET API | `dotnet` | `run --project .` | `C:\Projects\MyApi` |
| Node server | `node` | `server.js` | `C:\Projects\worker` |
| Python script | `python` | `app.py` | `C:\Projects\bot` |

### Tray menu — import / export

| Menu item | Action |
|---|---|
| **Import JSON…** | Pick a `.json` file (defaults to `nexarun-processes.json` if present) |
| **Add Process → Import JSON…** | Same import from the Add Process window |
| **Export JSON…** | Save definitions for another machine |
| **Processes window** | **Import JSON** / **Export JSON** toolbar buttons (same as tray) |
| **Open data folder** | Opens `%APPDATA%\NexaRun` |

### Processes Window

Right-click tray → **Processes** to see all running processes. Select a process to use the action buttons:

| Button | Action |
|---|---|
| **Stop** | Gracefully stop the process |
| **Restart** | Stop and start again |
| **Logs** | View live output from the process |
| **Edit** | Change settings (takes effect immediately) |
| **Delete** | Remove from the list |

### Dashboard

Right-click tray → **Dashboard** to see a 7-day history for any process — uptime bar chart, run history grid, and a **Logs** tab (same window). Use **↻ Refresh** for a full reload; background updates refresh the process list every 30 seconds only.

Tray crashes are written to **Event Viewer** (Application → **NexaRun-Tray**) and `%APPDATA%\NexaRun\logs\tray-crash.log`.

---

## CLI

The `nexarun` command is on your PATH after install. It talks to the **NexaRun Daemon** (Windows service). If the daemon is not running, commands fail with:

```text
Daemon is not running. Start it with: nexarun daemon start
```

**Data folder:** `%APPDATA%\NexaRun\` (`processes.json`, `history.json`, `logs\`)

**Dev usage:** `dotnet run --project NexaRun.Cli -- <command>`

### Command overview

```text
nexarun
├── start <executable>     Start one process
├── import <file.json>     Import processes from JSON
├── stop <name>            Stop a process
├── restart <name>         Restart a process
├── delete <name>          Remove from managed list
├── list                   List all processes
├── logs <name>            Show process logs
└── daemon
    ├── start              Start daemon (service or dev exe)
    └── stop               Stop daemon service
```

### `nexarun start`

Start a single process.

```powershell
nexarun start <executable> [options]
```

| Option | Description |
|--------|-------------|
| `--name <name>` | Process name (default: executable name) |
| `--args "<args>"` | Arguments for the executable |
| `--cwd <path>` | Working directory |
| `--no-autorestart` | Do not restart after crash |
| `--max-restarts <n>` | Max crash retries (default: **3**) |
| `--max-cpu <percent>` | Restart if CPU exceeds this % |
| `--max-memory <mb>` | Restart if memory exceeds this MB |
| `--log <path>` | Combined log file |
| `--out <path>` | Stdout log file |
| `--error <path>` | Stderr log file |
| `--time` | Timestamp each log line |

```powershell
nexarun start dotnet --name myapi --args "run --project ." --cwd "C:\Projects\MyApi"
nexarun start npm --name mysite --args "run start" --cwd "C:\Projects\my-site"
nexarun start node --name worker --args "server.js" --cwd "C:\Projects\worker"
nexarun start python --name bot --args "app.py" --cwd "C:\Projects\bot"
```

### `nexarun import`

Import one or more processes from a JSON file. See [Process JSON](#process-json-nexarun-processesjson) below.

```powershell
nexarun import <file.json> [options]
```

| Option | Description |
|--------|-------------|
| `--only <name>` | Import and start only this app name |
| `--no-start` | Register only; do not start processes |

```powershell
nexarun import nexarun-processes.json
nexarun import C:\deploy\processes.json --only api
nexarun import nexarun-processes.json --no-start
```

### `nexarun list`

```powershell
nexarun list
```

Shows id, name, status, pid, restarts, memory, and uptime.

### `nexarun stop` / `restart` / `delete`

```powershell
nexarun stop <name>
nexarun restart <name>
nexarun delete <name>
```

### `nexarun logs`

```powershell
nexarun logs <name> [options]
```

| Option | Default | Description |
|--------|---------|-------------|
| `--lines <n>` | `50` | Lines to show |
| `--out` | — | Stdout log only |
| `--err` | — | Stderr log only |
| `--follow` | — | Stream logs (poll every 1s; Ctrl+C to exit) |

```powershell
nexarun logs myapi
nexarun logs myapi --lines 200 --err
nexarun logs myapi --follow
```

Default log path: `%APPDATA%\NexaRun\logs\<name>.log`

### `nexarun daemon`

```powershell
nexarun daemon start
nexarun daemon stop
```

Starts or stops the Windows service `NexaRunDaemon` (or the dev daemon exe when not installed).

### Quick reference

| Command | Example |
|---------|---------|
| Start | `nexarun start dotnet --name api --args "run" --cwd C:\Api` |
| Import | `nexarun import nexarun-processes.json` |
| List | `nexarun list` |
| Stop | `nexarun stop api` |
| Restart | `nexarun restart api` |
| Delete | `nexarun delete api` |
| Logs | `nexarun logs api --follow` |
| Daemon | `nexarun daemon start` |

```powershell
nexarun --help
nexarun start --help
nexarun import --help
```

More detail: **[CLI-GUIDE.md](CLI-GUIDE.md)**

## Process JSON (`nexarun-processes.json`)

Import/export uses **JSON only** (no `.js` ecosystem files). Example in the repo root: `nexarun-processes.json`.

```json
{
  "version": 1,
  "apps": [
    {
      "name": "api",
      "script": "dotnet",
      "arguments": "run --project Api",
      "workingDirectory": "C:\\Projects\\MyApi",
      "autoRestart": true,
      "maxRestartAttempts": 3,
      "maxMemoryMb": 512,
      "outLogFile": "./logs/api-out.log",
      "errorLogFile": "./logs/api-err.log",
      "logFile": "./logs/api-combined.log",
      "logTimestamps": true,
      "environment": { "ASPNETCORE_ENVIRONMENT": "Development" }
    }
  ]
}
```

| Field | Description |
|---|---|
| `name` | Process name |
| `script` | Executable or command |
| `arguments` | Command-line arguments |
| `workingDirectory` | Working folder |
| `autoRestart` | Restart on crash |
| `maxRestartAttempts` | Max crash retries (default 3) |
| `maxMemoryMb` / `maxCpuPercent` | Restart when exceeded |
| `outLogFile` / `errorLogFile` / `logFile` | Log paths |
| `logTimestamps` | Timestamp each log line |
| `environment` | Env vars (object) |

---

## Process Logs

Process definitions and logs are stored under `%APPDATA%\NexaRun\` (`processes.json`, `history.json`, `logs\`). Legacy `%USERPROFILE%\.nexarun\` data is migrated automatically on first run.

By default, output is saved to `%APPDATA%\NexaRun\logs\<name>.log`. Configure log paths in the JSON file or with `--out` / `--error` / `--log` on `nexarun start`. Log files rotate at 10 MB (`.log.1` backup). Use `logTimestamps` or `--time` for timestamp prefixes.

---

## Auto-Restart

Crashed processes restart automatically with exponential backoff (1s, 2s, 4s, 8s, up to 30s max) so a tight crash loop doesn't hammer the system. By default NexaRun retries **3 times**; after that the process is marked errored until you start it again. Override with `--max-restarts` on the CLI. If a process stays up for 30 seconds, the restart counter resets.

If you set a CPU or memory limit, NexaRun will also restart the process when it exceeds the limit. The reason is logged to the process log file.

---

## Copyright

Copyright © Skybin Technology Private Limited. All rights reserved.
