namespace LosslessCutLauncher.Services;

public class Platform : IPlatformService
{
  private readonly PlatformSettings _platformSettings;

  public Platform(PlatformSettings platformSettings)
  {
    _platformSettings = platformSettings;
  }

  public string DownloadUrlFormat => _platformSettings.DownloadUrlFormat;
  public string ExecutableName => _platformSettings.ExecutableName;
}