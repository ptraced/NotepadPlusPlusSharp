using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;

namespace NotepadPlusPlusSharp.Dialogs;

public partial class FindReplaceWindow : Window
{
    private readonly Func<TextEditor?> _getActiveEditor;
    private int _lastSearchOffset = -1;

    public FindReplaceWindow(Func<TextEditor?> getActiveEditor)
    {
        InitializeComponent();
        _getActiveEditor = getActiveEditor;
    }

    public void ShowFind(string? selectedText = null)
    {
        FindTab.IsChecked = true;
        if (!string.IsNullOrEmpty(selectedText))
            FindTextBox.Text = selectedText;
        FindTextBox.Focus();
        FindTextBox.SelectAll();
        Show();
        Activate();
    }

    public void ShowReplace(string? selectedText = null)
    {
        ReplaceTab.IsChecked = true;
        if (!string.IsNullOrEmpty(selectedText))
            FindTextBox.Text = selectedText;
        FindTextBox.Focus();
        FindTextBox.SelectAll();
        Show();
        Activate();
    }

    private void FindTab_Checked(object sender, RoutedEventArgs e)
    {
        if (ReplacePanel != null) ReplacePanel.Visibility = Visibility.Collapsed;
        if (ReplaceButton != null) ReplaceButton.Visibility = Visibility.Collapsed;
        if (ReplaceAllButton != null) ReplaceAllButton.Visibility = Visibility.Collapsed;
        Title = "Find";
    }

    private void ReplaceTab_Checked(object sender, RoutedEventArgs e)
    {
        if (ReplacePanel != null) ReplacePanel.Visibility = Visibility.Visible;
        if (ReplaceButton != null) ReplaceButton.Visibility = Visibility.Visible;
        if (ReplaceAllButton != null) ReplaceAllButton.Visibility = Visibility.Visible;
        Title = "Replace";
    }

    private void FindNext_Click(object sender, RoutedEventArgs e) => FindNext();
    private void FindPrevious_Click(object sender, RoutedEventArgs e) => FindPrevious();

    private void Replace_Click(object sender, RoutedEventArgs e)
    {
        var editor = _getActiveEditor();
        if (editor == null) return;

        if (editor.SelectionLength > 0)
        {
            editor.Document.Replace(editor.SelectionStart, editor.SelectionLength, ReplaceTextBox.Text);
        }
        FindNext();
    }

    private void ReplaceAll_Click(object sender, RoutedEventArgs e)
    {
        var editor = _getActiveEditor();
        if (editor == null || string.IsNullOrEmpty(FindTextBox.Text)) return;

        var searchText = FindTextBox.Text;
        var replaceText = ReplaceTextBox.Text;
        var text = editor.Document.Text;
        int count = 0;

        if (UseRegexCheck.IsChecked == true)
        {
            try
            {
                var options = RegexOptions.None;
                if (MatchCaseCheck.IsChecked != true)
                    options |= RegexOptions.IgnoreCase;
                var regex = new Regex(searchText, options);
                var newText = regex.Replace(text, replaceText);
                count = regex.Matches(text).Count;
                if (count > 0)
                {
                    editor.Document.BeginUpdate();
                    editor.Document.Text = newText;
                    editor.Document.EndUpdate();
                }
            }
            catch (RegexParseException ex)
            {
                StatusText.Text = $"Regex error: {ex.Message}";
                return;
            }
        }
        else
        {
            var comparison = MatchCaseCheck.IsChecked == true
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            editor.Document.BeginUpdate();
            int pos = 0;
            while (true)
            {
                pos = text.IndexOf(searchText, pos, comparison);
                if (pos < 0) break;

                if (WholeWordCheck.IsChecked == true && !IsWholeWord(text, pos, searchText.Length))
                {
                    pos += searchText.Length;
                    continue;
                }

                text = text.Remove(pos, searchText.Length).Insert(pos, replaceText);
                pos += replaceText.Length;
                count++;
            }
            if (count > 0)
                editor.Document.Text = text;
            editor.Document.EndUpdate();
        }

        StatusText.Text = $"{count} occurrence(s) replaced.";
    }

    private void Count_Click(object sender, RoutedEventArgs e)
    {
        var editor = _getActiveEditor();
        if (editor == null || string.IsNullOrEmpty(FindTextBox.Text)) return;

        int count = CountOccurrences(editor.Document.Text, FindTextBox.Text);
        StatusText.Text = $"{count} occurrence(s) found.";
    }

    private int CountOccurrences(string text, string searchText)
    {
        if (UseRegexCheck.IsChecked == true)
        {
            try
            {
                var options = RegexOptions.None;
                if (MatchCaseCheck.IsChecked != true)
                    options |= RegexOptions.IgnoreCase;
                return Regex.Matches(text, searchText, options).Count;
            }
            catch { return 0; }
        }

        var comparison = MatchCaseCheck.IsChecked == true
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        int count = 0, pos = 0;
        while (true)
        {
            pos = text.IndexOf(searchText, pos, comparison);
            if (pos < 0) break;
            if (WholeWordCheck.IsChecked != true || IsWholeWord(text, pos, searchText.Length))
                count++;
            pos += searchText.Length;
        }
        return count;
    }

    public void FindNext()
    {
        var editor = _getActiveEditor();
        if (editor == null || string.IsNullOrEmpty(FindTextBox.Text)) return;

        var searchText = FindTextBox.Text;
        var text = editor.Document.Text;
        int startPos = editor.SelectionStart + editor.SelectionLength;

        int foundPos = FindInText(text, searchText, startPos, forward: true);

        if (foundPos >= 0)
        {
            SelectResult(editor, foundPos, searchText.Length);
            StatusText.Text = "";
        }
        else if (WrapAroundCheck.IsChecked == true && startPos > 0)
        {
            foundPos = FindInText(text, searchText, 0, forward: true);
            if (foundPos >= 0)
            {
                SelectResult(editor, foundPos, searchText.Length);
                StatusText.Text = "Wrapped around.";
            }
            else
            {
                StatusText.Text = "No matches found.";
            }
        }
        else
        {
            StatusText.Text = "No matches found.";
        }
    }

    public void FindPrevious()
    {
        var editor = _getActiveEditor();
        if (editor == null || string.IsNullOrEmpty(FindTextBox.Text)) return;

        var searchText = FindTextBox.Text;
        var text = editor.Document.Text;
        int startPos = editor.SelectionStart;

        int foundPos = FindInText(text, searchText, startPos, forward: false);

        if (foundPos >= 0)
        {
            SelectResult(editor, foundPos, searchText.Length);
            StatusText.Text = "";
        }
        else if (WrapAroundCheck.IsChecked == true)
        {
            foundPos = FindInText(text, searchText, text.Length, forward: false);
            if (foundPos >= 0)
            {
                SelectResult(editor, foundPos, searchText.Length);
                StatusText.Text = "Wrapped around.";
            }
            else
            {
                StatusText.Text = "No matches found.";
            }
        }
        else
        {
            StatusText.Text = "No matches found.";
        }
    }

    private int FindInText(string text, string searchText, int startPos, bool forward)
    {
        if (UseRegexCheck.IsChecked == true)
        {
            try
            {
                var options = RegexOptions.None;
                if (MatchCaseCheck.IsChecked != true)
                    options |= RegexOptions.IgnoreCase;

                if (forward)
                {
                    var match = Regex.Match(text.Substring(startPos), searchText, options);
                    return match.Success ? startPos + match.Index : -1;
                }
                else
                {
                    var matches = Regex.Matches(text.Substring(0, startPos), searchText, options);
                    return matches.Count > 0 ? matches[matches.Count - 1].Index : -1;
                }
            }
            catch { return -1; }
        }

        var comparison = MatchCaseCheck.IsChecked == true
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        if (forward)
        {
            int pos = startPos;
            while (pos < text.Length)
            {
                pos = text.IndexOf(searchText, pos, comparison);
                if (pos < 0) return -1;
                if (WholeWordCheck.IsChecked != true || IsWholeWord(text, pos, searchText.Length))
                    return pos;
                pos++;
            }
            return -1;
        }
        else
        {
            int pos = Math.Min(startPos - 1, text.Length - searchText.Length);
            while (pos >= 0)
            {
                pos = text.LastIndexOf(searchText, pos, pos + 1, comparison);
                if (pos < 0) return -1;
                if (WholeWordCheck.IsChecked != true || IsWholeWord(text, pos, searchText.Length))
                    return pos;
                pos--;
            }
            return -1;
        }
    }

    private static bool IsWholeWord(string text, int start, int length)
    {
        if (start > 0 && char.IsLetterOrDigit(text[start - 1]))
            return false;
        int end = start + length;
        if (end < text.Length && char.IsLetterOrDigit(text[end]))
            return false;
        return true;
    }

    private void SelectResult(TextEditor editor, int offset, int length)
    {
        editor.Select(offset, length);
        editor.ScrollTo(editor.Document.GetLineByOffset(offset).LineNumber, 0);
        editor.Focus();
        _lastSearchOffset = offset;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Hide();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            FindNext();
            e.Handled = true;
        }
        else if (e.Key == Key.F3)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
                FindPrevious();
            else
                FindNext();
            e.Handled = true;
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}
