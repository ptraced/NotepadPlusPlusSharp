using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NotepadPlusPlusSharp.Helpers;

public class SessionTab
{
    public string? FilePath { get; set; }
    public string FileName { get; set; } = "";
    public string? Content { get; set; }
    public bool IsModified { get; set; }
    public int CaretOffset { get; set; }
    public double ScrollOffsetY { get; set; }
    public double ScrollOffsetX { get; set; }
    public double FontSize { get; set; } = 14;
    public string? SyntaxHighlightingName { get; set; }
    public string Language { get; set; } = "Normal Text";
    public string EncodingName { get; set; } = "UTF-8";
    public string LineEnding { get; set; } = "Windows (CR LF)";
    public bool AutoDetectLanguage { get; set; }
}

public class SessionData
{
    public List<SessionTab> Tabs { get; set; } = new();
    public int ActiveTabIndex { get; set; }
    public double WindowLeft { get; set; } = 100;
    public double WindowTop { get; set; } = 100;
    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 750;
    public bool IsMaximized { get; set; }
    public bool WordWrap { get; set; }
    public bool ShowWhiteSpace { get; set; }
    public bool ShowEndOfLine { get; set; }
    public bool ShowLineNumbers { get; set; } = true;
    public bool ShowIndentGuide { get; set; } = true;
    public bool AlwaysOnTop { get; set; }
}

public static class SessionManager
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "NotepadPlusPlusSharp");

    private static readonly string SessionFilePath = Path.Combine(SettingsDir, "session.json");
    private static readonly string BackupSessionFilePath = Path.Combine(SettingsDir, "session.backup.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static void Save(SessionData session)
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);

            var tempPath = SessionFilePath + ".tmp";
            var json = JsonSerializer.Serialize(session, JsonOptions);
            File.WriteAllText(tempPath, json);

            if (File.Exists(SessionFilePath))
            {
                try { File.Copy(SessionFilePath, BackupSessionFilePath, overwrite: true); } catch { }
            }

            File.Move(tempPath, SessionFilePath, overwrite: true);
        }
        catch { }
    }

    public static SessionData? Load()
    {
        try
        {
            var path = SessionFilePath;

            if (!File.Exists(path))
                path = BackupSessionFilePath;

            if (!File.Exists(path))
                return null;

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SessionData>(json);
        }
        catch
        {
            try
            {
                if (File.Exists(BackupSessionFilePath))
                {
                    var json = File.ReadAllText(BackupSessionFilePath);
                    return JsonSerializer.Deserialize<SessionData>(json);
                }
            }
            catch { }

            return null;
        }
    }

    public static void Clear()
    {
        try { if (File.Exists(SessionFilePath)) File.Delete(SessionFilePath); } catch { }
        try { if (File.Exists(BackupSessionFilePath)) File.Delete(BackupSessionFilePath); } catch { }
    }
}
