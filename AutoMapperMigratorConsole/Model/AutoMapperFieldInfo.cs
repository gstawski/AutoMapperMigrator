using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMapperMigratorConsole.Model;

public sealed class AutoMapperFieldInfo
{
    public string SourceField { get; set; }

    public string DestinationField { get; set; }

    public bool Ignore { get; set; }

    public bool AfterMap { get; set; }

    public ExpressionSyntax SyntaxNode { get; set; }

    public string Code { get; set; }
}