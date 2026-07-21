using WinCron.Application;

using var shutdownSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdownSource.Cancel();
};

var application = new WinCronApplication(Console.Out, Console.Error);
return await application.RunAsync(args, shutdownSource.Token);
