using Location.Tools.APIGenerator.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text;

namespace Location.Tools.APIGenerator.Services;

public class SQLiteExtractionService
{
    private readonly ILogger<SQLiteExtractionService> _logger;

    public SQLiteExtractionService(ILogger<SQLiteExtractionService> logger)
    {
        _logger = logger;
    }

    public async Task<ExtractedData> ExtractDataFromZipAsync(string zipFilePath, List<ExtractableEntity> entities)
    {
        try
        {
            _logger.LogInformation("Extracting data from zip file: {ZipPath}", zipFilePath);

            // Parse user info from filename
            var userInfo = ParseUserInfoFromFilename(zipFilePath);
            _logger.LogInformation("Parsed user info: Email={Email}, GUID={Guid}, Date={Date}",
                userInfo.Email, userInfo.AppGuid, userInfo.Date);

            // Extract zip to temporary directory
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                using (var archive = ZipFile.OpenRead(zipFilePath))
                {
                    archive.ExtractToDirectory(tempDir);
                }

                // Find SQLite database file
                var sqliteFiles = Directory.GetFiles(tempDir, "*.db*", SearchOption.AllDirectories)
                    .Concat(Directory.GetFiles(tempDir, "*.sqlite*", SearchOption.AllDirectories))
                    .ToList();

                if (!sqliteFiles.Any())
                {
                    throw new InvalidOperationException("No SQLite database found in zip file");
                }

                var sqliteDbPath = sqliteFiles.First();
                _logger.LogDebug("Found SQLite database: {DbPath}", sqliteDbPath);

                // Extract data from SQLite
                var extractedData = await ExtractDataFromSQLiteAsync(sqliteDbPath, entities, userInfo);

                // Move photos to blob storage path (for future implementation)
                extractedData.PhotoFiles = GetPhotoFiles(tempDir);

                return extractedData;
            }
            finally
            {
                // Cleanup temp directory
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract data from zip: {ZipPath}", zipFilePath);
            throw;
        }
    }

    private UserInfo ParseUserInfoFromFilename(string zipFilePath)
    {
        // Expected format: {email}_{appGUID}_{date in DDMMYY}.zip
        var fileName = Path.GetFileNameWithoutExtension(zipFilePath);
        var parts = fileName.Split('_');

        if (parts.Length != 3)
        {
            throw new ArgumentException($"Invalid zip filename format. Expected: email_guid_date.zip, got: {fileName}");
        }

        return new UserInfo
        {
            Email = parts[0],
            AppGuid = parts[1],
            Date = parts[2]
        };
    }

    private async Task<ExtractedData> ExtractDataFromSQLiteAsync(string sqliteDbPath, List<ExtractableEntity> entities, UserInfo userInfo)
    {
        var extractedData = new ExtractedData
        {
            UserInfo = userInfo,
            TableData = new Dictionary<string, List<Dictionary<string, object>>>()
        };

        var connectionString = $"Data Source={sqliteDbPath};Version=3;";

        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        foreach (var entity in entities)
        {
            try
            {
                _logger.LogDebug("Extracting data from table: {TableName}", entity.TableName);

                var tableData = await ExtractTableDataAsync(connection, entity);
                extractedData.TableData[entity.TableName] = tableData;

                _logger.LogInformation("Extracted {RowCount} rows from table: {TableName}",
                    tableData.Count, entity.TableName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract data from table: {TableName}", entity.TableName);
                // Continue with other tables
            }
        }

        return extractedData;
    }

    private async Task<List<Dictionary<string, object>>> ExtractTableDataAsync(SqliteConnection connection, ExtractableEntity entity)
    {
        var tableData = new List<Dictionary<string, object>>();

        // Build SELECT query - hoover approach (get all data)
        var query = $"SELECT * FROM [{entity.TableName}]";

        using var command = new SqliteCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                var columnName = reader.GetName(i);
                var value = reader.GetValue(i);

                // Convert DBNull to null
                if (value == DBNull.Value)
                {
                    value = null;
                }

                row[columnName] = value;
            }

            tableData.Add(row);
        }

        return tableData;
    }

    private List<string> GetPhotoFiles(string tempDir)
    {
        var photoExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

        return Directory.GetFiles(tempDir, "*.*", SearchOption.AllDirectories)
            .Where(f => photoExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToList();
    }
}

// Supporting classes for extraction
public class ExtractedData
{
    public UserInfo UserInfo { get; set; } = new();
    public Dictionary<string, List<Dictionary<string, object>>> TableData { get; set; } = new();
    public List<string> PhotoFiles { get; set; } = new();
}

public class UserInfo
{
    public string Email { get; set; } = string.Empty;
    public string AppGuid { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
}