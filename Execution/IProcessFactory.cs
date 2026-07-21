using System.Diagnostics;

namespace WinCron.Execution;

public interface IProcessFactory
{
    Process Create(ProcessStartInfo startInfo);
}
