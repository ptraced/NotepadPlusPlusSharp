using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;

namespace NotepadPlusPlusSharp.Models;

public class DocumentModel : INotifyPropertyChanged
{
    private static int _newDocumentCount = 0;

    private string? _filePath;
    private string _fileName;
    private bool _isModified;
    private Encoding _encoding = new UTF8Encoding(false);
    private string _language = "Normal Text";
    private IHighlightingDefinition? _syntaxHighlighting;
    private TextDocument _document;
    private string _lineEnding = "Windows (CR LF)";
    private double _fontSize = 14;

    public DocumentModel()
    {
        _newDocumentCount++;
        _fileName = $"new {_newDocumentCount}";
        _document = new TextDocument();
    }

    public string? FilePath
    {
        get => _filePath;
        set
        {
            _filePath = value;
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
        get => _isModified;
        set { _isModified = value; OnPropertyChanged(); OnPropertyChanged(nameof(Title)); }
    }

    public Encoding Encoding
    {
        get => _encoding;
        set { _encoding = value; OnPropertyChanged(); OnPropertyChanged(nameof(EncodingName)); }
    }

    public string EncodingName
    {
        get
        {
            if (_encoding is UTF8Encoding utf8)
                return utf8.GetPreamble().Length > 0 ? "UTF-8-BOM" : "UTF-8";
            if (_encoding.CodePage == 1252 || _encoding.CodePage == 0)
                return "ANSI";
            if (_encoding is UnicodeEncoding unicode)
            {
                var preamble = unicode.GetPreamble();
                if (preamble.Length >= 2 && preamble[0] == 0xFF)
                    return "UCS-2 LE BOM";
                return "UCS-2 BE BOM";
            }
            return _encoding.EncodingName;
        }
    }

    public string Language
    {
        get => _language;
        set { _language = value; OnPropertyChanged(); }
    }

    public IHighlightingDefinition? SyntaxHighlighting
    {
        get => _syntaxHighlighting;
        set { _syntaxHighlighting = value; OnPropertyChanged(); }
    }

    public TextDocument Document
    {
        get => _document;
        set { _document = value; OnPropertyChanged(); }
    }

    public string LineEnding
    {
        get => _lineEnding;
        set { _lineEnding = value; OnPropertyChanged(); }
    }

    public bool AutoDetectLanguage { get; set; } = true;

    public bool IsLargeFile { get; set; }

    public long FileSize { get; set; }

    public double FontSize
    {
        get => _fontSize;
        set { _fontSize = Math.Max(6, Math.Min(72, value)); OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
