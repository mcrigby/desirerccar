using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CutilloRigby.DesireRc;

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .UseConsoleLifetime()
            .ConfigureHostOptions(options =>
            {
                options.ShutdownTimeout = TimeSpan.FromSeconds(30);
                options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
            })
            .ConfigureHostConfiguration(configurationBuilder => {
                configurationBuilder
                    .AddJsonFile("./appsettings.json")
                    .AddJsonFile("./appsettings.local.json", optional: true);
            })
            .ConfigureLogging((hostBuilder, logBuilder) => {
                logBuilder.ClearProviders();
                logBuilder.AddConfiguration(hostBuilder.Configuration.GetSection("Logging"));
                logBuilder.AddConsole();
            })
            .ConfigureServices((hostBuilder, services) =>
            {
                services.ConfigureServicesFromAssemblyContaining<IHostMarker>(hostBuilder.Configuration);
            })
            .Build();


        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Register(async () => await host.StopAsync());

        //var timer = new Timer((hmc6352) => {
        //    Console.WriteLine(((Iot.Device.HMC6352.HMC6352)hmc6352).Heading.Degrees);
        //}, host.Services.GetRequiredService<Iot.Device.HMC6352.HMC6352>(), 0, 1000);

        await host.StartAsync(lifetime.ApplicationStopping);
        await host.WaitForShutdownAsync(lifetime.ApplicationStopped);
    }
}
