namespace x3squaredcirecles.API.Generator.APIGenerator.Models;

/// <summary>
/// Entity marked with [ExportToSQL] and ready for extraction
/// </summary>
public class ExtractableEntity
{
    public string TableName { get; set; } = string.Empty;    // From [Table] or class name
    public Type EntityType { get; set; } = typeof(object);   // Actual entity type
    public string SchemaName { get; set; } = string.Empty;   // SQL Server schema
    public List<PropertyMappingInfo> PropertyMappings { get; set; } = new();
    public string ExportReason { get; set; } = string.Empty; // From [ExportToSQL] reason
}