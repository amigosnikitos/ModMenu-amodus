using System;
using System.Collections.Generic;

namespace Amodus;

/// <summary>
/// Logs an error one time only. Code that runs every frame (Update, OnGUI) uses it,
/// otherwise a single bug would write thousands of lines per second into the log.
/// </summary>
internal static class ErrorOnce
{
    private static readonly HashSet<string> Seen = new HashSet<string>();

    public static void Log(string where, Exception exception)
    {
        var key = where + "|" + exception.GetType().FullName + "|" + exception.Message;
        if (Seen.Add(key))
            AmodusPlugin.LogSource.LogError($"[{where}] {exception}");
    }
}
