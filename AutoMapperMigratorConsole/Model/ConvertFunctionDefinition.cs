namespace AutoMapperMigratorConsole.Model;

public sealed class ConvertFunctionDefinition
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