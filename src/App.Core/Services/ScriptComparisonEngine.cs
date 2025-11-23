using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Fade-In Feature: Script Comparison & Diff Engine
    /// 
    /// Features:
    /// - Compare different versions of scripts
    /// - Track changes between revisions
    /// - Diff viewing capabilities
    /// - Version history management
    /// - Generate change summaries
    /// </summary>
    public class ScriptComparisonEngine
    {
        public enum ChangeType
        {
            Added,
            Removed,
            Modified,
            Moved,
            Unchanged
        }

        public class ElementDiff
        {
            public string ElementId { get; set; } = string.Empty;
            public ChangeType Change { get; set; }
            public ScriptElementType ElementType { get; set; }
            public int OldPageNumber { get; set; }
            public int NewPageNumber { get; set; }
            public string OldContent { get; set; } = string.Empty;
            public string NewContent { get; set; } = string.Empty;
            public DateTime? ChangeTime { get; set; }
            public List<string> HighlightedDifferences { get; set; } = new();
        }

        public class ComparisonReport
        {
            public string ScriptTitle { get; set; } = string.Empty;
            public string OldVersion { get; set; } = string.Empty;
            public string NewVersion { get; set; } = string.Empty;
            public DateTime ComparisonDate { get; set; } = DateTime.UtcNow;
            public List<ElementDiff> ElementDiffs { get; set; } = new();
            public int TotalChanges { get; set; }
            public int AddedElements { get; set; }
            public int RemovedElements { get; set; }
            public int ModifiedElements { get; set; }
            public int MovedElements { get; set; }
            public double PageCountBefore { get; set; }
            public double PageCountAfter { get; set; }
            public int WordCountBefore { get; set; }
            public int WordCountAfter { get; set; }
        }

        public class SceneDiff
        {
            public string SceneHeading { get; set; } = string.Empty;
            public ChangeType Change { get; set; }
            public int OldSceneNumber { get; set; }
            public int NewSceneNumber { get; set; }
            public int LinesChanged { get; set; }
            public List<string> ChangedLines { get; set; } = new();
        }

        /// <summary>
        /// Compare two versions of a script
        /// </summary>
        public ComparisonReport CompareScripts(Script oldVersion, Script newVersion)
        {
            var report = new ComparisonReport
            {
                ScriptTitle = newVersion.Title,
                OldVersion = oldVersion.DraftVersion,
                NewVersion = newVersion.DraftVersion,
                PageCountBefore = oldVersion.GetEstimatedPageCount(),
                PageCountAfter = newVersion.GetEstimatedPageCount(),
                WordCountBefore = oldVersion.GetWordCount(),
                WordCountAfter = newVersion.GetWordCount()
            };

            // Compare elements
            var elementDiffs = CompareElements(oldVersion.Elements, newVersion.Elements);
            report.ElementDiffs = elementDiffs;

            // Calculate statistics
            report.TotalChanges = elementDiffs.Count;
            report.AddedElements = elementDiffs.Count(e => e.Change == ChangeType.Added);
            report.RemovedElements = elementDiffs.Count(e => e.Change == ChangeType.Removed);
            report.ModifiedElements = elementDiffs.Count(e => e.Change == ChangeType.Modified);
            report.MovedElements = elementDiffs.Count(e => e.Change == ChangeType.Moved);

            return report;
        }

        /// <summary>
        /// Compare element lists
        /// </summary>
        private List<ElementDiff> CompareElements(List<ScriptElement> oldElements, List<ScriptElement> newElements)
        {
            var diffs = new List<ElementDiff>();
            var processedNewIds = new HashSet<string>();

            // Find modified and moved elements
            foreach (var oldElement in oldElements)
            {
                var matchingNewElement = newElements.FirstOrDefault(e => e.Id == oldElement.Id);
                
                if (matchingNewElement == null)
                {
                    // Element was removed
                    diffs.Add(new ElementDiff
                    {
                        ElementId = oldElement.Id,
                        Change = ChangeType.Removed,
                        ElementType = oldElement.ElementType,
                        OldPageNumber = oldElement.PageNumber,
                        OldContent = oldElement.Text
                    });
                }
                else
                {
                    processedNewIds.Add(matchingNewElement.Id);

                    if (oldElement.Text != matchingNewElement.Text)
                    {
                        // Element was modified
                        var differences = HighlightTextDifferences(oldElement.Text, matchingNewElement.Text);
                        diffs.Add(new ElementDiff
                        {
                            ElementId = oldElement.Id,
                            Change = ChangeType.Modified,
                            ElementType = oldElement.ElementType,
                            OldPageNumber = oldElement.PageNumber,
                            NewPageNumber = matchingNewElement.PageNumber,
                            OldContent = oldElement.Text,
                            NewContent = matchingNewElement.Text,
                            HighlightedDifferences = differences
                        });
                    }
                    else if (oldElement.PageNumber != matchingNewElement.PageNumber)
                    {
                        // Element moved to different page
                        diffs.Add(new ElementDiff
                        {
                            ElementId = oldElement.Id,
                            Change = ChangeType.Moved,
                            ElementType = oldElement.ElementType,
                            OldPageNumber = oldElement.PageNumber,
                            NewPageNumber = matchingNewElement.PageNumber,
                            OldContent = oldElement.Text
                        });
                    }
                }
            }

            // Find added elements
            foreach (var newElement in newElements)
            {
                if (!processedNewIds.Contains(newElement.Id))
                {
                    diffs.Add(new ElementDiff
                    {
                        ElementId = newElement.Id,
                        Change = ChangeType.Added,
                        ElementType = newElement.ElementType,
                        NewPageNumber = newElement.PageNumber,
                        NewContent = newElement.Text
                    });
                }
            }

            return diffs;
        }

        /// <summary>
        /// Highlight text differences using word-level diff
        /// </summary>
        private List<string> HighlightTextDifferences(string oldText, string newText)
        {
            var differences = new List<string>();
            var oldWords = oldText.Split(' ');
            var newWords = newText.Split(' ');

            // Simple word-level diff
            var oldSet = new HashSet<string>(oldWords);
            var newSet = new HashSet<string>(newWords);

            var added = newSet.Except(oldSet).ToList();
            var removed = oldSet.Except(newSet).ToList();

            foreach (var word in added)
                differences.Add($"+ {word}");
            
            foreach (var word in removed)
                differences.Add($"- {word}");

            return differences;
        }

        /// <summary>
        /// Compare scenes between two versions
        /// </summary>
        public List<SceneDiff> CompareScenes(List<Scene> oldScenes, List<Scene> newScenes)
        {
            var sceneDiffs = new List<SceneDiff>();
            var processedNewIds = new HashSet<string>();

            foreach (var oldScene in oldScenes)
            {
                var matchingNewScene = newScenes.FirstOrDefault(s => s.Slugline == oldScene.Slugline);
                
                if (matchingNewScene == null)
                {
                    sceneDiffs.Add(new SceneDiff
                    {
                        SceneHeading = oldScene.Slugline,
                        Change = ChangeType.Removed,
                        OldSceneNumber = oldScenes.IndexOf(oldScene) + 1
                    });
                }
                else
                {
                    processedNewIds.Add(matchingNewScene.Id.ToString());

                    var oldLineCount = oldScene.Content?.Count ?? 0;
                    var newLineCount = matchingNewScene.Content?.Count ?? 0;

                    if (oldLineCount != newLineCount)
                    {
                        sceneDiffs.Add(new SceneDiff
                        {
                            SceneHeading = oldScene.Slugline,
                            Change = ChangeType.Modified,
                            OldSceneNumber = oldScenes.IndexOf(oldScene) + 1,
                            NewSceneNumber = newScenes.IndexOf(matchingNewScene) + 1,
                            LinesChanged = Math.Abs(newLineCount - oldLineCount)
                        });
                    }
                }
            }

            // Find added scenes
            foreach (var newScene in newScenes)
            {
                if (!processedNewIds.Contains(newScene.Id.ToString()))
                {
                    sceneDiffs.Add(new SceneDiff
                    {
                        SceneHeading = newScene.Slugline,
                        Change = ChangeType.Added,
                        NewSceneNumber = newScenes.IndexOf(newScene) + 1
                    });
                }
            }

            return sceneDiffs;
        }

        /// <summary>
        /// Generate formatted comparison report
        /// </summary>
        public string GenerateComparisonReport(ComparisonReport report)
        {
            var sb = new StringBuilder();

            sb.AppendLine("╔════════════════════════════════════════════════════════════════════════════════╗");
            sb.AppendLine($"║  SCRIPT COMPARISON REPORT: {report.ScriptTitle.PadRight(54)}║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════════════════════╝\n");

            sb.AppendLine($"Generated: {report.ComparisonDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Comparing: Version {report.OldVersion} -> Version {report.NewVersion}");
            sb.AppendLine();

            sb.AppendLine("=== DOCUMENT STATISTICS ===");
            sb.AppendLine($"Pages: {report.PageCountBefore:F0} -> {report.PageCountAfter:F0} ({(report.PageCountAfter - report.PageCountBefore):+0.0;-0.0;0})");
            sb.AppendLine($"Words: {report.WordCountBefore:N0} -> {report.WordCountAfter:N0} ({(report.WordCountAfter - report.WordCountBefore):+0;-0;0})");
            sb.AppendLine();

            sb.AppendLine("=== CHANGES SUMMARY ===");
            sb.AppendLine($"Total Changes: {report.TotalChanges}");
            sb.AppendLine($"  Added:    {report.AddedElements}");
            sb.AppendLine($"  Removed:  {report.RemovedElements}");
            sb.AppendLine($"  Modified: {report.ModifiedElements}");
            sb.AppendLine($"  Moved:    {report.MovedElements}");
            sb.AppendLine();

            sb.AppendLine("=== DETAILED CHANGES ===\n");

            // Removed elements
            var removed = report.ElementDiffs.Where(d => d.Change == ChangeType.Removed).ToList();
            if (removed.Count > 0)
            {
                sb.AppendLine("REMOVED ELEMENTS:");
                foreach (var diff in removed.Take(10))
                {
                    sb.AppendLine($"  - Page {diff.OldPageNumber}: {diff.ElementType}");
                    sb.AppendLine($"    \"{diff.OldContent.Substring(0, Math.Min(60, diff.OldContent.Length))}...\"");
                }
                if (removed.Count > 10)
                    sb.AppendLine($"  ... and {removed.Count - 10} more");
                sb.AppendLine();
            }

            // Added elements
            var added = report.ElementDiffs.Where(d => d.Change == ChangeType.Added).ToList();
            if (added.Count > 0)
            {
                sb.AppendLine("ADDED ELEMENTS:");
                foreach (var diff in added.Take(10))
                {
                    sb.AppendLine($"  + Page {diff.NewPageNumber}: {diff.ElementType}");
                    sb.AppendLine($"    \"{diff.NewContent.Substring(0, Math.Min(60, diff.NewContent.Length))}...\"");
                }
                if (added.Count > 10)
                    sb.AppendLine($"  ... and {added.Count - 10} more");
                sb.AppendLine();
            }

            // Modified elements
            var modified = report.ElementDiffs.Where(d => d.Change == ChangeType.Modified).ToList();
            if (modified.Count > 0)
            {
                sb.AppendLine("MODIFIED ELEMENTS:");
                foreach (var diff in modified.Take(10))
                {
                    sb.AppendLine($"  ~ {diff.ElementType}: Page {diff.OldPageNumber} -> {diff.NewPageNumber}");
                    if (diff.HighlightedDifferences.Count > 0)
                    {
                        foreach (var change in diff.HighlightedDifferences.Take(3))
                        {
                            sb.AppendLine($"    {change}");
                        }
                    }
                }
                if (modified.Count > 10)
                    sb.AppendLine($"  ... and {modified.Count - 10} more");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate change summary by element type
        /// </summary>
        public string GenerateChangesSummaryByType(ComparisonReport report)
        {
            var sb = new StringBuilder();
            var byType = report.ElementDiffs.GroupBy(d => d.ElementType);

            sb.AppendLine("=== CHANGES BY ELEMENT TYPE ===\n");

            foreach (var typeGroup in byType.OrderBy(g => g.Key))
            {
                var added = typeGroup.Count(e => e.Change == ChangeType.Added);
                var removed = typeGroup.Count(e => e.Change == ChangeType.Removed);
                var modified = typeGroup.Count(e => e.Change == ChangeType.Modified);
                var moved = typeGroup.Count(e => e.Change == ChangeType.Moved);

                if (added + removed + modified + moved > 0)
                {
                    sb.AppendLine($"{typeGroup.Key}:");
                    if (added > 0) sb.AppendLine($"  + Added: {added}");
                    if (removed > 0) sb.AppendLine($"  - Removed: {removed}");
                    if (modified > 0) sb.AppendLine($"  ~ Modified: {modified}");
                    if (moved > 0) sb.AppendLine($"  → Moved: {moved}");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Export comparison as unified diff format
        /// </summary>
        public string ExportAsUnifiedDiff(ComparisonReport report, int contextLines = 3)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine($"--- Version {report.OldVersion}");
            sb.AppendLine($"+++ Version {report.NewVersion}");
            sb.AppendLine($"@@ Comparison Date: {report.ComparisonDate:yyyy-MM-dd HH:mm:ss} @@\n");

            foreach (var diff in report.ElementDiffs)
            {
                switch (diff.Change)
                {
                    case ChangeType.Added:
                        sb.AppendLine($"+ {diff.ElementType} on page {diff.NewPageNumber}");
                        sb.AppendLine($"  {diff.NewContent}");
                        sb.AppendLine();
                        break;
                    case ChangeType.Removed:
                        sb.AppendLine($"- {diff.ElementType} on page {diff.OldPageNumber}");
                        sb.AppendLine($"  {diff.OldContent}");
                        sb.AppendLine();
                        break;
                    case ChangeType.Modified:
                        sb.AppendLine($"~ {diff.ElementType} modified");
                        sb.AppendLine($"- {diff.OldContent}");
                        sb.AppendLine($"+ {diff.NewContent}");
                        sb.AppendLine();
                        break;
                    case ChangeType.Moved:
                        sb.AppendLine($"→ {diff.ElementType} moved from page {diff.OldPageNumber} to {diff.NewPageNumber}");
                        sb.AppendLine();
                        break;
                }
            }

            return sb.ToString();
        }
    }
}
