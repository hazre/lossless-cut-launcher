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

  public Task<Version?> DownloadAndExtractBestAvailableAsync(IReadOnlyList<Version> candidates, CancellationToken cancellationToken)
  {
    if (candidates.Count == 0)
    {
      _logger.LogWarning("No version candidates supplied for download.");
      return Task.FromResult<Version?>(null);
    }

    return TryDownloadCandidateRecursiveAsync(candidates, 0, cancellationToken);
  }

  private async Task<Version?> TryDownloadCandidateRecursiveAsync(IReadOnlyList<Version> candidates, int index, CancellationToken cancellationToken)
  {
    if (index >= candidates.Count)
    {
      return null;
    }

    var version = candidates[index];

    try
    {
      await DownloadAndExtractSingleVersionAsync(version, cancellationToken);
      return version;
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      _logger.LogInformation("Download cancelled while trying LosslessCut v{Version}.", version);
      throw;
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to download version v{Version}. Trying older tag.", version);
      return await TryDownloadCandidateRecursiveAsync(candidates, index + 1, cancellationToken);
    }
  }

  private async Task DownloadAndExtractSingleVersionAsync(Version version, CancellationToken cancellationToken)
  {
    var url = string.Format(_platformService.DownloadUrlFormat, $"v{version}");
    _logger.LogInformation("Starting download for LosslessCut v{Version} from {Url}", version, url);

    var appDirectory = Path.Combine(AppContext.BaseDirectory, _launcherSettings.Value.BinDirectory);
    var tempDirectory = Path.Combine(AppContext.BaseDirectory, $"{_launcherSettings.Value.BinDirectory}.tmp-{Guid.NewGuid():N}");
    var backupDirectory = Path.Combine(AppContext.BaseDirectory, $"{_launcherSettings.Value.BinDirectory}.bak-{Guid.NewGuid():N}");

    try
    {
      using var httpClient = _httpClientFactory.CreateClient();
      using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
      response.EnsureSuccessStatusCode();

      _logger.LogInformation("Downloading LosslessCut v{Version}", version);

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

      Directory.CreateDirectory(tempDirectory);
      using var archive = ArchiveFactory.OpenArchive(memoryStream);
      archive.WriteToDirectory(tempDirectory, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
      await File.WriteAllTextAsync(Path.Combine(tempDirectory, "version.txt"), version.ToString(), cancellationToken);

      ReplaceInstalledDirectory(tempDirectory, appDirectory, backupDirectory);
      _logger.LogInformation("Successfully downloaded and extracted update to {BinDirectory}", appDirectory);
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      _logger.LogInformation("Update extraction cancelled for LosslessCut v{Version}", version);
      throw;
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
    finally
    {
      TryDeleteDirectory(tempDirectory, "temporary update directory");
    }
  }

  private void ReplaceInstalledDirectory(string tempDirectory, string appDirectory, string backupDirectory)
  {
    var hadExistingInstall = Directory.Exists(appDirectory);

    if (hadExistingInstall)
    {
      Directory.Move(appDirectory, backupDirectory);
      _logger.LogInformation("Moved existing directory {BinDirectory} to backup {BackupDirectory}", appDirectory, backupDirectory);
    }

    try
    {
      Directory.Move(tempDirectory, appDirectory);

      if (hadExistingInstall)
      {
        TryDeleteDirectory(backupDirectory, "previous installation backup directory");
      }
    }
    catch
    {
      if (hadExistingInstall && Directory.Exists(backupDirectory) && !Directory.Exists(appDirectory))
      {
        try
        {
          Directory.Move(backupDirectory, appDirectory);
          _logger.LogWarning("Restored previous installation after failed update swap.");
        }
        catch (Exception restoreEx)
        {
          _logger.LogError(restoreEx, "Failed to restore previous installation from backup {BackupDirectory}", backupDirectory);
        }
      }

      throw;
    }
  }

  private void TryDeleteDirectory(string directoryPath, string description)
  {
    if (!Directory.Exists(directoryPath))
    {
      return;
    }

    try
    {
      Directory.Delete(directoryPath, true);
    }
    catch (Exception cleanupEx)
    {
      _logger.LogWarning(cleanupEx, "Failed to clean {Description} {DirectoryPath}", description, directoryPath);
    }
  }
}
