# NexaRun CLI Guide

`nexarun` is the command-line tool for NexaRun on Windows. It talks to the **NexaRun Daemon** (Windows service) over a local named pipe. Most commands do nothing useful if the daemon is not running.

## Prerequisites

| Requirement | Notes |
|-------------|--------|
| **Daemon running** | Installed service `NexaRunDaemon`, or `nexarun daemon start` / `dotnet run --project NexaRun.Daemon` in dev |
| **CLI on PATH** | After install: `nexarun` from any terminal. Dev: `dotnet run --project NexaRun.Cli -- <command>` |
| **Data folder** | `%APPDATA%\NexaRun\` — `processes.json`, `history.json`, `logs\` |

If the daemon is down, you will see:

```text
Daemon is not running. Start it with: nexarun daemon start
```

---

## Command overview

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
    ├── start              Start daemon (Windows service or dev exe)
    └── stop               Stop daemon service
```

---

## `nexarun start`

Start a **single** process and register it with the daemon.

```powershell
nexarun start <executable> [options]
```

### Arguments

| Argument | Description |
|----------|-------------|
| `executable` | Command or program (`dotnet`, `npm`, `node`, `python`, path to `.exe`, etc.) |

### Options

| Option | Description |
|--------|-------------|
| `--name <name>` | Process name (default: executable file name) |
| `--args "<args>"` | Arguments passed to the executable |
| `--cwd <path>` | Working directory |
| `--no-autorestart` | Do not restart after crash |
| `--max-restarts <n>` | Max crash retries (default: **3**) |
| `--max-cpu <percent>` | Restart if CPU exceeds this % |
| `--max-memory <mb>` | Restart if memory exceeds this many MB |
| `--log <path>` | Combined log file |
| `--out <path>` | Stdout-only log file |
| `--error <path>` | Stderr-only log file |
| `--time` | Prefix log lines with timestamps |

### Examples

```powershell
# .NET API
nexarun start dotnet --name myapi --args "run --project ." --cwd "C:\Projects\MyApi"

# Next.js / Node
nexarun start npm --name mysite --args "run start" --cwd "C:\Projects\my-site"
nexarun start node --name worker --args "server.js" --cwd "C:\Projects\worker"

# Python
nexarun start python --name bot --args "app.py" --cwd "C:\Projects\bot"

# Logs + limits
nexarun start dotnet --name api --args "run" --cwd "C:\Api" ^
  --log "C:\logs\api.log" --max-memory 512 --max-restarts 5 --time
```

Commands like `npm` and `dotnet` are resolved via `cmd /c` (same as a normal terminal).

---

## `nexarun import`

Import one or more processes from a **JSON file** (export format or hand-written).

```powershell
nexarun import <file.json> [options]
```

### Arguments

| Argument | Description |
|----------|-------------|
| `file.json` | Path to `nexarun-processes.json` or any NexaRun export |

### Options

| Option | Description |
|--------|-------------|
| `--only <name>` | Import and start only the app with this `name` |
| `--no-start` | Register definitions only; do not start processes |

### JSON format

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
      "outLogFile": "./logs/out.log",
      "errorLogFile": "./logs/err.log",
      "logFile": "./logs/combined.log",
      "logTimestamps": true,
      "environment": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

| Field | Description |
|-------|-------------|
| `name` | Unique process name |
| `script` | Executable / command |
| `arguments` | Command-line arguments |
| `workingDirectory` | Working folder (use full paths when importing on another PC) |
| `autoRestart` | Restart on unexpected exit (default `true`) |
| `maxRestartAttempts` | Crash retries before status **Errored** (default `3`) |
| `maxMemoryMb` / `maxCpuPercent` | Restart when limit exceeded |
| `outLogFile` / `errorLogFile` / `logFile` | Log file paths |
| `logTimestamps` | Timestamp each log line |
| `environment` | Key/value env vars |

See `nexarun-processes.json` in the repo for a sample.

### Examples

```powershell
nexarun import nexarun-processes.json
nexarun import C:\deploy\nexarun-processes.json --only api
nexarun import processes.json --no-start
```

---

## `nexarun list`

Show all managed processes (table: id, name, status, pid, restarts, memory, uptime).

```powershell
nexarun list
```

---

## `nexarun stop`

Gracefully stop a running process (kills process tree).

```powershell
nexarun stop <name>
```

```powershell
nexarun stop myapi
```

---

## `nexarun restart`

Stop then start a process with its saved settings.

```powershell
nexarun restart <name>
```

```powershell
nexarun restart myapi
```

---

## `nexarun delete`

Remove a process from the managed list. Stops it first if it is running.

```powershell
nexarun delete <name>
```

```powershell
nexarun delete myapi
```

---

## `nexarun logs`

Print recent log output for a process.

```powershell
nexarun logs <name> [options]
```

### Options

| Option | Default | Description |
|--------|---------|-------------|
| `--lines <n>` | `50` | Number of lines to show |
| `--out` | — | Stdout log file only |
| `--err` | — | Stderr log file only |
| `--follow` | — | Poll and refresh logs every second (Ctrl+C to exit) |

Default log path: `%APPDATA%\NexaRun\logs\<name>.log`

### Examples

```powershell
nexarun logs myapi
nexarun logs myapi --lines 200
nexarun logs myapi --err
nexarun logs myapi --follow
```

---

## `nexarun daemon`

Control the background daemon (Windows service when installed).

```powershell
nexarun daemon start
nexarun daemon stop
```

| Subcommand | Behavior |
|------------|----------|
| `start` | Starts Windows service `NexaRunDaemon`, or launches `NexaRun.Daemon.exe` next to the CLI in dev layouts |
| `stop` | Stops the Windows service |

After install, the service is usually already running and set to start with Windows.

---

## Typical workflows

### Dev machine (first time)

```powershell
# Terminal 1 — daemon
dotnet run --project NexaRun.Daemon

# Terminal 2 — CLI
dotnet run --project NexaRun.Cli -- list
dotnet run --project NexaRun.Cli -- import nexarun-processes.json
dotnet run --project NexaRun.Cli -- logs api --follow
```

### Installed NexaRun

```powershell
nexarun daemon start
nexarun import nexarun-processes.json
nexarun list
nexarun logs api
```

### Move config to another PC

1. On source machine: export via tray **Export JSON** or copy `nexarun-processes.json`.
2. On target machine: edit `workingDirectory` and paths for the new machine.
3. Run `nexarun import nexarun-processes.json`.

### Quick single app (no JSON file)

```powershell
nexarun start dotnet --name gateway --args "run --project Gateway" --cwd "C:\Apps\Gateway"
```

---

## Auto-restart behavior

- Crashed processes restart with backoff: 1s → 2s → 4s → 8s (max **30s** between attempts).
- Default **3** crash retries (`--max-restarts` or JSON `maxRestartAttempts`); then status becomes **Errored**.
- Counter resets after **30 seconds** of stable uptime.
- CPU/memory limits trigger a **restart** (not throttle); reason is written to the log.

---

## Exit codes

| Code | Meaning |
|------|---------|
| `0` | Success |
| `1` | Error (daemon down, process not found, import/validation failed, etc.) |

---

## Help

Built-in help (System.CommandLine):

```powershell
nexarun --help
nexarun start --help
nexarun import --help
nexarun logs --help
```

---

## Related docs

- [README.md](README.md) — install, tray app, architecture
- `nexarun-processes.json` — sample import file
