namespace LosslessCutLauncher
{
  public record LauncherSettings
  {
    public required string AppName { get; init; }
    public required string BinDirectory { get; init; }
    public required string UpdateFeedUrl { get; init; }
    public required PlatformSettingsContainer PlatformSettings { get; init; }
  }

  public record PlatformSettingsContainer
  {
    public required PlatformSettings Windows { get; init; }
    public required PlatformSettings Linux { get; init; }
  }

  public record PlatformSettings
  {
    public required string DownloadUrlFormat { get; init; }
    public required string ExecutableName { get; init; }
  }
}

namespace LosslessCutLauncher.Settings
{
  public record SelfUpdateSettings
  {
    public required string RepositoryUrl { get; init; }
  }
}