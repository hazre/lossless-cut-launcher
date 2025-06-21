using System;
using System.Runtime.InteropServices;
using LosslessCutLauncher;
using LosslessCutLauncher.Settings;
using LosslessCutLauncher.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Velopack;
using Velopack.Sources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

VelopackApp.Build().Run();

try
{
  var configuration = new ConfigurationBuilder()
      .SetBasePath(AppContext.BaseDirectory)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
      .Build();

  var selfUpdateSettings = configuration.GetSection("SelfUpdate").Get<SelfUpdateSettings>();

  if (!string.IsNullOrEmpty(selfUpdateSettings?.RepositoryUrl))
  {
    var mgr = new UpdateManager(new GithubSource(selfUpdateSettings.RepositoryUrl, null, false));
    var newVersion = await mgr.CheckForUpdatesAsync();
    if (newVersion != null)
    {
      await mgr.DownloadUpdatesAsync(newVersion);
      mgr.ApplyUpdatesAndRestart(newVersion);
      // The application will exit and restart here, so no more code will be executed.
    }
  }
}
catch (Exception ex)
{
  Console.Error.WriteLine($"Error during self-update: {ex.Message}");
}

await Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .ConfigureLogging((hostContext, logging) =>
    {
      logging.ClearProviders();
      if (hostContext.HostingEnvironment.IsProduction())
      {
        logging.AddConsoleFormatter<CustomConsoleFormatter, ConsoleFormatterOptions>();
        logging.AddConsole(options =>
        {
          options.FormatterName = "custom";
        });
      }
      else
      {
        logging.AddConsole();
      }
    })
    .ConfigureServices((hostContext, services) =>
    {
      // Bind configuration
      services.Configure<LauncherSettings>(hostContext.Configuration.GetSection("Launcher"));
      services.Configure<SelfUpdateSettings>(hostContext.Configuration.GetSection("SelfUpdate"));

      // Register services
      services.AddHttpClient();
      services.AddSingleton<IUpdateCheckService, UpdateChecker>();
      services.AddSingleton<IPlatformService, Platform>();
      services.AddSingleton(serviceProvider =>
      {
        var launcherSettings = serviceProvider.GetRequiredService<IOptions<LauncherSettings>>().Value;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
          return launcherSettings.PlatformSettings.Windows;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
          return launcherSettings.PlatformSettings.Linux;
        }
        throw new PlatformNotSupportedException();
      });
      services.AddSingleton<IUpdateService, Updater>();
      services.AddSingleton<IApplicationLauncher, Launcher>();

      // Register the main orchestrator as a hosted service
      services.AddHostedService<Orchestrator>();
    })
    .Build()
    .RunAsync();