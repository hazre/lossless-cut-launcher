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
      var shouldLaunch = true;

      try
      {
        var candidates = await _updateCheckService.GetVersionCandidatesAsync(cancellationToken);
        var localVersion = GetLocalVersion();

        IReadOnlyList<Version> newerCandidates = localVersion == null
            ? candidates
            : candidates.Where(v => v > localVersion).ToList();

        if (newerCandidates.Count > 0)
        {
          _logger.LogInformation("Found {Count} candidate version(s) to try for update.", newerCandidates.Count);
          var installedVersion = await _updateService.DownloadAndExtractBestAvailableAsync(newerCandidates, cancellationToken);

          if (installedVersion != null)
          {
            _logger.LogInformation("Installed LosslessCut v{Version}", installedVersion);
          }
          else
          {
            _logger.LogWarning("Could not download any candidate release. Launching local executable if available.");
            Console.WriteLine("Could not download any LosslessCut release from available tags. Launching local executable if available.");
          }
        }
        else
        {
          if (candidates.Count == 0)
          {
            _logger.LogWarning("No downloadable version candidates found. Launching local executable if available.");
            Console.WriteLine("No downloadable LosslessCut releases found in tags feed. Launching local executable if available.");
          }
          else
          {
            _logger.LogInformation("Application is already up to date. Launching local executable.");
          }
        }
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        shouldLaunch = false;
        _logger.LogInformation("Orchestrator startup was cancelled.");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "An unhandled exception occurred.");
      }
      finally
      {
        if (shouldLaunch)
        {
          _applicationLauncher.Launch(Array.Empty<string>());
        }

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
