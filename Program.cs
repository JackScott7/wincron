using System.Text.Json.Nodes;
using WinCron;

CronParser parser = new();

if (!parser.Parse())
{
    Console.WriteLine("Invalid cron file. Exiting.");
    return;
}

Console.WriteLine("WinCron scheduler started. Waiting for minute ticks…");

int lastMinute = -1;

while (true)
{
    var now = DateTime.Now;

    if (now.Second == 0 && now.Minute != lastMinute)
    {
        lastMinute = now.Minute;

        foreach (var job in parser.Crons)
        {
            if (job.ShouldRunNow(now))
            {
                Console.WriteLine($"[{now:yyyy-MM-dd HH:mm:ss}] running → {job.Command}");
                job.Run();
            }
        }
    }

    Thread.Sleep(1000);
}