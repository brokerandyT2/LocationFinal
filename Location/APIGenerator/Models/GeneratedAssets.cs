namespace Location.Tools.APIGenerator.Models;

/// <summary>
/// Generated API assets ready for deployment
/// </summary>
public class GeneratedAssets
{
    public string FunctionAppName { get; set; } = string.Empty;
    public string BicepTemplate { get; set; } = string.Empty;
    public List<string> Controllers { get; set; } = new();
    public List<string> GeneratedEndpoints { get; set; } = new();
    public string OutputDirectory { get; set; } = string.Empty;
}