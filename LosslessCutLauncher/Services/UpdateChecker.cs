using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LosslessCutLauncher.Services;

public class UpdateChecker : IUpdateCheckService
{
  private readonly IHttpClientFactory _httpClientFactory;
  private readonly IOptions<LauncherSettings> _launcherSettings;
  private readonly ILogger<UpdateChecker> _logger;

  public UpdateChecker(
      IHttpClientFactory httpClientFactory,
      IOptions<LauncherSettings> launcherSettings,
      ILogger<UpdateChecker> logger)
  {
    _httpClientFactory = httpClientFactory;
    _launcherSettings = launcherSettings;
    _logger = logger;
  }

  public async Task<Version?> GetLatestVersionAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("Checking for updates...");

    try
    {
      var httpClient = _httpClientFactory.CreateClient();
      var response = await httpClient.GetStringAsync(_launcherSettings.Value.UpdateFeedUrl, cancellationToken);

      var doc = XDocument.Parse(response);
      var ns = "http://www.w3.org/2005/Atom";
      var versions = doc.Root?.Elements(XName.Get("entry", ns))
          .Select(entry => entry.Element(XName.Get("title", ns))?.Value)
          .Where(title => title != null)
          .Select(title => Version.TryParse(title!.TrimStart('v'), out var version) ? version : null)
          .Where(version => version != null)
          .OrderByDescending(v => v)
          .ToList() ?? new List<Version?>();

      var latestVersion = versions.FirstOrDefault();

      if (latestVersion != null)
      {
        _logger.LogInformation("Latest version found: {Version}", latestVersion);
      }
      else
      {
        _logger.LogWarning("Could not determine latest version from feed.");
      }

      return latestVersion;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error checking for updates");
      return null;
    }
  }
}