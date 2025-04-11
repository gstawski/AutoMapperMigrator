using System.Collections.Generic;

namespace AutoMapperMigratorConsole.Model;

public class ClassMapDefinition
{
    public ClassDefinition SourceClass { get; set; }
    public ClassDefinition DestinationClass { get; set; }
    public ICollection<AutoMapperFieldInfo> FieldsMap { get; set; }
}