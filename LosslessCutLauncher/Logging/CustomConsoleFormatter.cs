using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace LosslessCutLauncher
{
  public sealed class CustomConsoleFormatter : ConsoleFormatter
  {
    public CustomConsoleFormatter() : base("custom") { }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
      var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
      if (string.IsNullOrEmpty(message))
      {
        return;
      }

      var logLevel = logEntry.LogLevel;
      // var category = logEntry.Category;
      // var shortCategory = category.Split('.').LastOrDefault() ?? category;

      var timestamp = ConsoleStyle.GetTimestamp();
      var logLevelString = ConsoleStyle.GetLogLevelString(logLevel);
      var logLevelColor = ConsoleStyle.GetLogLevelColor(logLevel);

      textWriter.WriteLine($"{ConsoleStyle.TimeStampColor}{timestamp}{ConsoleStyle.ResetColor} {logLevelColor}{logLevelString}{ConsoleStyle.ResetColor} {message}");
    }
  }
}