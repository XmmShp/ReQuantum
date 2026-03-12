using System.ComponentModel.DataAnnotations;

namespace ReQuantum.MAUI.Persistence;

public class StorageEntry
{
    [Key]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
