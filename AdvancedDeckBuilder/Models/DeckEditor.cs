using AdvancedDeckBuilder.Json;
using CardSourceGenerator;
using DynamicData;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using YGOHandAnalysisFramework;
using YGOHandAnalysisFramework.Data;
using YGOHandAnalysisFramework.Data.Json;

namespace AdvancedDeckBuilder.Models;

public sealed class DeckEditor : ReactiveObject
{
    private SourceCache<DeckEditorCardInDeckEntry, YGOCards.YGOCardName> MainDeck { get; }

    private readonly ReadOnlyObservableCollection<DeckEditorCardInDeckEntry> _cards;
    public ReadOnlyObservableCollection<DeckEditorCardInDeckEntry> Cards => _cards;

    public Guid Id { get; } = Guid.NewGuid();

    public string Name
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    private readonly ObservableAsPropertyHelper<int> _deckSize;
    public int DeckSize => _deckSize.Value;

    public DeckExtendedDTO InitializationDTO { get; } = new();

    public DeckEditor()
    {
        MainDeck = new SourceCache<DeckEditorCardInDeckEntry, YGOCards.YGOCardName>(static entry => entry.CardId);

        MainDeck
            .Connect()
            .AutoRefresh(static entry => entry.AmountOfCards)
            .Filter(static entry => entry.AmountOfCards > 0)
            .Bind(out _cards)
            .Subscribe();

        MainDeck
            .Connect()
            .AutoRefresh(static entry => entry.AmountOfCards)
            .ToCollection()
            .Select(static collection => collection.Sum(static entry => entry.AmountOfCards))
            .ToProperty(this, nameof(DeckSize), out _deckSize);
    }

    public DeckEditor(DeckExtendedDTO deck) : this()
    {
        InitializationDTO = deck;

        Name = deck.Name;
        foreach (var card in deck.CardList.Cards)
        {
            MainDeck.AddOrUpdate(new DeckEditorCardInDeckEntry(card));
        }
    }

    public IObservable<IChangeSet<DeckEditorCardInDeckEntry, YGOCards.YGOCardName>> ConnectToDeckEditor()
    {
        return MainDeck.Connect();
    }

    public void IncreaseAmountOfCard(YGOCards.YGOCardName cardName)
    {
        var lookup = MainDeck.Lookup(cardName);
        var entry = lookup.HasValue ? lookup.Value : new DeckEditorCardInDeckEntry(cardName);

        if (!lookup.HasValue)
        {
            MainDeck.AddOrUpdate(entry);
        }

        entry.IncreaseAmount();
    }

    public void DecreaseAmountOfCard(YGOCards.YGOCardName cardName)
    {
        var lookup = MainDeck.Lookup(cardName);

        if (lookup.HasValue)
        {
            lookup.Value.DecreaseAmount();
        }
    }
}
