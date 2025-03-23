using AdvancedDeckBuilder.Json;
using AdvancedDeckBuilder.Models;
using CardSourceGenerator;
using DynamicData;
using DynamicData.PLinq;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using YGOHandAnalysisFramework.Data.Json;

namespace AdvancedDeckBuilder.ViewModels;

public class CardsGroupedByCategory : ViewModelBase
{
    private DeckEditorCategoryViewModel Key { get; }

    private readonly ReadOnlyObservableCollection<CardInDeckViewModel> _cards;
    public ReadOnlyObservableCollection<CardInDeckViewModel> Cards => _cards;

    private readonly ObservableAsPropertyHelper<string> _categoryName;
    public string CategoryName => _categoryName.Value;

    private readonly ObservableAsPropertyHelper<int> _cardsInCategory;
    public int CardsInCategory => _cardsInCategory.Value;

    public CardsGroupedByCategory(IGroup<CardInDeckViewModel, YGOCards.YGOCardName, DeckEditorCategoryViewModel> group)
    {
        Key = group.Key;

        this
            .WhenAnyValue(static viewModel => viewModel.Key.CategoryName)
            .ToProperty(this, static viewModel => viewModel.CategoryName, out _categoryName);

        group
            .Cache
            .Connect()
            .AutoRefresh(static card => card.AmountOfCards)
            .ToCollection()
            .Select(static collection => collection.Sum(static entry => entry.AmountOfCards))
            .ToProperty(this, static viewModel => viewModel.CardsInCategory, out _cardsInCategory);

        group
            .Cache
            .Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _cards)
            .Subscribe();
    }
}

public class DeckEditorCategoryViewModel : ViewModelBase
{
    public static DeckEditorCategoryViewModel EmptyCategory { get; } = new DeckEditorCategoryViewModel();

    public Guid Id { get; } = Guid.NewGuid();

    public string CategoryName
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;
}

public class CardInDeckViewModel : ViewModelBase
{
    private DeckEditorCardInDeckEntry Model { get; }

    public string CardName => Model.CardName;

    private readonly ObservableAsPropertyHelper<int> _amountOfCards;
    public int AmountOfCards => _amountOfCards.Value;

    public ReactiveCommand<Unit, Unit> AddCard => Model.AddCard;
    public ReactiveCommand<Unit, Unit> RemoveCard => Model.RemoveCard;

    private readonly ReadOnlyObservableCollection<DeckEditorCategoryViewModel> _categories;
    public ReadOnlyObservableCollection<DeckEditorCategoryViewModel> Categories => _categories;

    public DeckEditorCategoryViewModel SelectedCategory
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = DeckEditorCategoryViewModel.EmptyCategory;

    public CardInDeckViewModel(DeckEditorCardInDeckEntry model, IObservable<IChangeSet<DeckEditorCategoryViewModel, Guid>> categoriesSource)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));

        this
            .WhenAnyValue(static viewModel => viewModel.Model.AmountOfCards)
            .ToProperty(this, static viewModel => viewModel.AmountOfCards, out _amountOfCards);

        categoriesSource
            .AutoRefresh(static category => category.CategoryName)
            .SortBy(static category => category.CategoryName)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _categories)
            .Subscribe();
    }

    public CardGroupDTO GetDTO()
    {
        return new CardGroupDTO
        {
            Name = Model.CardName,
            Size = Model.AmountOfCards,
            Minimum = 0,
            Maximum = Model.AmountOfCards,
        };
    }
}

public sealed class DeckEditorViewModel : ViewModelBase
{
    private DeckEditor Editor { get; }

    private readonly ReadOnlyObservableCollection<CardsGroupedByCategory> _cardsByCategory;
    public ReadOnlyObservableCollection<CardsGroupedByCategory> CardsByCategory => _cardsByCategory;

    private readonly ObservableAsPropertyHelper<string> _name;
    public string Name
    {
        get => _name.Value;
        set => Editor.Name = value;
    }

    private readonly ObservableAsPropertyHelper<int> _deckSize;
    public int DeckSize => _deckSize.Value;

    public Guid Id => Editor.Id;

    public ReactiveCommand<YGOCards.YGOCardName, Unit> IncreaseCardAmount { get; }
    public ReactiveCommand<YGOCards.YGOCardName, Unit> DecreaseCardAmount { get; }

    private SourceCache<DeckEditorCategoryViewModel, Guid> CategoriesSource { get; }

    private readonly ReadOnlyObservableCollection<DeckEditorCategoryViewModel> _categories;
    public ReadOnlyObservableCollection<DeckEditorCategoryViewModel> Categories => _categories;

    public ReactiveCommand<Unit, Unit> AddNewCategory { get; }

    public DeckEditorViewModel(DeckEditor editor)
    {
        Editor = editor;
        CategoriesSource = new SourceCache<DeckEditorCategoryViewModel, Guid>(static viewModel => viewModel.Id);

        var cards = Editor
            .ConnectToDeckEditor()
            .AutoRefresh(static entry => entry.AmountOfCards)
            .Filter(static entry => entry.AmountOfCards > 0)
            .Transform(entry => new CardInDeckViewModel(entry, CategoriesSource.Connect()))
            .AutoRefresh(entry => entry.SelectedCategory)
            .Group(static card => card.SelectedCategory)
            .Transform(static group => new CardsGroupedByCategory(group))
            .AutoRefresh(static group => group.CategoryName)
            .SortBy(static group => group.CategoryName)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _cardsByCategory)
            .DisposeMany()
            .Subscribe();

        this
            .WhenAnyValue(static viewModel => viewModel.Editor.Name)
            .ToProperty(this, nameof(Name), out _name);

        this
            .WhenAnyValue(static viewModel => viewModel.Editor.DeckSize)
            .ToProperty(this, nameof(DeckSize), out _deckSize);

        IncreaseCardAmount = ReactiveCommand.Create<YGOCards.YGOCardName>(cardName => Editor.IncreaseAmountOfCard(cardName));
        DecreaseCardAmount = ReactiveCommand.Create<YGOCards.YGOCardName>(cardName => Editor.DecreaseAmountOfCard(cardName));

        CategoriesSource
            .Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _categories)
            .Subscribe();

        AddNewCategory = ReactiveCommand.Create(() => CategoriesSource.AddOrUpdate(new DeckEditorCategoryViewModel()));

        foreach(var (categoryName, cardsInCategory) in editor.InitializationDTO.Categories)
        {
            var category = new DeckEditorCategoryViewModel()
            {
                CategoryName = categoryName,
            };
            CategoriesSource.AddOrUpdate(category);

            foreach(var cardName in cardsInCategory)
            {
                foreach (var card in GetCards())
                {
                    if(card.CardName.Equals(cardName))
                    {
                        card.SelectedCategory = category;
                    }
                }
            }
        }
    }

    private List<CardInDeckViewModel> GetCards() => CardsByCategory.SelectMany(static category => category.Cards).ToList();

    public DeckExtendedDTO GetDTO()
    {
        var categories = new Dictionary<string, List<string>>();

        var cards = GetCards();

        foreach(var card in cards)
        {
            var categoryName = card.SelectedCategory.CategoryName;

            if(string.IsNullOrEmpty(categoryName))
            {
                continue;
            }

            if(!categories.TryGetValue(categoryName, out var list))
            {
                list = new List<string>();
                categories[categoryName] = list;
            }

            list.Add(card.CardName);
        }

        return new DeckExtendedDTO
        {
            Name = Name,
            CardList = new CardListDTO(cards.Select(static card => card.GetDTO())),
            Categories = categories.ToDictionary(static kv => kv.Key, static kv =>
            {
                IReadOnlyList<string> list = kv.Value;
                return list;
            }),
        };
    }
}
