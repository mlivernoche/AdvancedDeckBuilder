using CardSourceGenerator;
using DynamicData;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Fastenshtein;
using System.Diagnostics;
using DynamicData.Binding;
using AdvancedDeckBuilder.CardData;

namespace AdvancedDeckBuilder.ViewModels.Search;

public sealed class CardSearchViewModel : ViewModelBase
{
    record CardNameWrapper(YGOCards.YGOCardName CardName);

    public string SearchTerm
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    private readonly ReadOnlyObservableCollection<CardSearchResultViewModel> _searchResults;
    public ReadOnlyObservableCollection<CardSearchResultViewModel> SearchResults => _searchResults;

    private SourceCache<CardNameWrapper, YGOCards.YGOCardName> AllCardNames { get; }

    public ReactiveCommand<CardSearchResultViewModel, CardSearchResultViewModel> SearchResultSelected { get; }

    private int CardNameDemarcation { get; } = 3;

    public CardSearchViewModel()
    {
        AllCardNames = new SourceCache<CardNameWrapper, YGOCards.YGOCardName>(static wrapper => wrapper.CardName);
        AllCardNames.AddOrUpdate(CardDataLoader.LoadCardData().Where(static card => card.StartingLocation == YGOCards.StartingDeckLocation.MainDeck).Select(static card => new CardNameWrapper(card.Name)).ToList());
        SearchResultSelected = ReactiveCommand.Create<CardSearchResultViewModel, CardSearchResultViewModel>(static result => result);

        var searchTermChanged = this
            .WhenAnyValue(static viewModel => viewModel.SearchTerm)
            .Throttle(TimeSpan.FromMilliseconds(100));

        var beforeDemarcationStream = AllCardNames
            .Connect()
            .Filter(searchTermChanged.Select(BuildSearchFilterForBeforeDemarcation));

        var afterDemarcationStream = AllCardNames
            .Connect()
            .Filter(searchTermChanged.Select(BuildSearchFilterForAfterDemarcation));

        beforeDemarcationStream
            .Merge(afterDemarcationStream)
            .Transform(cardName => new CardSearchResultViewModel(SearchResultSelected)
            {
                CardId = cardName.CardName,
            })
            .Sort(searchTermChanged.Select(BuildSearchComparer))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _searchResults)
            .Subscribe();
    }

    private Func<CardNameWrapper, bool> BuildSearchFilterForBeforeDemarcation(string searchText)
    {
        if(string.IsNullOrEmpty(searchText) || searchText.Length > CardNameDemarcation)
        {
            return _ => false;
        }

        return card =>
        {
            if (card.CardName.Name.Length > CardNameDemarcation)
            {
                return false;
            }

            return card.CardName.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase);
        };
    }

    private Func<CardNameWrapper, bool> BuildSearchFilterForAfterDemarcation(string searchText)
    {
        if (string.IsNullOrEmpty(searchText) || searchText.Length < CardNameDemarcation)
        {
            return _ => false;
        }

        return card =>
        {
            if (card.CardName.Name.Length < CardNameDemarcation)
            {
                return false;
            }

            return card.CardName.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase);
        };
    }

    private IComparer<CardSearchResultViewModel> BuildSearchComparer(string searchText)
    {
        return new SearchComparer(searchText);
    }

    private record CardNameComparedToSearchTerm(string CardName, int Compared);

    private class SearchComparer : IComparer<CardSearchResultViewModel>
    {
        public string OriginalString { get; }
        public Levenshtein MeasuringTool { get; }

        public SearchComparer(string searchText)
        {
            OriginalString = searchText;
            MeasuringTool = new Levenshtein(searchText);
        }

        public int Compare(CardSearchResultViewModel? x, CardSearchResultViewModel? y)
        {
            var nameX = x?.CardName ?? string.Empty;
            var nameY = y?.CardName ?? string.Empty;

            if (string.IsNullOrEmpty(OriginalString))
            {
                return nameX.CompareTo(nameY);
            }

            var distanceX = MeasuringTool.DistanceFrom(nameX);
            var distanceY = MeasuringTool.DistanceFrom(nameY);

            return distanceX.CompareTo(distanceY);
        }
    }
}
