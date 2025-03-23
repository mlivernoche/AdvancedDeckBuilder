using AdvancedDeckBuilder.Json;
using AdvancedDeckBuilder.Models.Analysis;
using AdvancedDeckBuilder.ViewModels;
using CardSourceGenerator;
using DynamicData;
using ReactiveUI;
using SharpCompress.Archives.Tar;
using SharpCompress.Common;
using SharpCompress.Writers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using YGOHandAnalysisFramework;
using YGOHandAnalysisFramework.Data;
using YGOHandAnalysisFramework.Data.Archive;
using YGOHandAnalysisFramework.Data.Json;

namespace AdvancedDeckBuilder.Models;

public sealed class Project : ReactiveObject
{
    private SourceCache<DeckEditor, Guid> DeckEditors { get; }
    private SourceCache<Analyzer, Guid> AnalyzerEditors { get; }

    public string Name
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public Guid Id { get; }

    public Project()
    {
        DeckEditors = new SourceCache<DeckEditor, Guid>(static editor => editor.Id);
        AnalyzerEditors = new SourceCache<Analyzer, Guid>(static editor => editor.Id);
        Id = Guid.NewGuid();
    }

    public Project(ProjectDTO projectDTO) : this()
    {
        Name = projectDTO.Name;
        Id = projectDTO.Id;

        foreach (var deck in projectDTO.Decks)
        {
            DeckEditors.AddOrUpdate(new DeckEditor(deck));
        }

        foreach(var analyzer in projectDTO.Analyzers)
        {
            AnalyzerEditors.AddOrUpdate(new Analyzer(analyzer));
        }
    }

    public IObservable<IChangeSet<DeckEditor, Guid>> ConnectToDeckEditors()
    {
        return DeckEditors.Connect();
    }

    public bool HasDeck(Guid id)
    {
        return DeckEditors.Lookup(id).HasValue;
    }

    public void RemoveDeck(Guid id)
    {
        DeckEditors.Remove(id);
    }

    public void RemoveDeck(DeckEditor deckEditor)
    {
        DeckEditors.Remove(deckEditor);
    }

    public void AddNewDeck()
    {
        var count = DeckEditors.Count + 1;
        var editor = new DeckEditor()
        {
            Name = $"Deck #{count:N0}",
        };
        DeckEditors.AddOrUpdate(editor);
    }

    public void CopyDeck(DeckExtendedDTO deckDTO)
    {
        var count = DeckEditors.Count + 1;
        deckDTO.Name = $"Deck #{count:N0}";
        DeckEditors.AddOrUpdate(new DeckEditor(deckDTO));
    }

    public IObservable<IChangeSet<Analyzer, Guid>> ConnectToAnalyzers()
    {
        return AnalyzerEditors.Connect();
    }

    public void AddNewAnalyzer()
    {
        var count = AnalyzerEditors.Count + 1;
        var analyzer = new Analyzer()
        {
            Name = $"Analyzer #{count:N0}",
        };
        AnalyzerEditors.AddOrUpdate(analyzer);
    }

    public bool HasAnalyzer(Guid id)
    {
        return AnalyzerEditors.Lookup(id).HasValue;
    }

    public void RemoveAnalyzer(Guid id)
    {
        AnalyzerEditors.Remove(id);
    }

    public void RemoveAnalyzer(Analyzer analyzer)
    {
        AnalyzerEditors.Remove(analyzer);
    }
}
