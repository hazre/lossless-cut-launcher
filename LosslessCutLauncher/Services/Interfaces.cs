namespace LosslessCutLauncher.Services;

public interface IApplicationLauncher
{
  void Launch(string[] args);
}

public interface IPlatformService
{
  string DownloadUrlFormat { get; }
  string ExecutableName { get; }
}

public interface IUpdateCheckService
{
  Task<Version?> GetLatestVersionAsync(CancellationToken cancellationToken);
}

public interface IUpdateService
{
  Task DownloadAndExtractUpdateAsync(Version version, CancellationToken cancellationToken);
}