using System.Collections.Generic;

namespace AutoMapperMigratorConsole.Model.WorkspaceModels;

public sealed class WorkspaceAutoMapper
{
    public WorkspaceProject Project { get; set; }

    public List<AutoMapperCreateMap> Mappings { get; set; }
}