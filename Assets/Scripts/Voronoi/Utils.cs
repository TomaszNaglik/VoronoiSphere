using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Utils
{
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
}
