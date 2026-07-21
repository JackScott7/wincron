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
- Unit and integration test coverage.

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

See [WinCron configuration](docs/CONFIGURATION.md) for the complete grammar, matching rules, environment behavior, working-directory convention, DST policy, and logging details.

## Logs

When a job finishes, WinCron prints its command, exit status, duration, standard output, and standard error in the terminal. The same execution is appended as a JSON object to:

```text
%USERPROFILE%\wincron\output\runs.jsonl
```

Records include the scheduled occurrence, start and completion times, duration, command, exit code, standard output, standard error, cancellation state, and process-start errors.

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
