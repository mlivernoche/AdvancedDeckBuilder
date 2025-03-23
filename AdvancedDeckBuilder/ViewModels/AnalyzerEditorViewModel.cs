using AdvancedDeckBuilder.Json;
using AdvancedDeckBuilder.Models.Analysis;
using AdvancedDeckBuilder.Services;
using CommunityToolkit.Diagnostics;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YGOHandAnalysisFramework.Data.Json;

namespace AdvancedDeckBuilder.ViewModels;

public sealed class AnalyzerEditorViewModel : ViewModelBase
{
    private Analyzer Editor { get; }

    public Guid Id => Editor.Id;

    private readonly ObservableAsPropertyHelper<string> _sourcePath;
    public string SourcePath
    {
        get => _sourcePath.Value;
        set => Editor.SourcePath = value;
    }

    private readonly ObservableAsPropertyHelper<string> _name;
    public string Name
    {
        get => _name.Value;
        set => Editor.Name = value;
    }

    private readonly ReadOnlyObservableCollection<AnalyzerResults> _analyzerResults;
    public ReadOnlyObservableCollection<AnalyzerResults> AnalyzerResults => _analyzerResults;

    public AnalyzerResults? SelectedResults
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ReactiveCommand<Unit, Unit> UpdateSourcePath { get; }
    public ReactiveCommand<Unit, Unit> ClearResults { get; }

    public NumberSelectorViewModel CardListFillSizeSelector { get; }

    public bool UseWeightedProbabilities
    {
        get => Editor.UseWeightedProbabilities;
        set => Editor.UseWeightedProbabilities = value;
    }

    public bool IncludeGoingSecond
    {
        get => Editor.IncludeGoingSecond;
        set => Editor.IncludeGoingSecond = value;
    }

    public bool UseCache
    {
        get => Editor.UseCache;
        set => Editor.UseCache = value;
    }

    private readonly ObservableAsPropertyHelper<string> _arguments;
    public string Arguments => _arguments.Value;

    public AnalyzerEditorViewModel(Analyzer editor)
    {
        Editor = editor ?? throw new ArgumentNullException(nameof(editor));

        this
            .WhenAnyValue(static viewModel => viewModel.Editor.SourcePath)
            .ToProperty(this, nameof(SourcePath), out _sourcePath);

        this
            .WhenAnyValue(static viewModel => viewModel.Editor.Name)
            .ToProperty(this, nameof(Name), out _name);

        Editor
            .ConnectToResults()
            .SortBy(static result => result.RunTime)
            .Bind(out _analyzerResults)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe();
        SelectedResults = AnalyzerResults.LastOrDefault();

        UpdateSourcePath = ReactiveCommand.CreateFromTask(UpdateSourcePathImpl);
        ClearResults = ReactiveCommand.Create(() => Editor.ClearResults());

        CardListFillSizeSelector = new NumberSelectorViewModel(Editor.AvailableCardListFillSizes);
        CardListFillSizeSelector.SelectedNumber = Editor.CardListFillSize;
        this
            .WhenAnyValue(static vm => vm.CardListFillSizeSelector.SelectedNumber)
            .Subscribe(number => Editor.CardListFillSize = number);
        this
            .WhenAnyValue(static vm => vm.Editor.Arguments)
            .ToProperty(this, static vm => vm.Arguments, out _arguments);
    }

    private async Task UpdateSourcePathImpl()
    {
        var filesService = App.Current?.Services?.GetService<IFilesService>();
        Guard.IsNotNull(filesService);

        var file = await filesService.OpenFileAsync();
        if (file is null)
        {
            return;
        }

        SourcePath = file.Path.AbsolutePath;
    }

    public AnalyzerDTO GetDTO()
    {
        return new AnalyzerDTO
        {
            Name = Name,
            SourcePath = SourcePath,
            Arguments = Arguments,
            Results = AnalyzerResults.ToDictionary(static result => result.RunTime, static result => result.Content)
        };
    }

    public async Task RunAnalyzer(string workingDirectory, IEnumerable<DeckDTO> decks, CancellationToken cancellationToken = default)
    {
        var entry = new AnalyzerResults(DateTime.Now);
        Editor.AddEntry(entry);
        SelectedResults = entry;
        await Editor.RunAnalyzer(workingDirectory, entry, decks, cancellationToken);
    }
}
