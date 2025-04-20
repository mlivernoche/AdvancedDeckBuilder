using AdvancedDeckBuilder.Json;
using AdvancedDeckBuilder.Models;
using AdvancedDeckBuilder.Models.Analysis;
using AdvancedDeckBuilder.Services;
using AdvancedDeckBuilder.ViewModels.Search;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
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

namespace AdvancedDeckBuilder.ViewModels;

public sealed class LoadedProjectViewModel : ViewModelBase
{
    private Project Project { get; }

    public Guid Id => Project.Id;

    public string CacheLocation => Path.Combine(Environment.CurrentDirectory, "cache", Id.ToString());

    private readonly ReadOnlyObservableCollection<DeckEditorViewModel> _availableDeckEditors;
    public ReadOnlyObservableCollection<DeckEditorViewModel> AvailableDeckEditors => _availableDeckEditors;

    private readonly ObservableAsPropertyHelper<string> _name;
    public string Name
    {
        get => _name.Value;
        set => Project.Name = value;
    }

    public string? SaveLocation
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public DeckEditorViewModel? SelectedDeckEditor
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ReactiveCommand<CardSearchResultViewModel, Unit> AddCardToSelectedDeck { get; }
    public ReactiveCommand<Unit, Unit> CreateNewDeck { get; }
    public ReactiveCommand<Unit, Unit> CopyDeck { get; }
    public ReactiveCommand<Unit, Unit> DeleteSelectedDeck { get; }

    public ReactiveCommand<Unit, Unit> CreateNewAnalyzer { get; }
    public ReactiveCommand<Unit, Unit> DeleteSelectedAnalyzer { get; }

    private readonly ReadOnlyObservableCollection<AnalyzerEditorViewModel> _availableAnalyzers;
    public ReadOnlyObservableCollection<AnalyzerEditorViewModel> AvailableAnalyzers => _availableAnalyzers;

    public AnalyzerEditorViewModel? SelectedAnalyzer
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ReactiveCommand<Unit, Unit> RunSelectedAnalyzer { get; }
    private ReactiveCommand<Unit, Unit> RunSelectedAnalyzerWrapped { get; }
    public ReactiveCommand<Unit, Unit> CancelRunSelectedAnalyzer { get; }

    public CardSearchViewModel CardSearch { get; }

    public LoadedProjectViewModel(ProjectDTO project)
    {
        Project = new Project(project);

        _name = this
            .WhenAnyValue(static viewModel => viewModel.Project.Name)
            .ToProperty(this, nameof(Name), string.Empty);

        CreateNewDeck = ReactiveCommand.Create(CreateNewDeckImpl);
        CopyDeck = ReactiveCommand.Create(CopyDeckImpl, canExecute: this.WhenAnyValue(static vm => vm.SelectedDeckEditor).Select(static deck => deck != null));
        DeleteSelectedDeck = ReactiveCommand.Create(DeleteSelectedDeckImpl, this.WhenAnyValue(static viewModel => viewModel.SelectedDeckEditor).Select(CanDeleteSelectedDeckImpl));
        AddCardToSelectedDeck = ReactiveCommand.CreateFromTask<CardSearchResultViewModel>(AddCardToDeckEditor);

        Project
            .ConnectToDeckEditors()
            .Transform(static editor => new DeckEditorViewModel(editor))
            .Bind(out _availableDeckEditors)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe();
        SelectedDeckEditor = AvailableDeckEditors.FirstOrDefault();

        Project
            .ConnectToAnalyzers()
            .Transform(static analyzer => new AnalyzerEditorViewModel(analyzer))
            .Bind(out _availableAnalyzers)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe();
        SelectedAnalyzer = AvailableAnalyzers.FirstOrDefault();
        CreateNewAnalyzer = ReactiveCommand.Create(() =>
        {
            Project.AddNewAnalyzer();
            SelectedAnalyzer = AvailableAnalyzers.LastOrDefault();
        });
        DeleteSelectedAnalyzer = ReactiveCommand.Create(() =>
        {
            if (SelectedAnalyzer is AnalyzerEditorViewModel viewModel)
            {
                Project.RemoveAnalyzer(viewModel.Id);
            }
        }, this.WhenAnyValue(static viewModel => viewModel.SelectedAnalyzer).Select(CanDeleteSelectedAnalyzer));


        RunSelectedAnalyzerWrapped = ReactiveCommand.CreateFromTask(async cancellationToken =>
        {
            if (SelectedAnalyzer is AnalyzerEditorViewModel viewModel)
            {
                var decks = AvailableDeckEditors.Select(static deck => deck.GetDTO()).ToArray();
                await viewModel.RunAnalyzer(CacheLocation, decks, cancellationToken);
            }
        });
        RunSelectedAnalyzer = ReactiveCommand.CreateFromObservable(() => RunSelectedAnalyzerWrapped.Execute().TakeUntil(CancelRunSelectedAnalyzer));
        CancelRunSelectedAnalyzer = ReactiveCommand.Create(() => { }, canExecute: RunSelectedAnalyzer.IsExecuting);

        CardSearch = new CardSearchViewModel();
        CardSearch
            .SearchResultSelected
            .InvokeCommand(ReactiveCommand.CreateFromTask<CardSearchResultViewModel>(AddCardToDeckEditor));
    }

    public LoadedProjectViewModel() : this(new ProjectDTO())
    {
        Project.AddNewDeck();
        SelectedDeckEditor = AvailableDeckEditors.FirstOrDefault();

        Project.AddNewAnalyzer();
        SelectedAnalyzer = AvailableAnalyzers.FirstOrDefault();
    }

    private async Task AddCardToDeckEditor(CardSearchResultViewModel searchResult)
    {
        if (SelectedDeckEditor is DeckEditorViewModel viewModel)
        {
            await viewModel.IncreaseCardAmount.Execute(searchResult.CardId);
        }
    }

    private void CreateNewDeckImpl()
    {
        Project.AddNewDeck();

        SelectedDeckEditor ??= AvailableDeckEditors.LastOrDefault();
    }

    private void CopyDeckImpl()
    {
        if(SelectedDeckEditor is DeckEditorViewModel viewModel)
        {
            Project.CopyDeck(viewModel.GetDTO());
        }
    }

    private void DeleteSelectedDeckImpl()
    {
        if (SelectedDeckEditor is DeckEditorViewModel viewModel)
        {
            Project.RemoveDeck(viewModel.Id);
        }
    }

    private bool CanDeleteSelectedDeckImpl(DeckEditorViewModel? deckEditor)
    {
        return deckEditor != null && Project.HasDeck(deckEditor.Id);
    }

    private bool CanDeleteSelectedAnalyzer(AnalyzerEditorViewModel? analyzer)
    {
        return analyzer != null && Project.HasAnalyzer(analyzer.Id);
    }

    public ProjectDTO GetProjectDTO()
    {
        return new ProjectDTO
        {
            Name = Name,
            Id = Project.Id,
            Decks = AvailableDeckEditors.Select(static deck => deck.GetDTO()).ToArray(),
            Analyzers = AvailableAnalyzers.Select(static analyzer => analyzer.GetDTO()).ToArray(),
        };
    }
}
