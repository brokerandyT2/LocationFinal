// Location/Location.Core.Helpers/AdapterGeneration/AdapterAttributes.cs
using System;

namespace Location.Core.Helpers.AdapterGeneration
{
    /// <summary>
    /// Android/Kotlin target types for adapter generation
    /// </summary>
    public enum AndroidType
    {
        // Primitives
        String,
        Int,
        Long,
        Short,
        Byte,              // Signed -128 to 127
        Boolean,
        Double,
        Float,
        Char,

        // Date/Time
        LocalDateTime,
        Instant,
        LocalDate,
        LocalTime,
        Duration,
        ZonedDateTime,

        // Collections
        ByteArray,
        IntArray,
        StringArray,
        List,              // Generic List<T>
        MutableList,       // Generic MutableList<T>

        // Location/Hardware
        LatLng,
        Location,
        CameraDevice,
        Size,

        // Other
        UUID,
        URI
    }

    /// <summary>
    /// iOS/Swift target types for adapter generation
    /// </summary>
    public enum IOSType
    {
        // Primitives
        String,
        Int32,
        Int64,
        Int16,
        UInt8,             // Unsigned 0-255 (for .NET byte)
        Int8,              // Signed -128 to 127
        Bool,
        Double,
        Float,
        Character,

        // Date/Time
        Date,
        DateComponents,
        TimeInterval,

        // Collections
        UInt8Array,        // [UInt8] for byte arrays
        Int32Array,        // [Int32]
        StringArray,       // [String]
        Array,             // [T] generic array

        // Location/Hardware
        CLLocationCoordinate2D,
        CLLocation,
        AVCaptureDevice,
        CGSize,

        // Other
        UUID,
        URL
    }

    /// <summary>
    /// DateTime semantic types for proper mobile mapping
    /// </summary>
    public enum DateTimeSemantic
    {
        /// <summary>
        /// Local date and time (most common case)
        /// Android: LocalDateTime, iOS: Date
        /// </summary>
        LocalDateTime,

        /// <summary>
        /// UTC timestamp/instant in time
        /// Android: Instant, iOS: Date (with UTC timezone)
        /// </summary>
        UtcInstant,

        /// <summary>
        /// Date only, no time component matters
        /// Android: LocalDate, iOS: DateComponents (date only)
        /// </summary>
        DateOnly,

        /// <summary>
        /// Time only, no date component matters  
        /// Android: LocalTime, iOS: DateComponents (time only)
        /// </summary>
        TimeOnly,

        /// <summary>
        /// Duration/elapsed time
        /// Android: Duration, iOS: TimeInterval
        /// </summary>
        Duration,

        /// <summary>
        /// Date and time with specific timezone
        /// Android: ZonedDateTime, iOS: Date with TimeZone
        /// </summary>
        ZonedDateTime
    }

    /// <summary>
    /// Maps .NET property to specific mobile platform types
    /// Use only when default type mapping is insufficient
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class MapToAttribute : Attribute
    {
        public AndroidType Android { get; set; }
        public IOSType iOS { get; set; }

        public MapToAttribute(AndroidType android, IOSType ios)
        {
            Android = android;
            iOS = ios;
        }
    }

    /// <summary>
    /// Specifies the semantic meaning of DateTime properties for proper mobile mapping
    /// Solves the "DateTime means everything" problem in .NET
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DateTypeAttribute : Attribute
    {
        public DateTimeSemantic Semantic { get; set; }

        public DateTypeAttribute(DateTimeSemantic semantic)
        {
            Semantic = semantic;
        }
    }

    /// <summary>
    /// Controls platform availability for properties, commands, or entire ViewModels
    /// Default: Available on both platforms if no attribute present
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method)]
    public class AvailableAttribute : Attribute
    {
        public bool Android { get; set; }
        public bool iOS { get; set; }

        public AvailableAttribute(bool android, bool ios)
        {
            Android = android;
            iOS = ios;
        }
    }

    /// <summary>
    /// Completely excludes from adapter generation
    /// Use for internal properties, debug data, or complex types that shouldn't be exposed to mobile
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method)]
    public class ExcludeAttribute : Attribute
    {
        public string? Reason { get; set; }

        public ExcludeAttribute() { }
        public ExcludeAttribute(string reason) => Reason = reason;
    }

    /// <summary>
    /// Customizes the generated property/method name per platform
    /// Use when mobile naming conventions differ from .NET conventions
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class GenerateAsAttribute : Attribute
    {
        public string? AndroidName { get; set; }
        public string? IOSName { get; set; }

        /// <summary>
        /// Use the same custom name on both platforms
        /// </summary>
        public GenerateAsAttribute(string name)
        {
            AndroidName = name;
            IOSName = name;
        }

        /// <summary>
        /// Use different names per platform
        /// </summary>
        public GenerateAsAttribute(string androidName, string iosName)
        {
            AndroidName = androidName;
            IOSName = iosName;
        }
    }

    /// <summary>
    /// Generates a stub implementation with warning comment
    /// Use for properties/methods that require custom mobile implementation
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class WarnCustomImplementationNeededAttribute : Attribute
    {
        public string? Message { get; set; }

        public WarnCustomImplementationNeededAttribute() { }
        public WarnCustomImplementationNeededAttribute(string message) => Message = message;
    }

    /// <summary>
    /// Customizes command generation behavior
    /// Use for commands that need special threading or execution requirements
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CommandBehaviorAttribute : Attribute
    {
        /// <summary>
        /// Whether to expose the CanExecute property in mobile adapters
        /// </summary>
        public bool ExposeCanExecute { get; set; } = false;

        /// <summary>
        /// Whether command execution requires main thread
        /// </summary>
        public bool RequiresMainThread { get; set; } = false;

        /// <summary>
        /// Custom method name for the command in mobile adapters
        /// </summary>
        public string? CustomMethodName { get; set; }
    }

    /// <summary>
    /// Customizes ObservableCollection binding behavior for performance
    /// Use for large collections that need batching or incremental loading
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CollectionBehaviorAttribute : Attribute
    {
        /// <summary>
        /// Whether the collection supports batched updates for performance
        /// </summary>
        public bool SupportsBatching { get; set; } = false;

        /// <summary>
        /// Batch size for collection updates
        /// </summary>
        public int BatchSize { get; set; } = 50;

        /// <summary>
        /// Whether the collection supports incremental loading
        /// </summary>
        public bool SupportsIncrementalLoading { get; set; } = false;
    }

    /// <summary>
    /// Adds custom validation behavior to properties
    /// Use for properties that need mobile-side validation
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ValidationBehaviorAttribute : Attribute
    {
        /// <summary>
        /// Whether to validate when property value is set
        /// </summary>
        public bool ValidateOnSet { get; set; } = true;

        /// <summary>
        /// Name of validation method to call
        /// </summary>
        public string? ValidatorMethod { get; set; }

        /// <summary>
        /// Whether to propagate validation errors to mobile UI
        /// </summary>
        public bool PropagateErrors { get; set; } = true;
    }

    /// <summary>
    /// Specifies threading requirements for properties or methods
    /// Use for operations that must run on specific threads
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class ThreadingBehaviorAttribute : Attribute
    {
        /// <summary>
        /// Whether the operation requires main/UI thread
        /// </summary>
        public bool RequiresMainThread { get; set; } = false;

        /// <summary>
        /// Whether the operation requires background thread
        /// </summary>
        public bool RequiresBackgroundThread { get; set; } = false;
    }
}