using System.Threading.Tasks;

namespace AutoMapperMigratorConsole.Interfaces;

public interface IAnalyzeSolutionService
{
    Task AnalyzeSolution(string solutionPath);
}