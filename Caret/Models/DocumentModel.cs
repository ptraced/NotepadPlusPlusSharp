using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace Caret.Models;

public class DocumentModel : INotifyPropertyChanged
{
    private static int _newDocumentCount;
    private string _fileName;

    public DocumentModel()
    {
        _newDocumentCount++;
        _fileName = $"new {_newDocumentCount}";
    }

    public string? FilePath
    {
        get => field;
        set
        {
            field = value;
            if (value != null)
                _fileName = Path.GetFileName(value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(FileName));
            OnPropertyChanged(nameof(Title));
        }
    }

    public string FileName
    {
        get => _fileName;
        set { _fileName = value; OnPropertyChanged(); OnPropertyChanged(nameof(Title)); }
    }

    public string Title => IsModified ? $"{FileName} *" : FileName;

    public bool IsModified
    {
        get => field;
        set { field = value; OnPropertyChanged(); OnPropertyChanged(nameof(Title)); }
    }

    public Encoding Encoding
    {
        get => field;
        set { field = value; OnPropertyChanged(); OnPropertyChanged(nameof(EncodingName)); }
    } = new UTF8Encoding(false);

    public string EncodingName
    {
        get
        {
            if (Encoding is UTF8Encoding utf8)
                return utf8.GetPreamble().Length > 0 ? "UTF-8-BOM" : "UTF-8";
            if (Encoding.CodePage == 1252 || Encoding.CodePage == 0)
                return "ANSI";
            if (Encoding is UnicodeEncoding unicode)
            {
                var preamble = unicode.GetPreamble();
                if (preamble.Length >= 2 && preamble[0] == 0xFF)
                    return "UCS-2 LE BOM";
                return "UCS-2 BE BOM";
            }
            return Encoding.EncodingName;
        }
    }

    public string Language
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    } = "Normal Text";

    public IHighlightingDefinition? SyntaxHighlighting
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    }

    public TextDocument Document
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    } = new();

    public string LineEnding
    {
        get => field;
        set { field = value; OnPropertyChanged(); }
    } = "Windows (CR LF)";

    public bool AutoDetectLanguage { get; set; } = true;

    public bool IsLargeFile { get; set; }

    public long FileSize { get; set; }

    public double FontSize
    {
        get => field;
        set { field = Math.Max(6, Math.Min(72, value)); OnPropertyChanged(); }
    } = 14;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
