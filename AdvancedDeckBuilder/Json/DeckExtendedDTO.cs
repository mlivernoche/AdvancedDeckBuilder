using System.Collections.Generic;
using System.Collections.Immutable;
using YGOHandAnalysisFramework.Data.Json;

namespace AdvancedDeckBuilder.Json;

public class DeckExtendedDTO : DeckDTO
{
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Categories { get; set; } = ImmutableDictionary<string, IReadOnlyList<string>>.Empty;
}
