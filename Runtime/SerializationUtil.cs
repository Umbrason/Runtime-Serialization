using System;
using System.Collections.Generic;
using System.IO;


public static class SerializationUtil
{
    public static string IEnumerableToString<T>(IEnumerable<T> enumerable)
    {
        string output = "[";
        if (enumerable != null)
            foreach (T item in enumerable)
            {
                output += $"{item?.ToString()}, ";
            }
        else output += "null";
        if (output.Length >= 2)
            output.Remove(output.Length - 2, 2);
        output += "]";
        return output;
    }

    public static void CreateDirectory(string directoryPath)
    {
        var pathParts = directoryPath.Split(new string[] { ":\\" }, StringSplitOptions.None);
        var startDirectory = pathParts[0].Replace("\\\\?\\", "") + ":\\";
        var previousDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(startDirectory);

        var directoryQueue = new Queue<string>(pathParts[1].Split('\\'));
        while (directoryQueue.Count > 0)
        {
            var dir = $"\\{directoryQueue.Dequeue()}";
            var relativeDir = $".{dir}";
            Directory.CreateDirectory(relativeDir);
            Directory.SetCurrentDirectory(relativeDir);
        }
        Directory.SetCurrentDirectory(previousDirectory);
    }
}