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
        // cron order: minute hour dayOfMonth month dayOfWeek
        // DayOfWeek: Sunday=0 ... Saturday=6 (matches typical cron 0..6 with Sunday=0)
        return Match(this.Minute, now.Minute)
            && Match(this.Hour, now.Hour)
            && Match(this.DayOfMonth, now.Day)
            && Match(this.Month, now.Month)
            && Match(this.DayOfWeek, (int)now.DayOfWeek);
    }

    private static bool Match(string field, int value)
    {
        // keep it ultra-simple: "*" or exact number
        return field == "*" || field == value.ToString();
    }

    public override string ToString()
    {
        return $"{Minute} {Hour} {DayOfMonth} {Month} {DayOfWeek} {Command} {Args}";
    }
}
