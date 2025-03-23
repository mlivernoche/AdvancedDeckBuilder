using CardSourceGenerator;
using CommunityToolkit.Diagnostics;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using YGOHandAnalysisFramework.Data;
using YGOHandAnalysisFramework.Data.Json;

namespace AdvancedDeckBuilder.Models;

public sealed class DeckEditorCardInDeckEntry : ReactiveObject
{
    public YGOCards.YGOCardName CardId { get; }

    public string CardName => CardId.Name;

    public int AmountOfCards
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ReactiveCommand<Unit, Unit> AddCard { get; }
    public ReactiveCommand<Unit, Unit> RemoveCard { get; }

    public DeckEditorCardInDeckEntry(CardGroupDTO cardGroup)
    {
        Guard.IsTrue(YGOCards.AllCardNames.Contains(new(cardGroup.Name)));
        CardId = new(cardGroup.Name);

        AmountOfCards = cardGroup.Size;

        AddCard = ReactiveCommand.Create(IncreaseAmount);
        RemoveCard = ReactiveCommand.Create(DecreaseAmount);
    }

    public DeckEditorCardInDeckEntry(YGOCards.YGOCardName cardName)
    {
        Guard.IsTrue(YGOCards.AllCardNames.Contains(cardName));
        CardId = cardName;

        AddCard = ReactiveCommand.Create(IncreaseAmount);
        RemoveCard = ReactiveCommand.Create(DecreaseAmount);
    }

    public void IncreaseAmount()
    {
        AmountOfCards = Math.Max(0, Math.Min(3, AmountOfCards + 1));
    }

    public void DecreaseAmount()
    {
        AmountOfCards = Math.Max(0, AmountOfCards - 1);
    }
}
