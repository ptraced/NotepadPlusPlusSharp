using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using Caret.Helpers;
using Caret.Models;

namespace Caret.Dialogs;

public class BackupDisplayItem
{
    public string FileName { get; set; } = "";
    public string? OriginalFilePath { get; set; }
    public DateTime BackupDate { get; set; }
    public string Language { get; set; } = "";
    public string EncodingName { get; set; } = "";
    public long OriginalSize { get; set; }
    public string SizeDisplay => FormatSize(OriginalSize);
    public MongoDB.Bson.ObjectId Id { get; set; }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1024 * 1024)
            return $"{bytes / (1024.0 * 1024):F1} MB";
        if (bytes >= 1024)
            return $"{bytes / 1024.0:F1} KB";
        return $"{bytes} B";
    }
}

public partial class BackupWindow : Window
{
    private readonly Func<DocumentModel?> _getActiveDocument;
    private readonly Func<string?> _getActiveContent;
    private readonly Action<string, string, string> _onRestore;
    public string? RestoredContent { get; private set; }
    public string? RestoredFileName { get; private set; }
    public string? RestoredLanguage { get; private set; }

    public BackupWindow(
        Func<DocumentModel?> getActiveDocument,
        Func<string?> getActiveContent,
        Action<string, string, string> onRestore)
    {
        InitializeComponent();
        _getActiveDocument = getActiveDocument;
        _getActiveContent = getActiveContent;
        _onRestore = onRestore;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var doc = _getActiveDocument();
        if (doc != null)
        {
            var path = doc.FilePath ?? doc.FileName;
            CurrentDocLabel.Text = $"Current: {path}";
        }
    }

    private string ConnectionString => ConnectionStringBox.Text.Trim();
    private string Password => PasswordBox.Password;

    private bool ValidateInputs(bool requirePassword = true)
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            ShowError("Please enter a MongoDB connection string.");
            return false;
        }
        if (requirePassword && string.IsNullOrWhiteSpace(Password))
        {
            ShowError("Please enter an encryption password.");
            return false;
        }
        if (requirePassword && Password.Length < 8)
        {
            ShowError("Password must be at least 8 characters for adequate security.");
            return false;
        }
        return true;
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            ShowError("Please enter a connection string.");
            return;
        }

        TestConnectionButton.IsEnabled = false;
        TestConnectionButton.Content = "Testing...";
        try
        {
            var success = await BackupManager.TestConnectionAsync(ConnectionString);
            if (success)
                MessageBox.Show("Connected to MongoDB successfully.", "Connection Test",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            else
                ShowError("Could not connect to MongoDB. Make sure the server is running.");
        }
        catch (Exception ex)
        {
            ShowError($"Connection failed:\n{ex.Message}");
        }
        finally
        {
            TestConnectionButton.IsEnabled = true;
            TestConnectionButton.Content = "Test";
        }
    }

    private async void Backup_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs()) return;

        var doc = _getActiveDocument();
        var content = _getActiveContent();
        if (doc == null || content == null)
        {
            ShowError("No active document to back up.");
            return;
        }

        BackupButton.IsEnabled = false;
        BackupButton.Content = "Encrypting & Saving...";
        try
        {
            await BackupManager.CreateBackupAsync(
                ConnectionString,
                Password,
                doc.FileName,
                doc.FilePath,
                content,
                doc.Language,
                doc.EncodingName);

            MessageBox.Show(
                $"'{doc.FileName}' backed up and encrypted successfully.",
                "Backup Complete", MessageBoxButton.OK, MessageBoxImage.Information);

            await RefreshListAsync();
        }
        catch (Exception ex)
        {
            ShowError($"Backup failed:\n{ex.Message}");
        }
        finally
        {
            BackupButton.IsEnabled = true;
            BackupButton.Content = "Backup Current Document";
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            ShowError("Please enter a connection string.");
            return;
        }
        await RefreshListAsync();
    }

    private async Task RefreshListAsync()
    {
        try
        {
            var backups = await BackupManager.ListBackupsAsync(ConnectionString);
            var items = backups.Select(b => new BackupDisplayItem
            {
                Id = b.Id,
                FileName = b.FileName,
                OriginalFilePath = b.OriginalFilePath,
                BackupDate = b.BackupDate,
                Language = b.Language,
                EncodingName = b.EncodingName,
                OriginalSize = b.OriginalSize,
            }).ToList();

            BackupsList.ItemsSource = items;
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load backups:\n{ex.Message}");
        }
    }

    private void BackupsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var hasSelection = BackupsList.SelectedItem != null;
        RestoreButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
    }

    private async void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateInputs()) return;

        if (BackupsList.SelectedItem is not BackupDisplayItem selected)
        {
            ShowError("Please select a backup to restore.");
            return;
        }

        RestoreButton.IsEnabled = false;
        RestoreButton.Content = "Decrypting...";
        try
        {
            var content = await BackupManager.RestoreBackupAsync(
                ConnectionString, Password, selected.Id);

            _onRestore(content, selected.FileName, selected.Language);

            MessageBox.Show(
                $"'{selected.FileName}' restored successfully.",
                "Restore Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (CryptographicException)
        {
            ShowError("Decryption failed. Wrong password or corrupted backup.");
        }
        catch (Exception ex)
        {
            ShowError($"Restore failed:\n{ex.Message}");
        }
        finally
        {
            RestoreButton.IsEnabled = true;
            RestoreButton.Content = "Restore Selected";
        }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (BackupsList.SelectedItem is not BackupDisplayItem selected)
        {
            ShowError("Please select a backup to delete.");
            return;
        }

        var confirm = MessageBox.Show(
            $"Permanently delete the backup of '{selected.FileName}' from {selected.BackupDate:yyyy-MM-dd HH:mm}?",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            await BackupManager.DeleteBackupAsync(ConnectionString, selected.Id);
            await RefreshListAsync();
        }
        catch (Exception ex)
        {
            ShowError($"Delete failed:\n{ex.Message}");
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private static void ShowError(string message)
    {
        MessageBox.Show(message, "Backup Manager", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
