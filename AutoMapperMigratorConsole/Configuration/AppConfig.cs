using System.Xml.Serialization;

namespace AutoMapperMigratorConsole.Configuration;

[XmlRoot(ElementName = "AplicationConfiguration")]
public class AppConfig
{
    [XmlElement(ElementName = "OutputDirectoryPath")]
    public string OutputDirectoryPath { get; set; }

    [XmlElement(ElementName = "OutputFileName")]
    public string OutputFileName { get; set; }

    [XmlElement(ElementName = "MapperClassName")]
    public string MapperClassName { get; set; }

    [XmlElement(ElementName = "MapFunctionNamesPrefix")]
    public string MapFunctionNamesPrefix { get; set; }

    [XmlElement(ElementName = "UseFullNameSpaces")]
    public bool UseFullNameSpaces { get; set; }

    [XmlElement(ElementName = "DefaultNameSpaces")]
    public DefaultNameSpaces DefaultNameSpace { get; set; }

    [XmlElement(ElementName = "Collections")]
    public CollectionsTypes CollectionsType { get; set; }

    [XmlElement(ElementName = "ConvertFunctions")]
    public ConvertFunctions FunctionsItems { get; set; }
}