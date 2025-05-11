using System.Collections.Generic;

namespace AutoMapperMigratorConsole.Model;

public class ClassMapDefinition
{
    public ClassDefinition SourceClass { get; }
    public ClassDefinition DestinationClass { get; }
    public ICollection<AutoMapperFieldInfo> FieldsMap { get; }

    public ClassMapDefinition(ClassDefinition sourceClass, ClassDefinition destinationClass, List<AutoMapperFieldInfo> fieldsMap)
    {
        SourceClass = sourceClass;
        DestinationClass = destinationClass;
        FieldsMap = fieldsMap;
    }
}