using System.IO;


public class PdbHelper
{
    public static string GetPdbFileName(string assemblyFileName)
    {
        return Path.ChangeExtension(assemblyFileName, ".pdb");
    }
}

//public class PdbReaderProvider:
