# Changelog

All notable changes to WinCron are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.1.0] - 2026-07-21

### Added

- Added a self-contained, single-file `win-x64` publish profile.
- Added a single `wincron-setup.exe` installer that preserves configuration, installs WinCron on `PATH`, configures automatic Windows Service startup and recovery, and starts the daemon.
- Added Windows CI for formatting, Release builds, all tests, publish verification, and executable smoke testing.
- Added a tag-driven workflow that builds and signs the executable and installer, publishes a SHA-256 checksum and GitHub Release assets, and records a linked GitHub Deployment.
- Added an MIT license and complete package/repository metadata.

### Changed

- Changed the target framework to the explicit Windows target and made release publishing self-contained.
- Changed structured log placement to the selected configuration directory so the service uses `C:\ProgramData\WinCron\output`.

### Fixed

- Fixed service logs defaulting to the LocalSystem profile instead of the service configuration directory.

## [2.0.0] - 2026-07-21

### Added

- Added atomic live configuration reload with filesystem monitoring and debouncing.
- Added last-known-good behavior that rejects invalid, missing, partially written, or inaccessible reloads without stopping active scheduling.
- Added stable, user-defined job identifiers through `WINCRON_JOB_ID` with duplicate validation.
- Added Windows Service hosting through the .NET Generic Host and the internal `--service` host mode.
- Added service lifecycle coverage and configuration-watcher/reload integration tests.

### Changed

- Changed scheduler cancellation so configuration reload can stop future scheduling without canceling already running jobs.
- Changed configuration edits to take effect automatically without restarting WinCron.

### Fixed

- Fixed configuration changes being ignored until the WinCron process was restarted.
- Fixed invalid replacement configuration being able to displace a known-good scheduling configuration.

## [1.3.0] - 2026-07-21

### Added

- Added per-job `Allow`, `Skip`, `QueueOne`, and `TerminatePrevious` overlap policies with a bounded one-item queue and safe `Skip` default.
- Added per-job execution timeouts and maximum captured-output limits through scoped configuration assignments.
- Added live stdout and stderr forwarding while retaining bounded output for structured execution records.
- Added named single-instance locking based on the normalized configuration path.
- Added JSON log rotation with configurable size and retention options.
- Added timeout and output-truncation fields to structured execution results.

### Changed

- Changed logging sinks to fail independently so an unavailable file log does not suppress terminal reporting.
- Changed process output collection to drain streams incrementally instead of retaining unlimited output in memory.

### Fixed

- Fixed long-running jobs overlapping without an explicit concurrency policy.
- Fixed hung commands running indefinitely without a timeout.
- Fixed multiple WinCron processes silently scheduling the same configuration.
- Fixed the execution log growing indefinitely without rotation.

## [1.2.1] - 2026-07-21

### Changed

- Changed `--test` and `--list` into read-only operations that report missing explicitly selected configuration files instead of creating them.
- Changed an empty scheduler into an idle scheduler that remains active until canceled.

### Fixed

- Fixed semantically impossible calendar schedules being accepted, scanned minute by minute for eight years, and then silently discarded.
- Fixed the scheduler exiting immediately after reporting that it was running when no jobs were configured.

## [1.2.0] - 2026-07-21

### Added

- Added command-line support for configuration overrides, syntax testing, configuration listing, version output, help output, and stable success/runtime/usage exit codes.

### Changed

- Changed the default configuration location from `%USERPROFILE%\config.wc` to `%USERPROFILE%\wincron\config.wc`; existing legacy configuration is copied to the new location when needed.

### Fixed

- Fixed captured job output being written only to the JSON log by also displaying each completed job's status, standard output, and standard error in the WinCron terminal.

## [1.1.0] - 2026-07-21

### Added

- Added complete five-field cron parsing with exact values, wildcards, lists, inclusive ranges, wildcard steps, and range steps.
- Added case-insensitive `JAN-DEC` month names and `SUN-SAT` weekday names.
- Added support for both `0` and `7` as Sunday.
- Added line-numbered validation errors for malformed schedules, missing fields, out-of-range values, invalid steps, and missing commands.
- Added `KEY=VALUE` configuration lines whose environment values apply to subsequent jobs.
- Added `WINCRON_WORKING_DIRECTORY` for configuring subsequent jobs' working directories.
- Added immutable, modular models for cron fields, expressions, and job definitions.
- Added next-occurrence calculation using UTC instants and the Windows local time zone.
- Added explicit daylight-saving behavior: nonexistent spring-forward minutes are skipped and repeated fall-back minutes run for both UTC occurrences.
- Added a one-minute misfire grace period and a skip policy for stale occurrences after downtime or forward clock changes.
- Added duplicate-occurrence prevention and concurrent dispatch for jobs due at the same time.
- Added Windows shell execution through `%COMSPEC%` with the original command text preserved.
- Added scoped environment propagation, configured working directories, exit-code capture, standard output, standard error, cancellation state, and execution duration.
- Added graceful shutdown that terminates active child process trees to avoid orphaned processes.
- Added structured JSON Lines execution logs at `%USERPROFILE%\wincron\output\runs.jsonl`.
- Added unit and integration suites covering parsing, validation, object initialization, matching, DST transitions, clock jumps, multi-job timing, shell execution, environment propagation, working directories, process failures, output capture, configuration loading, and logging.
- Added analyzer and formatting enforcement with warnings treated as errors.
- Added a branded README, application icon, package assets, complete configuration documentation, and an implementation roadmap.

### Changed

- Replaced raw string schedule matching with parsed cron fields and classic cron day-of-month/day-of-week semantics.
- Replaced fragile once-per-second polling with calculated wake times and a priority-based scheduler.
- Changed schedules from fixed UTC wall-clock matching to documented Windows local-time behavior while retaining UTC ordering internally.
- Replaced direct executable invocation with shell-compatible command execution.
- Replaced separate unstructured stdout/stderr files with one structured record per execution.
- Reorganized the application into `Configuration`, `Domain`, `Scheduling`, and `Execution` modules.
- Updated the application entry point to use asynchronous configuration loading, graceful cancellation, the modular scheduler, and structured logging.
- Updated package metadata to embed the application icon and include the README, changelog, PNG logo, and ICO asset.

### Fixed

- Fixed short or commandless configuration lines causing index errors instead of validation results.
- Fixed inconsistent whitespace handling between validation and job construction.
- Fixed cron fields accepting numeric values outside their valid ranges.
- Fixed commands losing their original internal spacing during parsing.
- Fixed day-of-month and day-of-week incorrectly requiring both restricted fields to match.
- Fixed wildcard step expressions such as `*/1` being treated as restricted day fields.
- Fixed scheduled jobs being missed when a polling loop failed to wake at exactly second zero.
- Fixed duplicate dispatch risk within the same scheduled minute.
- Fixed redirected child processes potentially deadlocking when output buffers filled before process exit.
- Fixed fire-and-forget `async void` execution that prevented completion and failure tracking.
- Fixed missing exit-code and duration information in execution logs.
- Fixed process-start and cancellation paths that could leave child processes running.
- Fixed package asset paths that incorrectly nested files under `assets/assets`.

## [1.0.0] - 2026-01-14

### Added

- Added the initial .NET 10 console application prototype.
- Added basic `%USERPROFILE%\config.wc` loading with blank-line and comment filtering.
- Added basic support for wildcard, exact numeric, and wildcard-step schedule fields.
- Added UTC schedule matching through a once-per-second polling loop.
- Added direct process execution and basic timestamped stdout/stderr log files.
- Added initial project and repository metadata.
