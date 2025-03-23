using System;
using YGOHandAnalysisFramework.Data.Json;

namespace AdvancedDeckBuilder.Json;

public class ProjectDTO
{
    public string Name { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public DeckExtendedDTO[] Decks { get; set; } = [];
    public AnalyzerDTO[] Analyzers { get; set; } = [];

    public ProjectDTO()
    {
        Id = Guid.NewGuid();
    }
}
