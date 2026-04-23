using System;
using System.IO;
using UnityEngine;

public static class SessionDebugLogger
{
    private static readonly object SyncRoot = new();
    private static string logFilePath;
    private static bool sessionActive;
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
            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath) ?? ".");
            File.WriteAllText(LogFilePath, string.Empty);
            sessionActive = true;
            WriteLine("SESSION", $"Started new play session: {sessionLabel}");
        }
    }

    public static void SetGameTimeProvider(Func<string> provider)
    {
        lock (SyncRoot)
        {
            gameTimeProvider = provider;
        }
    }

    public static void Log(string category, string message)
    {
        lock (SyncRoot)
        {
            if (!sessionActive)
            {
                return;
            }

            WriteLine(category, message);
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
}

