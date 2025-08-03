using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace x3squaredcirecles.SQLSync.Generator.Services;

public class SqlScriptDiscovery
{
    private readonly ILogger<SqlScriptDiscovery> _logger;
    private readonly RepositoryDetector _repositoryDetector;

    public SqlScriptDiscovery(ILogger<SqlScriptDiscovery> logger, RepositoryDetector repositoryDetector)
    {
        _logger = logger;
        _repositoryDetector = repositoryDetector;
    }

    public async Task<List<SqlScriptFile>> DiscoverScriptsAsync(RepositoryInfo repositoryInfo)
    {
        var scripts = new List<SqlScriptFile>();
        var sqlScriptsPath = _repositoryDetector.GetSqlScriptsPath(repositoryInfo);

        if (!Directory.Exists(sqlScriptsPath))
        {
            _logger.LogDebug("SqlScripts directory not found: {Path}", sqlScriptsPath);
            return scripts;
        }

        _logger.LogInformation("Discovering SQL scripts in: {Path}", sqlScriptsPath);

        // Discover scripts in numbered phase directories (01-29)
        for (int phase = 1; phase <= 29; phase++)
        {
            var phaseScripts = await DiscoverPhaseScriptsAsync(sqlScriptsPath, phase);
            scripts.AddRange(phaseScripts);
        }

        // Sort scripts by phase, then by order within phase
        scripts = scripts.OrderBy(s => s.Phase).ThenBy(s => s.Order).ToList();

        _logger.LogInformation("Discovered {Count} SQL scripts across {PhaseCount} phases",
            scripts.Count, scripts.Select(s => s.Phase).Distinct().Count());

        return scripts;
    }

    private async Task<List<SqlScriptFile>> DiscoverPhaseScriptsAsync(string sqlScriptsPath, int phase)
    {
        var scripts = new List<SqlScriptFile>();
        var phaseInfo = GetPhaseInfo(phase);

        // Look for directories matching the phase pattern
        var phasePatterns = new[]
        {
            $"{phase:D2}-{phaseInfo.DirectoryName}",
            $"{phase:D2}_{phaseInfo.DirectoryName}",
            $"{phase:D2}.{phaseInfo.DirectoryName}",
            phaseInfo.DirectoryName
        };

        foreach (var pattern in phasePatterns)
        {
            var phasePath = Path.Combine(sqlScriptsPath, pattern);
            if (Directory.Exists(phasePath))
            {
                var phaseScripts = await DiscoverScriptsInDirectoryAsync(phasePath, phase, phaseInfo);
                scripts.AddRange(phaseScripts);
                _logger.LogDebug("Found {Count} scripts in phase {Phase} directory: {Directory}",
                    phaseScripts.Count, phase, pattern);
            }
        }

        return scripts;
    }

    private async Task<List<SqlScriptFile>> DiscoverScriptsInDirectoryAsync(string directoryPath, int phase, PhaseInfo phaseInfo)
    {
        var scripts = new List<SqlScriptFile>();

        try
        {
            var sqlFiles = Directory.GetFiles(directoryPath, "*.sql", SearchOption.AllDirectories)
                .Where(f => !Path.GetFileName(f).StartsWith("_compiled_deployment"))
                .OrderBy(f => f);

            foreach (var filePath in sqlFiles)
            {
                var scriptFile = await CreateSqlScriptFileAsync(filePath, phase, phaseInfo);
                scripts.Add(scriptFile);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering scripts in directory: {Directory}", directoryPath);
        }

        return scripts;
    }

    private async Task<SqlScriptFile> CreateSqlScriptFileAsync(string filePath, int phase, PhaseInfo phaseInfo)
    {
        var fileName = Path.GetFileName(filePath);
        var order = ExtractOrderFromFileName(fileName);
        var content = await File.ReadAllTextAsync(filePath);
        var lastModified = File.GetLastWriteTimeUtc(filePath);

        var scriptFile = new SqlScriptFile
        {
            FileName = fileName,
            FilePath = filePath,
            Phase = phase,
            Order = order,
            PhaseInfo = phaseInfo,
            Content = content,
            LastModified = lastModified,
            RequiresWarning = phaseInfo.RequiresWarning,
            IsNew = IsNewScript(filePath),
            IsModified = IsModifiedScript(filePath),
            Hash = ComputeContentHash(content)
        };

        return scriptFile;
    }

    private int ExtractOrderFromFileName(string fileName)
    {
        // Look for numeric prefix in filename (001_script.sql, 002-script.sql, etc.)
        var match = Regex.Match(fileName, @"^(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var order))
        {
            return order;
        }

        // Fallback to alphabetical ordering
        return fileName.GetHashCode();
    }

    private bool IsNewScript(string filePath)
    {
        // Check if script was added in the last commit
        // This is a simplified implementation - could be enhanced with git integration
        var creationTime = File.GetCreationTimeUtc(filePath);
        var lastWriteTime = File.GetLastWriteTimeUtc(filePath);

        return Math.Abs((creationTime - lastWriteTime).TotalMinutes) < 5;
    }

    private bool IsModifiedScript(string filePath)
    {
        // Check if script was modified recently
        var lastWriteTime = File.GetLastWriteTimeUtc(filePath);
        return lastWriteTime > DateTime.UtcNow.AddDays(-7);
    }

    private string ComputeContentHash(string content)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hashBytes)[..16]; // First 16 chars for brevity
    }

    private PhaseInfo GetPhaseInfo(int phase)
    {
        return phase switch
        {
            1 => new PhaseInfo(1, "create-tables", "Create Tables (Structure Only)", false),
            2 => new PhaseInfo(2, "primary-key-indexes", "Create Primary Key Indexes", false),
            3 => new PhaseInfo(3, "unique-indexes", "Create Unique Indexes", false),
            4 => new PhaseInfo(4, "reference-data", "Insert Reference/Lookup Data", false),
            5 => new PhaseInfo(5, "foreign-key-constraints", "Create Foreign Key Constraints", false),
            6 => new PhaseInfo(6, "non-clustered-indexes", "Create Non-Clustered Indexes", false),
            7 => new PhaseInfo(7, "composite-indexes", "Create Composite Indexes", false),
            8 => new PhaseInfo(8, "filtered-indexes", "Create Filtered Indexes", false),
            9 => new PhaseInfo(9, "computed-columns", "Create Computed Columns", false),
            10 => new PhaseInfo(10, "column-constraints", "Add Column Constraints (Advanced)", false),
            11 => new PhaseInfo(11, "user-defined-types", "Create User-Defined Data Types", false),
            12 => new PhaseInfo(12, "scalar-functions", "Create User-Defined Functions (Scalar)", false),
            13 => new PhaseInfo(13, "table-functions", "Create User-Defined Functions (Table-Valued)", false),
            14 => new PhaseInfo(14, "views", "Create Views", false),
            15 => new PhaseInfo(15, "stored-procedures", "Create Stored Procedures", false),
            16 => new PhaseInfo(16, "triggers", "Create Triggers", true),
            17 => new PhaseInfo(17, "roles", "Create Roles", true),
            18 => new PhaseInfo(18, "users", "Create Users", true),
            19 => new PhaseInfo(19, "object-permissions", "Grant Object Permissions", true),
            20 => new PhaseInfo(20, "schema-permissions", "Grant Schema Permissions", true),
            21 => new PhaseInfo(21, "synonyms", "Create Synonyms", false),
            22 => new PhaseInfo(22, "fulltext", "Create Full-Text Catalogs and Indexes", true),
            23 => new PhaseInfo(23, "partition-functions", "Create Partition Functions and Schemes", true),
            24 => new PhaseInfo(24, "table-partitioning", "Apply Table Partitioning", true),
            25 => new PhaseInfo(25, "database-options", "Set Database Options", true),
            26 => new PhaseInfo(26, "update-statistics", "Update Statistics", false),
            27 => new PhaseInfo(27, "data-validation", "Run Data Validation Scripts", true),
            28 => new PhaseInfo(28, "documentation", "Create Database Documentation", false),
            29 => new PhaseInfo(29, "maintenance", "Final Maintenance Tasks", true),
            _ => new PhaseInfo(phase, $"phase-{phase}", $"Custom Phase {phase}", true)
        };
    }

    public List<SqlScriptFile> FilterScriptsByPhase(List<SqlScriptFile> scripts, int phase)
    {
        return scripts.Where(s => s.Phase == phase).ToList();
    }

    public List<SqlScriptFile> FilterNewAndModifiedScripts(List<SqlScriptFile> scripts)
    {
        return scripts.Where(s => s.IsNew || s.IsModified).ToList();
    }

    public List<SqlScriptFile> FilterWarningScripts(List<SqlScriptFile> scripts)
    {
        return scripts.Where(s => s.RequiresWarning).ToList();
    }
}

public class SqlScriptFile
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int Phase { get; set; }
    public int Order { get; set; }
    public PhaseInfo PhaseInfo { get; set; } = new PhaseInfo(0, "", "", false);
    public string Content { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public bool RequiresWarning { get; set; }
    public bool IsNew { get; set; }
    public bool IsModified { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string? EnhancedContent { get; set; }
}

public class PhaseInfo
{
    public int Phase { get; set; }
    public string DirectoryName { get; set; }
    public string Description { get; set; }
    public bool RequiresWarning { get; set; }

    public PhaseInfo(int phase, string directoryName, string description, bool requiresWarning)
    {
        Phase = phase;
        DirectoryName = directoryName;
        Description = description;
        RequiresWarning = requiresWarning;
    }
}