using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class BundleHelper
{
    public static string getPrefix(string source, char seperator)
    {
        var idx = source.IndexOf(seperator);
        if (-1 != idx)
        {
            return source.Substring(0, idx);
        }
        return null;
    }

    public static bool hasPrefix(string source, string prefix)
    {
        return source.Substring(0, prefix.Length).Equals(prefix);
    }

    public static void createDirectoryIfNotExist(string dirPath)
    {
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
    }
}
