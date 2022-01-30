using System;
using System.Collections;
using System.Collections.Generic;

using System.Text;
using UnityEngine;

public class Utils
{
    public static System.Diagnostics.Stopwatch timer;
    private static long delta = 0;
    public static void LogArray(string title, int[] array)
    {
        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < array.Length; i++)
        {
            stringBuilder.Append(array[i] + "  ");
            
        }
        Debug.Log(title + stringBuilder);
    }

    internal static void LogArray(string title, Edge[] edges)
    {
        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < edges.Length; i++)
        {
            stringBuilder.Append(edges[i] + "  ");

        }
        Debug.Log(title + stringBuilder);
    }

    internal static void LogArray(string title, Vector3[] array)
    {
        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < array.Length; i++)
        {
            stringBuilder.Append(array[i].ToString() + "  ");

        }
        Debug.Log(title + stringBuilder);
    }

    internal static void StartTimer()
    {
        if (timer == null)
            timer = new System.Diagnostics.Stopwatch();
        delta = 0;
        timer.Start();
        
    }
    internal static void StopTimer()
    {

        timer.Stop();
        timer.Reset();
        
    }

    internal static void LogTime(String text)
    {
        long time = timer.ElapsedMilliseconds - delta;
        delta += time;
        Debug.Log(text + ": " + delta + "ms");
    }
}
