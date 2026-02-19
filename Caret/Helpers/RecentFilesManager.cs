using System.IO;
using System.Text.Json;

namespace Caret.Helpers;

public static class RecentFilesManager
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Caret");

    private static readonly string RecentFilesPath = Path.Combine(SettingsDir, "recent_files.json");
    private const int MaxRecentFiles = 15;
    private const long MaxRecentFileSize = 1 * 1024 * 1024;

    public static List<string> Load()
    {
        try
        {
            if (File.Exists(RecentFilesPath))
            {
                var info = new FileInfo(RecentFilesPath);
                if (info.Length > MaxRecentFileSize)
                    return new List<string>();

                var json = File.ReadAllText(RecentFilesPath);
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
        }
        catch { }
        return new List<string>();
    }

    public static void Save(List<string> recentFiles)
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(recentFiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(RecentFilesPath, json);
        }
        catch { }
    }

    public static List<string> AddFile(string filePath)
    {
        var recentFiles = Load();
        recentFiles.RemoveAll(f => string.Equals(f, filePath, StringComparison.OrdinalIgnoreCase));
        recentFiles.Insert(0, filePath);
        if (recentFiles.Count > MaxRecentFiles)
            recentFiles = recentFiles.Take(MaxRecentFiles).ToList();
        Save(recentFiles);
        return recentFiles;
    }

    public static void ClearAll()
    {
        Save(new List<string>());
    }
}
