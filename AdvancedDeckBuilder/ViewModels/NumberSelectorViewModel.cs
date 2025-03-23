using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedDeckBuilder.ViewModels;

public class NumberSelectorViewModel : ViewModelBase
{
    private readonly ReadOnlyObservableCollection<int> _availableNumbers;
    public ReadOnlyObservableCollection<int> AvailableNumbers => _availableNumbers;

    public int SelectedNumber
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public NumberSelectorViewModel(IEnumerable<int> numbers)
    {
        _availableNumbers = new ReadOnlyObservableCollection<int>([.. numbers]);
        SelectedNumber = _availableNumbers.FirstOrDefault();
    }

    public NumberSelectorViewModel(ReadOnlyObservableCollection<int> numbers)
    {
        _availableNumbers = numbers;
        SelectedNumber = _availableNumbers.FirstOrDefault();
    }
}
