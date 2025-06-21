using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace LosslessCutLauncher.Services;

public class Updater : IUpdateService
{
  private readonly IHttpClientFactory _httpClientFactory;
  private readonly IPlatformService _platformService;
  private readonly IOptions<LauncherSettings> _launcherSettings;
  private readonly ILogger<Updater> _logger;

  public Updater(
      IHttpClientFactory httpClientFactory,
      IPlatformService platformService,
      IOptions<LauncherSettings> launcherSettings,
      ILogger<Updater> logger)
  {
    _httpClientFactory = httpClientFactory;
    _platformService = platformService;
    _launcherSettings = launcherSettings;
    _logger = logger;
  }

  public async Task DownloadAndExtractUpdateAsync(Version version, CancellationToken cancellationToken)
  {
    var url = string.Format(_platformService.DownloadUrlFormat, $"v{version}");
    _logger.LogInformation("Starting download for LosslessCut v{Version} from {Url}", version, url);

    try
    {
      using var httpClient = _httpClientFactory.CreateClient();
      using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
      response.EnsureSuccessStatusCode();

      var appDirectory = Path.Combine(AppContext.BaseDirectory, _launcherSettings.Value.BinDirectory);
      if (Directory.Exists(appDirectory))
      {
        Directory.Delete(appDirectory, true);
        _logger.LogInformation("Deleted existing directory {BinDirectory}", appDirectory);
      }

      Directory.CreateDirectory(appDirectory);

      _logger.LogInformation($"Downloading LosslessCut v{version}");

      var totalBytes = response.Content.Headers.ContentLength ?? -1L;
      var totalBytesRead = 0L;
      var buffer = new byte[8192];
      var isMoreToRead = true;

      using var memoryStream = new MemoryStream();
      using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
      {
        do
        {
          var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
          if (bytesRead == 0)
          {
            isMoreToRead = false;
            continue;
          }

          await memoryStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
          totalBytesRead += bytesRead;

          if (totalBytes != -1)
          {
            const int totalBlocks = 30;
            var percent = (double)totalBytesRead / totalBytes;
            var progressBlocks = (int)(percent * totalBlocks);
            var progressBar = new string('█', progressBlocks) + new string('░', totalBlocks - progressBlocks);

            var timestamp = ConsoleStyle.GetTimestamp();
            var logLevelString = ConsoleStyle.GetLogLevelString(LogLevel.Information);
            var logLevelColor = ConsoleStyle.GetLogLevelColor(LogLevel.Information);

            Console.Write($"\r{ConsoleStyle.TimeStampColor}{timestamp}{ConsoleStyle.ResetColor} {logLevelColor}{logLevelString}{ConsoleStyle.ResetColor} [{progressBar}] {percent:P0} ");
          }

        } while (isMoreToRead);
      }
      Console.WriteLine();
      memoryStream.Position = 0;

      using var archive = ArchiveFactory.Open(memoryStream);
      archive.WriteToDirectory(appDirectory, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });

      await File.WriteAllTextAsync(Path.Combine(appDirectory, "version.txt"), version.ToString(), cancellationToken);

      _logger.LogInformation("Successfully downloaded and extracted update to {BinDirectory}", appDirectory);
    }
    catch (HttpRequestException ex)
    {
      _logger.LogError(ex, "Failed to download update from {Url}", url);
      throw;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "An error occurred during update extraction");
      throw;
    }
  }
}