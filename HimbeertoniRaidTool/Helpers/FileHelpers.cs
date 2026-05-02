using System.IO;
using Dalamud.Utility;
using Serilog;

namespace HimbeertoniRaidTool.Plugin.Helpers;

public static class FileHelpers
{
    internal static bool TryWrite(FileInfo file, string data, ILogger logger)
    {
        try
        {
            FilesystemUtil.WriteAllTextSafe(file.FullName, data);
            return true;
        }
        catch (Exception e)
        {
            logger.Error(e, "Could not write data file: {FileFullName}", file.FullName);
            return false;
        }
    }
    public static bool TryRead(FileInfo file, out string data, ILogger logger)
    {
        data = "";
        try
        {
            using var reader = file.OpenText();
            data = reader.ReadToEnd();
            return true;
        }
        catch (Exception e)
        {
            logger.Error(e, "Could not load data file");
            return false;
        }
    }
}