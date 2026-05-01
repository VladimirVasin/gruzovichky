using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum SessionDebugLogLevel
{
    Info,
    Verbose
}

public static class SessionDebugLogger
{
    private static readonly object SyncRoot = new();
    private static readonly HashSet<string> VerboseCategories = new(StringComparer.OrdinalIgnoreCase);
    private static string logFilePath;
    private static bool sessionActive;
    private static bool verboseLogging;
    private static Func<string> gameTimeProvider;

    public static string LogFilePath
    {
        get
        {
            if (string.IsNullOrWhiteSpace(logFilePath))
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
                logFilePath = Path.Combine(projectRoot, "debug.log");
            }

            return logFilePath;
        }
    }

    public static void StartNewSession(string sessionLabel)
    {
        lock (SyncRoot)
        {
            RefreshSettingsFromEnvironment();
            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath) ?? ".");
            File.WriteAllText(LogFilePath, string.Empty);
            sessionActive = true;
            WriteLine("SESSION", $"Started new play session: {sessionLabel}");
            WriteLine("SESSION", $"Debug logging verbose={(verboseLogging ? "on" : "off")}; categories={FormatVerboseCategories()}.");
        }
    }

    public static void SetGameTimeProvider(Func<string> provider)
    {
        lock (SyncRoot)
        {
            gameTimeProvider = provider;
        }
    }

    public static void Log(string category, string message, SessionDebugLogLevel level = SessionDebugLogLevel.Info)
    {
        lock (SyncRoot)
        {
            if (!sessionActive || !ShouldWrite(category, level))
            {
                return;
            }

            WriteLine(category, message);
        }
    }

    public static void LogVerbose(string category, string message)
    {
        Log(category, message, SessionDebugLogLevel.Verbose);
    }

    public static bool IsVerboseEnabled(string category)
    {
        lock (SyncRoot)
        {
            return ShouldWrite(category, SessionDebugLogLevel.Verbose);
        }
    }

    public static void EndSession(string reason)
    {
        lock (SyncRoot)
        {
            if (!sessionActive)
            {
                return;
            }

            WriteLine("SESSION", $"Ended play session: {reason}");
            sessionActive = false;
        }
    }

    private static void WriteLine(string category, string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        string gameTimePrefix = string.Empty;
        if (gameTimeProvider != null)
        {
            try
            {
                string gameTime = gameTimeProvider();
                if (!string.IsNullOrWhiteSpace(gameTime))
                {
                    gameTimePrefix = $" [GAME {gameTime}]";
                }
            }
            catch
            {
                // Keep logger resilient; if the game-time provider fails, fall back to system-time-only logging.
            }
        }

        File.AppendAllText(LogFilePath, $"[{timestamp}]{gameTimePrefix} [{category}] {message}{Environment.NewLine}");
    }

    private static bool ShouldWrite(string category, SessionDebugLogLevel level)
    {
        if (level == SessionDebugLogLevel.Info)
        {
            return true;
        }

        return verboseLogging || VerboseCategories.Contains(category ?? string.Empty);
    }

    private static void RefreshSettingsFromEnvironment()
    {
        verboseLogging = IsTruthy(Environment.GetEnvironmentVariable("GRUZOVICHKY_DEBUG_VERBOSE"));
        VerboseCategories.Clear();

        string categories = Environment.GetEnvironmentVariable("GRUZOVICHKY_DEBUG_VERBOSE_CATEGORIES");
        if (string.IsNullOrWhiteSpace(categories))
        {
            return;
        }

        string[] parts = categories.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string part in parts)
        {
            string category = part.Trim();
            if (!string.IsNullOrWhiteSpace(category))
            {
                VerboseCategories.Add(category);
            }
        }
    }

    private static bool IsTruthy(string value)
    {
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatVerboseCategories()
    {
        return VerboseCategories.Count == 0 ? "none" : string.Join(",", VerboseCategories);
    }
}
