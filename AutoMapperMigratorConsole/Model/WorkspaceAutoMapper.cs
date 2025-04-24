using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace AutoMapperMigratorConsole.Model;

public class AutoMapperFieldInfo
{
    public string SourceField { get; set; }

    public string DestinationField { get; set; }

    public bool Ignore { get; set; }

    public ExpressionSyntax SyntaxNode { get; set; }
}

public class AutoMapperCreateMap
{
    public string SourceType { get; set; }

    public string DestinationType { get; set; }

    public bool ReverseMap { get; set; }

    public ICollection<AutoMapperFieldInfo> FieldsMap { get; set; }

    public ExpressionStatementSyntax SyntaxNode { get; set; }
}

public class WorkspaceAutoMapper
{
    public WorkspaceProject Project { get; set; }

    public List<AutoMapperCreateMap> Mappings { get; set; }
}