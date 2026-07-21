using System.Diagnostics;
using WinCron.Domain;

namespace WinCron.Execution;

public interface IShellCommandBuilder
{
    ProcessStartInfo CreateStartInfo(CronJobDefinition job);
}
