namespace App.Core.Models;

public class Scene
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public SceneHeading Heading { get; set; } = new();
    public int? Number { get; set; }
    public string Slugline { get; set; } = string.Empty;
    public List<ScriptElement> Content { get; set; } = new();
    public string Notes { get; set; } = string.Empty;
    public IndexCardData IndexCard { get; set; } = new();
}

public class SceneHeading
{
    public string Location { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public bool IsInterior { get; set; }
    
    public override string ToString()
    {
        var intExt = IsInterior ? "INT." : "EXT.";
        return $"{intExt} {Location} - {Time}".Trim();
    }
}

public class IndexCardData
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Color { get; set; } = "#FFFFFF";
    public double PositionX { get; set; }
    public double PositionY { get; set; }
    public List<string> Tags { get; set; } = new();
}