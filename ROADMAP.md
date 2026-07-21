# WinCron Core Roadmap (Must-Haves Only)

Goal: match classic cron behavior on Windows before adding extras.

## 1) Parser & Config (crontab-compatible)
- [x] Support cron grammar: 5 time fields; ranges (1-5), lists (1,2,5), steps (*/5, 1-10/2), named months/days (JAN, MON); ignore blank lines and `#` comments.
- [x] Enforce field counts: 5 schedule fields + command (rest of line = command/args).
- [x] Clear validation errors with line numbers; reject malformed tokens early.
- [x] Support environment assignment lines (`KEY=VAL`) applied to subsequent jobs.

## 2) Matcher & Scheduler Semantics
- [x] Correct matching for minute, hour, dom, month, dow (Sunday=0/7) including steps/ranges/lists/names.
- [x] Move off per-second polling: compute next run times and sleep/wake appropriately.
- [x] Define and implement behavior for DST changes and clock shifts; choose a policy for missed runs after downtime (skip vs backfill) and apply consistently.
- [x] Prevent duplicate runs within the same minute tick; ensure idempotent scheduling loop.

## 3) Execution Behavior
- [x] Execute via a shell-compatible path (Windows equivalent of `/bin/sh -c`); preserve command text exactly after the 5th field.
- [x] Pass environment variables from config; support per-job working directory.
- [x] Capture exit code, stdout, and stderr; log each run with timestamp and duration.
- [x] Handle process errors cleanly; avoid orphaned processes on failure to start.

## 4) Compatibility & Tests
- [x] Unit tests for parser/matcher covering ranges, lists, steps, names, bad inputs, and dow/dom edge cases.
- [x] Integration tests: schedule a few jobs, verify timing, env propagation, command execution, and logging.
- [x] Document cron grammar, field meanings, dow/dom rules, and config location defaults.

## Implemented policy decisions

- Schedules use the Windows local time zone and are converted to UTC instants for ordering.
- Nonexistent spring-forward minutes are skipped; repeated fall-back minutes run for both UTC instants.
- Missed occurrences outside a one-minute grace period are skipped rather than backfilled.
- `WINCRON_WORKING_DIRECTORY` changes the working directory for subsequent jobs.
