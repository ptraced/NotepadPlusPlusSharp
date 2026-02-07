using System.Windows;
using System.Windows.Input;

namespace NotepadPlusPlusSharp.Dialogs;

public partial class GoToLineWindow : Window
{
    public int SelectedLine { get; private set; }
    public int MaxLine { get; set; } = 1;

    public GoToLineWindow(int currentLine, int maxLine)
    {
        InitializeComponent();
        MaxLine = maxLine;
        InfoText.Text = $"Line number (1 - {maxLine}):";
        LineNumberBox.Text = currentLine.ToString();
        LineNumberBox.Focus();
        LineNumberBox.SelectAll();
    }

    private void Go_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(LineNumberBox.Text, out int line))
        {
            SelectedLine = Math.Max(1, Math.Min(line, MaxLine));
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("Please enter a valid line number.", "Go To Line",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void LineNumberBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !int.TryParse(e.Text, out _);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }
}
