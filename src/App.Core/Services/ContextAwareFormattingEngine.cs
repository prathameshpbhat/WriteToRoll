using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Context-aware formatting engine
    /// Understands screenplay flow: what comes AFTER current line
    /// Examples:
    ///   TAB → Character name (UPPERCASE)
    ///   New line after Character → Dialogue
    ///   New line after Action → Action or Scene Heading
    /// </summary>
    public class ContextAwareFormattingEngine
    {
        private readonly IScreenwritingLogic _logic;

        public ContextAwareFormattingEngine(IScreenwritingLogic logic)
        {
            _logic = logic;
        }

        /// <summary>
        /// Detect element type based on CONTEXT and POSITION in screenplay
        /// Looks at previous line to determine what current line should be
        /// </summary>
        public ScriptElementType DetectElementTypeByContext(
            string currentLine,
            string previousLine,
            string nextLine,
            ScriptElementType? previousElementType)
        {
            if (string.IsNullOrWhiteSpace(currentLine))
                return ScriptElementType.Action;

            var trimmed = currentLine.Trim();
            var trimmedPrev = previousLine?.Trim() ?? "";

            // RULE 1: If previous line was CHARACTER and current line is indented/new line → DIALOGUE
            if (previousElementType == ScriptElementType.Character && !string.IsNullOrEmpty(trimmed))
            {
                return ScriptElementType.Dialogue;
            }

            // RULE 2: If previous line was DIALOGUE and current line is just "(" → PARENTHETICAL
            if (previousElementType == ScriptElementType.Dialogue && trimmed.StartsWith("("))
            {
                return ScriptElementType.Parenthetical;
            }

            // RULE 3: If previous line was PARENTHETICAL and current line is NOT "(" → DIALOGUE (same character continues)
            if (previousElementType == ScriptElementType.Parenthetical && !trimmed.StartsWith("("))
            {
                return ScriptElementType.Dialogue;
            }

            // RULE 4: SCENE HEADINGS (INT/EXT at line start)
            if (IsSceneHeading(trimmed))
                return ScriptElementType.SceneHeading;

            // RULE 5: TRANSITIONS (specific keywords)
            if (IsTransition(trimmed))
                return ScriptElementType.Transition;

            // RULE 6: CHARACTER names (uppercase, after Action or Scene)
            // If previous was ACTION or SCENE or TRANSITION, current line might be CHARACTER
            if ((previousElementType == ScriptElementType.Action ||
                 previousElementType == ScriptElementType.SceneHeading ||
                 previousElementType == ScriptElementType.Transition) &&
                IsLikelyCharacterName(trimmed))
            {
                return ScriptElementType.Character;
            }

            // RULE 7: Default to ACTION if unsure
            return ScriptElementType.Action;
        }

        /// <summary>
        /// Format based on context - what comes BEFORE (previous element type)
        /// </summary>
        public string FormatByContext(string currentLine, ScriptElementType detectedType, PageFormatting pageFormat)
        {
            if (string.IsNullOrWhiteSpace(currentLine))
                return currentLine;

            var trimmed = currentLine.Trim();

            return detectedType switch
            {
                ScriptElementType.SceneHeading => FormatSceneHeading(trimmed),
                ScriptElementType.Character => FormatCharacter(trimmed),
                ScriptElementType.Dialogue => FormatDialogue(trimmed),
                ScriptElementType.Parenthetical => FormatParenthetical(trimmed),
                ScriptElementType.Transition => FormatTransition(trimmed),
                _ => trimmed  // ACTION stays as-is
            };
        }

        /// <summary>
        /// Check if line is a scene heading (INT/EXT)
        /// </summary>
        private bool IsSceneHeading(string line)
        {
            var upper = line.ToUpperInvariant();
            return upper.StartsWith("INT ") || upper.StartsWith("EXT ") ||
                   upper.StartsWith("INT/") || upper.StartsWith("EXT/") ||
                   upper.StartsWith("INT.") || upper.StartsWith("EXT.");
        }

        /// <summary>
        /// Check if line is a transition
        /// </summary>
        private bool IsTransition(string line)
        {
            var upper = line.ToUpperInvariant();
            var transitionPatterns = new[] {
                "FADE IN", "FADE OUT", "FADE TO BLACK", "FADE TO WHITE",
                "CUT TO", "DISSOLVE TO", "SMASH TO", "SMASH CUT",
                "MATCH CUT", "MATCH ON", "WIPE TO", "IRIS IN", "IRIS OUT",
                "FLASH TO", "BACK TO", "MONTAGE", "END MONTAGE",
                "TO BE CONTINUED"
            };

            foreach (var pattern in transitionPatterns)
            {
                if (upper.Contains(pattern))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if line looks like a CHARACTER name
        /// Rules: Uppercase words, 1-4 words, no punctuation (except dots for V.O./O.S.)
        /// </summary>
        private bool IsLikelyCharacterName(string line)
        {
            // If line is all caps and short → likely character
            if (line.All(c => char.IsUpper(c) || char.IsWhiteSpace(c) || c == '.' || c == '\''))
            {
                var wordCount = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                return wordCount >= 1 && wordCount <= 4;
            }

            return false;
        }

        private string FormatSceneHeading(string line)
        {
            // Ensure INT/EXT prefix is uppercase
            var upper = line.ToUpperInvariant();
            if (!upper.Contains("."))
            {
                if (upper.StartsWith("INT/EXT"))
                    return "INT./EXT. " + line.Substring(7).Trim();
                if (upper.StartsWith("EXT/INT"))
                    return "EXT./INT. " + line.Substring(7).Trim();
                if (upper.StartsWith("INT"))
                    return "INT. " + line.Substring(3).Trim();
                if (upper.StartsWith("EXT"))
                    return "EXT. " + line.Substring(3).Trim();
            }
            return upper;
        }

        private string FormatCharacter(string line)
        {
            // Character names are UPPERCASE
            var formatted = line.ToUpperInvariant();

            // Handle special modifiers: V.O., O.S., CONT'D, O.C.
            formatted = NormalizeModifiers(formatted);

            return formatted;
        }

        private string FormatDialogue(string line)
        {
            // Dialogue is normal case (title case or as-typed)
            // Don't force uppercase for dialogue
            return line;
        }

        private string FormatParenthetical(string line)
        {
            // Parenthetical content is lowercase (except special codes)
            var content = line.Trim();

            if (content.StartsWith("(") && content.EndsWith(")"))
            {
                var inner = content.Substring(1, content.Length - 2);

                // Special codes stay uppercase
                if (inner.Contains("V.O.") || inner.Contains("O.S.") || 
                    inner.Contains("CONT'D") || inner.Contains("O.C."))
                {
                    return content;
                }

                // Regular parentheticals are lowercase
                return "(" + inner.ToLowerInvariant() + ")";
            }

            return content.ToLowerInvariant();
        }

        private string FormatTransition(string line)
        {
            // Transitions are UPPERCASE and end with colon
            var upper = line.ToUpperInvariant();

            if (!upper.EndsWith(":"))
                upper += ":";

            return upper;
        }

        private string NormalizeModifiers(string text)
        {
            var result = text;

            // Normalize V.O.
            result = Regex.Replace(result, @"\s+(VO|V\.O|V O)\s*", " V.O. ", RegexOptions.IgnoreCase);

            // Normalize O.S.
            result = Regex.Replace(result, @"\s+(OS|O\.S|O S)\s*", " O.S. ", RegexOptions.IgnoreCase);

            // Normalize CONT'D
            result = Regex.Replace(result, @"\s+(CONTD|CONT\'D|CONT D)\s*", " CONT'D ", RegexOptions.IgnoreCase);

            // Normalize O.C.
            result = Regex.Replace(result, @"\s+(OC|O\.C|O C)\s*", " O.C. ", RegexOptions.IgnoreCase);

            return result.Trim();
        }

        /// <summary>
        /// Get the previous element type from script history
        /// Scans backward from current line to find last non-empty line's type
        /// </summary>
        public ScriptElementType? GetPreviousElementType(string fullScript, int currentLineIndex)
        {
            var lines = fullScript.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            if (currentLineIndex <= 0)
                return null;

            // Scan backward to find previous non-empty line
            for (int i = currentLineIndex - 1; i >= 0; i--)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                // Detect this line's type
                if (IsSceneHeading(line))
                    return ScriptElementType.SceneHeading;
                if (IsTransition(line))
                    return ScriptElementType.Transition;
                if (line.StartsWith("(") && line.EndsWith(")"))
                    return ScriptElementType.Parenthetical;
                if (IsLikelyCharacterName(line))
                    return ScriptElementType.Character;
                if (line.StartsWith("\"") && line.EndsWith("\""))
                    return ScriptElementType.Dialogue;

                // Default to action
                return ScriptElementType.Action;
            }

            return null;
        }

        /// <summary>
        /// Get the next non-empty line to look ahead
        /// </summary>
        public string GetNextNonEmptyLine(string fullScript, int currentLineIndex)
        {
            var lines = fullScript.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            if (currentLineIndex >= lines.Length - 1)
                return "";

            for (int i = currentLineIndex + 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (!string.IsNullOrEmpty(line))
                    return line;
            }

            return "";
        }

        /// <summary>
        /// Comprehensive format on ENTER key
        /// Considers what user typed + context
        /// </summary>
        public (string formattedLine, ScriptElementType detectedType) FormatOnEnter(
            string currentLine,
            string previousLine,
            ScriptElementType? previousElementType,
            PageFormatting pageFormat)
        {
            if (string.IsNullOrWhiteSpace(currentLine))
                return (currentLine, ScriptElementType.Action);

            var detected = DetectElementTypeByContext(currentLine, previousLine, "", previousElementType);
            var formatted = FormatByContext(currentLine, detected, pageFormat);

            return (formatted, detected);
        }

        /// <summary>
        /// Tab key handler - convert to CHARACTER if appropriate
        /// Scenario: User types name, presses TAB → becomes CHARACTER (uppercase, centered)
        /// </summary>
        public string HandleTabKey(string currentLine, ScriptElementType? previousElementType)
        {
            if (string.IsNullOrWhiteSpace(currentLine))
                return currentLine;

            var trimmed = currentLine.Trim();

            // If previous was ACTION/SCENE/TRANSITION and user presses TAB after typing name
            // → treat as CHARACTER
            if ((previousElementType == ScriptElementType.Action ||
                 previousElementType == ScriptElementType.SceneHeading ||
                 previousElementType == ScriptElementType.Transition) &&
                IsLikelyCharacterName(trimmed))
            {
                return FormatCharacter(trimmed);
            }

            // Otherwise, just return with no change
            return trimmed;
        }
    }
}
