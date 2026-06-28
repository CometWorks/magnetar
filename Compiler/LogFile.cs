using System;
using System.Globalization;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Layouts;

namespace Pulsar.Compiler;

public static class LogFile
{
    private const string fileNameBase = "info";
    private const string fileExtension = ".log";
    private const string currentLogFileName = "info.current";
    private static Logger logger;
    private static LogFactory logFactory;

    public static void Init(string mainPath)
    {
        string file = ResolveCurrentLogPath(mainPath);
        LoggingConfiguration config = new();
        config.AddRuleForAllLevels(
            new NLog.Targets.FileTarget()
            {
                DeleteOldFileOnStartup = false,
                ReplaceFileContentsOnEachWrite = false,
                KeepFileOpen = false,
                FileName = file,
                Layout = new SimpleLayout(
                    "${longdate} [${level:uppercase=true}] (${threadid}) ${message:withexception=true}"
                ),
            }
        );
        logFactory = new LogFactory() { ThrowExceptions = false, Configuration = config };

        try
        {
            logger = logFactory.GetLogger("Pulsar");
        }
        catch
        {
            logger = null;
        }
    }

    private static string ResolveCurrentLogPath(string mainPath)
    {
        string markerPath = Path.Combine(mainPath, currentLogFileName);

        try
        {
            string fileName = File.ReadAllText(markerPath).Trim();
            if (IsInfoLogFileName(fileName))
                return Path.Combine(mainPath, fileName);
        }
        catch
        {
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff", CultureInfo.InvariantCulture);
        string fallbackFileName = $"{fileNameBase}_{timestamp}{fileExtension}";
        WriteCurrentLogMarker(mainPath, fallbackFileName);
        return Path.Combine(mainPath, fallbackFileName);
    }

    private static void WriteCurrentLogMarker(string mainPath, string fileName)
    {
        try
        {
            File.WriteAllText(Path.Combine(mainPath, currentLogFileName), fileName);
        }
        catch
        {
        }
    }

    private static bool IsInfoLogFileName(string fileName)
    {
        return !string.IsNullOrWhiteSpace(fileName) &&
               string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal) &&
               fileName.StartsWith(fileNameBase + "_", StringComparison.OrdinalIgnoreCase) &&
               fileName.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase);
    }

    public static void Error(string text)
    {
        WriteLine(text, LogLevel.Error);
    }

    public static void Warn(string text)
    {
        WriteLine(text, LogLevel.Warn);
    }

    public static void WriteLine(string text, LogLevel level = null)
    {
        try
        {
            level ??= LogLevel.Info;
            logger?.Log(level, text);
        }
        catch
        {
            Dispose();
        }
    }

    public static void Dispose()
    {
        if (logger is null)
            return;

        try
        {
            logFactory.Flush();
            logFactory.Dispose();
        }
        catch { }
        logger = null;
        logFactory = null;
    }
}
