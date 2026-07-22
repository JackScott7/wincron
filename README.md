<p align="center">
  <img src="assets/wincron-icon.png" alt="WinCron calendar and terminal icon" width="180">
</p>

<h1 align="center">WinCron</h1>

<p align="center">
  <a href="https://github.com/JackScott7/wincron/actions/workflows/ci.yml">
    <img src="https://img.shields.io/github/actions/workflow/status/JackScott7/wincron/ci.yml?branch=main&label=CI&logo=githubactions" alt="CI">
  </a>
  <a href="https://github.com/JackScott7/wincron/actions/workflows/release.yml">
    <img src="https://img.shields.io/github/actions/workflow/status/JackScott7/wincron/release.yml?label=Release&logo=githubactions" alt="Release">
  </a>
  <a href="https://github.com/JackScott7/wincron/releases/latest">
    <img src="https://img.shields.io/github/v/release/JackScott7/wincron?label=Version&logo=github" alt="Latest version">
  </a>
  <a href="https://github.com/JackScott7/wincron/blob/main/LICENSE">
    <img src="https://img.shields.io/github/license/JackScott7/wincron?label=License" alt="License">
  </a>
</p>

<p align="center">
  <strong>Run cron-style jobs on Windows without rebuilding every schedule in Task Scheduler.</strong>
</p>

WinCron brings familiar five-field cron scheduling to Windows. Keep your existing scripts and cron-style timing, execute them through the Windows command shell, and run them continuously as a native Windows Service.

One text file defines the schedule. WinCron validates it, reloads changes automatically, executes each command, and records what happened.

## Why WinCron?

Moving an automation script to Windows should not require translating its schedule into a collection of Task Scheduler dialogs, triggers, and actions. WinCron gives developers and operators a simple path: copy the command, keep the familiar schedule, and run it as a managed Windows daemon.

| | Windows Task Scheduler | Linux cron | WinCron |
| --- | --- | --- | --- |
| Primary interface | GUI, PowerShell, or XML | Crontab text | Crontab-style text |
| Five-field cron expressions | Requires translation | Native | Native supported subset |
| Existing Windows commands and scripts | Native | Requires a compatibility layer | Native through `%COMSPEC%` |
| Starts automatically at boot | Yes | Yes | Yes, as a Windows Service |
| Applies text-file changes automatically | Task must be recreated or updated | Yes | Yes, with last-known-good fallback |
| Execution history | Windows event/task history | Syslog or mail | Live terminal output and rotating JSON Lines logs |
| Best fit | Windows-specific task orchestration | Unix-like systems | Developers bringing cron-style automation to Windows |

## Quick start

### 1. Install

Download the latest [`wincron-setup.exe`](https://github.com/JackScott7/wincron/releases/latest/download/wincron-setup.exe) and run it as an administrator.

The installer includes the .NET runtime, adds `wincron` to the machine `PATH`, creates the service configuration, and starts WinCron automatically. No SDK or separate runtime is required.

### 2. Add a job

Edit `C:\ProgramData\WinCron\config.wc`:

```text
# Every five minutes
*/5 * * * * echo WinCron is running

# Weekdays at 09:30
30 9 * * MON-FRI powershell.exe -NoProfile -File C:\jobs\report.ps1
```

### 3. Validate it

```powershell
wincron --test --config C:\ProgramData\WinCron\config.wc
```

Save the file and you are done. The running service validates and activates the complete replacement configuration automatically. If an edit is invalid or temporarily incomplete, WinCron reports it and keeps the last valid schedule running.

To run interactively under your own account instead:

```powershell
wincron --config "$env:USERPROFILE\wincron\config.wc"
```

## Built to schedule, not to poll

> WinCron calculates the next occurrence for each job, orders upcoming work by UTC instant, and sleeps until something is due. It does not wake every second to scan the entire configuration.

That design keeps idle work low, avoids depending on an exact “second zero” polling tick, and gives clock changes, missed occurrences, and duplicate prevention explicit scheduling semantics.

## Features

### Scheduling

- Five-field cron expressions with exact values, wildcards, lists, ranges, steps, and named months or weekdays.
- Sunday represented by either `0` or `7`, with classic day-of-month/day-of-week matching.
- Local-time scheduling with documented daylight-saving and misfire behavior.
- Calculated next occurrences, stable job identifiers, and impossible-schedule validation.

### Execution

- Windows shell execution through `%COMSPEC%` with the original command preserved.
- Scoped environment assignments and per-job working directories.
- `Allow`, `Skip`, `QueueOne`, and `TerminatePrevious` overlap policies.
- Per-job timeouts, live stdout/stderr, bounded capture, exit codes, timestamps, and duration.

### Reliability

- Native Windows Service hosting, automatic startup, and restart-on-failure recovery.
- Atomic configuration reload with last-known-good fallback.
- Single-instance protection for each normalized configuration path.
- Graceful cancellation, child-process-tree cleanup, rotating JSON logs, and isolated log sinks.

### Developer experience

- One readable configuration file instead of generated task definitions.
- Read-only configuration validation and listing commands.
- Self-contained x64 installer with no end-user .NET dependency.
- Modern C# modules, strict analyzers, Windows CI, and unit/integration coverage.

## Command line

Running `wincron` without arguments starts the foreground scheduler with `%USERPROFILE%\wincron\config.wc`.

| Argument | Behavior |
| --- | --- |
| `--config <path>` | Use a different configuration file. `--config=<path>` is also accepted. |
| `-T`, `--test` | Validate the selected configuration and exit without running jobs. |
| `-l`, `--list` | Print the selected configuration and exit. |
| `-V`, `--version` | Print the WinCron version and exit. |
| `-h`, `--help` | Print usage information and exit. |

Examples:

```powershell
wincron --config C:\jobs\custom.wc
wincron --test --config C:\jobs\custom.wc
wincron --list --config C:\jobs\custom.wc
```

Successful operations return `0`, invalid configurations or runtime failures return `1`, and invalid command-line usage returns `2`.

`--test` and `--list` are read-only. They report a missing explicitly selected file without creating it. Normal foreground startup retains backward-compatible first-run creation of `%USERPROFILE%\wincron\config.wc`.

## Configuration

Each job has five schedule fields followed by its command:

```text
minute hour day-of-month month day-of-week command
```

Environment and execution settings apply to subsequent jobs:

```text
WINCRON_JOB_ID=weekday-report
WINCRON_WORKING_DIRECTORY=C:\jobs\reports
WINCRON_OVERLAP_POLICY=QueueOne
WINCRON_TIMEOUT_SECONDS=900
WINCRON_MAX_OUTPUT_CHARACTERS=1048576
REPORT_MODE=daily

0 8 * * MON-FRI powershell.exe -NoProfile -File .\report.ps1
```

Job identifiers contain 1–64 letters, digits, dots, underscores, or hyphens, start with a letter or digit, and must be unique.

See [WinCron configuration](docs/CONFIGURATION.md) for the complete grammar, environment scoping, working-directory behavior, execution controls, DST policy, and reload semantics.

## Logs

WinCron forwards stdout and stderr while a foreground job runs and records every completed execution as JSON Lines. Logs live in an `output` directory beside the selected configuration:

```text
%USERPROFILE%\wincron\output\runs.jsonl
C:\ProgramData\WinCron\output\runs.jsonl
```

Records contain job identity, command, scheduled/start/completion times, duration, exit code, bounded output, truncation flags, cancellation state, timeout state, and startup errors. The active log rotates at 10 MiB and retains five rotated files by default.

## Service security

> [!IMPORTANT]
> The release installer runs the WinCron service as LocalSystem so jobs can run at boot without an interactive user. Commands therefore execute with machine-level privileges. Only administrators should be able to modify `C:\ProgramData\WinCron\config.wc`.

Use foreground mode under a normal user account when elevated execution is not required.

The installer places WinCron under `C:\Program Files\WinCron`, preserves configuration and logs during upgrades or uninstall, configures service recovery, and removes the executable and service registration cleanly.

## Build from source

Requirements:

- Windows x64.
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
dotnet restore WinCron.sln
dotnet build WinCron.sln --configuration Release --no-restore
dotnet test WinCron.sln --configuration Release --no-build
dotnet run --project WinCron.csproj
```

Create a self-contained executable:

```powershell
dotnet publish WinCron.csproj --configuration Release --runtime win-x64 --self-contained true
```

The suite covers parsing, validation, object initialization, cron matching, DST transitions, clock jumps, reload behavior, overlap policies, timeouts, command execution, output capture, service lifecycle, configuration files, and structured logging.

## Project structure

```text
Application/    CLI orchestration and single-instance ownership
CommandLine/    Argument parsing, modes, and usage text
Configuration/  Configuration loading, parsing, and file watching
Domain/         Immutable cron expressions, jobs, and execution policies
Execution/      Shell execution, bounded output, and structured logging
Hosting/        Windows Service and Generic Host integration
Scheduling/     Next-occurrence calculation, reload, and dispatch control
installer/      Inno Setup definition and initial service configuration
.github/        Windows CI and signed release workflows
tests/          Unit and integration tests
```

## Project status and releases

The implementation status and remaining work are tracked honestly in [ROADMAP.md](ROADMAP.md). Release history is in [CHANGELOG.md](CHANGELOG.md), and maintainers can follow [Releasing WinCron](docs/RELEASING.md) for signing, versioning, installer generation, and GitHub publishing.

Tagged release workflows require the `WINDOWS_SIGNING_CERTIFICATE_BASE64` and `WINDOWS_SIGNING_CERTIFICATE_PASSWORD` repository secrets. They Authenticode-sign `wincron.exe` and `wincron-setup.exe`, verify both signatures, and publish a SHA-256 checksum.

## License

WinCron is available under the [MIT License](LICENSE).
