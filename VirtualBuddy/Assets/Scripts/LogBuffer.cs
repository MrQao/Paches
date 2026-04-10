using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class LogBuffer
{
    private static readonly List<string> logs = new List<string>();
    private static int maxCount = 50;

    public static void Log(string message)
    {
        if (logs.Count >= maxCount)
            logs.RemoveAt(0);
        logs.Add(message);
    }

    public static string GetFormattedLogs()
    {
        StringBuilder sb = new StringBuilder();
        for (int i = logs.Count - 1; i >= 0; i--)  // 从最后一条开始
        {
            sb.AppendLine(logs[i]);
        }
        return sb.ToString();
    }

    public static void Clear()
    {
        logs.Clear();
    }
}

