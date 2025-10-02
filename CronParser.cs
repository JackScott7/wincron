using System.Text.RegularExpressions;

namespace WinCron;
/// <summary>
/// Base Cron Parser class, exposes .Parse() that will parse WinCron's config.wc file
/// </summary>
internal class CronParser
{
    public List<CronJob> Crons { get; set; } = [];
    private readonly string cronConfPath = $@"{Environment.GetEnvironmentVariable("USERPROFILE")}\config.wc";
    private readonly string confData = "";

    /// <summary>
    /// Parses config.wc file and returns whether Parsing was successfull or not
    /// </summary>
    /// <returns>Success parsing or not</returns>
    public bool Parse() => this.IsValidCron(this.confData);

    private static string[] GetCronsAsArray(string config)
    {
        return config.Split([Environment.NewLine], StringSplitOptions.None)
            .Where(line => !(line.TrimStart().StartsWith('#') || string.IsNullOrWhiteSpace(line)))
            .ToArray();
    }

    private static bool ParseCron(string cron)
    {
        string[] parts = Regex.Split(cron.Trim(), @"\s+");

        Regex regexFieldMatch = new(@"^(?:\*|\d+|\*/\d+)$", RegexOptions.Compiled);

        for (int i = 0; i < 5; i++) {
            if (!regexFieldMatch.IsMatch(parts[i]))
            {
                return false;
            }
        }
        return true;
    }

    private bool IsValidCron(string cron)
    {
        var config = GetCronsAsArray(cron);

        if (config.Length == 0)
        {
            Console.WriteLine(".wc File is empty, no crons detected");
            return false;
        }

        foreach (var line in config)
        {
            if (!ParseCron(line))
            {
                Console.WriteLine($"Invalid cron job: {line}");
                return false;
            }
            string[] parts = line.Trim().Split(" ");
            string minute = parts[0];
            string hour = parts[1];
            string dayOfMonth = parts[2];
            string month = parts[3];
            string dayOfWeek = parts[4];
            string command = parts[5];
            string args = parts.Length > 6 ? string.Join(" ", parts.Skip(6)) : string.Empty;

            this.Crons.Add(new CronJob(minute, hour, dayOfMonth, month, dayOfWeek, command, args));
        }
        return true;
    }

    public CronParser()
    {
        if (!File.Exists(this.cronConfPath))
        {
            Console.WriteLine("Cron file not found. Creating a new one.");
            File.Create(this.cronConfPath);
            return;
        }
        this.confData = File.ReadAllText(cronConfPath);
    }
}
