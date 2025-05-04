using System.Collections.Generic;
using AutoMapperMigratorConsole.Configuration;

namespace AutoMapperMigratorConsole.Model;

public class AppConfiguration
{
    public bool UseFullNameSpace { get; set; }
    public string OutputPath { get; set; }

    public string OutputFileName { get; set; }

    public string MapperClassName { get; set; }

    public string MapFunctionNamesPrefix { get; set; }

    public List<string> SearchClassPostfixes { get; set; }

    public List<string> DefaultNameSpaces { get; set; }

    public Dictionary<string,Function> ConvertFunctions { get; set; }

    public Dictionary<string,byte> CollectionTypes { get; set; }
}