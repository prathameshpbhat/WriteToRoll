using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Fade-In Feature: Automatic Format Fixer & Cleanup
    /// 
    /// Features:
    /// - Fix capitalization issues
    /// - Correct common formatting mistakes
    /// - Detect and fix spacing problems
    /// - Validate scene headings
    /// - Auto-correct dialogue issues
    /// - One-click script cleanup
    /// </summary>
    public class FormatFixerService
    {
        public class FormatIssue
        {
            public string IssueType { get; set; } = string.Empty;
            public int ElementIndex { get; set; }
            public string CurrentValue { get; set; } = string.Empty;
            public string SuggestedValue { get; set; } = string.Empty;
            public int Severity { get; set; } // 1=warning, 2=major, 3=critical
            public bool AutoFixable { get; set; }
        }

        public class FormatFixReport
        {
            public int TotalIssuesFound { get; set; }
            public int IssuesFixed { get; set; }
            public List<FormatIssue> Issues { get; set; } = new();
            public List<string> FixedElements { get; set; } = new();
        }

        private const string CHARACTER_NAME_PATTERN = @"^[A-Z][A-Z\s\-]+$";
        private const string SCENE_HEADING_PATTERN = @"^(INT\.|EXT\.|INT/EXT\.)\s+";
        private static readonly string[] VALID_TRANSITIONS = { "CUT TO:", "FADE TO:", "FADE IN:", "FADE OUT:", "FADE TO BLACK:", "CUT TO BLACK:", "DISSOLVE TO:", "MATCH CUT:", "TIME CUT:", "SMASH CUT:" };
        private static readonly string[] VALID_PARENTHETICALS = { "V.O.", "O.S.", "CONT'D", "INTERCUT", "B.G.", "BEAT", "PAUSE" };

        public List<FormatIssue> DetectAllIssues(Script script)
        {
            var issues = new List<FormatIssue>();

            if (script?.Elements == null)
                return issues;

            for (int i = 0; i < script.Elements.Count; i++)
            {
                var element = script.Elements[i];
                var elementIssues = DetectElementIssues(element, i);
                issues.AddRange(elementIssues);
            }

            return issues;
        }

        /// <summary>
        /// Detect all formatting issues in a single element
        /// </summary>
        public List<FormatIssue> DetectElementIssues(ScriptElement element, int index)
        {
            var issues = new List<FormatIssue>();

            switch (element)
            {
                case CharacterElement ce:
                    issues.AddRange(DetectCharacterIssues(ce, index));
                    break;
                case SceneHeadingElement she:
                    issues.AddRange(DetectSceneHeadingIssues(she, index));
                    break;
                case DialogueElement de:
                    issues.AddRange(DetectDialogueIssues(de, index));
                    break;
                case TransitionElement te:
                    issues.AddRange(DetectTransitionIssues(te, index));
                    break;
                case ParentheticalElement pe:
                    issues.AddRange(DetectParentheticalIssues(pe, index));
                    break;
                case ActionElement ae:
                    issues.AddRange(DetectActionIssues(ae, index));
                    break;
            }

            return issues;
        }

        /// <summary>
        /// Detect character name issues
        /// </summary>
        private List<FormatIssue> DetectCharacterIssues(CharacterElement element, int index)
        {
            var issues = new List<FormatIssue>();
            var name = element.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                issues.Add(new FormatIssue
                {
                    IssueType = "Empty Character Name",
                    ElementIndex = index,
                    CurrentValue = "",
                    Severity = 3,
                    AutoFixable = false
                });
                return issues;
            }

            // Check if character name contains lowercase letters (should be all caps)
            if (name != name.ToUpper() && !name.Contains("("))
            {
                issues.Add(new FormatIssue
                {
                    IssueType = "Character Name Case",
                    ElementIndex = index,
                    CurrentValue = name,
                    SuggestedValue = name.ToUpper(),
                    Severity = 2,
                    AutoFixable = true
                });
            }

            // Check for duplicate parentheticals that should be in the name
            if (name.Contains("(V.O.)") || name.Contains("(O.S.)") || name.Contains("(CONT'D)"))
            {
                // These should be in parenthetical, not character name
                var cleanName = Regex.Replace(name, @"\s*\([^)]*\)$", "");
                issues.Add(new FormatIssue
                {
                    IssueType = "Parenthetical in Character Name",
                    ElementIndex = index,
                    CurrentValue = name,
                    SuggestedValue = cleanName,
                    Severity = 2,
                    AutoFixable = true
                });
            }

            return issues;
        }

        /// <summary>
        /// Detect scene heading issues
        /// </summary>
        private List<FormatIssue> DetectSceneHeadingIssues(SceneHeadingElement element, int index)
        {
            var issues = new List<FormatIssue>();
            var heading = element.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(heading))
            {
                issues.Add(new FormatIssue
                {
                    IssueType = "Empty Scene Heading",
                    ElementIndex = index,
                    CurrentValue = "",
                    Severity = 3,
                    AutoFixable = false
                });
                return issues;
            }

            // Check if heading starts with INT/EXT
            if (!Regex.IsMatch(heading, SCENE_HEADING_PATTERN, RegexOptions.IgnoreCase))
            {
                issues.Add(new FormatIssue
                {
                    IssueType = "Invalid Scene Heading Format",
                    ElementIndex = index,
                    CurrentValue = heading,
                    SuggestedValue = "INT. " + heading,
                    Severity = 2,
                    AutoFixable = false
                });
            }

            // Check if lowercase is used (should be uppercase)
            if (heading != heading.ToUpper())
            {
                issues.Add(new FormatIssue
                {
                    IssueType = "Scene Heading Case",
                    ElementIndex = index,
                    CurrentValue = heading,
                    SuggestedValue = heading.ToUpper(),
                    Severity = 1,
                    AutoFixable = true
                });
            }

            // Check for time of day (should be at end)
            if (heading.Contains(" - ") && !Regex.IsMatch(heading, @"\s-\s(DAY|NIGHT|DUSK|DAWN|MORNING|AFTERNOON|EVENING)$", RegexOptions.IgnoreCase))
            {
                issues.Add(new FormatIssue
                {
                    IssueType = "Invalid Time of Day Format",
                    ElementIndex = index,
                    CurrentValue = heading,
                    Severity = 1,
                    AutoFixable = false
                });
            }

            return issues;
        }

        /// <summary>
        /// Detect dialogue issues
        /// </summary>
        private List<FormatIssue> DetectDialogueIssues(DialogueElement element, int index)
        {
            var issues = new List<FormatIssue>();
            var dialogue = element.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(dialogue))
            {
                issues.Add(new FormatIssue
                {
                    IssueType = "Empty Dialogue",
                    ElementIndex = index,
                    CurrentValue = "",
                    Severity = 2,
                    AutoFixable = false
                });
                return issues;
            }

            // Check for proper punctuation at end
            if (!dialogue.EndsWith(".") && !dialogue.EndsWith("?") && !dialogue.EndsWith("!") && !dialogue.EndsWith("\""))
            {
                issues.Add(new FormatIssue
                {
                    IssueType = "Missing Dialogue Punctuation",
                    ElementIndex = index,
                    CurrentValue = dialogue,
                    SuggestedValue = dialogue + ".",
                    Severity = 1,
                    AutoFixable = true
                });
            }

            // Check for excessive ellipses
            if (dialogue.Contains("....") || dialogue.Contains("  "))
            {
                var cleaned = Regex.Replace(dialogue, @"\.{4,}", "...");
                cleaned = Regex.Replace(cleaned, @"  +", " ");
                issues.Add(new FormatIssue
                {
                    IssueType = "Excessive Punctuation/Spacing",
                    ElementIndex = index,
                    CurrentValue = dialogue,
                    SuggestedValue = cleaned,
                    Severity = 1,
                    AutoFixable = true
                });
            }

            return issues;
        }

        /// <summary>
        /// Detect transition issues
        /// </summary>
        private List<FormatIssue> DetectTransitionIssues(TransitionElement element, int index)
        {
            var issues = new List<FormatIssue>();
            var transition = element.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(transition))
            {
                issues.Add(new FormatIssue
                {
                    IssueType = "Empty Transition",
                    ElementIndex = index,
                    CurrentValue = "",
                    Severity = 1,
                    AutoFixable = false
                });
                return issues;
            }

            // Check if transition is valid
            var isValidTransition = VALID_TRANSITIONS.Any(t => transition.ToUpper().StartsWith(t));
            if (!isValidTransition && !transition.Contains("-"))
            {
                issues.Add(new FormatIssue
                {
                    IssueType = "Non-standard Transition",
                    ElementIndex = index,
                    CurrentValue = transition,
                    Severity = 1,
                    AutoFixable = false
                });
            }

            // Check case
            if (transition != transition.ToUpper())
            {
                issues.Add(new FormatIssue
                {
                    IssueType = "Transition Case",
                    ElementIndex = index,
                    CurrentValue = transition,
                    SuggestedValue = transition.ToUpper(),
                    Severity = 1,
                    AutoFixable = true
                });
            }

            return issues;
        }

        /// <summary>
        /// Detect parenthetical issues
        /// </summary>
        private List<FormatIssue> DetectParentheticalIssues(ParentheticalElement element, int index)
        {
            var issues = new List<FormatIssue>();
            var paren = element.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(paren))
            {
                issues.Add(new FormatIssue
                {
                    IssueType = "Empty Parenthetical",
                    ElementIndex = index,
                    CurrentValue = "",
                    Severity = 1,
                    AutoFixable = false
                });
                return issues;
            }

            // Check if parenthetical is lowercase when it should be uppercase
            var upperParen = paren.ToUpper();
            if (paren != upperParen && VALID_PARENTHETICALS.Contains(upperParen))
            {
                issues.Add(new FormatIssue
                {
                    IssueType = "Parenthetical Case",
                    ElementIndex = index,
                    CurrentValue = paren,
                    SuggestedValue = upperParen,
                    Severity = 1,
                    AutoFixable = true
                });
            }

            return issues;
        }

        /// <summary>
        /// Detect action element issues
        /// </summary>
        private List<FormatIssue> DetectActionIssues(ActionElement element, int index)
        {
            var issues = new List<FormatIssue>();
            var action = element.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(action))
            {
                issues.Add(new FormatIssue
                {
                    IssueType = "Empty Action",
                    ElementIndex = index,
                    CurrentValue = "",
                    Severity = 1,
                    AutoFixable = false
                });
                return issues;
            }

            // Check for multiple spaces
            if (action.Contains("  "))
            {
                var cleaned = Regex.Replace(action, @"  +", " ");
                issues.Add(new FormatIssue
                {
                    IssueType = "Excessive Spacing",
                    ElementIndex = index,
                    CurrentValue = action,
                    SuggestedValue = cleaned,
                    Severity = 1,
                    AutoFixable = true
                });
            }

            // Check for character names in CAPS (might be a character introduction)
            var capWords = Regex.Matches(action, @"\b[A-Z]{2,}\b");
            if (capWords.Count > 3)
            {
                issues.Add(new FormatIssue
                {
                    IssueType = "Many Capitalized Words",
                    ElementIndex = index,
                    CurrentValue = action.Substring(0, Math.Min(50, action.Length)),
                    Severity = 1,
                    AutoFixable = false
                });
            }

            return issues;
        }

        /// <summary>
        /// Fix all auto-fixable issues in a script
        /// </summary>
        public FormatFixReport FixAllIssues(Script script)
        {
            var report = new FormatFixReport();
            var issues = DetectAllIssues(script);
            report.TotalIssuesFound = issues.Count;

            foreach (var issue in issues.Where(i => i.AutoFixable))
            {
                if (ApplyFix(script, issue))
                {
                    report.IssuesFixed++;
                    report.FixedElements.Add($"{issue.IssueType}: {issue.CurrentValue} → {issue.SuggestedValue}");
                }
            }

            report.Issues = issues;
            return report;
        }

        /// <summary>
        /// Apply fix to a specific issue
        /// </summary>
        public bool ApplyFix(Script script, FormatIssue issue)
        {
            try
            {
                if (issue.ElementIndex >= script.Elements.Count)
                    return false;

                var element = script.Elements[issue.ElementIndex];
                var newValue = issue.SuggestedValue;

                switch (element)
                {
                    case CharacterElement ce:
                        ce.Name = newValue;
                        return true;
                    case SceneHeadingElement she:
                        she.Text = newValue;
                        return true;
                    case DialogueElement de:
                        de.Text = newValue;
                        return true;
                    case TransitionElement te:
                        te.Text = newValue;
                        return true;
                    case ParentheticalElement pe:
                        pe.Text = newValue;
                        return true;
                    case ActionElement ae:
                        ae.Text = newValue;
                        return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying fix: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Generate format audit report
        /// </summary>
        public string GenerateFormatAudit(Script script)
        {
            var sb = new System.Text.StringBuilder();
            var issues = DetectAllIssues(script);
            var criticalCount = issues.Count(i => i.Severity == 3);
            var majorCount = issues.Count(i => i.Severity == 2);
            var warningCount = issues.Count(i => i.Severity == 1);

            sb.AppendLine("=== FORMAT AUDIT REPORT ===");
            sb.AppendLine($"Total Issues Found: {issues.Count}");
            sb.AppendLine($"  Critical: {criticalCount}");
            sb.AppendLine($"  Major: {majorCount}");
            sb.AppendLine($"  Warnings: {warningCount}");
            sb.AppendLine();
            sb.AppendLine("Issues by Type:");
            foreach (var type in issues.Select(i => i.IssueType).Distinct())
            {
                sb.AppendLine($"  {type}: {issues.Count(i => i.IssueType == type)}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get specific issue type fixes
        /// </summary>
        public List<FormatIssue> GetIssuesByType(Script script, string issueType)
        {
            return DetectAllIssues(script).Where(i => i.IssueType == issueType).ToList();
        }

        /// <summary>
        /// Quick cleanup - removes common issues
        /// </summary>
        public void QuickCleanup(Script script)
        {
            if (script?.Elements == null)
                return;

            // Remove empty elements
            script.Elements.RemoveAll(e =>
                string.IsNullOrWhiteSpace(e switch
                {
                    CharacterElement ce => ce.Name,
                    ActionElement ae => ae.Text,
                    DialogueElement de => de.Text,
                    _ => null
                })
            );

            // Fix common issues
            foreach (var element in script.Elements)
            {
                if (element is ActionElement ae)
                    ae.Text = Regex.Replace(ae.Text, @"  +", " ");

                if (element is DialogueElement de)
                    de.Text = Regex.Replace(de.Text, @"  +", " ");
            }
        }
    }
}
