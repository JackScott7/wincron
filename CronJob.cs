using System.Diagnostics;

namespace WinCron;
internal class CronJob(string minute, string hour, string dayOfMonth, string month, string dayOfWeek, string command, string args)
{
    public string Minute { get; set; } = minute;
    public string Hour { get; set; } = hour;
    public string DayOfMonth { get; set; } = dayOfMonth;
    public string Month { get; set; } = month;
    public string DayOfWeek { get; set; } = dayOfWeek;
    public string Command { get; set; } = command;
    public string? Args { get; set; } = args;

    public void Run()
    {
        Console.WriteLine($"[+] Running {this.Command} with Args: {this.Args}");
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
            }
        };
        process.Start();
        process.WaitForExit();
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        File.AppendAllText("C:\\Users\\clone\\wincron\\output\\stdout.log", $"{this}\n{stdout}");
        if (!string.IsNullOrEmpty(stderr))
        {
            File.AppendAllText("C:\\Users\\clone\\wincron\\output\\stderr.log", $"{this}\n{stderr}");
        }
    }

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
