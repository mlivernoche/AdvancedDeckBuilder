using AdvancedDeckBuilder.Json;
using AdvancedDeckBuilder.Models;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AdvancedDeckBuilder.Services;

public class FilesService : IFilesService
{
    private readonly Window _target;

    public FilesService(Window target)
    {
        _target = target;
    }

    public async Task<IStorageFile?> OpenFileAsync()
    {
        var files = await _target.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Open File",
            AllowMultiple = false,
        });

        return files.Count >= 1 ? files[0] : null;
    }

    public async Task<IStorageFile?> SaveFileAsync()
    {
        return await _target.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = "Save File",
        });
    }

    public async Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions options)
    {
        return await _target.StorageProvider.SaveFilePickerAsync(options);
    }

    public async Task<FilePickerSaveOptions> GetFileOptions(string? saveLocation)
    {
        var fileName = Path.GetFileName(saveLocation);
        var directory = Path.GetDirectoryName(saveLocation) ?? Environment.CurrentDirectory;

        return new FilePickerSaveOptions()
        {
            Title = $"Saving {fileName} As...",
            DefaultExtension = "json",
            ShowOverwritePrompt = true,
            SuggestedFileName = fileName,
            SuggestedStartLocation = await _target.StorageProvider.TryGetFolderFromPathAsync(directory),
        };
    }
}
