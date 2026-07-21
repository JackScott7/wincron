using System.Diagnostics;
using System.Text;
using WinCron.Domain;

namespace WinCron.Execution;

public sealed class WindowsShellCommandBuilder : IShellCommandBuilder
{
    private readonly string shellExecutablePath;

    public WindowsShellCommandBuilder(string? shellExecutablePath = null)
    {
        this.shellExecutablePath = shellExecutablePath
            ?? Environment.GetEnvironmentVariable("COMSPEC")
            ?? "cmd.exe";
    }

    public ProcessStartInfo CreateStartInfo(CronJobDefinition job)
    {
        ArgumentNullException.ThrowIfNull(job);

        var startInfo = new ProcessStartInfo
        {
            FileName = shellExecutablePath,
            WorkingDirectory = job.WorkingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = false,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        startInfo.ArgumentList.Add("/D");
        startInfo.ArgumentList.Add("/S");
        startInfo.ArgumentList.Add("/C");
        startInfo.ArgumentList.Add(job.CommandText);

        foreach (var environmentVariable in job.EnvironmentVariables)
        {
            startInfo.Environment[environmentVariable.Key] = environmentVariable.Value;
        }

        return startInfo;
    }
}
