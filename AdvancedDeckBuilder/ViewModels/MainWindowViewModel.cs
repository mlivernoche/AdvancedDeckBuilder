using AdvancedDeckBuilder.Json;
using AdvancedDeckBuilder.Models;
using AdvancedDeckBuilder.Services;
using AdvancedDeckBuilder.ViewModels.Search;
using CardSourceGenerator;
using CommunityToolkit.Diagnostics;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AdvancedDeckBuilder.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public LoadedProjectViewModel? LoadedProject
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public ReactiveCommand<Unit, Unit> NewProject { get; }
    public ReactiveCommand<Unit, Unit> OpenProject { get; }
    public ReactiveCommand<Unit, Unit> SaveAsProject { get; }

    public MainWindowViewModel()
    {
        LoadedProject = new LoadedProjectViewModel();

        NewProject = ReactiveCommand.Create(CreateNewProjectImpl);
        OpenProject = ReactiveCommand.CreateFromTask(OpenProjectsImpl);
        SaveAsProject = ReactiveCommand.CreateFromTask(SaveAsImpl);
    }

    private void CreateNewProjectImpl()
    {
        LoadedProject = new LoadedProjectViewModel();
    }

    private async Task OpenProjectsImpl()
    {
        var filesService = App.Current?.Services?.GetService<IFilesService>();
        Guard.IsNotNull(filesService);

        var file = await filesService.OpenFileAsync();
        if (file is null)
        {
            return;
        }

        await using var readStream = await file.OpenReadAsync();
        var savedProject = await JsonSerializer.DeserializeAsync<ProjectDTO>(readStream);

        if(savedProject is null)
        {
            return;
        }

        LoadedProject = new LoadedProjectViewModel(savedProject)
        {
            SaveLocation = file.Path.LocalPath,
        };
    }

    private async Task SaveAsImpl()
    {
        var loadedProject = LoadedProject;

        if(loadedProject is null)
        {
            return;
        }

        var projectDTO = loadedProject.GetProjectDTO();

        var filesService = App.Current?.Services?.GetService<IFilesService>();
        Guard.IsNotNull(filesService);

        var options = await filesService.GetFileOptions(loadedProject.SaveLocation);

        var file = await filesService.SaveFileAsync(options);
        if(file is null)
        {
            return;
        }

        loadedProject.SaveLocation = file.Path.LocalPath;
        await using var writeStream = await file.OpenWriteAsync();
        await JsonSerializer.SerializeAsync(writeStream, projectDTO);
    }
}
