using System.Collections.Generic;
using System.Xml.Serialization;

namespace AutoMapperMigratorConsole.Configuration;

[XmlRoot(ElementName="ConvertFunctions")]
public class ConvertFunctions
{
    [XmlElement(ElementName="Function")]
    public List<Function> Function { get; set; }
}