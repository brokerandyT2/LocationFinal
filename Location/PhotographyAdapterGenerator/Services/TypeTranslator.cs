namespace Location.Photography.Tools.AdapterGenerator.Services;

public class TypeTranslator
{
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
            "DateTime" => "LocalDateTime", // As requested - local time calculations
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
            "DateTime" => "Date", // Swift Date type for DateTime
            "ObservableCollection`1" => $"[{GetSwiftElementType(dotnetType)}]",
            _ => dotnetType.Name
        };
    }

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