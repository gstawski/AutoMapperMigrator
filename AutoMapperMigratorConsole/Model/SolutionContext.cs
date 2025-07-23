using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace AutoMapperMigratorConsole.Model;

public sealed class SolutionContext
{
    private readonly Dictionary<string, ISymbol> _models;
    private readonly string _mapFunctionPrefix;
    private readonly Dictionary<string, Dictionary<string, int>> _functionsNames = new();

    public string DefaultNamespace { get; }

    public List<ClassMapDefinition> ClassMaps { get; } = new();

    public SolutionContext(Dictionary<string,ISymbol> models, string defaultNamespace, string mapFunctionPrefix)
    {
        DefaultNamespace = defaultNamespace;
        _models = models;
        _mapFunctionPrefix = mapFunctionPrefix;
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

    public ISymbol TryGetSolutionSymbol(string type, List<string> namespaces)
    {
        if (type.EndsWith("?"))
        {
            type = type.Substring(0, type.Length - 1);
        }

        foreach (var space in namespaces)
        {
            if (_models.TryGetValue($"{space}.{type}", out var solutionType))
            {
                return solutionType;
            }
        }

        if (type.Contains('.'))
        {
            return TryGetSolutionSymbol(type);
        }

        return null;
    }

    public ISymbol TryGetSolutionSymbol(string type)
    {
        if (string.IsNullOrEmpty(type))
        {
            return null;
        }

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

    public string FindFunctionName(string name)
    {
        return _mapFunctionPrefix + name;
    }

    public string FindFunctionName(string name, string sourceType)
    {
        var symbol = TryGetSolutionSymbol(sourceType);
        if (symbol == null)
        {
            return _mapFunctionPrefix + name;
        }

        return _mapFunctionPrefix + FunctionName(name, symbol.ToDisplayString());
    }

    public string GetFunctionName(string descNameWithNamespace, string sourceNameWithNamespace)
    {
        var name = descNameWithNamespace.Split('.')[^1];

        return  _mapFunctionPrefix + FunctionName(name, sourceNameWithNamespace);
    }

    private string FunctionName(string name, string nameWithNamespace)
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