// Enhanced SQLiteExtractionService with Extended ASCII support
// File: x3squaredCircles.API.Generator/Services/SQLiteExtractionService.cs

using x3squaredcirecles.API.Generator.APIGenerator.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text;
using System.Globalization;

namespace x3squaredcirecles.API.Generator.APIGenerator.Services;

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
            _logger.LogInformation("Extracting data from zip file with extended ASCII support: {ZipPath}", zipFilePath);

            // Parse user info from filename (handle extended ASCII in email addresses)
            var userInfo = ParseUserInfoFromFilenameWithExtendedASCII(zipFilePath);
            _logger.LogInformation("Parsed user info: Email={Email}, GUID={Guid}, Date={Date}",
                userInfo.Email, userInfo.AppGuid, userInfo.Date);

            // Extract zip to temporary directory with proper encoding handling
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                await ExtractZipWithExtendedASCIIAsync(zipFilePath, tempDir);

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

                // Extract data from SQLite with extended ASCII preservation
                var extractedData = await ExtractDataFromSQLiteWithExtendedASCIIAsync(sqliteDbPath, entities, userInfo);

                // Process photos with extended ASCII filename support
                extractedData.PhotoFiles = GetPhotoFilesWithExtendedASCII(tempDir);

                _logger.LogInformation("Successfully extracted data with extended ASCII support: {RowCount} total rows from {TableCount} tables",
                    extractedData.TableData.Values.Sum(rows => rows.Count), extractedData.TableData.Count);

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

    private UserInfo ParseUserInfoFromFilenameWithExtendedASCII(string zipFilePath)
    {
        // Expected format: {email}_{appGUID}_{date in DDMMYY}.zip
        // Handle extended ASCII characters in email addresses (e.g., José@example.com)
        var fileName = Path.GetFileNameWithoutExtension(zipFilePath);
        var parts = fileName.Split('_');

        if (parts.Length != 3)
        {
            throw new ArgumentException($"Invalid zip filename format. Expected: email_guid_date.zip, got: {fileName}");
        }

        // Decode any URL-encoded extended ASCII characters in email
        var email = System.Web.HttpUtility.UrlDecode(parts[0]);

        // Normalize extended ASCII characters for consistency
        email = email.Normalize(NormalizationForm.FormC);

        // Validate email contains valid extended ASCII characters
        if (ContainsExtendedASCII(email))
        {
            _logger.LogDebug("Email contains extended ASCII characters: {Email}", email);
        }

        return new UserInfo
        {
            Email = email,
            AppGuid = parts[1],
            Date = parts[2]
        };
    }

    private async Task ExtractZipWithExtendedASCIIAsync(string zipFilePath, string tempDir)
    {
        try
        {
            // Use UTF-8 encoding to properly handle extended ASCII filenames in zip entries
            using var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Read, Encoding.UTF8);

            foreach (var entry in archive.Entries)
            {
                // Handle extended ASCII characters in entry names
                var entryName = entry.FullName;

                // Normalize the entry name to handle extended ASCII characters
                var normalizedEntryName = entryName.Normalize(NormalizationForm.FormC);

                var destinationPath = Path.Combine(tempDir, normalizedEntryName);
                var destinationDir = Path.GetDirectoryName(destinationPath);

                if (!string.IsNullOrEmpty(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                if (!string.IsNullOrEmpty(entry.Name))
                {
                    _logger.LogDebug("Extracting file with extended ASCII support: {FileName}", entry.FullName);
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }

            _logger.LogDebug("Successfully extracted zip with extended ASCII filename support");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract zip file with extended ASCII support");
            throw;
        }
    }

    private async Task<ExtractedData> ExtractDataFromSQLiteWithExtendedASCIIAsync(string sqliteDbPath, List<ExtractableEntity> entities, UserInfo userInfo)
    {
        var extractedData = new ExtractedData
        {
            UserInfo = userInfo,
            TableData = new Dictionary<string, List<Dictionary<string, object>>>()
        };

        // Configure SQLite connection for proper text handling with extended ASCII
        var connectionStringBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = sqliteDbPath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Shared,
            // Ensure proper text encoding
            DefaultTimeout = 30
        };

        var connectionString = connectionStringBuilder.ConnectionString;

        using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        // Set SQLite to handle text encoding properly
        await ExecuteSqliteConfigurationAsync(connection);

        foreach (var entity in entities)
        {
            try
            {
                _logger.LogDebug("Extracting data from table with extended ASCII support: {TableName}", entity.TableName);

                var tableData = await ExtractTableDataWithExtendedASCIIAsync(connection, entity);
                extractedData.TableData[entity.TableName] = tableData;

                var extendedASCIICount = CountExtendedASCIIRows(tableData);

                _logger.LogInformation("Extracted {RowCount} rows from table: {TableName} ({ExtendedASCIICount} rows contain extended ASCII)",
                    tableData.Count, entity.TableName, extendedASCIICount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract data from table: {TableName}", entity.TableName);
                // Continue with other tables
            }
        }

        return extractedData;
    }

    private async Task ExecuteSqliteConfigurationAsync(SqliteConnection connection)
    {
        try
        {
            // Configure SQLite for proper text encoding
            var configCommands = new[]
            {
                "PRAGMA encoding = 'UTF-8';",
                "PRAGMA text_encoding = 'UTF-8';",
                "PRAGMA case_sensitive_like = false;"
            };

            foreach (var command in configCommands)
            {
                using var sqlCommand = new SqliteCommand(command, connection);
                await sqlCommand.ExecuteNonQueryAsync();
            }

            _logger.LogDebug("SQLite configured for extended ASCII support");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to configure SQLite for extended ASCII - proceeding with defaults");
        }
    }

    private async Task<List<Dictionary<string, object>>> ExtractTableDataWithExtendedASCIIAsync(SqliteConnection connection, ExtractableEntity entity)
    {
        var tableData = new List<Dictionary<string, object>>();

        // Build SELECT query - hoover approach (get all data) with proper text handling
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

                // Handle extended ASCII in text values
                if (value is string stringValue)
                {
                    // Normalize extended ASCII characters for consistency
                    var normalizedValue = stringValue.Normalize(NormalizationForm.FormC);

                    // Log if extended ASCII characters are found
                    if (ContainsExtendedASCII(normalizedValue))
                    {
                        _logger.LogDebug("Found extended ASCII in {Table}.{Column}: {Value}",
                            entity.TableName, columnName,
                            normalizedValue.Length > 50 ? normalizedValue.Substring(0, 50) + "..." : normalizedValue);
                    }

                    row[columnName] = normalizedValue;
                }
                else if (value == DBNull.Value)
                {
                    row[columnName] = null;
                }
                else
                {
                    row[columnName] = value;
                }
            }

            tableData.Add(row);
        }

        return tableData;
    }

    private List<string> GetPhotoFilesWithExtendedASCII(string tempDir)
    {
        var photoExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp" };

        try
        {
            var photoFiles = Directory.GetFiles(tempDir, "*.*", SearchOption.AllDirectories)
                .Where(f => photoExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            // Handle extended ASCII characters in photo filenames
            var processedPhotoFiles = new List<string>();

            foreach (var photoFile in photoFiles)
            {
                var fileName = Path.GetFileName(photoFile);

                if (ContainsExtendedASCII(fileName))
                {
                    _logger.LogDebug("Found photo with extended ASCII filename: {FileName}", fileName);
                }

                // Normalize the filename for consistent handling
                var normalizedPath = photoFile.Normalize(NormalizationForm.FormC);
                processedPhotoFiles.Add(normalizedPath);
            }

            _logger.LogDebug("Found {Count} photo files, {ExtendedASCIICount} with extended ASCII filenames",
                processedPhotoFiles.Count,
                processedPhotoFiles.Count(f => ContainsExtendedASCII(Path.GetFileName(f))));

            return processedPhotoFiles;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process photo files with extended ASCII support");
            return new List<string>();
        }
    }

    private bool ContainsExtendedASCII(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        // Check for extended ASCII characters (128-255) and Unicode characters (> 255)
        return text.Any(c => c > 127);
    }

    private int CountExtendedASCIIRows(List<Dictionary<string, object>> tableData)
    {
        return tableData.Count(row =>
            row.Values.OfType<string>().Any(value => ContainsExtendedASCII(value))
        );
    }

    /// <summary>
    /// Validates SQLite database encoding and extended ASCII support
    /// </summary>
    public async Task<SQLiteEncodingInfo> AnalyzeSQLiteEncodingAsync(string sqliteDbPath)
    {
        var encodingInfo = new SQLiteEncodingInfo();

        try
        {
            using var connection = new SqliteConnection($"Data Source={sqliteDbPath};Mode=ReadOnly");
            await connection.OpenAsync();

            // Get SQLite encoding information
            using var encodingCommand = new SqliteCommand("PRAGMA encoding;", connection);
            var encoding = await encodingCommand.ExecuteScalarAsync();
            encodingInfo.DatabaseEncoding = encoding?.ToString() ?? "Unknown";

            // Get text encoding
            using var textEncodingCommand = new SqliteCommand("PRAGMA text_encoding;", connection);
            var textEncoding = await textEncodingCommand.ExecuteScalarAsync();
            encodingInfo.TextEncoding = textEncoding?.ToString() ?? "Unknown";

            // Test extended ASCII character handling
            var testQuery = @"
                SELECT 'Test éxtëndëd characters: áéíóú àèìòù âêîôû ñç © ® ™' as TestText
                UNION ALL
                SELECT 'Currency: €50, £40, ¥500, ©2024' as TestText
                UNION ALL
                SELECT 'Math: ± × ÷ ≠ ≤ ≥ α β γ' as TestText;
            ";

            using var testCommand = new SqliteCommand(testQuery, connection);
            using var reader = await testCommand.ExecuteReaderAsync();

            var testResults = new List<string>();
            while (await reader.ReadAsync())
            {
                var testText = reader.GetString(0);
                testResults.Add(testText);

                if (ContainsExtendedASCII(testText))
                {
                    encodingInfo.ExtendedASCIISupported = true;
                }
            }

            encodingInfo.TestResults = testResults;
            encodingInfo.IsValid = testResults.Count == 3 && encodingInfo.ExtendedASCIISupported;

            _logger.LogInformation("SQLite encoding analysis: Database={DbEncoding}, Text={TextEncoding}, ExtendedASCII={Supported}",
                encodingInfo.DatabaseEncoding, encodingInfo.TextEncoding, encodingInfo.ExtendedASCIISupported);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze SQLite encoding");
            encodingInfo.ErrorMessage = ex.Message;
        }

        return encodingInfo;
    }

    /// <summary>
    /// Repairs potential encoding issues in extracted text data
    /// </summary>
    public string RepairTextEncoding(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        try
        {
            // Normalize to consistent Unicode form
            var normalized = input.Normalize(NormalizationForm.FormC);

            // Handle common encoding issues
            var repaired = normalized
                .Replace("Ã©", "é")  // Fix common UTF-8 to Latin-1 encoding issue
                .Replace("Ã¡", "á")
                .Replace("Ã­", "í")
                .Replace("Ã³", "ó")
                .Replace("Ã±", "ñ")
                .Replace("Ã§", "ç")
                .Replace("â‚¬", "€") // Euro symbol
                .Replace("Â£", "£")  // Pound symbol
                .Replace("Â©", "©")  // Copyright symbol
                .Replace("Â®", "®"); // Registered symbol

            if (repaired != normalized)
            {
                _logger.LogDebug("Repaired text encoding: '{Original}' -> '{Repaired}'",
                    normalized.Length > 50 ? normalized.Substring(0, 50) + "..." : normalized,
                    repaired.Length > 50 ? repaired.Substring(0, 50) + "..." : repaired);
            }

            return repaired;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to repair text encoding for: {Input}",
                input.Length > 100 ? input.Substring(0, 100) + "..." : input);
            return input; // Return original if repair fails
        }
    }
}

// Supporting classes for extended ASCII analysis
public class ExtractedData
{
    public UserInfo UserInfo { get; set; } = new();
    public Dictionary<string, List<Dictionary<string, object>>> TableData { get; set; } = new();
    public List<string> PhotoFiles { get; set; } = new();
    public ExtendedASCIIAnalysis ExtendedASCIIAnalysis { get; set; } = new();
}

public class UserInfo
{
    public string Email { get; set; } = string.Empty;
    public string AppGuid { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
}

public class SQLiteEncodingInfo
{
    public string DatabaseEncoding { get; set; } = string.Empty;
    public string TextEncoding { get; set; } = string.Empty;
    public bool ExtendedASCIISupported { get; set; }
    public bool IsValid { get; set; }
    public List<string> TestResults { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class ExtendedASCIIAnalysis
{
    public int TotalStringFields { get; set; }
    public int ExtendedASCIIFields { get; set; }
    public int UnicodeFields { get; set; }
    public double ExtendedASCIIPercentage { get; set; }
    public Dictionary<string, List<string>> SampleExtendedASCIIValues { get; set; } = new();
}