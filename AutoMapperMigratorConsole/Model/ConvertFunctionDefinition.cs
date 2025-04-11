namespace AutoMapperMigratorConsole.Model;

public class ConvertFunctionDefinition
{
    public ConvertFunctionDefinition(string functionName, string body, bool mapFunction)
    {
        FunctionName = functionName;
        Body = body;
        MapFunction = mapFunction;
    }

    public string FunctionName {get; }
    public string Body { get; }
    public bool MapFunction { get; }
}

/*
public class ConvertFunctionDefinition
{
    public ConvertFunctionDefinition(string functionName, SyntaxKind type, string sourceType, bool nullAble, bool mapFunction)
    {
        FunctionName = functionName;
        Type = type;
        SourceType = GetSyntaxKind(sourceType);
        NullAble = nullAble;
        SourceTypeString = sourceType;
        MapFunction = mapFunction;
    }

    private static SyntaxKind GetSyntaxKind(string sourceType)
    {
        var lowerSourceType = sourceType.ToLower().TrimEnd('?');
        return lowerSourceType switch
        {
            "string" => SyntaxKind.StringKeyword,
            "int" => SyntaxKind.IntKeyword,
            "long" => SyntaxKind.LongKeyword,
            "decimal" => SyntaxKind.DecimalKeyword,
            "double" => SyntaxKind.DoubleKeyword,
            "float" => SyntaxKind.FloatKeyword,
            "bool" => SyntaxKind.BoolKeyword,
            _ => SyntaxKind.None
        };
    }

    public string FunctionName {get; }
    public SyntaxKind Type { get; }
    public SyntaxKind SourceType { get;  }

    public string SourceTypeString { get; }
    public bool NullAble { get; }

    public bool MapFunction { get; }
}
*/