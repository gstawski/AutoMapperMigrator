using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using AutoMapperMigratorConsole.Model;

namespace AutoMapperMigratorConsole.Configuration;

public static class LoadConfig
{
    public static AppConfiguration ReadApplicationConfiguration()
    {
        var convertFunctions = new Dictionary<string, FunctionConfiguration>();

        var data = File.ReadAllText("ConvertFunctionsConfiguration.xml");

        XmlSerializer serializer = new XmlSerializer(typeof(AppConfig));
        using (TextReader reader = new StringReader(data))
        {
#pragma warning disable CA5369
            var result = serializer.Deserialize(reader) as AppConfig;
#pragma warning restore CA5369


            if (result == null
                || result.FunctionsItems == null
                || result.FunctionsItems.Function == null
                || result.FunctionsItems.Function.Count == 0)
            {
                throw new ArgumentException("Error while deserializing ConvertFunctionsConfiguration.xml");
            }

            var functions = result.FunctionsItems.Function;
            foreach (FunctionConfiguration function in functions)
            {
                if (function.UseNameAsKey == 0)
                {
                    if (!function.FunctionBody.Contains($" {function.OutputTypeName} ", StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"Output type {function.OutputTypeName} not found in body {function.FunctionBody}");
                    }

                    if (!function.FunctionBody.Contains($"({function.InputTypeName} ", StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"Input type {function.InputTypeName} not found in body {function.FunctionBody}");
                    }
                }

                string key;

                if (function.UseNameAsKey == 1)
                {
                    key = function.FunctionName;
                }
                else
                {
                    key = $"{function.InputTypeName.ToLower(CultureInfo.InvariantCulture)}-{function.OutputTypeName.ToLower(CultureInfo.InvariantCulture)}";
                }

                if (!convertFunctions.TryAdd(key, function))
                {
                    throw new ArgumentException($"Function duplicate {function.FunctionName} InputTypeName={function.InputTypeName} OutputTypeName={function.OutputTypeName} Body={function.FunctionBody}");
                }
            }

            var collectionTypes = new Dictionary<string, byte>();

            byte i = 0;
            foreach (var collectionType in result.CollectionsType.Names)
            {
                if (!collectionTypes.TryAdd(collectionType, i++))
                {
                    throw new ArgumentException($"Collection type duplicate {collectionType}");
                }
            }

            var classPostfixes = !string.IsNullOrEmpty(result.SearchClassPostfixes)
                ? result.SearchClassPostfixes.Split([','], StringSplitOptions.RemoveEmptyEntries).ToList()
                : new List<string>();

            return new AppConfiguration
            {
                UseFullNameSpace = result.UseFullNameSpaces,
                MapFunctionNamesPrefix = !string.IsNullOrEmpty(result.MapFunctionNamesPrefix) ? result.MapFunctionNamesPrefix : "Map",
                OutputPath = result.OutputDirectoryPath,
                OutputFileName = result.OutputFileName,
                MapperClassName = result.MapperClassName,
                DefaultNameSpaces = result.DefaultNameSpace.NameSpaces,
                SearchClassPostfixes = classPostfixes,
                ConvertFunctions = convertFunctions,
                CollectionTypes = collectionTypes,
            };
        }
    }
}