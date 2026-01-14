using WinCron.lib;

CronParser parser = new();

if (!parser.Parse())
{
    Console.WriteLine("Invalid cron file. Exiting.");
    return;
}

Console.WriteLine($"WinCron started at {DateTime.Now:HH:mm:ss}.");
Console.WriteLine($"Loaded {parser.Crons.Count} crons.");

int lastMinute = -1;

while (true)
{
    var now = DateTime.Now;

    if (now.Second == 0 && now.Minute != lastMinute)
    {
        lastMinute = now.Minute;

        var runnableJobs = (from _ in parser.Crons
                            where _.ShouldRunNow(now)
                            select _).ToList<CronJob>();

        runnableJobs.ForEach(job => job.Run());

    }

    Thread.Sleep(1000);
}