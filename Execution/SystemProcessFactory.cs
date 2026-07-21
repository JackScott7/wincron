using System.Diagnostics;

namespace WinCron.Execution;

public sealed class SystemProcessFactory : IProcessFactory
{
    public Process Create(ProcessStartInfo startInfo)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        return new Process { StartInfo = startInfo };
    }
}
