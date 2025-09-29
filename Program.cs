using System.Text.Json.Nodes;
using WinCron;

CronParser parser = new();
if (parser.Parse())
{
    Console.WriteLine("Cron is valid.");
    parser.Crons.ForEach(cron => { 
        Console.WriteLine($"Cron job: {cron}");
        cron.Run();
    });

}

