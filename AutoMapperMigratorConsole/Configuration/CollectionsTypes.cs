using System.Collections.Generic;
using System.Xml.Serialization;

namespace AutoMapperMigratorConsole.Configuration;

[XmlRoot(ElementName = "Collections")]
public class CollectionsTypes
{
    [XmlElement(ElementName="TypeName")]
    public List<string> Names { get; set; }
}