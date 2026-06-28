using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Layouts;

namespace Pulsar.Shared;

public interface IGameLog
{
    bool Open();
    bool Exists();
    void Write(string line);
}

public static class LogFile
{
    public static IGameLog GameLog = null;

    private const string fileNameBase = "info";
    private const string fileExtension = ".log";
    private const string currentLogFileName = "info.current";
    private static Logger logger;
    private static LogFactory logFactory;
    private static string file;

    public static void Init(string mainPath)
    {
        file = CreateTimestampedLogPath(mainPath);
        WriteCurrentLogMarker(mainPath, file);
        LoggingConfiguration config = new();
        config.AddRuleForAllLevels(
            new NLog.Targets.FileTarget()
            {
                DeleteOldFileOnStartup = false,
                ReplaceFileContentsOnEachWrite = false,
                FileName = file,
                KeepFileOpen = false,
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

    private static string CreateTimestampedLogPath(string mainPath)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmssfff", CultureInfo.InvariantCulture);
        string fileName = $"{fileNameBase}_{timestamp}{fileExtension}";
        string path = Path.Combine(mainPath, fileName);

        for (int suffix = 1; File.Exists(path); suffix++)
        {
            fileName = $"{fileNameBase}_{timestamp}_{suffix}{fileExtension}";
            path = Path.Combine(mainPath, fileName);
        }

        return path;
    }

    private static void WriteCurrentLogMarker(string mainPath, string logPath)
    {
        try
        {
            File.WriteAllText(
                Path.Combine(mainPath, currentLogFileName),
                Path.GetFileName(logPath));
        }
        catch
        {
        }
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

    public static void Open()
    {
        if (file is not null)
            Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
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
