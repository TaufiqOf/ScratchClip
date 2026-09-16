namespace ScratchClip.Models;

public class StorageItem
{
    public string FilePath { get; init; } = string.Empty;
    public string FullPath { get; init; } = string.Empty;

    public string? IconPath { get; init; }
}