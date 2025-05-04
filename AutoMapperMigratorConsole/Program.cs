using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapperMigratorConsole.Configuration;
using AutoMapperMigratorConsole.Interfaces;
using AutoMapperMigratorConsole.Services;
using Microsoft.Build.Locator;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMapperMigratorConsole
{
    internal class Program
    {
        private static void RegisterLocator()
        {
            if (!MSBuildLocator.IsRegistered)
            {
                var instances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
                MSBuildLocator.RegisterInstance(instances.OrderByDescending(x => x.Version).First());
            }

            Console.WriteLine("MSBuildLocator.RegisterInstance done!");
        }

        private static void DisplayHelp()
        {
            Console.WriteLine("? - Display this help");
            Console.WriteLine(string.Empty);
            Console.WriteLine("Find all AutoMapper profiles in the solution and try generate mapping functions:");
            Console.WriteLine("AutoMapperMigratorConsole.exe c:\\path\\to\\project\\to\\analyze\\solution.sln");
            Console.WriteLine(string.Empty);
            Console.WriteLine("Generate mapping function between two any classes in solution:");
            Console.WriteLine("AutoMapperMigratorConsole.exe c:\\path\\to\\project\\to\\analyze\\solution.sln sourceClass destinationClass");
        }

        private static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Hello World!");

                if (args.Length == 0)
                {
                    Console.WriteLine("Please provide the solution path as an argument.");

                    DisplayHelp();

                    return;
                }

                if (args.Length == 1 && args[0].Contains("?"))
                {
                    DisplayHelp();
                    return;
                }

                var solutionPath = args[0];

                if (string.IsNullOrWhiteSpace(solutionPath))
                {
                    Console.WriteLine("Please provide the solution path as an argument.");
                    return;
                }

                RegisterLocator();

                var serviceCollection = new ServiceCollection();

                var conf = LoadConfig.ReadApplicationConfiguration();

                serviceCollection.AddSingleton(conf);

                serviceCollection.AddSingleton<IAnalyzeSolutionService, AnalyzeSolutionService>();
                serviceCollection.AddSingleton<ICodeTreeConvertToTypeService, CodeTreeConvertToTypeService>();
                serviceCollection.AddSingleton<ICodeTreeGeneratorService, CodeTreeGeneratorService>();

                var serviceProvider = serviceCollection.BuildServiceProvider();
                var analyzeSolutionService = serviceProvider.GetService<IAnalyzeSolutionService>();

                if (args.Length == 3)
                {
                    await analyzeSolutionService.AnalyzeForOneMap(solutionPath, args[1], args[2]);
                }
                else
                {
                    await analyzeSolutionService.AnalyzeSolution(solutionPath);
                }

                Console.WriteLine("\r\n\r\nAll done!");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}