using System.Diagnostics;
using System.Text;

namespace WinCron.lib;
/// <summary>
/// Base CronJob class that represents a Cron Job
/// </summary>
internal class CronJob
{
    public string Minute { get; set; }
    public string Hour { get; set; }
    public string DayOfMonth { get; set; }
    public string Month { get; set; }
    public string DayOfWeek { get; set; }
    public string Command { get; set; }
    public string? Args { get; set; }
    private readonly string logDirectory = Path.Combine(
        Environment.GetEnvironmentVariable("USERPROFILE") ?? ".",
        "wincron",
        "output");

    public CronJob(string minute, string hour, string dayOfMonth, string month, string dayOfWeek, string command, string args)
    {
        this.Minute = minute;
        this.Hour = hour;
        this.DayOfMonth = dayOfMonth;
        this.Month = month;
        this.DayOfWeek = dayOfWeek;
        this.Command = command;
        this.Args = args;

        // Creates the directory only if it doesn't exist
        Directory.CreateDirectory(logDirectory);
    }

    /// <summary>
    /// Run this CronJob with the specified command + args (if any)
    /// </summary>
    async public void Run()
    {
        Console.WriteLine($"[+] Running {this.Command} with Args: {this.Args}");
        try
        {
            Process process = new()
            {
                StartInfo =
                {
                    FileName = this.Command,
                    Arguments = this.Args,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    StandardInputEncoding = Encoding.UTF8,
                    StandardOutputEncoding = Encoding.UTF8,
                }
            };
            process.Start();

            await process.WaitForExitAsync();

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();

            File.AppendAllText($@"{this.logDirectory}\stdout.log", $"[{DateTime.Now}] {this}\n{stdout}");

            if (!string.IsNullOrEmpty(stderr))
            {
                File.AppendAllText($@"{this.logDirectory}\stderr.log", $"[{DateTime.Now}] {this}\n{stderr}");
            }
        }
        catch (Exception ex)
        {
            File.AppendAllText($@"{this.logDirectory}\stderr.log", $"[{DateTime.Now}] {this}\n{ex.Message}");
        }
    }

    /// <summary>
    /// Dictates whether this CronJob should run now at this moment tick
    /// </summary>
    /// <returns>true if should run now, false otherwise</returns>
    public bool ShouldRunNow(DateTime now)
    {
        return Match(this.Minute, now.Minute, 0)
            && Match(this.Hour, now.Hour, 0)
            && Match(this.DayOfMonth, now.Day, 1)
            && Match(this.Month, now.Month, 1)
            && Match(this.DayOfWeek, (int)now.DayOfWeek, 0);
    }

    private static bool Match(string field, int value, int baseline)
    {
        if (field == "*") return true;

        if (field.StartsWith("*/", StringComparison.Ordinal)) // if this date starts with */, e.g */5, */15
        {
            if (int.TryParse(field.AsSpan(2), out var step) && step > 0)
                return ((value - baseline) % step) == 0;
            return false;
        }

        return int.TryParse(field, out var exact) && value == exact;
    }

    public override string ToString()
    {
        return $"{Minute} {Hour} {DayOfMonth} {Month} {DayOfWeek} {Command} {Args}";
    }
}
