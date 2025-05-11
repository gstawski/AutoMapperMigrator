using System.Collections.Generic;
using System.Linq;
using AutoMapperMigratorConsole.Model;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMapperMigratorConsole.WalkerCollectors;

public class AutoMapperMappingsWalker : CSharpSyntaxWalker
{
    private readonly List<AutoMapperFieldInfo> _mappings = new();
    private readonly Dictionary<string, byte> _uniquePropertyCheck = new();

    public ICollection<AutoMapperFieldInfo> Mappings => _mappings;

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        if (node.Expression is MemberAccessExpressionSyntax mae1 &&
            mae1.Name.Identifier.Text == "ForMember")
        {
            var args = node.ArgumentList.Arguments;
            if (args.Count == 2)
            {
                var destExpression = args[0].Expression as LambdaExpressionSyntax;
                var sourceExpression = args[1]
                    .Expression
                    .DescendantNodes()
                    .OfType<LambdaExpressionSyntax>()
                    .LastOrDefault();

                if (destExpression?.Body is MemberAccessExpressionSyntax destMae)
                {
                    if (sourceExpression?.Body is MemberAccessExpressionSyntax sourceMae)
                    {
                        var mapping = new AutoMapperFieldInfo
                        {
                            SourceField = sourceMae.Name.Identifier.Text,
                            DestinationField = destMae.Name.Identifier.Text
                        };

                        if (!UniquenessCheck(mapping))
                        {
                            _mappings.Add(mapping);
                        }
                    }
                    else if (sourceExpression?.Body is ConditionalExpressionSyntax conditionalExpressionSyntax)
                    {
                        var mapping = new AutoMapperFieldInfo
                        {
                            SourceField = string.Empty,
                            DestinationField = destMae.Name.Identifier.Text,
                            Ignore = false,
                            SyntaxNode = conditionalExpressionSyntax
                        };

                        if (!UniquenessCheck(mapping))
                        {
                            _mappings.Add(mapping);
                        }
                    }
                    else if (sourceExpression?.Body is InvocationExpressionSyntax expressionSyntax)
                    {
                        var mapping = new AutoMapperFieldInfo
                        {
                            SourceField = string.Empty,
                            DestinationField = destMae.Name.Identifier.Text,
                            Ignore = false,
                            SyntaxNode = expressionSyntax
                        };
                        if (!UniquenessCheck(mapping))
                        {
                            _mappings.Add(mapping);
                        }
                    }
                }

                var destLambda = args[0].Expression as SimpleLambdaExpressionSyntax;
                var sourceLambda = args[1].Expression as SimpleLambdaExpressionSyntax;

                if (destLambda != null && sourceLambda != null)
                {
                    if (destLambda.Body is MemberAccessExpressionSyntax destMae2 &&
                        sourceLambda.Body is MemberAccessExpressionSyntax sourceMae2)
                    {
                        var mapping = new AutoMapperFieldInfo
                        {
                            SourceField = sourceMae2.Name.Identifier.Text,
                            DestinationField = destMae2.Name.Identifier.Text
                        };

                        if (!UniquenessCheck(mapping))
                        {
                            _mappings.Add(mapping);
                        }
                    }
                    else if (destLambda.Body is MemberAccessExpressionSyntax destMae3 &&
                             sourceLambda.Body is InvocationExpressionSyntax sourceMae3)
                    {
                        if (sourceMae3.ArgumentList.Arguments.Count == 1)
                        {
                            var simpleLambda = sourceMae3.ArgumentList.Arguments[0].Expression as SimpleLambdaExpressionSyntax;

                            if (simpleLambda != null)
                            {
                                var mapping = new AutoMapperFieldInfo
                                {
                                    DestinationField = destMae3.Name.Identifier.Text,
                                    SyntaxNode = simpleLambda
                                };

                                if (!UniquenessCheck(mapping))
                                {
                                    _mappings.Add(mapping);
                                }
                            }
                        }
                    }
                }
            }
        }
        else if (node.Expression is MemberAccessExpressionSyntax mae2 &&
                 mae2.Name.Identifier.Text == "ForPath")
        {
            var args = node.ArgumentList.Arguments;
            if (args.Count == 2)
            {
                var destExpression = args[0].Expression as LambdaExpressionSyntax;
                var sourceExpression = args[1]
                    .Expression
                    .DescendantNodes()
                    .OfType<LambdaExpressionSyntax>()
                    .LastOrDefault();

                if (destExpression?.Body is MemberAccessExpressionSyntax destMae1 &&
                    sourceExpression?.Body is MemberAccessExpressionSyntax sourceMae1)
                {
                    var mapping = new AutoMapperFieldInfo
                    {
                        SourceField = sourceMae1.Name.Identifier.Text,
                        DestinationField = destMae1.Name.Identifier.Text
                    };
                    if (!UniquenessCheck(mapping))
                    {
                        _mappings.Add(mapping);
                    }
                }

                var destLambda = args[0].Expression as SimpleLambdaExpressionSyntax;
                var sourceLambda = args[1].Expression as SimpleLambdaExpressionSyntax;

                if (destLambda != null && sourceLambda != null)
                {
                    if (destLambda.Body is MemberAccessExpressionSyntax destMae2 &&
                        sourceLambda.Body is MemberAccessExpressionSyntax sourceMae2)
                    {
                        var mapping = new AutoMapperFieldInfo
                        {
                            SourceField = sourceMae2.Name.Identifier.Text,
                            DestinationField = destMae2.Name.Identifier.Text
                        };

                        if (!UniquenessCheck(mapping))
                        {
                            _mappings.Add(mapping);
                        }
                    }
                    else if (destLambda.Body is MemberAccessExpressionSyntax destMae3 &&
                             sourceLambda.Body is InvocationExpressionSyntax sourceMae3 &&
                             (sourceMae3.Expression as MemberAccessExpressionSyntax)?.Name.ToString() == "Ignore")
                    {
                        var mapping = new AutoMapperFieldInfo
                        {
                            SourceField = string.Empty,
                            DestinationField = destMae3.Name.Identifier.Text,
                            Ignore = true
                        };

                        if (!UniquenessCheck(mapping))
                        {
                            _mappings.Add(mapping);
                        }
                    }
                }
            }
        }
        else if (node.Expression is MemberAccessExpressionSyntax mae3 &&
                 mae3.Name.Identifier.Text == "AfterMap")
        {
            var args = node.ArgumentList.Arguments;
            if (args.Count == 1)
            {
                var lambda = args[0].Expression as ParenthesizedLambdaExpressionSyntax;
                if (lambda != null)
                {
                    var body = lambda.Body.ToString();
                    var parms = lambda.ParameterList;

                    if (parms.Parameters.Count == 2)
                    {
                        var p1 = parms.Parameters[0].Identifier.Text + ".";
                        var p2 = parms.Parameters[1].Identifier.Text + ".";

                        body = body.Replace(p1, "source.");
                        body = body.Replace(p2, "desc.");

                        var mapping = new AutoMapperFieldInfo
                        {
                            Code = body,
                            AfterMap = true
                        };
                        _mappings.Add(mapping);
                    }
                }
            }
        }

        base.VisitInvocationExpression(node);
    }

    private bool UniquenessCheck(AutoMapperFieldInfo mapping)
    {
        if (_uniquePropertyCheck.ContainsKey($"{mapping.SourceField}-{mapping.DestinationField}"))
        {
            return true;
        }

        if (_uniquePropertyCheck.ContainsKey($"X-{mapping.DestinationField}"))
        {
            return true;
        }

        _uniquePropertyCheck.TryAdd($"{mapping.SourceField}-{mapping.DestinationField}", 0);
        _uniquePropertyCheck.TryAdd($"X-{mapping.DestinationField}", 0);
        return false;
    }
}