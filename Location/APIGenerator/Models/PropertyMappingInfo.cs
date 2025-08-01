namespace Location.Tools.APIGenerator.Models;

/// <summary>
/// Property mapping from SQLite to SQL Server with attribute metadata
/// </summary>
public class PropertyMappingInfo
{
    public string PropertyName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;   // From [SqlColumn] or property name
    public Type PropertyType { get; set; } = typeof(object);
    public string SqlServerType { get; set; } = string.Empty; // Mapped SQL Server data type
    public bool HasCustomMapping { get; set; }               // Has [SqlType] attribute
}