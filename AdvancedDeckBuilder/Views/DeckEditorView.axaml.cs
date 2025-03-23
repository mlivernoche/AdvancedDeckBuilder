using AdvancedDeckBuilder.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace AdvancedDeckBuilder.Views;

public partial class DeckEditorView : ReactiveUserControl<DeckEditorViewModel>
{
    public DeckEditorView()
    {
        InitializeComponent();
    }
}