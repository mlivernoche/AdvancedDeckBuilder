using AdvancedDeckBuilder.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace AdvancedDeckBuilder.Views;

public partial class NumberSelectorView : ReactiveUserControl<NumberSelectorViewModel>
{
    public NumberSelectorView()
    {
        InitializeComponent();
    }
}