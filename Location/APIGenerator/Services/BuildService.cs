using Location.Tools.APIGenerator.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace Location.Tools.APIGenerator.Services;

public class BuildService
{
    private readonly ILogger<BuildService> _logger;

    public BuildService(ILogger<BuildService> logger)
    {
        _logger = logger;
    }

    public async Task<string> BuildFunctionAppAsync(AssemblyInfo assemblyInfo, GeneratedAssets generatedAssets, GeneratorOptions options)
    {
        try
        {
            _logger.LogInformation("Building Function App from generated source");

            // Generate complete project structure
            await GenerateProjectStructureAsync(assemblyInfo, generatedAssets, options);

            // Build the project
            var publishPath = await CompileAndPublishAsync(generatedAssets.OutputDirectory, assemblyInfo);

            _logger.LogInformation("Function App build completed: {PublishPath}", publishPath);
            return publishPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build Function App");
            throw;
        }
    }

    private async Task GenerateProjectStructureAsync(AssemblyInfo assemblyInfo, GeneratedAssets generatedAssets, GeneratorOptions options)
    {
        _logger.LogDebug("Generating complete project structure");

        // Generate Program.cs
        var programCs = GenerateProgramCs(assemblyInfo);
        await File.WriteAllTextAsync(Path.Combine(generatedAssets.OutputDirectory, "Program.cs"), programCs);

        // Generate .csproj file
        var csprojContent = GenerateCsprojFile(assemblyInfo);
        await File.WriteAllTextAsync(Path.Combine(generatedAssets.OutputDirectory, "FunctionApp.csproj"), csprojContent);

        // Generate Startup.cs for DI
        var startupCs = GenerateStartupCs(assemblyInfo);
        await File.WriteAllTextAsync(Path.Combine(generatedAssets.OutputDirectory, "Startup.cs"), startupCs);

        _logger.LogDebug("Generated complete project structure");
    }

    private string GenerateProgramCs(AssemblyInfo assemblyInfo)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using Microsoft.Azure.Functions.Worker;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using Microsoft.Extensions.Hosting;");
        sb.AppendLine("using Microsoft.Extensions.Logging;");
        sb.AppendLine("using Microsoft.Data.SqlClient;");
        sb.AppendLine("using Azure.Storage.Blobs;");
        sb.AppendLine();
        sb.AppendLine("var host = new HostBuilder()");
        sb.AppendLine("    .ConfigureFunctionsWebApplication()");
        sb.AppendLine("    .ConfigureServices(services => {");
        sb.AppendLine("        services.AddApplicationInsightsTelemetryWorkerService();");
        sb.AppendLine("        services.ConfigureFunctionsApplicationInsights();");
        sb.AppendLine("        ");
        sb.AppendLine("        // Add logging");
        sb.AppendLine("        services.AddLogging(builder => {");
        sb.AppendLine("            builder.AddConsole();");
        sb.AppendLine("            builder.AddApplicationInsights();");
        sb.AppendLine("        });");
        sb.AppendLine("        ");
        sb.AppendLine("        // Add SQL Server connection");
        sb.AppendLine("        services.AddTransient<SqlConnection>(provider => {");
        sb.AppendLine("            var connectionString = Environment.GetEnvironmentVariable(\"SQL_CONNECTION_STRING\");");
        sb.AppendLine("            return new SqlConnection(connectionString);");
        sb.AppendLine("        });");
        sb.AppendLine("        ");
        sb.AppendLine("        // Add Blob Storage client");
        sb.AppendLine("        services.AddTransient<BlobServiceClient>(provider => {");
        sb.AppendLine("            var connectionString = Environment.GetEnvironmentVariable(\"BLOB_STORAGE_CONNECTION_STRING\");");
        sb.AppendLine("            return new BlobServiceClient(connectionString);");
        sb.AppendLine("        });");
        sb.AppendLine("    })");
        sb.AppendLine("    .Build();");
        sb.AppendLine();
        sb.AppendLine("host.Run();");

        return sb.ToString();
    }

    private string GenerateCsprojFile(AssemblyInfo assemblyInfo)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
        sb.AppendLine("  <PropertyGroup>");
        sb.AppendLine("    <TargetFramework>net9.0</TargetFramework>");
        sb.AppendLine("    <AzureFunctionsVersion>v4</AzureFunctionsVersion>");
        sb.AppendLine("    <OutputType>Exe</OutputType>");
        sb.AppendLine("    <ImplicitUsings>enable</ImplicitUsings>");
        sb.AppendLine("    <Nullable>enable</Nullable>");
        sb.AppendLine($"    <AssemblyTitle>Location {assemblyInfo.Source} API v{assemblyInfo.MajorVersion}</AssemblyTitle>");
        sb.AppendLine($"    <AssemblyVersion>{assemblyInfo.MajorVersion}.0.0</AssemblyVersion>");
        sb.AppendLine($"    <FileVersion>{assemblyInfo.MajorVersion}.0.0</FileVersion>");
        sb.AppendLine("  </PropertyGroup>");
        sb.AppendLine();
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.Azure.Functions.Worker\" Version=\"1.21.0\" />");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.Azure.Functions.Worker.Sdk\" Version=\"1.16.4\" />");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.Azure.Functions.Worker.Extensions.Http\" Version=\"3.1.0\" />");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.ApplicationInsights.WorkerService\" Version=\"2.22.0\" />");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.Azure.Functions.Worker.ApplicationInsights\" Version=\"1.2.0\" />");
        sb.AppendLine("    ");
        sb.AppendLine("    <!-- Data access -->");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.Data.SqlClient\" Version=\"5.1.6\" />");
        sb.AppendLine("    <PackageReference Include=\"Azure.Storage.Blobs\" Version=\"12.19.1\" />");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.EntityFrameworkCore\" Version=\"9.0.0\" />");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.EntityFrameworkCore.SqlServer\" Version=\"9.0.0\" />");
        sb.AppendLine("    <PackageReference Include=\"Microsoft.EntityFrameworkCore.Sqlite\" Version=\"9.0.0\" />");
        sb.AppendLine("    ");
        sb.AppendLine("    <!-- JSON serialization -->");
        sb.AppendLine("    <PackageReference Include=\"System.Text.Json\" Version=\"9.0.0\" />");
        sb.AppendLine("    ");
        sb.AppendLine("    <!-- File operations -->");
        sb.AppendLine("    <PackageReference Include=\"System.IO.Compression\" Version=\"4.3.0\" />");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine();
        sb.AppendLine("  <ItemGroup>");
        sb.AppendLine("    <None Update=\"host.json\">");
        sb.AppendLine("      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>");
        sb.AppendLine("    </None>");
        sb.AppendLine("    <None Update=\"local.settings.json\">");
        sb.AppendLine("      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>");
        sb.AppendLine("      <CopyToPublishDirectory>Never</CopyToPublishDirectory>");
        sb.AppendLine("    </None>");
        sb.AppendLine("  </ItemGroup>");
        sb.AppendLine("</Project>");

        return sb.ToString();
    }

    private string GenerateStartupCs(AssemblyInfo assemblyInfo)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using Microsoft.Azure.Functions.Worker;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using Microsoft.Extensions.Hosting;");
        sb.AppendLine();
        sb.AppendLine($"namespace Location.{assemblyInfo.Source}.API.V{assemblyInfo.MajorVersion};");
        sb.AppendLine();
        sb.AppendLine("public class Startup");
        sb.AppendLine("{");
        sb.AppendLine("    public void ConfigureServices(IServiceCollection services)");
        sb.AppendLine("    {");
        sb.AppendLine("        // Services are configured in Program.cs");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private async Task<string> CompileAndPublishAsync(string projectDirectory, AssemblyInfo assemblyInfo)
    {
        _logger.LogInformation("Compiling Function App project");

        // Restore packages
        await RunDotNetCommandAsync("restore", projectDirectory);

        // Build project
        await RunDotNetCommandAsync("build --configuration Release", projectDirectory);

        // Publish project
        var publishDir = Path.Combine(projectDirectory, "publish");
        await RunDotNetCommandAsync($"publish --configuration Release --output \"{publishDir}\" --no-build", projectDirectory);

        _logger.LogInformation("Function App compiled and published to: {PublishDir}", publishDir);
        return publishDir;
    }

    private async Task RunDotNetCommandAsync(string arguments, string workingDirectory)
    {
        _logger.LogDebug("Running: dotnet {Arguments}", arguments);

        var processInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start dotnet process");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
        {
            _logger.LogError("dotnet command failed with exit code {ExitCode}", process.ExitCode);
            _logger.LogError("stdout: {Output}", output);
            _logger.LogError("stderr: {Error}", error);
            throw new InvalidOperationException($"dotnet {arguments} failed with exit code {process.ExitCode}");
        }

        if (!string.IsNullOrWhiteSpace(output))
        {
            _logger.LogDebug("dotnet output: {Output}", output.Trim());
        }
    }
}