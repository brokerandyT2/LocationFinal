using x3squaredcirecles.API.Generator.APIGenerator.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Reflection;

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
            _logger.LogInformation("Processing backup with EF Core dual contexts");

            // Step 1: Parse user info from filename
            var userInfo = ParseUserInfoFromFilename(zipFilePath);
            _logger.LogInformation("Processing backup for user: {Email}", userInfo.Email);

            // Step 2: Extract SQLite database from zip
            var sqliteDbPath = await ExtractSQLiteFromZipAsync(zipFilePath);
            _logger.LogDebug("Extracted SQLite database: {DbPath}", sqliteDbPath);

            try
            {
                // Step 3: Create dual EF contexts
                using var sourceContext = CreateSQLiteContext(sqliteDbPath, entities);
                using var destContext = CreateSQLServerContext(sqlServerConnectionString, entities);

                // Step 4: Transfer data using EF Core
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

    private async Task<string> ExtractSQLiteFromZipAsync(string zipFilePath)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

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

        return sqliteFiles.First();
    }

    private DynamicDbContext CreateSQLiteContext(string sqliteDbPath, List<ExtractableEntity> entities)
    {
        var connectionString = $"Data Source={sqliteDbPath};";
        var options = new DbContextOptionsBuilder<DynamicDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new DynamicDbContext(options, entities);
    }

    private DynamicDbContext CreateSQLServerContext(string connectionString, List<ExtractableEntity> entities)
    {
        var options = new DbContextOptionsBuilder<DynamicDbContext>()
            .UseSqlServer(connectionString)
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

        _logger.LogInformation("Starting data transfer for {EntityCount} entities", entities.Count);

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

        // Save all changes to SQL Server
        await destContext.SaveChangesAsync();
        _logger.LogInformation("Successfully saved {RowCount} total rows to SQL Server", result.TotalRowsTransferred);

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

        // Add user context and transfer to SQL Server
        foreach (var sourceEntity in sourceEntities)
        {
            // Create new instance for SQL Server (avoid EF tracking issues)
            var destEntity = CloneEntityForSQLServer(sourceEntity, entity.EntityType, userInfo);

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

    private object CloneEntityForSQLServer(object sourceEntity, Type entityType, UserInfo userInfo)
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
            property.SetValue(destEntity, value);
        }

        return destEntity;
    }
}

// Dynamic DbContext that can work with any entity types
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
            // Configure entity for EF Core
            var entityBuilder = modelBuilder.Entity(entity.EntityType);

            // Set table name and schema
            entityBuilder.ToTable(entity.TableName, entity.SchemaName);

            // Configure properties based on metadata
            foreach (var propertyMapping in entity.PropertyMappings)
            {
                var property = entityBuilder.Property(propertyMapping.PropertyName);

                if (propertyMapping.HasCustomMapping)
                {
                    // Apply custom SQL Server type mapping
                    property.HasColumnType(propertyMapping.SqlServerType);
                }

                // Set column name
                property.HasColumnName(propertyMapping.ColumnName);
            }
        }
    }

    public object GetDbSet(Type entityType)
    {
        var method = typeof(DbContext).GetMethod("Set", new Type[0])?.MakeGenericMethod(entityType);
        return method?.Invoke(this, null) ?? throw new InvalidOperationException($"Cannot create DbSet for type: {entityType.Name}");
    }
}

// Result model for extraction
public class ExtractionResult
{
    public UserInfo UserInfo { get; set; } = new();
    public int TotalRowsTransferred { get; set; }
    public int TablesProcessed { get; set; }
    public List<string> PhotoUrls { get; set; } = new();
}