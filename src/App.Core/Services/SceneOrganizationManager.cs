using System;
using System.Collections.Generic;
using System.Linq;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Fade-In Feature: Scene Organization and Navigator
    /// 
    /// Features:
    /// - Create nested sequences (scenes within sequences)
    /// - Group related scenes
    /// - Color-coding system (by scene, sequence, plot points, character arcs, themes)
    /// - Drag-and-drop reordering
    /// - Visual scene organization
    /// - Create index cards for scenes
    /// </summary>
    public class SceneOrganizationManager
    {
        public enum ColoringStrategy
        {
            ByScene,
            BySequence,
            ByPlotPoint,
            ByCharacterArc,
            ByTheme,
            ByCustom
        }

        public class SceneNode
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public int SceneIndex { get; set; }
            public string SceneHeading { get; set; } = string.Empty;
            public string Synopsis { get; set; } = string.Empty;
            public string Color { get; set; } = "#FFFFFF";
            public List<string> Tags { get; set; } = new();
            public List<string> CharactersInScene { get; set; } = new();
            public int PageStart { get; set; }
            public int PageEnd { get; set; }
            public int LineCount { get; set; }
            public double EstimatedDuration { get; set; }
            public string PlotPoint { get; set; } = string.Empty;
            public List<string> Themes { get; set; } = new();
            public string CharacterArc { get; set; } = string.Empty;
            public List<string> Notes { get; set; } = new();
            public bool IsOmitted { get; set; }
            public bool IsLocked { get; set; }
        }

        public class SequenceNode
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public List<SceneNode> Scenes { get; set; } = new();
            public List<SequenceNode> NestedSequences { get; set; } = new();
            public int Level { get; set; }  // 0 = act, 1 = sequence, 2+ = nested
            public string Color { get; set; } = "#FFFFFF";
            public bool IsExpanded { get; set; } = true;
        }

        public class SceneNavigationStructure
        {
            public List<SequenceNode> Acts { get; set; } = new();
            public Dictionary<string, SceneNode> SceneIndex { get; set; } = new();
            public Dictionary<string, SequenceNode> SequenceIndex { get; set; } = new();
        }

        private SceneNavigationStructure _structure = new();
        private ColoringStrategy _currentStrategy = ColoringStrategy.BySequence;
        private Dictionary<string, string> _colorPalette = new();

        public SceneOrganizationManager()
        {
            InitializeColorPalette();
        }

        /// <summary>
        /// Initialize color palette for different coloring strategies
        /// </summary>
        private void InitializeColorPalette()
        {
            _colorPalette = new Dictionary<string, string>
            {
                { "act1", "#FFE0E0" },        // Light red for Act 1
                { "act2a", "#E0F0FF" },      // Light blue for Act 2a
                { "act2b", "#E0FFE0" },      // Light green for Act 2b
                { "act3", "#FFFFE0" },       // Light yellow for Act 3
                { "plotpoint1", "#FFB6C1" }, // Pink for major plot point
                { "midpoint", "#87CEEB" },   // Sky blue for midpoint
                { "plotpoint2", "#FFD700" }, // Gold for second plot point
                { "climax", "#FF4500" },     // Orange red for climax
                { "resolution", "#90EE90" }  // Light green for resolution
            };
        }

        /// <summary>
        /// Build scene organization from script elements
        /// </summary>
        public void BuildStructure(List<ScriptElement> elements, List<Scene> scenes)
        {
            _structure = new SceneNavigationStructure();
            var sceneNodes = new List<SceneNode>();

            // Create scene nodes
            foreach (var scene in scenes)
            {
                var node = new SceneNode
                {
                    SceneIndex = scenes.IndexOf(scene),
                    SceneHeading = scene.Title,
                    Synopsis = scene.Synopsis,
                    PageStart = scene.PageStart,
                    PageEnd = scene.PageEnd,
                    LineCount = scene.ElementsCount,
                    EstimatedDuration = (scene.PageEnd - scene.PageStart + 1) * 1.0,  // ~1 minute per page
                    CharactersInScene = ExtractCharactersFromScene(elements, scene)
                };
                sceneNodes.Add(node);
                _structure.SceneIndex[node.Id] = node;
            }

            // Organize into acts (3-act structure)
            var actCount = 3;
            var scenesPerAct = (int)Math.Ceiling((double)sceneNodes.Count / actCount);

            for (int act = 0; act < actCount; act++)
            {
                var actStart = act * scenesPerAct;
                var actEnd = Math.Min(actStart + scenesPerAct, sceneNodes.Count);
                
                var actSequence = new SequenceNode
                {
                    Title = $"Act {act + 1}",
                    Level = 0,
                    Color = _colorPalette[$"act{act + 1}"]
                };

                for (int i = actStart; i < actEnd; i++)
                {
                    actSequence.Scenes.Add(sceneNodes[i]);
                }

                _structure.Acts.Add(actSequence);
                _structure.SequenceIndex[actSequence.Id] = actSequence;
            }
        }

        /// <summary>
        /// Create a nested sequence within an act
        /// </summary>
        public SequenceNode CreateSequence(string actId, string sequenceTitle, string description, int level = 1)
        {
            var sequence = new SequenceNode
            {
                Title = sequenceTitle,
                Description = description,
                Level = level,
                Color = "#F0F0F0"
            };

            if (_structure.SequenceIndex.ContainsKey(actId))
            {
                var act = _structure.SequenceIndex[actId];
                act.NestedSequences.Add(sequence);
                _structure.SequenceIndex[sequence.Id] = sequence;
                return sequence;
            }

            return sequence;
        }

        /// <summary>
        /// Add scene to a sequence
        /// </summary>
        public void AddSceneToSequence(string sequenceId, string sceneId)
        {
            if (!_structure.SequenceIndex.ContainsKey(sequenceId) || !_structure.SceneIndex.ContainsKey(sceneId))
                return;

            var sequence = _structure.SequenceIndex[sequenceId];
            var scene = _structure.SceneIndex[sceneId];

            // Remove from current parent if needed
            foreach (var act in _structure.Acts)
            {
                act.Scenes.RemoveAll(s => s.Id == sceneId);
                foreach (var subSeq in act.NestedSequences)
                {
                    subSeq.Scenes.RemoveAll(s => s.Id == sceneId);
                }
            }

            sequence.Scenes.Add(scene);
        }

        /// <summary>
        /// Apply color coding based on strategy
        /// </summary>
        public void ApplyColorCoding(ColoringStrategy strategy)
        {
            _currentStrategy = strategy;

            foreach (var act in _structure.Acts)
            {
                ApplyColorToSequence(act, strategy);
            }
        }

        /// <summary>
        /// Apply color to sequence and its scenes
        /// </summary>
        private void ApplyColorToSequence(SequenceNode sequence, ColoringStrategy strategy)
        {
            switch (strategy)
            {
                case ColoringStrategy.BySequence:
                    foreach (var scene in sequence.Scenes)
                    {
                        scene.Color = sequence.Color;
                    }
                    break;

                case ColoringStrategy.ByScene:
                    // Each scene gets unique color
                    var colors = new[] { "#FFE0E0", "#E0F0FF", "#E0FFE0", "#FFFFE0", "#FFE0F0", "#E0FFFF" };
                    for (int i = 0; i < sequence.Scenes.Count; i++)
                    {
                        sequence.Scenes[i].Color = colors[i % colors.Length];
                    }
                    break;

                case ColoringStrategy.ByPlotPoint:
                    foreach (var scene in sequence.Scenes)
                    {
                        if (!string.IsNullOrEmpty(scene.PlotPoint) && _colorPalette.ContainsKey(scene.PlotPoint.ToLower()))
                        {
                            scene.Color = _colorPalette[scene.PlotPoint.ToLower()];
                        }
                    }
                    break;

                case ColoringStrategy.ByCharacterArc:
                    // Color by primary character development
                    var arcColors = new Dictionary<string, string>
                    {
                        { "setup", "#FFE0E0" },
                        { "development", "#E0F0FF" },
                        { "climax", "#FFD700" },
                        { "resolution", "#90EE90" }
                    };
                    foreach (var scene in sequence.Scenes)
                    {
                        if (!string.IsNullOrEmpty(scene.CharacterArc) && arcColors.ContainsKey(scene.CharacterArc.ToLower()))
                        {
                            scene.Color = arcColors[scene.CharacterArc.ToLower()];
                        }
                    }
                    break;

                case ColoringStrategy.ByTheme:
                    // Color by primary theme
                    var themeColors = new Dictionary<string, string>
                    {
                        { "love", "#FFB6C1" },
                        { "betrayal", "#FF6347" },
                        { "redemption", "#90EE90" },
                        { "sacrifice", "#FFD700" },
                        { "courage", "#4169E1" },
                        { "loss", "#808080" }
                    };
                    foreach (var scene in sequence.Scenes)
                    {
                        if (scene.Themes.Count > 0)
                        {
                            var primaryTheme = scene.Themes[0].ToLower();
                            if (themeColors.ContainsKey(primaryTheme))
                                scene.Color = themeColors[primaryTheme];
                        }
                    }
                    break;
            }

            // Recursively apply to nested sequences
            foreach (var nestedSeq in sequence.NestedSequences)
            {
                ApplyColorToSequence(nestedSeq, strategy);
            }
        }

        /// <summary>
        /// Reorder scenes (drag and drop)
        /// </summary>
        public bool ReorderScenes(string sequenceId, string sceneId, int newPosition)
        {
            if (!_structure.SequenceIndex.ContainsKey(sequenceId) || !_structure.SceneIndex.ContainsKey(sceneId))
                return false;

            var sequence = _structure.SequenceIndex[sequenceId];
            var scene = sequence.Scenes.FirstOrDefault(s => s.Id == sceneId);
            
            if (scene == null) return false;

            sequence.Scenes.Remove(scene);
            if (newPosition < 0) newPosition = 0;
            if (newPosition > sequence.Scenes.Count) newPosition = sequence.Scenes.Count;
            
            sequence.Scenes.Insert(newPosition, scene);
            return true;
        }

        /// <summary>
        /// Mark scene as omitted (not deleted, but skipped)
        /// </summary>
        public void OmitScene(string sceneId)
        {
            if (_structure.SceneIndex.ContainsKey(sceneId))
            {
                _structure.SceneIndex[sceneId].IsOmitted = true;
            }
        }

        /// <summary>
        /// Lock scene to prevent changes
        /// </summary>
        public void LockScene(string sceneId)
        {
            if (_structure.SceneIndex.ContainsKey(sceneId))
            {
                _structure.SceneIndex[sceneId].IsLocked = true;
            }
        }

        /// <summary>
        /// Add tag to scene
        /// </summary>
        public void TagScene(string sceneId, string tag)
        {
            if (_structure.SceneIndex.ContainsKey(sceneId))
            {
                var scene = _structure.SceneIndex[sceneId];
                if (!scene.Tags.Contains(tag))
                    scene.Tags.Add(tag);
            }
        }

        /// <summary>
        /// Set plot point for scene
        /// </summary>
        public void SetPlotPoint(string sceneId, string plotPoint)
        {
            if (_structure.SceneIndex.ContainsKey(sceneId))
            {
                _structure.SceneIndex[sceneId].PlotPoint = plotPoint;
            }
        }

        /// <summary>
        /// Add theme to scene
        /// </summary>
        public void AddThemeToScene(string sceneId, string theme)
        {
            if (_structure.SceneIndex.ContainsKey(sceneId))
            {
                var scene = _structure.SceneIndex[sceneId];
                if (!scene.Themes.Contains(theme))
                    scene.Themes.Add(theme);
            }
        }

        /// <summary>
        /// Get scene navigator structure
        /// </summary>
        public SceneNavigationStructure GetStructure()
        {
            return _structure;
        }

        /// <summary>
        /// Generate outline/table of contents
        /// </summary>
        public string GenerateOutline()
        {
            var outline = new System.Text.StringBuilder();
            outline.AppendLine("=== SCRIPT OUTLINE ===\n");

            foreach (var act in _structure.Acts)
            {
                outline.AppendLine($"{act.Title}");
                outline.AppendLine(new string('-', act.Title.Length));
                
                foreach (var scene in act.Scenes)
                {
                    if (!scene.IsOmitted)
                    {
                        outline.AppendLine($"  {scene.SceneHeading}");
                        if (!string.IsNullOrEmpty(scene.Synopsis))
                            outline.AppendLine($"    {scene.Synopsis}");
                        outline.AppendLine($"    Pages: {scene.PageStart}-{scene.PageEnd}");
                    }
                }

                foreach (var sequence in act.NestedSequences)
                {
                    outline.AppendLine($"\n  {sequence.Title}");
                    foreach (var scene in sequence.Scenes)
                    {
                        if (!scene.IsOmitted)
                            outline.AppendLine($"    {scene.SceneHeading}");
                    }
                }

                outline.AppendLine();
            }

            return outline.ToString();
        }

        /// <summary>
        /// Extract characters from scene
        /// </summary>
        private List<string> ExtractCharactersFromScene(List<ScriptElement> elements, Scene scene)
        {
            var characters = new HashSet<string>();
            // Implementation would extract character names from scene elements
            return characters.ToList();
        }

        /// <summary>
        /// Get total page count
        /// </summary>
        public int GetTotalPageCount()
        {
            int maxPage = 1;
            foreach (var scene in _structure.SceneIndex.Values)
            {
                if (scene.PageEnd > maxPage)
                    maxPage = scene.PageEnd;
            }
            return maxPage;
        }

        /// <summary>
        /// Get structure statistics
        /// </summary>
        public string GetStatistics()
        {
            var stats = new System.Text.StringBuilder();
            var totalScenes = _structure.SceneIndex.Count;
            var omittedScenes = _structure.SceneIndex.Values.Count(s => s.IsOmitted);
            var activeScenes = totalScenes - omittedScenes;
            var totalPages = GetTotalPageCount();
            var estimatedMinutes = _structure.SceneIndex.Values.Sum(s => s.EstimatedDuration);

            stats.AppendLine("=== SCENE STRUCTURE STATISTICS ===");
            stats.AppendLine($"Total Scenes: {totalScenes}");
            stats.AppendLine($"Active Scenes: {activeScenes}");
            stats.AppendLine($"Omitted Scenes: {omittedScenes}");
            stats.AppendLine($"Total Pages: {totalPages}");
            stats.AppendLine($"Estimated Duration: {estimatedMinutes:F0} minutes (~{Math.Round(estimatedMinutes / 60.0)} hours)");
            stats.AppendLine($"Current Coloring Strategy: {_currentStrategy}");

            return stats.ToString();
        }
    }
}
