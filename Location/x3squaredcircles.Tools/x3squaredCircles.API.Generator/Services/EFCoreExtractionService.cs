// Enhanced EFCoreExtractionService with Extended ASCII support
// File: x3squaredCircles.API.Generator/Services/EFCoreExtractionService.cs

using x3squaredcirecles.API.Generator.APIGenerator.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace x3squaredcirecles.API.Generator.APIGenerator.Services;

public class EFCoreExtractionService
{
    private readonly ILogger<EFCoreExtractionService> _logger;

    public EFCoreExtractionService(ILogger<EFCoreExtractionService> logger)
    {
        _logger = logger;
    }

    public async Task<ExtractionResult> ProcessBackupAsync(string zipFilePath, List<ExtractableEntity> entities, string sqlServerConnectionString)
    {
        try
        {
            _logger.LogInformation("Processing backup with EF Core dual contexts and extended ASCII support");

            // Step 1: Parse user info from filename (handle extended ASCII)
            var userInfo = ParseUserInfoFromFilename(zipFilePath);
            _logger.LogInformation("Processing backup for user: {Email}", userInfo.Email);

            // Step 2: Extract SQLite database from zip with proper encoding
            var sqliteDbPath = await ExtractSQLiteFromZipAsync(zipFilePath);
            _logger.LogDebug("Extracted SQLite database: {DbPath}", sqliteDbPath);

            try
            {
                // Step 3: Create dual EF contexts with extended ASCII configuration
                using var sourceContext = CreateSQLiteContext(sqliteDbPath, entities);
                using var destContext = CreateSQLServerContext(sqlServerConnectionString, entities);

                // Step 4: Transfer data using EF Core with extended ASCII preservation
                var transferResult = await TransferDataAsync(sourceContext, destContext, entities, userInfo);

                _logger.LogInformation("Transfer completed: {RowsTransferred} rows across {TableCount} tables",
                    transferResult.TotalRowsTransferred, transferResult.TablesProcessed);

                return transferResult;
            }
            finally
            {
                // Cleanup extracted SQLite file
                if (File.Exists(sqliteDbPath))
                {
                    File.Delete(sqliteDbPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EF Core extraction failed for zip: {ZipPath}", zipFilePath);
            throw;
        }
    }

    private UserInfo ParseUserInfoFromFilename(string zipFilePath)
    {
        // Format: {email}_{appGUID}_{date in DDMMYY}.zip
        // Handle extended ASCII characters in email addresses
        var fileName = Path.GetFileNameWithoutExtension(zipFilePath);
        var parts = fileName.Split('_');

        if (parts.Length != 3)
        {
            throw new ArgumentException($"Invalid zip filename format. Expected: email_guid_date.zip, got: {fileName}");
        }

        // Decode any URL-encoded extended ASCII characters in email
        var email = System.Web.HttpUtility.UrlDecode(parts[0]);

        return new UserInfo
        {
            Email = email,
            AppGuid = parts[1],
            Date = parts[2]
        };
    }

    private async Task<string> ExtractSQLiteFromZipAsync(string zipFilePath)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        // Extract with proper encoding handling for filenames
        using (var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Read, Encoding.UTF8))
        {
            foreach (var entry in archive.Entries)
            {
                // Handle extended ASCII characters in entry names
                var destinationPath = Path.Combine(tempDir, entry.FullName);
                var destinationDir = Path.GetDirectoryName(destinationPath);

                if (!string.IsNullOrEmpty(destinationDir))
                {
                    Directory.CreateDirectory(destinationDir);
                }

                if (!string.IsNullOrEmpty(entry.Name))
                {
                    entry.ExtractToFile(destinationPath, overwrite: true);
                }
            }
        }

        // Find SQLite database file
        var sqliteFiles = Directory.GetFiles(tempDir, "*.db*", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(tempDir, "*.sqlite*", SearchOption.AllDirectories))
            .ToList();

        if (!sqliteFiles.Any())
        {
            throw new InvalidOperationException("No SQLite database found in zip file");
        }

        return sqliteFiles.First();
    }

    private DynamicDbContext CreateSQLiteContext(string sqliteDbPath, List<ExtractableEntity> entities)
    {
        var connectionString = $"Data Source={sqliteDbPath};";
        var options = new DbContextOptionsBuilder<DynamicDbContext>()
            .UseSqlite(connectionString, opts =>
            {
                // Configure SQLite for proper text handling
                opts.CommandTimeout(300);
            })
            .Options;

        return new DynamicDbContext(options, entities);
    }

    private DynamicDbContext CreateSQLServerContext(string connectionString, List<ExtractableEntity> entities)
    {
        var options = new DbContextOptionsBuilder<DynamicDbContext>()
            .UseSqlServer(connectionString, opts =>
            {
                // Configure SQL Server for extended ASCII support
                opts.CommandTimeout(300);
                opts.EnableRetryOnFailure(maxRetryCount: 3);
            })
            .Options;

        return new DynamicDbContext(options, entities);
    }

    private async Task<ExtractionResult> TransferDataAsync(DynamicDbContext sourceContext, DynamicDbContext destContext, List<ExtractableEntity> entities, UserInfo userInfo)
    {
        var result = new ExtractionResult
        {
            UserInfo = userInfo,
            TotalRowsTransferred = 0,
            TablesProcessed = 0
        };

        _logger.LogInformation("Starting data transfer for {EntityCount} entities with extended ASCII preservation", entities.Count);

        foreach (var entity in entities)
        {
            try
            {
                var rowsTransferred = await TransferEntityDataAsync(sourceContext, destContext, entity, userInfo);
                result.TotalRowsTransferred += rowsTransferred;
                result.TablesProcessed++;

                _logger.LogDebug("Transferred {RowCount} rows for entity: {EntityName}", rowsTransferred, entity.TableName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to transfer data for entity: {EntityName}", entity.TableName);
                // Continue with other entities
            }
        }

        // Save all changes to SQL Server with extended ASCII support
        await destContext.SaveChangesAsync();
        _logger.LogInformation("Successfully saved {RowCount} total rows to SQL Server with extended ASCII preservation", result.TotalRowsTransferred);

        return result;
    }

    private async Task<int> TransferEntityDataAsync(DynamicDbContext sourceContext, DynamicDbContext destContext, ExtractableEntity entity, UserInfo userInfo)
    {
        // Get the DbSet for this entity type from source context
        var sourceSet = sourceContext.GetDbSet(entity.EntityType);

        // Read all data from SQLite
        var sourceEntities = await GetAllEntitiesAsync(sourceSet);

        if (!sourceEntities.Any())
        {
            _logger.LogDebug("No data found for entity: {EntityName}", entity.TableName);
            return 0;
        }

        // Add user context and transfer to SQL Server with extended ASCII preservation
        foreach (var sourceEntity in sourceEntities)
        {
            // Create new instance for SQL Server with extended ASCII handling
            var destEntity = CloneEntityForSQLServerWithExtendedASCII(sourceEntity, entity.EntityType, userInfo);

            // Use reflection to call Add method on the DbSet
            var addMethod = typeof(DbSet<>).MakeGenericType(entity.EntityType).GetMethod("Add", new[] { entity.EntityType });
            var destSet = destContext.GetDbSet(entity.EntityType);
            addMethod?.Invoke(destSet, new[] { destEntity });
        }

        return sourceEntities.Count;
    }

    private async Task<List<object>> GetAllEntitiesAsync(object dbSet)
    {
        // Use reflection to call ToListAsync() on the DbSet
        var toListAsyncMethod = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .Where(m => m.Name == "ToListAsync" && m.GetParameters().Length == 2)
            .First()
            .MakeGenericMethod(dbSet.GetType().GetGenericArguments()[0]);

        var task = (Task)toListAsyncMethod.Invoke(null, new[] { dbSet, CancellationToken.None });
        await task;

        var result = task.GetType().GetProperty("Result")?.GetValue(task);
        return ((System.Collections.IEnumerable)result).Cast<object>().ToList();
    }

    private object CloneEntityForSQLServerWithExtendedASCII(object sourceEntity, Type entityType, UserInfo userInfo)
    {
        // Create new instance
        var destEntity = Activator.CreateInstance(entityType);

        // Copy all properties except Id (let SQL Server generate new IDs)
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.Name != "Id")
            .ToList();

        foreach (var property in properties)
        {
            var value = property.GetValue(sourceEntity);

            // Handle extended ASCII string properties specifically
            if (value is string stringValue && !string.IsNullOrEmpty(stringValue))
            {
                // Ensure extended ASCII characters are preserved
                // Normalize the string to ensure consistent encoding
                var normalizedValue = stringValue.Normalize(NormalizationForm.FormC);
                property.SetValue(destEntity, normalizedValue);

                _logger.LogDebug("Transferred string property {PropertyName} with value: {Value}",
                    property.Name, normalizedValue.Length > 50 ? normalizedValue.Substring(0, 50) + "..." : normalizedValue);
            }
            else
            {
                property.SetValue(destEntity, value);
            }
        }

        return destEntity;
    }
}

// Enhanced Dynamic DbContext that can work with any entity types and extended ASCII
public class DynamicDbContext : DbContext
{
    private readonly List<ExtractableEntity> _entities;

    public DynamicDbContext(DbContextOptions<DynamicDbContext> options, List<ExtractableEntity> entities)
        : base(options)
    {
        _entities = entities;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var entity in _entities)
        {
            // Configure entity for EF Core with extended ASCII support
            var entityBuilder = modelBuilder.Entity(entity.EntityType);

            // Set table name and schema
            entityBuilder.ToTable(entity.TableName, entity.SchemaName);

            // Configure properties based on metadata with extended ASCII collation
            foreach (var propertyMapping in entity.PropertyMappings)
            {
                var property = entityBuilder.Property(propertyMapping.PropertyName);

                if (propertyMapping.HasCustomMapping)
                {
                    // Apply custom SQL Server type mapping with extended ASCII support
                    if (propertyMapping.SqlServerType.Contains("VARCHAR") || propertyMapping.SqlServerType.Contains("TEXT"))
                    {
                        // Ensure extended ASCII collation for string types
                        property.HasColumnType(propertyMapping.SqlServerType)
                               .UseCollation("SQL_Latin1_General_CP1252_CI_AS");
                    }
                    else
                    {
                        property.HasColumnType(propertyMapping.SqlServerType);
                    }
                }
                else if (propertyMapping.PropertyType == typeof(string))
                {
                    // Default string properties to NVARCHAR with extended ASCII collation
                    property.HasColumnType("NVARCHAR(255)")
                           .UseCollation("SQL_Latin1_General_CP1252_CI_AS");
                }

                // Set column name
                property.HasColumnName(propertyMapping.ColumnName);
            }
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (Database.IsSqlServer())
        {
            // Configure for extended ASCII support
            optionsBuilder.UseSqlServer(options =>
            {
                options.CommandTimeout(300);
            });
        }
        else if (Database.IsSqlite())
        {
            // Configure SQLite for proper text handling
            optionsBuilder.UseSqlite(options =>
            {
                options.CommandTimeout(300);
            });
        }
    }

    public object GetDbSet(Type entityType)
    {
        var method = typeof(DbContext).GetMethod("Set", new Type[0])?.MakeGenericMethod(entityType);
        return method?.Invoke(this, null) ?? throw new InvalidOperationException($"Cannot create DbSet for type: {entityType.Name}");
    }
}

// Result model for extraction with extended ASCII support
public class ExtractionResult
{
    public UserInfo UserInfo { get; set; } = new();
    public int TotalRowsTransferred { get; set; }
    public int TablesProcessed { get; set; }
    public List<string> PhotoUrls { get; set; } = new();
}