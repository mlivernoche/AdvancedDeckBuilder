using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace AdvancedDeckBuilder.Json;

public class AnalyzerDTO
{
    public string Name { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string Arguments { get; set; } = string.Empty;
    public IReadOnlyDictionary<DateTime, string> Results { get; set; } = ImmutableDictionary<DateTime, string>.Empty;
}
