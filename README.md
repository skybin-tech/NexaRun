# NexaRun

NexaRun is a process manager for Windows — keep your apps running forever. Start any process (Node.js, .NET, Python, anything), and NexaRun will monitor it, restart it if it crashes, and enforce CPU and memory limits. Manage everything from a clean system tray GUI or the command line.

---

## Installation

Run `NexaRun-Setup.exe` and follow the wizard.

The installer will:
- Install the **NexaRun Daemon** as a Windows Service that starts automatically with Windows
- Add the **`nexarun` CLI** to your system PATH
- Launch the **NexaRun tray app** so you can start managing processes immediately

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

Right-click tray → **Dashboard** to see a 7-day history for any process — uptime bar chart and a full run history with start time, duration, and outcome.

---

## CLI

The `nexarun` command is available in any terminal after installation.

```bash
# Add and start a process
nexarun start dotnet --name myapi --args "run --project ." --cwd "C:\Projects\MyApi"
nexarun start npm --name mysite --args "run start" --cwd "C:\Projects\my-site"

# Manage processes
nexarun list
nexarun stop myapi
nexarun restart myapi
nexarun logs myapi
nexarun logs myapi --lines 100
nexarun delete myapi

# Daemon control
nexarun daemon start
nexarun daemon stop
```

---

## Process Logs

Every process's output is saved to `%USERPROFILE%\.nexarun\logs\<name>.log` with timestamps. Open it any time with the **Logs** button or the `nexarun logs` command.

---

## Auto-Restart

Crashed processes restart automatically with exponential backoff (1s, 2s, 4s, 8s, up to 30s max) so a tight crash loop doesn't hammer the system.

If you set a CPU or memory limit, NexaRun will also restart the process when it exceeds the limit. The reason is logged to the process log file.

---

## Copyright

Copyright © Skybin Technology Private Limited. All rights reserved.
