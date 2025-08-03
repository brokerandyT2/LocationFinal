using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace x3squaredcirecles.SQLSync.Generator.Services;

public class SqlScriptEnhancer
{
    private readonly ILogger<SqlScriptEnhancer> _logger;

    public SqlScriptEnhancer(ILogger<SqlScriptEnhancer> logger)
    {
        _logger = logger;
    }

    public async Task<string> EnhanceScriptAsync(SqlScriptFile scriptFile)
    {
        try
        {
            _logger.LogDebug("Enhancing script: {FileName} (Phase {Phase})",
                scriptFile.FileName, scriptFile.Phase);

            var enhanced = scriptFile.Phase switch
            {
                15 => EnhanceStoredProcedure(scriptFile.Content),
                14 => EnhanceView(scriptFile.Content),
                12 or 13 => EnhanceFunction(scriptFile.Content),
                16 => EnhanceTrigger(scriptFile.Content),
                11 => EnhanceUserDefinedType(scriptFile.Content),
                17 => EnhanceRole(scriptFile.Content),
                18 => EnhanceUser(scriptFile.Content),
                _ => EnhanceGenericScript(scriptFile.Content)
            };

            // Add header comment with metadata
            enhanced = AddScriptHeader(enhanced, scriptFile);

            scriptFile.EnhancedContent = enhanced;
            return enhanced;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enhance script {FileName}, using original content",
                scriptFile.FileName);
            return scriptFile.Content;
        }
    }

    private string EnhanceStoredProcedure(string content)
    {
        var procedures = ExtractCreateStatements(content, @"CREATE\s+PROCEDURE\s+(\[?\w+\]?\.?\[?\w+\]?)", "PROCEDURE");

        if (!procedures.Any())
            return content;

        var sb = new StringBuilder();
        var remaining = content;

        foreach (var (name, statement) in procedures)
        {
            var dropStatement = $"IF EXISTS (SELECT * FROM sys.procedures WHERE name = '{ExtractObjectName(name)}')\n" +
                               $"    DROP PROCEDURE {name}\n" +
                               $"GO\n\n";

            sb.AppendLine(dropStatement);
            sb.AppendLine(statement);
            sb.AppendLine("GO\n");

            remaining = remaining.Replace(statement, "");
        }

        // Add any remaining content
        if (!string.IsNullOrWhiteSpace(remaining.Trim()))
        {
            sb.AppendLine(remaining.Trim());
        }

        return sb.ToString();
    }

    private string EnhanceView(string content)
    {
        var views = ExtractCreateStatements(content, @"CREATE\s+VIEW\s+(\[?\w+\]?\.?\[?\w+\]?)", "VIEW");

        if (!views.Any())
            return content;

        var sb = new StringBuilder();
        var remaining = content;

        foreach (var (name, statement) in views)
        {
            var dropStatement = $"IF EXISTS (SELECT * FROM sys.views WHERE name = '{ExtractObjectName(name)}')\n" +
                               $"    DROP VIEW {name}\n" +
                               $"GO\n\n";

            sb.AppendLine(dropStatement);
            sb.AppendLine(statement);
            sb.AppendLine("GO\n");

            remaining = remaining.Replace(statement, "");
        }

        if (!string.IsNullOrWhiteSpace(remaining.Trim()))
        {
            sb.AppendLine(remaining.Trim());
        }

        return sb.ToString();
    }

    private string EnhanceFunction(string content)
    {
        var functions = ExtractCreateStatements(content, @"CREATE\s+FUNCTION\s+(\[?\w+\]?\.?\[?\w+\]?)", "FUNCTION");

        if (!functions.Any())
            return content;

        var sb = new StringBuilder();
        var remaining = content;

        foreach (var (name, statement) in functions)
        {
            var dropStatement = $"IF EXISTS (SELECT * FROM sys.objects WHERE name = '{ExtractObjectName(name)}' AND type IN ('FN', 'TF', 'IF'))\n" +
                               $"    DROP FUNCTION {name}\n" +
                               $"GO\n\n";

            sb.AppendLine(dropStatement);
            sb.AppendLine(statement);
            sb.AppendLine("GO\n");

            remaining = remaining.Replace(statement, "");
        }

        if (!string.IsNullOrWhiteSpace(remaining.Trim()))
        {
            sb.AppendLine(remaining.Trim());
        }

        return sb.ToString();
    }

    private string EnhanceTrigger(string content)
    {
        var triggers = ExtractCreateStatements(content, @"CREATE\s+TRIGGER\s+(\[?\w+\]?\.?\[?\w+\]?)", "TRIGGER");

        if (!triggers.Any())
            return content;

        var sb = new StringBuilder();
        var remaining = content;

        foreach (var (name, statement) in triggers)
        {
            var dropStatement = $"IF EXISTS (SELECT * FROM sys.triggers WHERE name = '{ExtractObjectName(name)}')\n" +
                               $"    DROP TRIGGER {name}\n" +
                               $"GO\n\n";

            sb.AppendLine(dropStatement);
            sb.AppendLine(statement);
            sb.AppendLine("GO\n");

            remaining = remaining.Replace(statement, "");
        }

        if (!string.IsNullOrWhiteSpace(remaining.Trim()))
        {
            sb.AppendLine(remaining.Trim());
        }

        return sb.ToString();
    }

    private string EnhanceUserDefinedType(string content)
    {
        var types = ExtractCreateStatements(content, @"CREATE\s+TYPE\s+(\[?\w+\]?\.?\[?\w+\]?)", "TYPE");

        if (!types.Any())
            return content;

        var sb = new StringBuilder();
        var remaining = content;

        foreach (var (name, statement) in types)
        {
            var dropStatement = $"IF EXISTS (SELECT * FROM sys.types WHERE name = '{ExtractObjectName(name)}' AND is_user_defined = 1)\n" +
                               $"    DROP TYPE {name}\n" +
                               $"GO\n\n";

            sb.AppendLine(dropStatement);
            sb.AppendLine(statement);
            sb.AppendLine("GO\n");

            remaining = remaining.Replace(statement, "");
        }

        if (!string.IsNullOrWhiteSpace(remaining.Trim()))
        {
            sb.AppendLine(remaining.Trim());
        }

        return sb.ToString();
    }

    private string EnhanceRole(string content)
    {
        var roles = ExtractCreateStatements(content, @"CREATE\s+ROLE\s+(\[?\w+\]?)", "ROLE");

        if (!roles.Any())
            return content;

        var sb = new StringBuilder();
        var remaining = content;

        foreach (var (name, statement) in roles)
        {
            var dropStatement = $"IF EXISTS (SELECT * FROM sys.database_principals WHERE name = '{ExtractObjectName(name)}' AND type = 'R')\n" +
                               $"    DROP ROLE {name}\n" +
                               $"GO\n\n";

            sb.AppendLine(dropStatement);
            sb.AppendLine(statement);
            sb.AppendLine("GO\n");

            remaining = remaining.Replace(statement, "");
        }

        if (!string.IsNullOrWhiteSpace(remaining.Trim()))
        {
            sb.AppendLine(remaining.Trim());
        }

        return sb.ToString();
    }

    private string EnhanceUser(string content)
    {
        var users = ExtractCreateStatements(content, @"CREATE\s+USER\s+(\[?\w+\]?)", "USER");

        if (!users.Any())
            return content;

        var sb = new StringBuilder();
        var remaining = content;

        foreach (var (name, statement) in users)
        {
            var dropStatement = $"IF EXISTS (SELECT * FROM sys.database_principals WHERE name = '{ExtractObjectName(name)}' AND type = 'S')\n" +
                               $"    DROP USER {name}\n" +
                               $"GO\n\n";

            sb.AppendLine(dropStatement);
            sb.AppendLine(statement);
            sb.AppendLine("GO\n");

            remaining = remaining.Replace(statement, "");
        }

        if (!string.IsNullOrWhiteSpace(remaining.Trim()))
        {
            sb.AppendLine(remaining.Trim());
        }

        return sb.ToString();
    }

    private string EnhanceGenericScript(string content)
    {
        // For generic scripts, just ensure they have proper transaction handling
        if (!content.Contains("BEGIN TRANSACTION") && !content.Contains("BEGIN TRAN"))
        {
            return WrapInTransaction(content);
        }

        return content;
    }

    private List<(string name, string statement)> ExtractCreateStatements(string content, string pattern, string objectType)
    {
        var results = new List<(string, string)>();

        try
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var matches = regex.Matches(content);

            foreach (Match match in matches)
            {
                var objectName = match.Groups[1].Value.Trim();
                var startIndex = match.Index;

                // Find the end of this CREATE statement
                var endIndex = FindStatementEnd(content, startIndex, objectType);

                if (endIndex > startIndex)
                {
                    var statement = content.Substring(startIndex, endIndex - startIndex).Trim();
                    results.Add((objectName, statement));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract {ObjectType} statements from script", objectType);
        }

        return results;
    }

    private int FindStatementEnd(string content, int startIndex, string objectType)
    {
        // Look for GO statement or end of content
        var goPattern = @"\bGO\b";
        var goMatch = Regex.Match(content.Substring(startIndex), goPattern, RegexOptions.IgnoreCase);

        if (goMatch.Success)
        {
            return startIndex + goMatch.Index;
        }

        // For stored procedures and functions, look for END statement
        if (objectType == "PROCEDURE" || objectType == "FUNCTION")
        {
            var endPattern = @"\bEND\b(?!\s+(?:IF|WHILE|CASE|TRY|CATCH))";
            var endMatch = Regex.Match(content.Substring(startIndex), endPattern, RegexOptions.IgnoreCase);

            if (endMatch.Success)
            {
                return startIndex + endMatch.Index + endMatch.Length;
            }
        }

        return content.Length;
    }

    private string ExtractObjectName(string fullName)
    {
        // Remove brackets and schema prefix to get just the object name
        var cleaned = fullName.Trim('[', ']');
        var parts = cleaned.Split('.');
        return parts.Last().Trim('[', ']');
    }

    private string WrapInTransaction(string content)
    {
        var sb = new StringBuilder();
        sb.AppendLine("BEGIN TRANSACTION");
        sb.AppendLine("BEGIN TRY");
        sb.AppendLine();
        sb.AppendLine(content.Trim());
        sb.AppendLine();
        sb.AppendLine("    COMMIT TRANSACTION");
        sb.AppendLine("END TRY");
        sb.AppendLine("BEGIN CATCH");
        sb.AppendLine("    ROLLBACK TRANSACTION");
        sb.AppendLine("    THROW");
        sb.AppendLine("END CATCH");

        return sb.ToString();
    }

    private string AddScriptHeader(string content, SqlScriptFile scriptFile)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"-- ========== {scriptFile.PhaseInfo.Description} ==========");
        sb.AppendLine($"-- Script: {scriptFile.FileName}");
        sb.AppendLine($"-- Phase: {scriptFile.Phase}");
        sb.AppendLine($"-- Order: {scriptFile.Order}");
        sb.AppendLine($"-- Last Modified: {scriptFile.LastModified:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"-- Hash: {scriptFile.Hash}");

        if (scriptFile.RequiresWarning)
        {
            sb.AppendLine("-- ⚠️  WARNING: This script requires manual approval in production");
        }

        if (scriptFile.IsNew)
        {
            sb.AppendLine("-- 🆕 NEW: This is a new script");
        }

        if (scriptFile.IsModified)
        {
            sb.AppendLine("-- 📝 MODIFIED: This script has been recently modified");
        }

        sb.AppendLine();
        sb.AppendLine(content);

        return sb.ToString();
    }

    public async Task<List<SqlScriptFile>> EnhanceAllScriptsAsync(List<SqlScriptFile> scripts)
    {
        _logger.LogInformation("Enhancing {Count} SQL scripts", scripts.Count);

        var tasks = scripts.Select(async script =>
        {
            await EnhanceScriptAsync(script);
            return script;
        });

        var enhancedScripts = await Task.WhenAll(tasks);

        _logger.LogInformation("Enhanced {Count} SQL scripts successfully", enhancedScripts.Length);
        return enhancedScripts.ToList();
    }

    public string CombineScriptsForPhase(List<SqlScriptFile> phaseScripts)
    {
        if (!phaseScripts.Any())
            return string.Empty;

        var sb = new StringBuilder();
        var phase = phaseScripts.First().Phase;
        var phaseInfo = phaseScripts.First().PhaseInfo;

        sb.AppendLine($"-- ========================================");
        sb.AppendLine($"-- Phase {phase}: {phaseInfo.Description}");
        sb.AppendLine($"-- Scripts: {phaseScripts.Count}");
        sb.AppendLine($"-- ========================================");
        sb.AppendLine();

        foreach (var script in phaseScripts.OrderBy(s => s.Order))
        {
            var content = script.EnhancedContent ?? script.Content;
            sb.AppendLine(content);
            sb.AppendLine();
        }

        return sb.ToString();
    }
}