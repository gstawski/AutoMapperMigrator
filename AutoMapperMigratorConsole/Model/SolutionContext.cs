using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace AutoMapperMigratorConsole.Model;

public class SolutionContext
{
    private readonly Dictionary<string, ISymbol> _models;
    private readonly Dictionary<string, Dictionary<string, int>> _functionsNames = new();

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

    public ISymbol FindClassSymbol(string className)
    {
        if (_models.TryGetValue(className, out ISymbol model))
        {
            return model;
        }

        var nameWithDot = "." + className;
        foreach (var m in _models)
        {
            if (m.Key.EndsWith(nameWithDot))
            {
                return m.Value;
            }
        }

        return null;
    }

    public string FindFunctionName(string prefix, string name)
    {
        var symbol = TryGetSolutionSymbol(name);
        if (symbol == null)
        {
            return prefix + name;
        }

        return GetFunctionName(prefix + name, symbol.ToDisplayString());
    }

    public string GetFunctionName(string name, string nameWithNamespace)
    {
        if (_functionsNames.TryGetValue(name, out var dct))
        {
            if (dct.TryGetValue(nameWithNamespace, out var number))
            {
                if (number > 0)
                {
                    return name+number;
                }

                return name;
            }

            int count = dct.Count;
            dct.TryAdd(nameWithNamespace, count);
            return name+count;
        }

        _functionsNames.Add(name, new Dictionary<string,int> { { nameWithNamespace, 0 } });
        return name;
    }
}