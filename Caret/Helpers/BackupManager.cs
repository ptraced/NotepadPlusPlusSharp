using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Caret.Helpers;

public class BackupEntry
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string FileName { get; set; } = "";
    public string? OriginalFilePath { get; set; }
    public DateTime BackupDate { get; set; }
    public string Language { get; set; } = "Normal Text";
    public string EncodingName { get; set; } = "UTF-8";
    public long OriginalSize { get; set; }

    public byte[] Salt { get; set; } = [];
    public byte[] Nonce { get; set; } = [];
    public byte[] CipherText { get; set; } = [];
    public byte[] AuthTag { get; set; } = [];
}

public static class BackupManager
{
    private const string DatabaseName = "CaretBackups";
    private const string CollectionName = "backups";
    private const int SaltSize = 32;
    private const int NonceSize = 12; // AES-GCM standard
    private const int TagSize = 16;   // AES-GCM standard
    private const int KeySize = 32;   // AES-256
    private const int Pbkdf2Iterations = 600_000;

    private static IMongoCollection<BackupEntry> GetCollection(string connectionString)
    {
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(DatabaseName);
        return database.GetCollection<BackupEntry>(CollectionName);
    }

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA512,
            KeySize);
    }

    private static (byte[] cipherText, byte[] nonce, byte[] tag, byte[] salt) Encrypt(string plainText, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = DeriveKey(password, salt);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherText = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherText, tag);

        CryptographicOperations.ZeroMemory(key);

        return (cipherText, nonce, tag, salt);
    }

    private static string Decrypt(byte[] cipherText, byte[] nonce, byte[] tag, byte[] salt, string password)
    {
        var key = DeriveKey(password, salt);
        var plainBytes = new byte[cipherText.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipherText, tag, plainBytes);

        CryptographicOperations.ZeroMemory(key);

        return Encoding.UTF8.GetString(plainBytes);
    }

    public static async Task<bool> TestConnectionAsync(string connectionString)
    {
        try
        {
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(DatabaseName);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await database.ListCollectionNamesAsync(cancellationToken: cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task CreateBackupAsync(
        string connectionString,
        string password,
        string fileName,
        string? filePath,
        string content,
        string language,
        string encodingName)
    {
        var (cipherText, nonce, tag, salt) = Encrypt(content, password);

        var entry = new BackupEntry
        {
            FileName = fileName,
            OriginalFilePath = filePath,
            BackupDate = DateTime.UtcNow,
            Language = language,
            EncodingName = encodingName,
            OriginalSize = Encoding.UTF8.GetByteCount(content),
            Salt = salt,
            Nonce = nonce,
            CipherText = cipherText,
            AuthTag = tag,
        };

        var collection = GetCollection(connectionString);
        await collection.InsertOneAsync(entry);
    }

    public static async Task<List<BackupEntry>> ListBackupsAsync(string connectionString)
    {
        var collection = GetCollection(connectionString);
        var sort = Builders<BackupEntry>.Sort.Descending(b => b.BackupDate);
        return await collection.Find(_ => true).Sort(sort).ToListAsync();
    }

    public static async Task<string> RestoreBackupAsync(string connectionString, string password, ObjectId backupId)
    {
        var collection = GetCollection(connectionString);
        var entry = await collection.Find(b => b.Id == backupId).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Backup not found.");

        return Decrypt(entry.CipherText, entry.Nonce, entry.AuthTag, entry.Salt, password);
    }

    public static async Task DeleteBackupAsync(string connectionString, ObjectId backupId)
    {
        var collection = GetCollection(connectionString);
        await collection.DeleteOneAsync(b => b.Id == backupId);
    }

    public static async Task DeleteAllBackupsAsync(string connectionString)
    {
        var collection = GetCollection(connectionString);
        await collection.DeleteManyAsync(_ => true);
    }
}
