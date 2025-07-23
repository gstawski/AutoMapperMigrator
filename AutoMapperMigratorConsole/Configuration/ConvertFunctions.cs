using System.Collections.Generic;
using System.Xml.Serialization;

namespace AutoMapperMigratorConsole.Configuration;

[XmlRoot(ElementName="ConvertFunctions")]
public sealed class ConvertFunctions
{
    [XmlElement(ElementName="Function")]
    public List<Function> Function { get; set; }
}