namespace App.Core.Models;

public class OutlineNode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? SceneId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public bool IsSection { get; set; }
    public List<OutlineNode> Children { get; set; } = new();
}