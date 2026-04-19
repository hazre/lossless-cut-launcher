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
  Task<IReadOnlyList<Version>> GetVersionCandidatesAsync(CancellationToken cancellationToken);
}

public interface IUpdateService
{
  Task<Version?> DownloadAndExtractBestAvailableAsync(IReadOnlyList<Version> candidates, CancellationToken cancellationToken);
}
