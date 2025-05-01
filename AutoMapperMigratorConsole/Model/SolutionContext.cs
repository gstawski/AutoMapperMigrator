using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace AutoMapperMigratorConsole.Model;

public class SolutionContext
{
    private readonly Dictionary<string, ISymbol> _models;

    public string DefaultNamespace { get; }

    public List<ClassMapDefinition> ClassMaps { get; } = new();

    public SolutionContext(Dictionary<string,ISymbol> models, string defaultNamespace)
    {
        DefaultNamespace = defaultNamespace;
        _models = models;
    }

    public bool IsSolutionType(string type)
    {
        var f = _models.ContainsKey(type);
        if (!f)
        {
            var key = "." + type;
            foreach (var m in _models.Keys)
            {
                if (m.EndsWith(key))
                {
                    return true;
                }
            }
        }

        return f;
    }

    public ISymbol TryGetSolutionSymbol(string type)
    {
        if (type.EndsWith("?"))
        {
            type = type.Substring(0, type.Length - 1);
        }

        if (_models.TryGetValue(type, out var solutionType))
        {
            return solutionType;
        }

        var key = "." + type;
        foreach (var m in _models.Keys)
        {
            if (m.EndsWith(key))
            {
                return _models[m];
            }
        }

        return null;
    }
}