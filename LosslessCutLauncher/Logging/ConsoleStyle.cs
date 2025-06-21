using Microsoft.Extensions.Logging;

namespace LosslessCutLauncher
{
  public static class ConsoleStyle
  {
    public const string TimeStampColor = "\u001b[36m"; // Cyan
    public const string ResetColor = "\u001b[0m";

    public static string GetTimestamp()
    {
      return DateTime.Now.ToString("HH:mm:ss.fff");
    }

    public static string GetLogLevelString(LogLevel logLevel)
    {
      return logLevel switch
      {
        LogLevel.Trace => "[TRACE]",
        LogLevel.Debug => "[DEBUG]",
        LogLevel.Information => "[INFO]",
        LogLevel.Warning => "[WARN]",
        LogLevel.Error => "[ERROR]",
        LogLevel.Critical => "[CRITICAL]",
        _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
      };
    }

    public static string GetLogLevelColor(LogLevel logLevel)
    {
      return logLevel switch
      {
        LogLevel.Trace => "\u001b[37m",    // White
        LogLevel.Debug => "\u001b[37m",    // White
        LogLevel.Information => "\u001b[32m", // Green
        LogLevel.Warning => "\u001b[33m",  // Yellow
        LogLevel.Error => "\u001b[31m",    // Red
        LogLevel.Critical => "\u001b[35m", // Magenta
        _ => ResetColor
      };
    }
  }
}