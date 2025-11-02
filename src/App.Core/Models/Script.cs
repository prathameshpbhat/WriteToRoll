namespace App.Core.Models;

public class Script
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string DraftVersion { get; set; } = "1.0";
    public string FilePath { get; set; } = string.Empty;
    public bool IsDirty { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    public List<Scene> Scenes { get; set; } = new();
    public List<Character> Characters { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}