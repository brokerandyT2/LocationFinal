using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using x3squaredcircles.MobileAdapter.Generator.Configuration;
using x3squaredcircles.MobileAdapter.Generator.Discovery;
using x3squaredcircles.MobileAdapter.Generator.Logging;

namespace x3squaredcircles.MobileAdapter.Generator.TypeMapping
{
    public class TypeMappingEngine
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, Dictionary<string, string>> _builtInMappings;

        public TypeMappingEngine(ILogger logger)
        {
            _logger = logger;
            _builtInMappings = InitializeBuiltInMappings();
        }

        public async Task<Dictionary<string, TypeMappingInfo>> AnalyzeTypeMappingsAsync(
            List<DiscoveredClass> discoveredClasses,
            GeneratorConfiguration config)
        {
            var typeMappings = new Dictionary<string, TypeMappingInfo>();
            var targetPlatform = config.GetSelectedPlatform().ToString().ToLower();

            try
            {
                _logger.LogInfo("Analyzing type mappings for code generation...");

                // Load custom type mappings if provided
                var customMappings = LoadCustomTypeMappings(config.TypeMapping.CustomTypeMappings);

                // Collect all unique types from discovered classes
                var allTypes = CollectAllTypes(discoveredClasses);
                _logger.LogDebug($"Found {allTypes.Count} unique types to map");

                foreach (var sourceType in allTypes)
                {
                    var mappingInfo = await CreateTypeMappingAsync(sourceType, targetPlatform, customMappings, config);
                    if (mappingInfo != null)
                    {
                        typeMappings[sourceType] = mappingInfo;
                    }
                }

                // Validate mappings
                var validationErrors = ValidateTypeMappings(typeMappings, targetPlatform);
                if (validationErrors.Any())
                {
                    foreach (var error in validationErrors)
                    {
                        _logger.LogWarning($"Type mapping validation: {error}");
                    }
                }

                _logger.LogInfo($"Type mapping analysis completed. Mapped {typeMappings.Count} types.");
                return typeMappings;
            }
            catch (Exception ex)
            {
                _logger.LogError("Type mapping analysis failed", ex);
                throw;
            }
        }

        private Dictionary<string, Dictionary<string, string>> InitializeBuiltInMappings()
        {
            return new Dictionary<string, Dictionary<string, string>>
            {
                ["android"] = new Dictionary<string, string>
                {
                    // C# to Kotlin mappings
                    ["string"] = "String",
                    ["String"] = "String",
                    ["int"] = "Int",
                    ["Int32"] = "Int",
                    ["long"] = "Long",
                    ["Int64"] = "Long",
                    ["short"] = "Short",
                    ["Int16"] = "Short",
                    ["byte"] = "Byte",
                    ["Byte"] = "Byte",
                    ["bool"] = "Boolean",
                    ["Boolean"] = "Boolean",
                    ["float"] = "Float",
                    ["Single"] = "Float",
                    ["double"] = "Double",
                    ["Double"] = "Double",
                    ["decimal"] = "BigDecimal",
                    ["Decimal"] = "BigDecimal",
                    ["DateTime"] = "LocalDateTime",
                    ["DateTimeOffset"] = "OffsetDateTime",
                    ["TimeSpan"] = "Duration",
                    ["Guid"] = "UUID",
                    ["Uri"] = "Uri",
                    ["byte[]"] = "ByteArray",
                    ["char"] = "Char",
                    ["Char"] = "Char",
                    ["object"] = "Any",
                    ["Object"] = "Any",
                    ["void"] = "Unit",
                    ["Void"] = "Unit",

                    // Collection types
                    ["List<T>"] = "List<T>",
                    ["IList<T>"] = "MutableList<T>",
                    ["IEnumerable<T>"] = "List<T>",
                    ["ICollection<T>"] = "MutableList<T>",
                    ["Dictionary<TKey,TValue>"] = "Map<TKey, TValue>",
                    ["IDictionary<TKey,TValue>"] = "MutableMap<TKey, TValue>",
                    ["HashSet<T>"] = "Set<T>",
                    ["ISet<T>"] = "MutableSet<T>",
                    ["Queue<T>"] = "Queue<T>",
                    ["Stack<T>"] = "Stack<T>",

                    // Async types
                    ["Task"] = "Unit",
                    ["Task<T>"] = "T",

                    // Java/Kotlin types (pass-through)
                    ["String"] = "String",
                    ["Int"] = "Int",
                    ["Long"] = "Long",
                    ["Boolean"] = "Boolean",
                    ["Float"] = "Float",
                    ["Double"] = "Double",
                    ["Any"] = "Any",
                    ["Unit"] = "Unit",

                    // JavaScript/TypeScript to Kotlin
                    ["number"] = "Double",
                    ["boolean"] = "Boolean",
                    ["any"] = "Any",
                    ["Array<T>"] = "List<T>",
                    ["Promise<T>"] = "T",

                    // Python to Kotlin
                    ["str"] = "String",
                    ["int"] = "Long",
                    ["float"] = "Double",
                    ["bool"] = "Boolean",
                    ["list"] = "List<Any>",
                    ["dict"] = "Map<String, Any>",
                    ["tuple"] = "List<Any>",
                    ["set"] = "Set<Any>",
                    ["None"] = "Unit",
                    ["Any"] = "Any"
                },
                ["ios"] = new Dictionary<string, string>
                {
                    // C# to Swift mappings
                    ["string"] = "String",
                    ["String"] = "String",
                    ["int"] = "Int32",
                    ["Int32"] = "Int32",
                    ["long"] = "Int64",
                    ["Int64"] = "Int64",
                    ["short"] = "Int16",
                    ["Int16"] = "Int16",
                    ["byte"] = "UInt8",
                    ["Byte"] = "UInt8",
                    ["bool"] = "Bool",
                    ["Boolean"] = "Bool",
                    ["float"] = "Float",
                    ["Single"] = "Float",
                    ["double"] = "Double",
                    ["Double"] = "Double",
                    ["decimal"] = "Decimal",
                    ["Decimal"] = "Decimal",
                    ["DateTime"] = "Date",
                    ["DateTimeOffset"] = "Date",
                    ["TimeSpan"] = "TimeInterval",
                    ["Guid"] = "UUID",
                    ["Uri"] = "URL",
                    ["byte[]"] = "Data",
                    ["char"] = "Character",
                    ["Char"] = "Character",
                    ["object"] = "Any",
                    ["Object"] = "Any",
                    ["void"] = "Void",
                    ["Void"] = "Void",

                    // Collection types
                    ["List<T>"] = "[T]",
                    ["IList<T>"] = "[T]",
                    ["IEnumerable<T>"] = "[T]",
                    ["ICollection<T>"] = "[T]",
                    ["Dictionary<TKey,TValue>"] = "[TKey: TValue]",
                    ["IDictionary<TKey,TValue>"] = "[TKey: TValue]",
                    ["HashSet<T>"] = "Set<T>",
                    ["ISet<T>"] = "Set<T>",

                    // Async types
                    ["Task"] = "Void",
                    ["Task<T>"] = "T",

                    // Java/Kotlin to Swift
                    ["String"] = "String",
                    ["Int"] = "Int32",
                    ["Long"] = "Int64",
                    ["Boolean"] = "Bool",
                    ["Float"] = "Float",
                    ["Double"] = "Double",
                    ["Any"] = "Any",
                    ["Unit"] = "Void",

                    // JavaScript/TypeScript to Swift
                    ["number"] = "Double",
                    ["boolean"] = "Bool",
                    ["string"] = "String",
                    ["any"] = "Any",
                    ["Array<T>"] = "[T]",
                    ["Promise<T>"] = "T",

                    // Python to Swift
                    ["str"] = "String",
                    ["int"] = "Int64",
                    ["float"] = "Double",
                    ["bool"] = "Bool",
                    ["list"] = "[Any]",
                    ["dict"] = "[String: Any]",
                    ["tuple"] = "[Any]",
                    ["set"] = "Set<Any>",
                    ["None"] = "Void",
                    ["Any"] = "Any"
                }
            };
        }

        private Dictionary<string, Dictionary<string, string>> LoadCustomTypeMappings(string customMappingsJson)
        {
            var customMappings = new Dictionary<string, Dictionary<string, string>>();

            if (string.IsNullOrWhiteSpace(customMappingsJson))
                return customMappings;

            try
            {
                var jsonDocument = JsonDocument.Parse(customMappingsJson);

                foreach (var property in jsonDocument.RootElement.EnumerateObject())
                {
                    var sourceType = property.Name;
                    var platformMappings = new Dictionary<string, string>();

                    if (property.Value.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var platformProperty in property.Value.EnumerateObject())
                        {
                            platformMappings[platformProperty.Name.ToLower()] = platformProperty.Value.GetString();
                        }
                    }
                    else if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        // Single mapping applies to all platforms
                        var targetType = property.Value.GetString();
                        platformMappings["android"] = targetType;
                        platformMappings["ios"] = targetType;
                    }

                    customMappings[sourceType] = platformMappings;
                }

                _logger.LogDebug($"Loaded {customMappings.Count} custom type mappings");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to parse custom type mappings: {ex.Message}");
            }

            return customMappings;
        }

        private HashSet<string> CollectAllTypes(List<DiscoveredClass> discoveredClasses)
        {
            var types = new HashSet<string>();

            foreach (var discoveredClass in discoveredClasses)
            {
                // Add property types
                foreach (var property in discoveredClass.Properties)
                {
                    types.Add(property.Type);
                    if (!string.IsNullOrEmpty(property.CollectionElementType))
                    {
                        types.Add(property.CollectionElementType);
                    }
                }

                // Add method return types and parameter types
                foreach (var method in discoveredClass.Methods)
                {
                    types.Add(method.ReturnType);

                    foreach (var parameter in method.Parameters)
                    {
                        types.Add(parameter.Type);
                    }
                }
            }

            // Remove null/empty types
            types.RemoveWhere(string.IsNullOrWhiteSpace);

            return types;
        }

        private async Task<TypeMappingInfo> CreateTypeMappingAsync(
            string sourceType,
            string targetPlatform,
            Dictionary<string, Dictionary<string, string>> customMappings,
            GeneratorConfiguration config)
        {
            try
            {
                var mappingInfo = new TypeMappingInfo
                {
                    SourceType = sourceType,
                    TargetPlatform = targetPlatform,
                    IsNullable = IsNullableType(sourceType, config),
                    IsCollection = IsCollectionType(sourceType),
                    MappingSource = TypeMappingSource.Unknown
                };

                // Normalize the source type for lookup
                var normalizedSourceType = NormalizeType(sourceType);

                // Try custom mappings first
                if (customMappings.ContainsKey(sourceType) &&
                    customMappings[sourceType].ContainsKey(targetPlatform))
                {
                    mappingInfo.TargetType = customMappings[sourceType][targetPlatform];
                    mappingInfo.MappingSource = TypeMappingSource.Custom;
                    _logger.LogDebug($"Custom mapping: {sourceType} -> {mappingInfo.TargetType}");
                }
                // Try built-in mappings
                else if (_builtInMappings.ContainsKey(targetPlatform) &&
                         _builtInMappings[targetPlatform].ContainsKey(normalizedSourceType))
                {
                    mappingInfo.TargetType = _builtInMappings[targetPlatform][normalizedSourceType];
                    mappingInfo.MappingSource = TypeMappingSource.BuiltIn;
                    _logger.LogDebug($"Built-in mapping: {sourceType} -> {mappingInfo.TargetType}");
                }
                // Try generic type mapping
                else if (TryMapGenericType(sourceType, targetPlatform, out var genericMapping))
                {
                    mappingInfo.TargetType = genericMapping;
                    mappingInfo.MappingSource = TypeMappingSource.Generic;
                    _logger.LogDebug($"Generic mapping: {sourceType} -> {mappingInfo.TargetType}");
                }
                // Fallback mapping
                else
                {
                    mappingInfo.TargetType = GetFallbackMapping(sourceType, targetPlatform, config);
                    mappingInfo.MappingSource = TypeMappingSource.Fallback;
                    _logger.LogDebug($"Fallback mapping: {sourceType} -> {mappingInfo.TargetType}");
                }

                // Apply nullable wrapper if needed
                if (mappingInfo.IsNullable && config.TypeMapping.PreserveNullableTypes)
                {
                    mappingInfo.TargetType = ApplyNullableWrapper(mappingInfo.TargetType, targetPlatform);
                }

                return mappingInfo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to create type mapping for {sourceType}: {ex.Message}");
                return null;
            }
        }

        private string NormalizeType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return type;

            // Remove nullable indicator
            type = type.TrimEnd('?');

            // Remove array indicators
            type = type.Replace("[]", "");

            // Normalize generic type syntax
            if (type.Contains('<') && type.Contains('>'))
            {
                var genericStart = type.IndexOf('<');
                var baseName = type.Substring(0, genericStart);
                var genericPart = type.Substring(genericStart);

                // Count type parameters
                var typeParamCount = genericPart.Split(',').Length;
                return $"{baseName}<{string.Join(",", Enumerable.Repeat("T", typeParamCount))}>";
            }

            return type;
        }

        private bool IsNullableType(string type, GeneratorConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(type))
                return true;

            // Explicit nullable syntax
            if (type.EndsWith("?"))
                return true;

            // Language-specific nullable indicators
            var selectedLanguage = config.GetSelectedLanguage();
            switch (selectedLanguage)
            {
                case SourceLanguage.CSharp:
                    return type.Contains("null") || !IsCSharpValueType(type);

                case SourceLanguage.TypeScript:
                    return type.Contains("null") || type.Contains("undefined") || type.Contains("|");

                case SourceLanguage.Java:
                case SourceLanguage.Kotlin:
                    return !IsJavaKotlinPrimitiveType(type);

                case SourceLanguage.JavaScript:
                case SourceLanguage.Python:
                    return true; // Dynamically typed languages

                default:
                    return true;
            }
        }

        private bool IsCollectionType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return false;

            var collectionIndicators = new[]
            {
                "List", "Array", "Collection", "Set", "Map", "Dictionary", "IEnumerable", "IList", "ICollection",
                "[]", "list", "dict", "tuple", "set"
            };

            return collectionIndicators.Any(indicator => type.Contains(indicator));
        }

        private bool IsCSharpValueType(string type)
        {
            var valueTypes = new[]
            {
                "int", "long", "short", "byte", "float", "double", "decimal", "bool", "char",
                "Int32", "Int64", "Int16", "Byte", "Single", "Double", "Decimal", "Boolean", "Char",
                "DateTime", "DateTimeOffset", "TimeSpan", "Guid"
            };

            return valueTypes.Contains(type);
        }

        private bool IsJavaKotlinPrimitiveType(string type)
        {
            var primitives = new[]
            {
                "boolean", "byte", "char", "short", "int", "long", "float", "double",
                "Boolean", "Byte", "Char", "Short", "Int", "Long", "Float", "Double"
            };

            return primitives.Contains(type);
        }

        private bool TryMapGenericType(string sourceType, string targetPlatform, out string targetType)
        {
            targetType = sourceType;

            if (!sourceType.Contains('<') || !sourceType.Contains('>'))
                return false;

            try
            {
                var genericStart = sourceType.IndexOf('<');
                var baseName = sourceType.Substring(0, genericStart);
                var genericPart = sourceType.Substring(genericStart + 1, sourceType.LastIndexOf('>') - genericStart - 1);

                // Map the base type
                if (!_builtInMappings[targetPlatform].ContainsKey($"{baseName}<T>"))
                    return false;

                var baseMapping = _builtInMappings[targetPlatform][$"{baseName}<T>"];

                // Map the generic type parameters
                var typeParams = genericPart.Split(',').Select(t => t.Trim()).ToList();
                var mappedParams = new List<string>();

                foreach (var typeParam in typeParams)
                {
                    if (_builtInMappings[targetPlatform].ContainsKey(typeParam))
                    {
                        mappedParams.Add(_builtInMappings[targetPlatform][typeParam]);
                    }
                    else
                    {
                        mappedParams.Add(typeParam); // Keep as-is if no mapping found
                    }
                }

                // Construct the final mapped type
                targetType = baseMapping.Replace("T", string.Join(", ", mappedParams));
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GetFallbackMapping(string sourceType, string targetPlatform, GeneratorConfiguration config)
        {
            // If the source type looks like a class name (starts with uppercase), keep it as-is
            if (!string.IsNullOrEmpty(sourceType) && char.IsUpper(sourceType[0]) && !sourceType.Contains('<'))
            {
                return sourceType;
            }

            // Platform-specific fallbacks
            return targetPlatform switch
            {
                "android" => "Any",
                "ios" => "Any",
                _ => "Object"
            };
        }

        private string ApplyNullableWrapper(string targetType, string targetPlatform)
        {
            if (string.IsNullOrWhiteSpace(targetType))
                return targetType;

            return targetPlatform switch
            {
                "android" => $"{targetType}?",
                "ios" => $"{targetType}?",
                _ => targetType
            };
        }

        private List<string> ValidateTypeMappings(Dictionary<string, TypeMappingInfo> typeMappings, string targetPlatform)
        {
            var errors = new List<string>();

            foreach (var mapping in typeMappings.Values)
            {
                if (string.IsNullOrWhiteSpace(mapping.TargetType))
                {
                    errors.Add($"No target type mapped for source type: {mapping.SourceType}");
                }

                if (mapping.MappingSource == TypeMappingSource.Fallback)
                {
                    errors.Add($"Using fallback mapping for: {mapping.SourceType} -> {mapping.TargetType}");
                }
            }

            return errors;
        }
    }

    public class TypeMappingInfo
    {
        public string SourceType { get; set; }
        public string TargetType { get; set; }
        public string TargetPlatform { get; set; }
        public bool IsNullable { get; set; }
        public bool IsCollection { get; set; }
        public TypeMappingSource MappingSource { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public enum TypeMappingSource
    {
        Unknown,
        Custom,
        BuiltIn,
        Generic,
        Fallback
    }
}