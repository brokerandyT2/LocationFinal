namespace x3squaredcirecles.API.Generator.APIGenerator.Models;

/// <summary>
/// Information about discovered Infrastructure assembly
/// </summary>
public class AssemblyInfo
{
    public string Vertical { get; set; } = string.Empty;      // "photography", "fishing", etc.
    public int MajorVersion { get; set; }                     // 3 (from v3.2.1)
    public string AssemblyPath { get; set; } = string.Empty;  // Full path to Infrastructure.dll
    public string Source { get; set; } = string.Empty;       // "Photography", "Core", etc.
}