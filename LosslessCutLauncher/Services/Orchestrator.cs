using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LosslessCutLauncher.Services
{
  public class Orchestrator : IHostedService
  {
    private readonly IUpdateCheckService _updateCheckService;
    private readonly IUpdateService _updateService;
    private readonly IApplicationLauncher _applicationLauncher;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IOptions<LauncherSettings> _launcherSettings;
    private readonly ILogger<Orchestrator> _logger;

    public Orchestrator(
        IUpdateCheckService updateCheckService,
        IUpdateService updateService,
        IApplicationLauncher applicationLauncher,
        IHostApplicationLifetime hostApplicationLifetime,
        IOptions<LauncherSettings> launcherSettings,
        ILogger<Orchestrator> logger)
    {
      _updateCheckService = updateCheckService;
      _updateService = updateService;
      _applicationLauncher = applicationLauncher;
      _hostApplicationLifetime = hostApplicationLifetime;
      _launcherSettings = launcherSettings;
      _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
      _logger.LogInformation("Orchestrator service started.");

      try
      {
        var latestVersion = await _updateCheckService.GetLatestVersionAsync(cancellationToken);
        var localVersion = GetLocalVersion();

        if (latestVersion == null)
        {
          _logger.LogWarning("Could not determine latest version. Launching currently installed version if available.");
          if (localVersion != null)
          {
            _applicationLauncher.Launch(new string[] { });
          }
          else
          {
            _logger.LogError("No local version found and could not fetch latest version. Application cannot start.");
          }
        }
        else
        {
          if (localVersion == null || latestVersion > localVersion)
          {
            _logger.LogInformation("New version available. Downloading and updating...");
            await _updateService.DownloadAndExtractUpdateAsync(latestVersion, cancellationToken);
            _applicationLauncher.Launch(new string[] { });
          }
          else
          {
            _logger.LogInformation("Application is up to date. Launching...");
            _applicationLauncher.Launch(new string[] { });
          }
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "An unhandled exception occurred.");
      }
      finally
      {
        _hostApplicationLifetime.StopApplication();
      }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
      _logger.LogInformation("Orchestrator service stopped.");
      return Task.CompletedTask;
    }

    private Version? GetLocalVersion()
    {
      var versionFilePath = Path.Combine(AppContext.BaseDirectory, _launcherSettings.Value.BinDirectory, "version.txt");
      if (!File.Exists(versionFilePath))
      {
        _logger.LogInformation("Local version file not found.");
        return null;
      }

      var versionString = File.ReadAllText(versionFilePath);
      if (Version.TryParse(versionString, out var version))
      {
        _logger.LogInformation("Found local version: {Version}", version);
        return version;
      }

      _logger.LogWarning("Could not parse local version: {VersionString}", versionString);
      return null;
    }
  }
}