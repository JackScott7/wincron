<p align="center">
  <img src="assets/wincron-icon.png" alt="WinCron calendar and terminal icon" width="180">
</p>

<h1 align="center">WinCron</h1>

<p align="center">
  Classic cron-style job scheduling for Windows, built with modern C# and .NET 10.
</p>

WinCron reads a familiar five-field crontab, calculates future occurrences without per-second polling, and executes commands through the Windows command shell. It supports cron ranges, lists, steps, named months and weekdays, scoped environment variables, working directories, DST-aware scheduling, and structured execution logs.

## Features

- Classic five-field cron expressions.
- Lists, ranges, wildcard steps, range steps, and named months/weekdays.
- Sunday represented by either `0` or `7`.
- Classic day-of-month/day-of-week matching semantics.
- Local-time scheduling with explicit daylight-saving behavior.
- Next-occurrence calculation instead of continuous polling.
- Scoped `KEY=VALUE` environment assignments.
- Per-job working directories.
- Shell-compatible Windows command execution through `%COMSPEC%`.
- Captured exit code, standard output, standard error, timestamps, and duration.
- Graceful shutdown with child-process-tree cleanup.
- Per-job overlap policies, execution timeouts, and bounded captured output.
- Live job output with rotating structured JSON logs.
- Single-instance protection for each configuration file.
- Automatic, atomic configuration reload with last-known-good fallback.
- Foreground and native Windows Service hosting modes.
- Stable user-defined job identifiers.
- Unit and integration test coverage.
- Validation rejects schedules that can never produce a calendar occurrence.
- Empty configurations keep the scheduler idle and ready instead of terminating unexpectedly.

## Requirements

- Windows x64.
- The release installer includes the .NET runtime; no SDK or separate runtime is required.
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) is required only to build from source.

## Install

Download `wincron-setup.exe` from the matching GitHub Release and run it as an administrator. The installer:

- Installs the self-contained executable under `C:\Program Files\WinCron` and adds it to the machine `PATH`.
- Creates `C:\ProgramData\WinCron\config.wc` only when it does not already exist.
- Preserves configuration and logs during upgrades and uninstall.
- Installs the automatic `WinCron` Windows Service with restart-on-failure recovery.
- Starts WinCron at boot without requiring an interactive login.

The service runs as LocalSystem, so every configured command runs with machine-level privileges. Only administrators should be allowed to modify `C:\ProgramData\WinCron\config.wc`. Use foreground mode under a normal user account when elevated execution is not required.

## Quick start

Build the project:

```powershell
dotnet build WinCron.sln --configuration Release
```

Create `%USERPROFILE%\wincron\config.wc`:

```text
# Every five minutes
*/5 * * * * echo WinCron is running

# Weekdays at 09:30
30 9 * * MON-FRI powershell.exe -NoProfile -File C:\jobs\report.ps1
```

Start WinCron:

```powershell
dotnet run --project WinCron.csproj
```

Press `Ctrl+C` for graceful shutdown.

Changes saved to the active `config.wc` are validated and reloaded automatically. If a replacement file is invalid or temporarily unavailable, WinCron reports the error and continues using the last valid configuration.

## Command line

Running `wincron` without arguments starts the scheduler with `%USERPROFILE%\wincron\config.wc`.

| Argument | Behavior |
| --- | --- |
| `--config <path>` | Use a different configuration file. `--config=<path>` is also accepted. |
| `-T`, `--test` | Validate the selected configuration and exit without running jobs. |
| `-l`, `--list` | Print the selected configuration and exit. |
| `-V`, `--version` | Print the WinCron version and exit. |
| `-h`, `--help` | Print usage information and exit. |

`--config` can be combined with normal execution, `--test`, or `--list`:

```powershell
wincron --config C:\jobs\custom.wc
wincron --test --config C:\jobs\custom.wc
wincron --list --config C:\jobs\custom.wc
```

Successful operations return exit code `0`, invalid configurations or runtime failures return `1`, and invalid command-line usage returns `2`.

`--test` and `--list` are read-only. When an explicitly selected configuration file does not exist, WinCron reports an error and does not create the file. Normal scheduler startup continues to create the default configuration on first use.

WinCron currently uses its documented UTC-instant DST policy: nonexistent spring-forward local minutes are skipped, while repeated fall-back minutes run for both UTC instants. This differs from cron implementations that compensate fixed-time jobs across short clock changes.

## Configuration

Each job contains five schedule fields followed by its command:

```text
minute hour day-of-month month day-of-week command
```

Environment assignments affect subsequent jobs:

```text
REPORT_MODE=daily
WINCRON_WORKING_DIRECTORY=C:\jobs\reports
0 8 * * MON-FRI powershell.exe -NoProfile -File .\report.ps1
```

Execution controls are also scoped to subsequent jobs:

```text
# Allow, Skip, QueueOne, or TerminatePrevious; Skip is the default.
WINCRON_OVERLAP_POLICY=QueueOne

# Positive timeout in seconds and captured characters per output stream.
WINCRON_TIMEOUT_SECONDS=900
WINCRON_MAX_OUTPUT_CHARACTERS=1048576

*/5 * * * * powershell.exe -NoProfile -File C:\jobs\report.ps1
```

Use a stable identifier before a job when its identity must survive line movement during configuration reload:

```text
WINCRON_JOB_ID=nightly-backup
0 2 * * * powershell.exe -NoProfile -File C:\jobs\backup.ps1
```

Identifiers contain 1-64 letters, digits, dots, underscores, or hyphens, must start with a letter or digit, and must be unique.

See [WinCron configuration](docs/CONFIGURATION.md) for the complete grammar, matching rules, environment behavior, working-directory convention, DST policy, and logging details.

## Logs

Standard output and standard error are forwarded to the terminal while a job runs. When it finishes, WinCron prints its command, outcome, and duration. The bounded captured output is appended as a JSON object to:

```text
%USERPROFILE%\wincron\output\runs.jsonl
```

Records include the scheduled occurrence, start and completion times, duration, command, exit code, bounded standard output and error, truncation flags, cancellation and timeout state, and process-start errors. `runs.jsonl` rotates at 10 MiB and retains five rotated files by default. Logs are stored in an `output` directory beside the selected configuration file.

## Testing

```powershell
dotnet test WinCron.sln --configuration Release
```

The suite covers parsing, validation, object initialization, environment scoping, cron matching, DST transitions, clock jumps, duplicate prevention, multi-job timing, command execution, working directories, output capture, errors, and structured logging.

## Project structure

```text
Configuration/  Configuration loading and crontab parsing
Domain/         Immutable cron fields, expressions, and job definitions
Execution/      Command-shell execution and structured logging
Hosting/        Windows Service and Generic Host integration
Scheduling/     Next-occurrence calculation and dispatch loop
installer/      Inno Setup definition and initial service configuration
.github/        Windows CI and signed release workflows
tests/          Unit and integration tests
```

The implementation status and completed milestones are recorded in [ROADMAP.md](ROADMAP.md). Release history is available in [CHANGELOG.md](CHANGELOG.md).

Release maintainers should also read [Releasing WinCron](docs/RELEASING.md) for signing secrets, tag/version requirements, installer generation, and deployment recording.

## Windows Service hosting

`--service` is an internal host switch intended for the Windows Service Control Manager. A service installation should always provide a machine-level configuration path:

```powershell
sc.exe create WinCron start= auto binPath= '"C:\Program Files\WinCron\wincron.exe" --service --config "C:\ProgramData\WinCron\config.wc"'
sc.exe failure WinCron reset= 86400 actions= restart/5000/restart/15000/restart/60000
sc.exe start WinCron
```

The release installer automates this setup, service recovery, upgrades, and removal. Foreground use remains non-administrative and backward compatible.

## Release signing

Tagged release workflows require the `WINDOWS_SIGNING_CERTIFICATE_BASE64` and `WINDOWS_SIGNING_CERTIFICATE_PASSWORD` repository secrets. The workflow Authenticode-signs both `wincron.exe` and `wincron-setup.exe`, verifies the signatures, and publishes a SHA-256 checksum. Manual workflow runs can build unsigned test artifacts but do not publish a release.
