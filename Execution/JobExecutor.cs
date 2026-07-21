using System.Diagnostics;
using WinCron.Domain;
using WinCron.Scheduling;

namespace WinCron.Execution;

public sealed class JobExecutor : IJobDispatcher
{
    private readonly IShellCommandBuilder commandBuilder;
    private readonly IProcessFactory processFactory;
    private readonly IJobExecutionLogger executionLogger;
    private readonly TimeProvider timeProvider;

    public JobExecutor(
        IJobExecutionLogger executionLogger,
        IShellCommandBuilder? commandBuilder = null,
        IProcessFactory? processFactory = null,
        TimeProvider? timeProvider = null)
    {
        this.executionLogger = executionLogger ?? throw new ArgumentNullException(nameof(executionLogger));
        this.commandBuilder = commandBuilder ?? new WindowsShellCommandBuilder();
        this.processFactory = processFactory ?? new SystemProcessFactory();
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task DispatchAsync(
        CronJobDefinition job,
        DateTimeOffset scheduledOccurrenceUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        var startedAtUtc = timeProvider.GetUtcNow();
        int? exitCode = null;
        var standardOutput = string.Empty;
        var standardError = string.Empty;
        var wasCanceled = false;
        string? errorMessage = null;

        using var process = processFactory.Create(commandBuilder.CreateStartInfo(job));

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException("The command shell did not start.");
            }

            var standardOutputTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
            var standardErrorTask = process.StandardError.ReadToEndAsync(CancellationToken.None);

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                wasCanceled = true;
                TerminateProcessTree(process);
                await process.WaitForExitAsync(CancellationToken.None);
            }

            standardOutput = await standardOutputTask;
            standardError = await standardErrorTask;
            exitCode = process.ExitCode;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            errorMessage = exception.Message;
            TerminateProcessTree(process);
        }

        var result = new JobExecutionResult(
            job.Id,
            job.CommandText,
            scheduledOccurrenceUtc,
            startedAtUtc,
            timeProvider.GetUtcNow(),
            exitCode,
            standardOutput,
            standardError,
            wasCanceled,
            errorMessage);

        await executionLogger.WriteAsync(result, CancellationToken.None);
    }

    private static void TerminateProcessTree(Process process)
    {
        try
        {
            if (process.Id > 0 && !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process never started or exited between the state check and termination.
        }
        catch (System.ComponentModel.Win32Exception)
        {
            // The process already exited or the operating system denied termination.
        }
    }
}
