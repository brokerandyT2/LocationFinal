using Location.Core.Helpers.AdapterGeneration;

namespace Location.Photography.Tools.AdapterGenerator.Services;

public class TypeTranslator
{
    // NEW: Attribute-aware Kotlin type mapping
    public string GetKotlinType(PropertyMetadata propertyMetadata)
    {
        // Check for custom MapTo attribute first
        if (propertyMetadata.MapToAttribute != null)
        {
            return GetKotlinTypeFromEnum(propertyMetadata.MapToAttribute.Android);
        }

        // Check for DateTime semantic mapping
        if (propertyMetadata.Type == typeof(DateTime) && propertyMetadata.DateTypeAttribute != null)
        {
            return GetKotlinDateTimeType(propertyMetadata.DateTypeAttribute.Semantic);
        }

        // Fall back to standard type mapping
        return GetKotlinType(propertyMetadata.Type);
    }

    // NEW: Attribute-aware Swift type mapping
    public string GetSwiftType(PropertyMetadata propertyMetadata)
    {
        // Check for custom MapTo attribute first
        if (propertyMetadata.MapToAttribute != null)
        {
            return GetSwiftTypeFromEnum(propertyMetadata.MapToAttribute.iOS);
        }

        // Check for DateTime semantic mapping
        if (propertyMetadata.Type == typeof(DateTime) && propertyMetadata.DateTypeAttribute != null)
        {
            return GetSwiftDateTimeType(propertyMetadata.DateTypeAttribute.Semantic);
        }

        // Fall back to standard type mapping
        return GetSwiftType(propertyMetadata.Type);
    }

    // Original methods remain unchanged for backward compatibility
    public string GetKotlinType(Type? dotnetType)
    {
        if (dotnetType == null) return "Any";

        return dotnetType.Name switch
        {
            "String" => "String",
            "Int32" => "Int",
            "Int64" => "Long",
            "Boolean" => "Boolean",
            "Double" => "Double",
            "Single" => "Float",
            "DateTime" => "LocalDateTime", // Default - local time calculations
            "ObservableCollection`1" => $"List<{GetElementType(dotnetType)}>",
            _ => dotnetType.Name
        };
    }

    public string GetSwiftType(Type? dotnetType)
    {
        if (dotnetType == null) return "Any";

        return dotnetType.Name switch
        {
            "String" => "String",
            "Int32" => "Int32",
            "Int64" => "Int64",
            "Boolean" => "Bool",
            "Double" => "Double",
            "Single" => "Float",
            "DateTime" => "Date", // Default - Swift Date type for DateTime
            "ObservableCollection`1" => $"[{GetSwiftElementType(dotnetType)}]",
            _ => dotnetType.Name
        };
    }

    // NEW: Convert AndroidType enum to Kotlin type string
    private string GetKotlinTypeFromEnum(AndroidType androidType)
    {
        return androidType switch
        {
            AndroidType.String => "String",
            AndroidType.Int => "Int",
            AndroidType.Long => "Long",
            AndroidType.Short => "Short",
            AndroidType.Byte => "Byte",
            AndroidType.Boolean => "Boolean",
            AndroidType.Double => "Double",
            AndroidType.Float => "Float",
            AndroidType.Char => "Char",
            AndroidType.LocalDateTime => "LocalDateTime",
            AndroidType.Instant => "Instant",
            AndroidType.LocalDate => "LocalDate",
            AndroidType.LocalTime => "LocalTime",
            AndroidType.Duration => "Duration",
            AndroidType.ZonedDateTime => "ZonedDateTime",
            AndroidType.ByteArray => "ByteArray",
            AndroidType.IntArray => "IntArray",
            AndroidType.StringArray => "Array<String>",
            AndroidType.List => "List",
            AndroidType.MutableList => "MutableList",
            AndroidType.LatLng => "LatLng",
            AndroidType.Location => "Location",
            AndroidType.CameraDevice => "CameraDevice",
            AndroidType.Size => "Size",
            AndroidType.UUID => "UUID",
            AndroidType.URI => "URI",
            _ => "Any"
        };
    }

    // NEW: Convert IOSType enum to Swift type string
    private string GetSwiftTypeFromEnum(IOSType iosType)
    {
        return iosType switch
        {
            IOSType.String => "String",
            IOSType.Int32 => "Int32",
            IOSType.Int64 => "Int64",
            IOSType.Int16 => "Int16",
            IOSType.UInt8 => "UInt8",
            IOSType.Int8 => "Int8",
            IOSType.Bool => "Bool",
            IOSType.Double => "Double",
            IOSType.Float => "Float",
            IOSType.Character => "Character",
            IOSType.Date => "Date",
            IOSType.DateComponents => "DateComponents",
            IOSType.TimeInterval => "TimeInterval",
            IOSType.UInt8Array => "[UInt8]",
            IOSType.Int32Array => "[Int32]",
            IOSType.StringArray => "[String]",
            IOSType.Array => "Array",
            IOSType.CLLocationCoordinate2D => "CLLocationCoordinate2D",
            IOSType.CLLocation => "CLLocation",
            IOSType.AVCaptureDevice => "AVCaptureDevice",
            IOSType.CGSize => "CGSize",
            IOSType.UUID => "UUID",
            IOSType.URL => "URL",
            _ => "Any"
        };
    }

    // NEW: Convert DateTime semantic to Kotlin type
    private string GetKotlinDateTimeType(DateTimeSemantic semantic)
    {
        return semantic switch
        {
            DateTimeSemantic.LocalDateTime => "LocalDateTime",
            DateTimeSemantic.UtcInstant => "Instant",
            DateTimeSemantic.DateOnly => "LocalDate",
            DateTimeSemantic.TimeOnly => "LocalTime",
            DateTimeSemantic.Duration => "Duration",
            DateTimeSemantic.ZonedDateTime => "ZonedDateTime",
            _ => "LocalDateTime" // Default fallback
        };
    }

    // NEW: Convert DateTime semantic to Swift type
    private string GetSwiftDateTimeType(DateTimeSemantic semantic)
    {
        return semantic switch
        {
            DateTimeSemantic.LocalDateTime => "Date",
            DateTimeSemantic.UtcInstant => "Date", // Swift Date handles UTC
            DateTimeSemantic.DateOnly => "DateComponents",
            DateTimeSemantic.TimeOnly => "DateComponents",
            DateTimeSemantic.Duration => "TimeInterval",
            DateTimeSemantic.ZonedDateTime => "Date", // Swift Date with TimeZone
            _ => "Date" // Default fallback
        };
    }

    // NEW: Get property name considering GenerateAs attribute
    public string GetKotlinPropertyName(PropertyMetadata propertyMetadata)
    {
        if (propertyMetadata.GenerateAsAttribute != null &&
            !string.IsNullOrEmpty(propertyMetadata.GenerateAsAttribute.AndroidName))
        {
            return propertyMetadata.GenerateAsAttribute.AndroidName;
        }

        return propertyMetadata.CamelCaseName;
    }

    // NEW: Get property name considering GenerateAs attribute
    public string GetSwiftPropertyName(PropertyMetadata propertyMetadata)
    {
        if (propertyMetadata.GenerateAsAttribute != null &&
            !string.IsNullOrEmpty(propertyMetadata.GenerateAsAttribute.IOSName))
        {
            return propertyMetadata.GenerateAsAttribute.IOSName;
        }

        return propertyMetadata.CamelCaseName;
    }

    // NEW: Get command method name considering GenerateAs attribute
    public string GetKotlinCommandName(CommandMetadata commandMetadata)
    {
        // Check CommandBehavior attribute for custom method name first
        if (commandMetadata.CommandBehaviorAttribute != null &&
            !string.IsNullOrEmpty(commandMetadata.CommandBehaviorAttribute.CustomMethodName))
        {
            return commandMetadata.CommandBehaviorAttribute.CustomMethodName;
        }

        // Check GenerateAs attribute
        if (commandMetadata.GenerateAsAttribute != null &&
            !string.IsNullOrEmpty(commandMetadata.GenerateAsAttribute.AndroidName))
        {
            return commandMetadata.GenerateAsAttribute.AndroidName;
        }

        return commandMetadata.MethodName;
    }

    // NEW: Get command method name considering GenerateAs attribute
    public string GetSwiftCommandName(CommandMetadata commandMetadata)
    {
        // Check CommandBehavior attribute for custom method name first
        if (commandMetadata.CommandBehaviorAttribute != null &&
            !string.IsNullOrEmpty(commandMetadata.CommandBehaviorAttribute.CustomMethodName))
        {
            return commandMetadata.CommandBehaviorAttribute.CustomMethodName;
        }

        // Check GenerateAs attribute
        if (commandMetadata.GenerateAsAttribute != null &&
            !string.IsNullOrEmpty(commandMetadata.GenerateAsAttribute.IOSName))
        {
            return commandMetadata.GenerateAsAttribute.IOSName;
        }

        return commandMetadata.MethodName;
    }

    // NEW: Check if property should be excluded for platform
    public bool ShouldExcludePropertyForPlatform(PropertyMetadata propertyMetadata, string platform)
    {
        if (propertyMetadata.AvailableAttribute != null)
        {
            return platform.ToLower() switch
            {
                "android" => !propertyMetadata.AvailableAttribute.Android,
                "ios" => !propertyMetadata.AvailableAttribute.iOS,
                _ => false
            };
        }
        return false;
    }

    // NEW: Check if command should be excluded for platform
    public bool ShouldExcludeCommandForPlatform(CommandMetadata commandMetadata, string platform)
    {
        if (commandMetadata.AvailableAttribute != null)
        {
            return platform.ToLower() switch
            {
                "android" => !commandMetadata.AvailableAttribute.Android,
                "ios" => !commandMetadata.AvailableAttribute.iOS,
                _ => false
            };
        }
        return false;
    }

    // NEW: Generate conversion logic for DateTime semantic types (Kotlin)
    public string GetKotlinDateTimeConversion(PropertyMetadata propertyMetadata, string dotnetPropertyAccess)
    {
        if (propertyMetadata.Type != typeof(DateTime) || propertyMetadata.DateTypeAttribute == null)
        {
            return dotnetPropertyAccess; // No conversion needed
        }

        return propertyMetadata.DateTypeAttribute.Semantic switch
        {
            DateTimeSemantic.LocalDateTime => $"{dotnetPropertyAccess}.toLocalDateTime()",
            DateTimeSemantic.UtcInstant => $"Instant.ofEpochMilli({dotnetPropertyAccess}.toUnixTimeMilliseconds())",
            DateTimeSemantic.DateOnly => $"{dotnetPropertyAccess}.toLocalDate()",
            DateTimeSemantic.TimeOnly => $"{dotnetPropertyAccess}.toLocalTime()",
            DateTimeSemantic.Duration => $"Duration.ofMillis({dotnetPropertyAccess}.toUnixTimeMilliseconds())",
            DateTimeSemantic.ZonedDateTime => $"{dotnetPropertyAccess}.toZonedDateTime()",
            _ => $"{dotnetPropertyAccess}.toLocalDateTime()" // Default
        };
    }

    // NEW: Generate conversion logic for DateTime semantic types (Swift)
    public string GetSwiftDateTimeConversion(PropertyMetadata propertyMetadata, string dotnetPropertyAccess)
    {
        if (propertyMetadata.Type != typeof(DateTime) || propertyMetadata.DateTypeAttribute == null)
        {
            return dotnetPropertyAccess; // No conversion needed
        }

        return propertyMetadata.DateTypeAttribute.Semantic switch
        {
            DateTimeSemantic.LocalDateTime => dotnetPropertyAccess, // Swift Date handles this
            DateTimeSemantic.UtcInstant => dotnetPropertyAccess, // Swift Date handles UTC
            DateTimeSemantic.DateOnly => $"Calendar.current.dateComponents([.year, .month, .day], from: {dotnetPropertyAccess})",
            DateTimeSemantic.TimeOnly => $"Calendar.current.dateComponents([.hour, .minute, .second], from: {dotnetPropertyAccess})",
            DateTimeSemantic.Duration => $"{dotnetPropertyAccess}.timeIntervalSinceReferenceDate",
            DateTimeSemantic.ZonedDateTime => dotnetPropertyAccess, // Swift Date with TimeZone
            _ => dotnetPropertyAccess // Default
        };
    }

    // Existing helper methods remain unchanged
    private string GetElementType(Type collectionType)
    {
        if (collectionType.IsGenericType)
        {
            var elementType = collectionType.GetGenericArguments().FirstOrDefault();
            return GetKotlinType(elementType);
        }
        return "Any";
    }

    private string GetSwiftElementType(Type collectionType)
    {
        if (collectionType.IsGenericType)
        {
            var elementType = collectionType.GetGenericArguments().FirstOrDefault();
            return GetSwiftType(elementType);
        }
        return "Any";
    }
}