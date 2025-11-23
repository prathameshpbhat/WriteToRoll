using System;
using System.Collections.Generic;
using System.Linq;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Implements Hollywood standard revision color tracking
    /// Fade-In Feature: Revision Colors - Professional revision management
    /// 
    /// Color Sequence:
    /// 1. White (original)
    /// 2. Blue (1st revision)
    /// 3. Pink (2nd revision)
    /// 4. Yellow (3rd revision)
    /// 5. Green (4th revision)
    /// 6. Goldenrod (5th revision)
    /// 7. Salmon (6th revision)
    /// 8. Tan (7th revision)
    /// Plus custom colors for studio-specific requirements
    /// </summary>
    public class RevisionColorManager
    {
        public enum RevisionColor
        {
            White,
            Blue,
            Pink,
            Yellow,
            Green,
            Goldenrod,
            Salmon,
            Tan,
            Custom
        }

        private static readonly Dictionary<RevisionColor, string> ColorHexCodes = new()
        {
            { RevisionColor.White, "#FFFFFF" },
            { RevisionColor.Blue, "#ADD8E6" },      // Light Blue
            { RevisionColor.Pink, "#FFB6C1" },      // Light Pink
            { RevisionColor.Yellow, "#FFFF99" },    // Light Yellow
            { RevisionColor.Green, "#90EE90" },     // Light Green
            { RevisionColor.Goldenrod, "#FFD700" }, // Goldenrod
            { RevisionColor.Salmon, "#FA8072" },    // Salmon
            { RevisionColor.Tan, "#D2B48C" }        // Tan
        };

        private static readonly List<RevisionColor> StandardSequence = new()
        {
            RevisionColor.White,
            RevisionColor.Blue,
            RevisionColor.Pink,
            RevisionColor.Yellow,
            RevisionColor.Green,
            RevisionColor.Goldenrod,
            RevisionColor.Salmon,
            RevisionColor.Tan
        };

        public class RevisionInfo
        {
            public int RevisionNumber { get; set; }
            public RevisionColor Color { get; set; }
            public DateTime AppliedDate { get; set; }
            public string Description { get; set; } = string.Empty;
            public string CustomHexColor { get; set; } = string.Empty;
            public bool IsLocked { get; set; }
            public List<string> ModifiedElementIds { get; set; } = new();
        }

        private Dictionary<string, RevisionInfo> _elementRevisions = new();
        private List<RevisionInfo> _revisionHistory = new();
        private int _currentRevisionPass = 1;

        /// <summary>
        /// Get the standard revision color for a given pass number
        /// </summary>
        public RevisionColor GetRevisionColor(int revisionPass)
        {
            if (revisionPass < 1) return RevisionColor.White;
            if (revisionPass - 1 < StandardSequence.Count)
                return StandardSequence[revisionPass - 1];
            return RevisionColor.Custom;
        }

        /// <summary>
        /// Get hex color code for a revision color
        /// </summary>
        public string GetColorHexCode(RevisionColor color, string customHex = "")
        {
            if (color == RevisionColor.Custom && !string.IsNullOrEmpty(customHex))
                return customHex;
            return ColorHexCodes.ContainsKey(color) ? ColorHexCodes[color] : ColorHexCodes[RevisionColor.White];
        }

        /// <summary>
        /// Mark elements as revised in current revision pass
        /// </summary>
        public void MarkElementsAsRevised(List<string> elementIds, string description = "")
        {
            var color = GetRevisionColor(_currentRevisionPass);
            foreach (var elementId in elementIds)
            {
                if (!_elementRevisions.ContainsKey(elementId))
                {
                    _elementRevisions[elementId] = new RevisionInfo();
                }

                _elementRevisions[elementId].RevisionNumber = _currentRevisionPass;
                _elementRevisions[elementId].Color = color;
                _elementRevisions[elementId].AppliedDate = DateTime.UtcNow;
                _elementRevisions[elementId].Description = description;
            }
        }

        /// <summary>
        /// Lock elements (prevent further changes in printing)
        /// </summary>
        public void LockElements(List<string> elementIds)
        {
            foreach (var elementId in elementIds)
            {
                if (!_elementRevisions.ContainsKey(elementId))
                {
                    _elementRevisions[elementId] = new RevisionInfo();
                }
                _elementRevisions[elementId].IsLocked = true;
            }
        }

        /// <summary>
        /// Advance to next revision pass
        /// </summary>
        public void AdvanceRevisionPass(string description = "")
        {
            _currentRevisionPass++;
            var revInfo = new RevisionInfo
            {
                RevisionNumber = _currentRevisionPass,
                Color = GetRevisionColor(_currentRevisionPass),
                AppliedDate = DateTime.UtcNow,
                Description = description,
                ModifiedElementIds = _elementRevisions
                    .Where(x => x.Value.RevisionNumber == _currentRevisionPass)
                    .Select(x => x.Key)
                    .ToList()
            };
            _revisionHistory.Add(revInfo);
        }

        /// <summary>
        /// Get revision info for a specific element
        /// </summary>
        public RevisionInfo GetElementRevision(string elementId)
        {
            return _elementRevisions.ContainsKey(elementId) 
                ? _elementRevisions[elementId] 
                : null;
        }

        /// <summary>
        /// Get all elements with a specific revision color
        /// </summary>
        public List<string> GetElementsByRevisionColor(RevisionColor color)
        {
            return _elementRevisions
                .Where(x => x.Value.Color == color && !x.Value.IsLocked)
                .Select(x => x.Key)
                .ToList();
        }

        /// <summary>
        /// Get revision history
        /// </summary>
        public List<RevisionInfo> GetRevisionHistory()
        {
            return _revisionHistory;
        }

        /// <summary>
        /// Get pages that contain revisions
        /// </summary>
        public List<int> GetRevisedPages(List<ScriptElement> elements)
        {
            var revisedPages = new HashSet<int>();
            foreach (var element in elements)
            {
                if (_elementRevisions.ContainsKey(element.Id))
                {
                    revisedPages.Add(element.PageNumber);
                }
            }
            return revisedPages.OrderBy(p => p).ToList();
        }

        /// <summary>
        /// Generate revision summary
        /// </summary>
        public string GenerateRevisionSummary()
        {
            if (_elementRevisions.Count == 0)
                return "No revisions yet.";

            var summary = $"Total Revisions: {_revisionHistory.Count}\n";
            summary += $"Elements Modified: {_elementRevisions.Count}\n";
            foreach (var revision in _revisionHistory)
            {
                summary += $"  Pass {revision.RevisionNumber} ({revision.Color}): {revision.ModifiedElementIds.Count} elements - {revision.Description}\n";
            }
            return summary;
        }

        /// <summary>
        /// Clear all revisions (start fresh)
        /// </summary>
        public void ClearAllRevisions()
        {
            _elementRevisions.Clear();
            _revisionHistory.Clear();
            _currentRevisionPass = 1;
        }
    }
}
