using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMapperMigratorConsole.Model;

public sealed class AutoMapperCreateMap
{
    public string SourceType { get; set; }

    public string DestinationType { get; set; }

    public bool ReverseMap { get; set; }

    public ICollection<AutoMapperFieldInfo> FieldsMap { get; set; }

    public ExpressionStatementSyntax SyntaxNode { get; set; }
}