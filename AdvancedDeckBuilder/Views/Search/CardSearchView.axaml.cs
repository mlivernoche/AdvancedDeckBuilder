using AdvancedDeckBuilder.ViewModels.Search;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace AdvancedDeckBuilder.Views.Search;

public partial class CardSearchView : ReactiveUserControl<CardSearchViewModel>
{
    public CardSearchView()
    {
        InitializeComponent();
    }
}