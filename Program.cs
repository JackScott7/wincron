using WinCron.Application;
using WinCron.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

if (args.Contains("--service", StringComparer.OrdinalIgnoreCase))
{
    var serviceArguments = args
        .Where(argument => !string.Equals(argument, "--service", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(serviceArguments);
    builder.Services.AddWindowsService(options => options.ServiceName = "WinCron");
    builder.Services.AddSingleton(new WindowsServiceArguments(serviceArguments));
    builder.Services.AddHostedService<WinCronWindowsService>();

    using var host = builder.Build();
    await host.RunAsync();
    return 0;
}

using var shutdownSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    shutdownSource.Cancel();
};

var application = new WinCronApplication(Console.Out, Console.Error);
return await application.RunAsync(args, shutdownSource.Token);
