using AdvancedDeckBuilder.Json;
using AdvancedDeckBuilder.Models;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace AdvancedDeckBuilder.Services;

public interface IFilesService
{
    Task<IStorageFile?> OpenFileAsync();
    Task<IStorageFile?> SaveFileAsync();
    Task<IStorageFile?> SaveFileAsync(FilePickerSaveOptions options);
    Task<FilePickerSaveOptions> GetFileOptions(string? saveLocation);
}
