using AdvancedDeckBuilder.Json;
using AdvancedDeckBuilder.Services;
using CliWrap;
using CliWrap.Buffered;
using CliWrap.EventStream;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using YGOHandAnalysisFramework.Data.Archive;
using YGOHandAnalysisFramework.Data.Extensions.Json;
using YGOHandAnalysisFramework.Data.Json;
using YGOHandAnalysisFramework.Features.Console;
using YGOHandAnalysisFramework.Features.Console.Json;

namespace AdvancedDeckBuilder.Models.Analysis;

public sealed class Analyzer : ReactiveObject
{
    private SourceCache<AnalyzerResults, DateTime> ResultsCache { get; }

    public Guid Id { get; } = Guid.NewGuid();

    public string Name
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public string SourcePath
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public int CardListFillSize
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool UseWeightedProbabilities
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool IncludeGoingSecond
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool UseCache
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = true;

    public ReadOnlyObservableCollection<int> AvailableCardListFillSizes { get; }

    private readonly ObservableAsPropertyHelper<string> _arguments;
    public string Arguments => _arguments.Value;

    public Analyzer()
    {
        ResultsCache = new SourceCache<AnalyzerResults, DateTime>(static results => results.RunTime);
        AvailableCardListFillSizes = new ReadOnlyObservableCollection<int>([.. Enumerable.Range(40, 21)]);
        this
            .WhenAnyValue(
                static vm => vm.CardListFillSize,
                static vm => vm.UseWeightedProbabilities,
                static vm => vm.IncludeGoingSecond,
                static vm => vm.UseCache,
                static (fillSize, useWeightedProbabilities, includedGoingSecond, useCache) =>
                {
                    var handSizes = new List<int> { 5 };

                    if(includedGoingSecond)
                    {
                        handSizes.Add(6);
                    }

                    return new ConsoleOptions
                    {
                        CardListFillSize = fillSize,
                        CreateWeightedProbabilities = useWeightedProbabilities,
                        HandSizes = handSizes,
                        UseCache = useCache,
                    };
                })
            .Select(static options => ConsoleConfiguration.CreateCommandLineArguments(options))
            .ToProperty(this, static vm => vm.Arguments, out _arguments);
    }

    public Analyzer(AnalyzerDTO analyzerDTO) : this()
    {
        Name = analyzerDTO.Name;
        SourcePath = analyzerDTO.SourcePath;

        if(ConsoleConfiguration.TryGetOptions(analyzerDTO.Arguments).GetResult(out var consoleOptions, out var _))
        {
            CardListFillSize = consoleOptions.CardListFillSize;
            UseWeightedProbabilities = consoleOptions.CreateWeightedProbabilities;
        }

        foreach(var result in analyzerDTO.Results)
        {
            var entry = new AnalyzerResults(result.Key);
            entry.AddResult(result.Value);
            AddEntry(entry);
        }
    }

    public IObservable<IChangeSet<AnalyzerResults, DateTime>> ConnectToResults()
    {
        return ResultsCache.Connect();
    }

    public void ClearResults(AnalyzerResults result)
    {
        ResultsCache.Remove(result);
    }

    public void AddEntry(AnalyzerResults entry)
    {
        ResultsCache.AddOrUpdate(entry);
    }

    public async Task RunAnalyzer(string workingDirectory, AnalyzerResults entry, IEnumerable<DeckDTO> decks, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileName = Path.GetFileNameWithoutExtension(SourcePath);

            if (string.IsNullOrEmpty(fileName))
            {
                entry.AddResult($"{nameof(SourcePath)} does not have a valid file name: {SourcePath}.");
                return;
            }

            var cacheLocation = Path.Combine(workingDirectory, fileName);

            if (!Directory.Exists(cacheLocation))
            {
                Directory.CreateDirectory(cacheLocation);
            }

            if (!ConsoleConfiguration.TryGetOptions(Arguments).GetResult(out var consoleOptions, out var consoleOptionsError))
            {
                foreach (var error in consoleOptionsError)
                {
                    entry.AddResult(error.Tag.ToString());
                }
                return;
            }

            consoleOptions.CacheLocation = cacheLocation;

            await RunAnalyzer(consoleOptions, entry, decks, cancellationToken);
        }
        catch (Exception ex)
        {
            var exception = ex;
            while (exception != null)
            {
                entry.AddResult(ex.Message);
                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    entry.AddResult(ex.StackTrace);
                }
                exception = exception.InnerException;
            }
        }
    }

    private async Task RunAnalyzer(ConsoleOptions consoleOptions, AnalyzerResults entry, IEnumerable<DeckDTO> decks, CancellationToken cancellationToken = default)
    {
        try
        {
            var tracker = App.Current?.Services?.GetService<IAnalyzerTracker>();

            if (tracker is null)
            {
                entry.AddResult($"Cannot load {nameof(IAnalyzerTracker)} service.");
                return;
            }

            await using var archiveStream = new MemoryStream();
            await Archive.WriteDecksToArchive(archiveStream, decks);

            var cmd = Cli.Wrap(SourcePath)
                .WithWorkingDirectory(Path.GetDirectoryName(SourcePath) ?? Environment.CurrentDirectory)
                .WithArguments(ConsoleConfiguration.CreateCommandLineArguments(consoleOptions))
                .WithStandardInputPipe(PipeSource.FromBytes(archiveStream.ToArray()));

            var stopwatch = Stopwatch.StartNew();

            int? processId = default;
            await foreach (var cmdEvent in cmd.ListenAsync(cancellationToken))
            {
                switch (cmdEvent)
                {
                    case StartedCommandEvent started:
                        processId = started.ProcessId;
                        tracker.AddProcess(started.ProcessId);
                        entry.AddResult($"Analyzer {Name} started in process {started.ProcessId}");
                        break;
                    case StandardOutputCommandEvent stdOut:
                        if (JsonExtensions.TryParseValue<MessageDTO>(stdOut.Text, out var message))
                        {
                            entry.AddResult(message.Message);
                        }
                        else
                        {
                            entry.AddResult(stdOut.Text);
                        }
                        break;
                    case StandardErrorCommandEvent error:
                        entry.AddResult(error.Text);
                        break;
                    case ExitedCommandEvent exited:
                        entry.AddResult($"Analyzer {Name} exited with code {exited.ExitCode}");
                        break;
                }
            }

            stopwatch.Stop();
            entry.AddResult($"Time to execute took {stopwatch.Elapsed.TotalMilliseconds:N2} ms / {stopwatch.Elapsed.TotalSeconds:N2} s.");

            if (processId.HasValue)
            {
                tracker.RemoveProcess(processId.Value);
            }
        }
        catch (Exception ex)
        {
            var exception = ex;
            while (exception != null)
            {
                entry.AddResult(ex.Message);
                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    entry.AddResult(ex.StackTrace);
                }
                exception = exception.InnerException;
            }
        }
    }
}
