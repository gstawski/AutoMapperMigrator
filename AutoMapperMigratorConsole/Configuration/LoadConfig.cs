using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using AutoMapperMigratorConsole.Model;

namespace AutoMapperMigratorConsole.Configuration;

public static class LoadConfig
{
    public static AppConfiguration ReadApplicationConfiguration()
    {
        var convertFunctions = new Dictionary<string, Function>();

        var data = File.ReadAllText("ConvertFunctionsConfiguration.xml");

        XmlSerializer serializer = new XmlSerializer(typeof(AppConfig));
        using (TextReader reader = new StringReader(data))
        {
            AppConfig result = (AppConfig)serializer.Deserialize(reader);

            if (result == null
                || result.FunctionsItems == null
                || result.FunctionsItems.Function == null
                || result.FunctionsItems.Function.Count == 0)
            {
                throw new Exception("Error while deserializing ConvertFunctionsConfiguration.xml");
            }

            var functions = result.FunctionsItems.Function;
            foreach (Function function in functions)
            {
                if (function.UseNameAsKey == 0)
                {
                    if (!function.FunctionBody.Contains($" {function.FunctionName}("))
                    {
                        throw new Exception($"Function {function.FunctionName} not found in body {function.FunctionBody}");
                    }

                    if (!function.FunctionBody.Contains($" {function.OutputTypeName} "))
                    {
                        throw new Exception($"Output type {function.OutputTypeName} not found in body {function.FunctionBody}");
                    }

                    if (!function.FunctionBody.Contains($"({function.InputTypeName} "))
                    {
                        throw new Exception($"Input type {function.InputTypeName} not found in body {function.FunctionBody}");
                    }
                }

                string key;

                if (function.UseNameAsKey == 1)
                {
                    key = function.FunctionName;
                }
                else
                {
                    key = $"{function.InputTypeName.ToLower()}-{function.OutputTypeName.ToLower()}";
                }

                if (!convertFunctions.TryAdd(key, function))
                {
                    throw new Exception($"Function duplicate {function.FunctionName} InputTypeName={function.InputTypeName} OutputTypeName={function.OutputTypeName} Body={function.FunctionBody}");
                }
            }

            var collectionTypes = new Dictionary<string, byte>();

            byte i = 0;
            foreach (var collectionType in result.CollectionsType.Names)
            {
                if (!collectionTypes.TryAdd(collectionType, i++))
                {
                    throw new Exception($"Collection type duplicate {collectionType}");
                }
            }

            return new AppConfiguration
            {
                UseFullNameSpace = result.UseFullNameSpaces,
                MapFunctionNamesPrefix = !string.IsNullOrEmpty(result.MapFunctionNamesPrefix) ? result.MapFunctionNamesPrefix : "Map",
                OutputPath = result.OutputDirectoryPath,
                OutputFileName = result.OutputFileName,
                MapperClassName = result.MapperClassName,
                DefaultNameSpaces = result.DefaultNameSpace?.NameSpaces,
                ConvertFunctions = convertFunctions,
                CollectionTypes = collectionTypes,
            };
        }
    }
}