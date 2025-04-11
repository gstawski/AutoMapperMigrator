using System.Collections.Generic;
using System.Linq;

namespace AutoMapperMigratorConsole.Model;

public class PropertyDefinition
{
    private static string SimplyTypeName(string typeName)
    {
        if (typeName.Contains(".") && !typeName.Contains("<"))
        {
            var split = typeName.Split('.');
            return split[^1];
        }

        return typeName;
    }

    public PropertyDefinition(string name, string type, int order, bool isSetPublic, bool isSimpleType)
    {
        Name = name;
        Type = type;
        Order = order;
        IsPublicSet = isSetPublic;
        IsSimpleType = isSimpleType;
        IsNullable = type.EndsWith("?");
        RawType = SimplyTypeName(type);
    }

    public PropertyDefinition(string name, string type, int order, string propertyAssignment, bool isSimpleType)
    {
        Name = name;
        Type = type;
        Order = order;
        IsPublicSet = true;
        IsSimpleType = isSimpleType;
        PropertyAssignment = propertyAssignment;
        IsNullable = type.EndsWith("?");
        RawType = SimplyTypeName(type);
    }

    public string Name { get; }

    public string PropertyAssignment { get; }

    public string Type { get; }

    public string RawType { get; }

    public bool IsNullable { get; }

    public int Order { get; }

    public bool IsPublicSet { get; }

    public bool IsSimpleType { get; }
}

public class ConstructorDefinition
{
    public Dictionary<string, PropertyDefinition> PropertiesAndTypes { get; set; }
}

public class ClassDefinition
{
    private string _typeNameWithNamespace;

    public ClassDefinition(string typeName, string fullNamespace, Dictionary<string, PropertyDefinition> propertyDefinitions, List<ConstructorDefinition> constructors)
    {
        TypeName = typeName;
        PropertiesAndTypes = propertyDefinitions;

        foreach (var property in PropertiesAndTypes.Values)
        {
            if (property.IsPublicSet == false)
            {
                HasPrivateSetProperties = true;
                break;
            }
        }

        Constructors = constructors.OrderByDescending(x => x.PropertiesAndTypes.Count).ToList();
        ShortTypeName = NormalizeName(typeName);
        Namespace = fullNamespace;
    }

    public string TypeName { get; }

    public string ShortTypeName { get; }

    public string Namespace { get; }

    public bool HasPrivateSetProperties { get; }

    public bool HasProperties => PropertiesAndTypes.Count > 0;

    public string TypeNameWithNamespace
    {
        get
        {
            if (_typeNameWithNamespace != null)
            {
                return _typeNameWithNamespace;
            }

            if (!TypeName.Contains("."))
            {
                _typeNameWithNamespace = $"{Namespace}.{TypeName}";
                return _typeNameWithNamespace;
            }

            var index = TypeName.LastIndexOf('.');

            _typeNameWithNamespace = $"{Namespace}{TypeName.Substring(index)}";
            return _typeNameWithNamespace;
        }
    }

    public Dictionary<string, PropertyDefinition> PropertiesAndTypes { get; }

    public List<ConstructorDefinition> Constructors { get; }

    private static string NormalizeName(string name)
    {
        if (name.Contains("."))
        {
            var index = name.LastIndexOf('.');
            name = name.Substring(index + 1);
        }

        return name;
    }
}