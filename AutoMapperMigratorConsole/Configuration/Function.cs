using System.Xml.Serialization;

namespace AutoMapperMigratorConsole.Configuration;

[XmlRoot(ElementName = "Function")]
public class Function
{
    [XmlElement(ElementName="FunctionName")]
    public string FunctionName { get; set; }

    [XmlElement(ElementName="InputTypeName")]
    public string InputTypeName { get; set; }

    [XmlElement(ElementName="OutputTypeName")]
    public string OutputTypeName { get; set; }

    [XmlElement(ElementName="FunctionNameIsKey")]
    public int UseNameAsKey { get; set; }

    [XmlElement(ElementName="FunctionBody")]
    public string FunctionBody { get; set; }
}