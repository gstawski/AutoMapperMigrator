using System.Collections.Generic;
using System.Xml.Serialization;

namespace AutoMapperMigratorConsole.Configuration;

[XmlRoot(ElementName = "DefaultNameSpaces")]
public class DefaultNameSpaces
{
    [XmlElement(ElementName="DefaultNameSpace")]
    public List<string> NameSpaces { get; set; }
}