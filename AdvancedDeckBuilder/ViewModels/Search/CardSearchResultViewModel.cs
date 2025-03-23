using CardSourceGenerator;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedDeckBuilder.ViewModels.Search;

public sealed class CardSearchResultViewModel : ViewModelBase
{
    public YGOCards.YGOCardName CardId { get; init; }
    public string CardName => CardId.Name;

    public ReactiveCommand<CardSearchResultViewModel, CardSearchResultViewModel> ResultSelected { get; }

    public CardSearchResultViewModel(ReactiveCommand<CardSearchResultViewModel, CardSearchResultViewModel> notifySelection)
    {
        ResultSelected = ReactiveCommand.CreateFromTask<CardSearchResultViewModel, CardSearchResultViewModel>(async result => await notifySelection.Execute(result));
    }
}
