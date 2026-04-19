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

  public async Task<IReadOnlyList<Version>> GetVersionCandidatesAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("Checking for updates...");

    try
    {
      using var httpClient = _httpClientFactory.CreateClient();
      var response = await httpClient.GetStringAsync(_launcherSettings.Value.UpdateFeedUrl, cancellationToken);

      var doc = XDocument.Parse(response);
      var ns = "http://www.w3.org/2005/Atom";
      var candidates = doc.Root?.Elements(XName.Get("entry", ns))
          .Select(entry => entry.Element(XName.Get("title", ns))?.Value)
          .Where(title => !string.IsNullOrWhiteSpace(title))
          .Select(title => Version.TryParse(title!.TrimStart('v'), out var version) ? version : null)
          .Where(version => version is not null)
          .Cast<Version>()
          .Distinct()
          .OrderByDescending(v => v)
          .ToList() ?? new List<Version>();

      if (candidates.Count > 0)
      {
        _logger.LogInformation("Found {Count} version candidates from update feed.", candidates.Count);
      }
      else
      {
        _logger.LogWarning("No valid version candidates found in feed.");
      }

      return candidates;
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      _logger.LogInformation("Update check cancelled.");
      throw;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error checking for updates");
      return Array.Empty<Version>();
    }
  }
}
