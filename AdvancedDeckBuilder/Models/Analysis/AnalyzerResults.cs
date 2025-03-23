using DynamicData;
using ReactiveUI;
using System;
using System.Reactive.Linq;
using System.Text;

namespace AdvancedDeckBuilder.Models.Analysis;

public record AnalyzerResultsEntry(int Id, string Content);

public class AnalyzerResults : ReactiveObject
{
    SourceCache<AnalyzerResultsEntry, int> Results { get; }

    public DateTime RunTime { get; }

    private readonly ObservableAsPropertyHelper<string> _content;
    public string Content => _content.Value;

    public AnalyzerResults(DateTime dateTime)
    {
        RunTime = dateTime;
        Results = new SourceCache<AnalyzerResultsEntry, int>(static entry => entry.Id);

        Results
            .Connect()
            .SortBy(static entry => entry.Id)
            .ToCollection()
            .Select(static entries =>
            {
                var sb = new StringBuilder();

                foreach (var entry in entries)
                {
                    sb.AppendLine(entry.Content);
                }

                return sb.ToString();
            })
            .ToProperty(this, static vm => vm.Content, out _content, initialValue: string.Empty);
    }

    public void AddResult(string content)
    {
        Results.AddOrUpdate(new AnalyzerResultsEntry(Results.Count + 1, content));
    }
}
