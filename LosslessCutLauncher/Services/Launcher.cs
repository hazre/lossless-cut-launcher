using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LosslessCutLauncher.Services;

public class Launcher : IApplicationLauncher
{
  private readonly IPlatformService _platformService;
  private readonly IOptions<LauncherSettings> _options;
  private readonly ILogger<Launcher> _logger;

  public Launcher(
      IPlatformService platformService,
      IOptions<LauncherSettings> options,
      ILogger<Launcher> logger)
  {
    _platformService = platformService;
    _options = options;
    _logger = logger;
  }

  public void Launch(string[] args)
  {
    var basePath = AppContext.BaseDirectory;
    var executablePath = Path.Combine(
        basePath,
        _options.Value.BinDirectory,
        _platformService.ExecutableName);

    if (!File.Exists(executablePath))
    {
      _logger.LogError("LosslessCut executable not found at {Path}", executablePath);
      return;
    }

    var processStartInfo = new ProcessStartInfo
    {
      FileName = executablePath,
      Arguments = string.Join(" ", args),
      UseShellExecute = false
    };

    _logger.LogInformation("Launching LosslessCut from {Path}", executablePath);
    Process.Start(processStartInfo);
  }
}