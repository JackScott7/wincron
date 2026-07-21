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
- Unit and integration test coverage.
- Validation rejects schedules that can never produce a calendar occurrence.
- Empty configurations keep the scheduler idle and ready instead of terminating unexpectedly.

## Requirements

- Windows x64.
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) to build from source.

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

See [WinCron configuration](docs/CONFIGURATION.md) for the complete grammar, matching rules, environment behavior, working-directory convention, DST policy, and logging details.

## Logs

Standard output and standard error are forwarded to the terminal while a job runs. When it finishes, WinCron prints its command, outcome, and duration. The bounded captured output is appended as a JSON object to:

```text
%USERPROFILE%\wincron\output\runs.jsonl
```

Records include the scheduled occurrence, start and completion times, duration, command, exit code, bounded standard output and error, truncation flags, cancellation and timeout state, and process-start errors. `runs.jsonl` rotates at 10 MiB and retains five rotated files by default.

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
Scheduling/     Next-occurrence calculation and dispatch loop
tests/          Unit and integration tests
```

The implementation status and completed milestones are recorded in [ROADMAP.md](ROADMAP.md). Release history is available in [CHANGELOG.md](CHANGELOG.md).
