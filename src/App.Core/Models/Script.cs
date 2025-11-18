using System;
using System.Collections.Generic;
using System.Linq;

namespace App.Core.Models
{
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
        public List<ScriptElement> Elements { get; set; } = new();
        public List<Scene> Scenes { get; set; } = new();
        public List<Character> Characters { get; set; } = new();
        public ScriptTitlePage TitlePage { get; set; } = new();
        public List<ScriptSection> Sections { get; set; } = new();
        public List<ScriptVersion> Versions { get; set; } = new();
        public List<IndexCard> IndexCards { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();

        public IEnumerable<SceneHeadingElement> GetSceneHeadings() => Elements.OfType<SceneHeadingElement>();
        public IEnumerable<DialogueElement> GetAllDialogue() => Elements.OfType<DialogueElement>();
        public int GetElementCount() => Elements.Count;
        public int GetEstimatedPageCount() => Math.Max(1, (int)Math.Ceiling(Elements.Sum(e => e.GetLineCount()) / 55.0));
        public int GetWordCount() => Elements.OfType<DialogueElement>().Sum(d => d.Text.Split().Length) + Elements.OfType<ActionElement>().Sum(a => a.Text.Split().Length);
        public void MarkModified() { IsDirty = true; ModifiedAt = DateTime.UtcNow; }
        public ScriptVersion CreateSnapshot(string label = "") => new() { Timestamp = DateTime.UtcNow, VersionLabel = label, ElementSnapshot = Elements.Select(e => e.Clone()).ToList(), Changes = new() { Description = label } };
    }

    public class ScriptTitlePage { public string Title { get; set; } = string.Empty; public string Credit { get; set; } = string.Empty; public string Author { get; set; } = string.Empty; public string Source { get; set; } = string.Empty; public DateTime? DraftDate { get; set; } public string Contact { get; set; } = string.Empty; public string CopyRight { get; set; } = string.Empty; }
    public class ScriptVersion { public Guid Id { get; set; } = Guid.NewGuid(); public DateTime Timestamp { get; set; } = DateTime.UtcNow; public string VersionLabel { get; set; } = string.Empty; public List<ScriptElement> ElementSnapshot { get; set; } = new(); public ScriptChangeSummary Changes { get; set; } = new(); }
    public class ScriptChangeSummary { public int ElementsAdded { get; set; } public int ElementsRemoved { get; set; } public int ElementsModified { get; set; } public string Description { get; set; } = string.Empty; }
    public class ScriptSection { public string Id { get; set; } = Guid.NewGuid().ToString(); public int Level { get; set; } public string Title { get; set; } = string.Empty; public int StartElementIndex { get; set; } public int EndElementIndex { get; set; } public string Synopsis { get; set; } = string.Empty; }
    public class IndexCard { public string Id { get; set; } = Guid.NewGuid().ToString(); public string SceneHeading { get; set; } = string.Empty; public string Synopsis { get; set; } = string.Empty; public string Notes { get; set; } = string.Empty; public List<string> Characters { get; set; } = new(); public int SceneNumber { get; set; } public string Color { get; set; } = "#FFFFFF"; public List<string> Tags { get; set; } = new(); }
    public class CharacterStatistics { public string Name { get; set; } = string.Empty; public int AppearanceCount { get; set; } public int DialogueLineCount { get; set; } public int TotalWordCount { get; set; } }
    
    // Legacy class for backward compatibility
    public class Character { public string Id { get; set; } = Guid.NewGuid().ToString(); public string Name { get; set; } = string.Empty; public int AppearanceCount { get; set; } public int DialogueCount { get; set; } public List<string> SceneNumbers { get; set; } = new(); }
}
