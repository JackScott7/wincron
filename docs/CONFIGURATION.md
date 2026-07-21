# WinCron configuration

WinCron reads `%USERPROFILE%\wincron\config.wc`. If the `wincron` directory or configuration file does not exist, WinCron creates it. Saved changes are validated and reloaded automatically; an invalid or temporarily unavailable replacement leaves the last valid configuration active.

Reload replaces the scheduled-job set atomically. Already running commands are allowed to finish under their original definitions; future occurrences use the replacement configuration. An overlap-controlled execution already tracked by a stable job identifier continues to participate in that identifier's concurrency policy.

For compatibility, when the new configuration is missing and `%USERPROFILE%\config.wc` exists, WinCron copies the legacy file into the `wincron` directory. The original file is preserved.

Each job has five schedule fields followed by the command. The command is retained exactly from its first non-whitespace character onward.

```text
minute hour day-of-month month day-of-week command
```

| Field | Values | Names |
| --- | --- | --- |
| Minute | `0-59` | — |
| Hour | `0-23` | — |
| Day of month | `1-31` | — |
| Month | `1-12` | `JAN-DEC` |
| Day of week | `0-7` | `SUN-SAT` |

Day-of-week values `0` and `7` both mean Sunday. Names are case-insensitive.

Fields support:

- `*` for every allowed value.
- Exact values, such as `15`.
- Lists, such as `1,2,5` or `MON,WED,FRI`.
- Inclusive ranges, such as `1-5` or `MON-FRI`.
- Steps across a wildcard or range, such as `*/5` or `1-10/2`.

Blank lines and lines whose first non-whitespace character is `#` are ignored.

## Examples

```text
# Every five minutes
*/5 * * * * echo periodic job

# At 09:30, Monday through Friday
30 9 * * MON-FRI powershell.exe -NoProfile -File C:\jobs\report.ps1

# At midnight on the first day of January
0 0 1 JAN * C:\jobs\annual-backup.cmd
```

## Environment variables and working directory

An environment assignment applies to every job following it, until another assignment replaces that value:

```text
REPORT_MODE=daily
WINCRON_WORKING_DIRECTORY=C:\jobs\reports
0 8 * * MON-FRI powershell.exe -NoProfile -File .\report.ps1

REPORT_MODE=weekly
0 8 * * MON powershell.exe -NoProfile -File .\report.ps1
```

`WINCRON_WORKING_DIRECTORY` sets the working directory for subsequent jobs and is also available to their processes. Without it, jobs use the current user's profile directory.

## Execution controls

The following assignments also apply to subsequent jobs:

| Variable | Values | Default |
| --- | --- | --- |
| `WINCRON_OVERLAP_POLICY` | `Allow`, `Skip`, `QueueOne`, or `TerminatePrevious` | `Skip` |
| `WINCRON_TIMEOUT_SECONDS` | Positive number of seconds | `3600` |
| `WINCRON_MAX_OUTPUT_CHARACTERS` | Positive captured-character limit for each output stream | `1048576` |
| `WINCRON_JOB_ID` | Unique 1-64 character stable identifier | Source line identifier |

`QueueOne` keeps only the most recently scheduled pending occurrence. `TerminatePrevious` cancels the active execution and starts its replacement after termination completes. These control variables remain available in the child-process environment for backward compatibility with environment assignment behavior.

`WINCRON_JOB_ID` gives a job stable identity across line movement and live reload. Set it before each named job; duplicate or malformed identifiers reject the complete reload.

## Scheduling semantics

- Schedules use the Windows local time zone. Internally, occurrences are ordered as UTC instants.
- Minute, hour, and month must always match.
- If either day-of-month or day-of-week is unrestricted, the restricted field must match.
- If both day fields are restricted, a job runs when either field matches, following classic cron behavior.
- A local minute skipped by the spring DST transition does not run.
- A local minute repeated by the fall DST transition runs once for each corresponding UTC instant.
- WinCron skips occurrences older than its one-minute misfire grace period after a forward clock jump. It does not backfill downtime.
- Each job occurrence is identified by job and UTC instant, preventing duplicate dispatch within the same minute.

## Command execution and logs

Commands run through `%COMSPEC%`—normally `cmd.exe`—using `/D /S /C`. Different jobs may run concurrently, while each job follows its configured overlap policy. A named lock prevents two WinCron processes from scheduling the same normalized configuration path.

Every execution records:

- Scheduled, start, and completion timestamps.
- Duration and exit code.
- Bounded standard output and standard error with truncation flags.
- Cancellation, timeout, or process-start error state.

Standard output and error are streamed to the WinCron terminal while jobs run. Completed jobs print their outcome and duration. Records are appended as one JSON object per line to `%USERPROFILE%\wincron\output\runs.jsonl`; the active log rotates at 10 MiB and retains five rotated files. Press `Ctrl+C` to stop WinCron; active child process trees are terminated with a bounded wait.

## Command-line arguments

WinCron starts the scheduler when invoked without arguments. The following arguments are supported:

```text
wincron [--config <path>]
wincron (-T | --test) [--config <path>]
wincron (-l | --list) [--config <path>]
wincron (-V | --version)
wincron (-h | --help)
```

- `--config <path>` or `--config=<path>` selects a non-default configuration.
- `-T` or `--test` validates the selected configuration and exits without executing jobs.
- `-l` or `--list` writes the selected configuration to standard output.
- `-V` or `--version` writes the semantic version.
- `-h` or `--help` writes usage information.

Exit code `0` means success, `1` means configuration or runtime failure, and `2` means invalid command-line usage.

## Build and test

```powershell
dotnet build WinCron.sln --configuration Release
dotnet test WinCron.sln --configuration Release
dotnet run --project WinCron.csproj
```

The test suite covers field parsing, validation, classic day matching, object construction, environment scoping, next-occurrence calculation, DST behavior, clock jumps, duplicate prevention, multi-job timing, shell execution, working directories, output capture, errors, configuration files, and structured logging.
