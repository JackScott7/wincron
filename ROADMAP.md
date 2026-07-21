# WinCron Roadmap

Goal: develop WinCron from a functional foreground scheduler into a reliable, distributable Windows cron daemon with clearly documented compatibility boundaries.

## Status legend

- [x] Implemented and covered by the current project.
- [ ] Planned or still requiring implementation and verification.

## 1. Completed foundation — v1.2.0

### 1.1 Configuration and parsing

- [x] Read the default configuration from `%USERPROFILE%\wincron\config.wc`.
- [x] Create the default configuration directory and file when they do not exist.
- [x] Copy the legacy `%USERPROFILE%\config.wc` file to the new location without deleting the original.
- [x] Support a custom configuration path through `--config <path>` and `--config=<path>`.
- [x] Ignore blank lines and full-line `#` comments.
- [x] Preserve the complete command text following the five schedule fields.
- [x] Report malformed configuration entries with their source line numbers.

### 1.2 Cron grammar

- [x] Support five-field schedules: minute, hour, day of month, month, and day of week.
- [x] Support exact values, wildcards, lists, inclusive ranges, wildcard steps, and range steps.
- [x] Support named months and weekdays.
- [x] Treat Sunday as either `0` or `7`.
- [x] Apply classic day-of-month/day-of-week OR matching when both fields are restricted.

### 1.3 Job environment

- [x] Apply `KEY=VALUE` assignments to subsequent jobs.
- [x] Snapshot the applicable environment for each parsed job.
- [x] Support per-job working directories through `WINCRON_WORKING_DIRECTORY`.

### 1.4 Scheduling

- [x] Calculate future occurrences instead of polling every second.
- [x] Order occurrences by UTC while matching them in the Windows local time zone.
- [x] Prevent duplicate dispatch of the same job occurrence.
- [x] Skip missed occurrences outside the one-minute misfire grace period.
- [x] Handle forward and backward clock changes according to the currently documented WinCron policy.

### 1.5 Command execution and logging

- [x] Execute commands through `cmd.exe /D /S /C`.
- [x] Capture the exit code, standard output, standard error, timestamps, and duration.
- [x] Display completed job output in the foreground WinCron terminal.
- [x] Append structured execution records to `%USERPROFILE%\wincron\output\runs.jsonl`.
- [x] Attempt to terminate active child process trees during shutdown.

### 1.6 Command-line interface

- [x] Run the scheduler when no mode argument is supplied.
- [x] Print help with `-h` or `--help`.
- [x] Print the application version with `-V` or `--version`.
- [x] Validate configuration with `-T` or `--test`.
- [x] Print configuration with `-l` or `--list`.
- [x] Return distinct success, runtime-error, and usage-error exit codes.

### 1.7 Test foundation

- [x] Cover parsing, validation, field matching, object initialization, and environment scoping.
- [x] Cover occurrence calculation, DST transitions, clock jumps, duplicate prevention, and multi-job timing.
- [x] Cover shell execution, working directories, output capture, failures, configuration loading, and logging.

## 2. Correctness and safety fixes — target v1.2.1

### 2.1 Empty and unschedulable configurations

- [x] Keep WinCron alive when the configuration contains zero jobs instead of exiting after printing the running banner.
- [x] Report an explicit idle-state message when no jobs are currently scheduled.
- [x] Detect semantically impossible schedules such as `0 0 31 2 *` during validation.
- [x] Return a clear line-specific error for a schedule that can never produce an occurrence.
- [x] Ensure one impossible job cannot silently disappear while other jobs continue running.
- [x] Add tests for empty, all-impossible, and mixed valid/impossible configurations.

### 2.2 Read-only CLI operations

- [x] Separate configuration creation from configuration reading.
- [x] Ensure `--test` never creates or modifies an explicitly supplied configuration file.
- [x] Ensure `--list` never creates or modifies an explicitly supplied configuration file.
- [x] Return a runtime error when an explicitly supplied configuration file does not exist.
- [x] Preserve first-run creation behavior only for the default scheduler configuration.
- [x] Add filesystem tests proving that validation and listing are read-only.

### 2.3 Scheduler diagnostics

- [x] Report when a job has no future occurrence instead of silently excluding it from the queue.
- [ ] Route scheduler and dispatch errors through an injected logging abstraction rather than writing directly to global `Console.Error`.
- [ ] Ensure a failing diagnostic or log sink does not hide the original scheduler or execution failure.

### 2.4 Shutdown reliability

- [ ] Add a bounded shutdown deadline for active jobs.
- [ ] Avoid waiting indefinitely when operating-system process termination fails or is denied.
- [ ] Record whether shutdown termination succeeded, failed, or timed out.
- [ ] Add integration tests for cancellation, failed termination, and shutdown deadlines.

### 2.5 DST documentation accuracy

- [x] Stop describing the current DST behavior as complete classic-cron parity until compatibility is finalized.
- [x] Clearly distinguish the current WinCron DST policy from Vixie Cron, Debian cron, and Cronie behavior.
- [x] Add regression tests tied to the documented policy so implementation and documentation cannot drift.

## 3. Production execution controls — target v1.3.0

### 3.1 Overlapping job policy

- [x] Detect when a previous execution of the same job is still active.
- [x] Support an explicit per-job overlap policy: `allow`, `skip`, `queue-one`, or `terminate-previous`.
- [x] Select and document a safe default overlap policy.
- [ ] Log every skipped, queued, or terminated overlapping occurrence.
- [x] Prevent queued occurrences from growing without a bound.
- [x] Add concurrency tests for every overlap policy.

### 3.2 Single-instance protection

- [x] Prevent multiple WinCron processes from scheduling the same configuration simultaneously.
- [x] Use a named Windows synchronization primitive derived from a normalized configuration path.
- [x] Return a clear error when another instance owns the same configuration.
- [x] Permit simultaneous instances when they intentionally use different configuration files.
- [ ] Add multi-process tests for mutex acquisition, release, and abandoned ownership.

### 3.3 Execution timeouts

- [x] Support a configurable default job timeout.
- [x] Support an optional per-job timeout override.
- [x] Terminate the complete process tree when a timeout expires.
- [x] Distinguish timeout, user cancellation, startup failure, and nonzero exit status in execution results.
- [x] Add timeout and long-running-command integration tests.

### 3.4 Output handling

- [x] Stream standard output and standard error while a job is running.
- [x] Avoid retaining unlimited child-process output in memory.
- [x] Configure maximum captured output per stream.
- [x] Mark execution records when output has been truncated.
- [x] Preserve concurrent stdout/stderr draining so child processes cannot deadlock on full pipes.
- [x] Add high-volume and never-ending output tests.

### 3.5 Log lifecycle and privacy

- [x] Add configurable maximum log-file size.
- [x] Rotate execution logs without losing complete JSON Lines records.
- [x] Add configurable retention by file count.
- [ ] Recover cleanly from locked, unavailable, or unwritable log directories.
- [ ] Define how commands, environment values, stdout, and stderr are redacted when they may contain secrets.
- [x] Isolate log sinks so one failed sink does not fail all logging.
- [ ] Add rotation, retention, permissions, concurrency, and redaction tests.

### 3.6 Windows process containment

- [ ] Execute each job inside a Windows Job Object.
- [ ] Enable `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` where compatible.
- [ ] Terminate and account for the complete associated process group.
- [ ] Handle nested Job Objects and documented child-process breakaway behavior.
- [ ] Add integration tests that create descendants and verify that shutdown leaves no orphaned processes.

### 3.7 Scheduler performance

- [ ] Replace minute-by-minute eight-year scanning with a field-aware next-occurrence algorithm.
- [ ] Bound the work required to reject sparse or impossible schedules.
- [ ] Add benchmarks for common, sparse, leap-year, and impossible expressions.
- [ ] Define acceptable occurrence-calculation performance thresholds.

## 4. Daemon operation and live configuration — target v2.0.0

### 4.1 Configuration reload

- [x] Monitor the active configuration file for changes.
- [x] Debounce duplicate filesystem notifications.
- [x] Parse and validate a complete replacement configuration before activating it.
- [x] Keep the last valid configuration running when a reload is invalid or incomplete.
- [x] Add stable job identifiers so unchanged jobs retain execution state across reloads.
- [x] Reconcile added, removed, and modified jobs atomically.
- [x] Define how reload interacts with currently running and queued jobs.
- [x] Report successful and rejected reloads with actionable diagnostics.
- [ ] Add reload tests covering partial writes, rapid saves, renames, deletion, and restoration.

### 4.2 Windows Service hosting

- [x] Move long-running service scheduling onto the .NET Generic Host/Worker Service lifecycle.
- [x] Support foreground interactive mode and Windows Service mode from the same executable.
- [x] Integrate start, stop, cancellation, and failure reporting with the Windows Service Control Manager.
- [ ] Define the service account, configuration location, output location, and file permissions.
- [ ] Add service recovery configuration for unexpected failures.
- [ ] Support automatic startup after reboot and operation without an interactive login.
- [x] Add Windows Service lifecycle tests.

### 4.3 Service management

- [ ] Provide documented install, uninstall, start, stop, restart, and status workflows.
- [ ] Decide whether service management belongs in WinCron commands, a PowerShell installer, or a dedicated installer.
- [ ] Require administrative privileges only for service-management operations.
- [ ] Preserve normal non-administrator use in foreground mode.

### 4.4 Final DST and clock-change semantics

- [ ] Choose between strict classic-cron DST compatibility and the current UTC-instant policy.
- [ ] If classic compatibility is selected, run skipped fixed-time jobs soon after a forward transition.
- [ ] If classic compatibility is selected, avoid rerunning fixed-time jobs during a backward transition.
- [ ] Define separate behavior for wildcard schedules during clock transitions.
- [ ] Define behavior for clock corrections greater than three hours.
- [ ] Define startup and downtime misfire behavior independently from DST behavior.
- [ ] Document the selected policy with examples and exhaustive transition tests.

### 4.5 Configurable scheduling policy

- [ ] Make the misfire grace period configurable instead of hard-coded.
- [ ] Support global defaults with optional per-job overrides where appropriate.
- [ ] Validate policy values before starting or reloading the scheduler.
- [ ] Include effective policy values in diagnostics and job listings.

## 5. Distribution and release engineering — target v2.0.0

### 5.1 Windows executable publishing

- [ ] Add a repeatable `win-x64` publish profile.
- [ ] Publish a single-file executable.
- [ ] Provide a self-contained distribution that does not require the .NET SDK or runtime.
- [ ] Evaluate trimming and ReadyToRun only after compatibility testing.
- [ ] Test the published executable on clean supported Windows installations.

### 5.2 Installation and upgrades

- [ ] Provide an installer or a documented reproducible installation process.
- [ ] Preserve user configuration and logs during upgrades.
- [ ] Define supported Windows versions and architectures.
- [ ] Support clean uninstall without deleting user data unless explicitly requested.
- [ ] Document upgrade, downgrade, rollback, and recovery procedures.

### 5.3 Signing and integrity

- [ ] Authenticode-sign release executables and installers.
- [ ] Clarify that .NET strong-name assembly signing is not executable trust signing.
- [ ] Publish SHA-256 checksums for release artifacts.
- [ ] Verify signatures and checksums in the release pipeline.

### 5.4 Continuous integration

- [ ] Add a Windows CI workflow for restore, Release build, and tests.
- [ ] Run formatting and analyzer verification in CI.
- [ ] Build and smoke-test publish artifacts in CI.
- [ ] Add packaging validation and artifact retention.
- [ ] Prevent releases when required checks fail.

### 5.5 Repository and package metadata

- [ ] Add an explicit repository license.
- [ ] Add package license, repository, authorship, and source metadata.
- [ ] Add Source Link support for published symbols.
- [ ] Confirm the Windows-specific target framework and supported runtime identifiers.
- [ ] Document executable installation and use separately from source-development instructions.

## 6. Cron compatibility extensions — post-v2.0

### 6.1 Schedule macros

- [ ] Support `@reboot`.
- [ ] Support `@hourly`, `@daily`, `@weekly`, `@monthly`, and `@yearly`/`@annually`.
- [ ] Define macro behavior during reload, downtime, and service startup.
- [ ] Add parser, scheduler, and documentation coverage for every macro.

### 6.2 Time zones

- [ ] Support a global configured time zone independent of the Windows local time zone.
- [ ] Evaluate `CRON_TZ` compatibility for scoped or per-job time zones.
- [ ] Validate Windows and IANA time-zone identifiers with actionable errors.
- [ ] Display each job's effective time zone when listing configuration.
- [ ] Add cross-zone and DST tests.

### 6.3 Job identity and metadata

- [ ] Support stable user-defined job identifiers.
- [ ] Support optional friendly job names and descriptions.
- [ ] Reject duplicate identifiers with line-specific errors.
- [ ] Include identifiers and names in logs, status output, and reload reconciliation.

### 6.4 Retry policy

- [ ] Keep retries disabled by default to preserve cron-like execution semantics.
- [ ] Support explicit per-job retry count and delay policies.
- [ ] Bound retry duration and prevent retries from bypassing overlap limits.
- [ ] Distinguish scheduled attempts from retry attempts in logs.

## 7. Operations and observability — post-v2.0

### 7.1 Status and health

- [ ] Add machine-readable health and status output.
- [ ] Report active configuration, last successful reload, next occurrence, running jobs, and recent failures.
- [ ] Return stable exit codes suitable for monitoring systems.
- [ ] Avoid exposing secrets in health output.

### 7.2 Execution history

- [ ] Add commands to query recent runs and failures.
- [ ] Filter history by job identifier, time range, and outcome.
- [ ] Support human-readable and machine-readable output formats.
- [ ] Keep queries efficient across rotated logs.

### 7.3 Operational diagnostics

- [ ] Add configurable log verbosity.
- [ ] Add scheduler, parser, reload, process, and service diagnostic categories.
- [ ] Integrate with Windows Event Log when running as a service.
- [ ] Include enough context to troubleshoot failures without exposing sensitive values.

## 8. Required verification expansion

### 8.1 Reliability tests

- [ ] Test locked, deleted, malformed, and permission-denied configuration files.
- [ ] Test locked, full, and permission-denied log destinations.
- [ ] Test simultaneous WinCron processes and simultaneous job completions.
- [ ] Test abrupt shutdown, service restart, and machine-clock changes.

### 8.2 Parser robustness

- [ ] Add property-based or fuzz tests for cron fields and configuration lines.
- [ ] Add boundary tests for minimum and maximum dates and leap years.
- [ ] Verify that malformed input fails quickly without excessive CPU or memory use.

### 8.3 End-to-end verification

- [ ] Test every command-line mode through the published executable.
- [ ] Test foreground Ctrl+C shutdown with active child and descendant processes.
- [ ] Test first-run default configuration creation separately from read-only CLI modes.
- [ ] Test service installation, startup, reload, execution, shutdown, upgrade, and removal.

### 8.4 Release gates

- [ ] Require all unit, integration, end-to-end, analyzer, formatting, and packaging checks to pass.
- [ ] Require clean-install verification for supported Windows versions.
- [ ] Require documentation, changelog, and project version agreement before publishing.
- [ ] Require signed artifacts and verified checksums for stable releases.

## Implemented policy decisions pending compatibility review

- Schedules currently use the Windows local time zone and are converted to UTC instants for ordering.
- Nonexistent spring-forward minutes are currently skipped.
- Repeated fall-back minutes currently run for both corresponding UTC instants.
- Missed occurrences outside a one-minute grace period are currently skipped instead of backfilled.
- `WINCRON_WORKING_DIRECTORY` changes the working directory for subsequent jobs.
