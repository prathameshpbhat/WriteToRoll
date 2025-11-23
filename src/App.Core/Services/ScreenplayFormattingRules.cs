using System;
using System.Text.RegularExpressions;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Applies professional screenplay formatting rules per industry standards
    /// Based on: Courier 12pt, A4 Letter format, proper margins and positioning
    /// Reference: "How to Write a Movie Script Like Professional Screenwriters"
    /// </summary>
    public interface IScreenplayFormattingRules
    {
        string FormatSceneHeading(string text);
        string FormatAction(string text);
        string FormatCharacter(string text);
        string FormatDialogue(string text);
        string FormatParenthetical(string text);
        string FormatTransition(string text);
        bool IsValidElement(string line, out ScriptElementType elementType);
    }

    public class ScreenplayFormattingRules : IScreenplayFormattingRules
    {
        private readonly PageFormatting _pageFormat;

        public ScreenplayFormattingRules(PageFormatting pageFormat)
        {
            _pageFormat = pageFormat ?? PageFormatting.StandardLetter();
        }

        /// <summary>
        /// Format scene heading: INT/EXT. LOCATION - TIME OF DAY
        /// All caps, left margin 1.5"
        /// </summary>
        public string FormatSceneHeading(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // Normalize: INT/EXT. LOCATION - TIME format
            text = Regex.Replace(text.Trim(), @"\s+", " ");

            // Ensure INT/EXT format
            if (!Regex.IsMatch(text, @"^(INT|EXT|INT/EXT)", RegexOptions.IgnoreCase))
            {
                text = "INT. " + text;
            }

            // Ensure period after INT/EXT
            text = Regex.Replace(text, @"^(INT|EXT|INT/EXT)\s*\.?\s+", "$1. ", RegexOptions.IgnoreCase);

            return text.ToUpper();
        }

        /// <summary>
        /// Format action: Left margin 1.5", right margin 1", normal case
        /// Third-person present tense, visual/audible description
        /// </summary>
        public string FormatAction(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // Action is written normally (not all caps), proper sentence structure
            text = text.Trim();
            
            // Ensure first letter capitalized
            if (text.Length > 0)
            {
                text = char.ToUpper(text[0]) + text.Substring(1);
            }

            return text;
        }

        /// <summary>
        /// Format character name: Centered at 3.7" from left, ALL CAPS
        /// Positioned above dialogue block
        /// </summary>
        public string FormatCharacter(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            text = text.Trim().ToUpper();

            // Remove extensions like (V.O.), (O.S.) from character name
            // Those go in parenthetical
            text = Regex.Replace(text, @"\s*\(V\.O\.\)\s*|\s*\(O\.S\.\)\s*", string.Empty);

            return text;
        }

        /// <summary>
        /// Format dialogue: Indented left 2.5", right 1"
        /// Below character name, normal case (maintain speaker's exact words)
        /// </summary>
        public string FormatDialogue(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            text = text.Trim();

            // Ensure proper quote handling
            if (!text.StartsWith("\""))
                text = "\"" + text;
            if (!text.EndsWith("\"") && !text.EndsWith("\""))
                text = text + "\"";

            return text;
        }

        /// <summary>
        /// Format parenthetical: (direction for dialogue delivery)
        /// Lowercase, indented 3.1" from left, left of dialogue
        /// Examples: (beat), (looking back), (V.O.), (O.S.), (CONT'D)
        /// </summary>
        public string FormatParenthetical(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            text = text.Trim();

            // Ensure parentheses
            if (!text.StartsWith("("))
                text = "(" + text;
            if (!text.EndsWith(")"))
                text = text + ")";

            // Special cases: (V.O.), (O.S.), (CONT'D) should stay uppercase
            if (Regex.IsMatch(text, @"\((V\.O\.|O\.S\.|CONT'D|CONT'D)\)"))
            {
                // These stay as is
                return text;
            }

            // Lowercase everything else inside parentheses
            var content = text.Substring(1, text.Length - 2).ToLower();
            return "(" + content + ")";
        }

        /// <summary>
        /// Format transition: CUT TO:, FADE IN:, DISSOLVE:, etc.
        /// Right-aligned or top-left per convention
        /// Professional transitions: CUT, DISSOLVE, FADE, SMASH CUT, MATCH CUT, WIPE, IRIS, FLASH, BACK TO, MONTAGE
        /// </summary>
        public string FormatTransition(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            text = text.Trim().ToUpper();

            // Ensure proper transition format
            var validTransitions = new[] { "CUT", "DISSOLVE", "FADE", "SMASH", "MATCH", "WIPE", "IRIS", "FLASH", "BACK", "MONTAGE", "TO BLACK", "FADE IN", "FADE OUT" };
            
            // Ensure proper ending (: for most transitions, . for MONTAGE)
            if (text.Contains("MONTAGE"))
            {
                if (!text.EndsWith(":"))
                    text = text.TrimEnd('.') + ":";
            }
            else if (!text.EndsWith(":"))
            {
                text = text.TrimEnd('.') + " TO:";
            }

            return text;
        }

        /// <summary>
        /// Detects element type from line content
        /// </summary>
        public bool IsValidElement(string line, out ScriptElementType elementType)
        {
            elementType = ScriptElementType.Action;

            if (string.IsNullOrWhiteSpace(line))
            {
                elementType = ScriptElementType.Action;
                return true;
            }

            line = line.Trim();

            // Scene heading detection
            if (Regex.IsMatch(line, @"^(INT|EXT|INT/EXT)", RegexOptions.IgnoreCase))
            {
                elementType = ScriptElementType.SceneHeading;
                return true;
            }

            // Transition detection
            if (Regex.IsMatch(line, @"^(CUT|FADE|DISSOLVE|SMASH|MATCH|WIPE|IRIS|MONTAGE)", RegexOptions.IgnoreCase) && line.Contains(":"))
            {
                elementType = ScriptElementType.Transition;
                return true;
            }

            // Parenthetical detection (must be line starting with open paren)
            if (line.StartsWith("(") && line.EndsWith(")"))
            {
                elementType = ScriptElementType.Parenthetical;
                return true;
            }

            // Character name detection: all caps, no punctuation except extensions
            if (Regex.IsMatch(line, @"^[A-Z\s]+(\s+\(V\.O\.\)|\s+\(O\.S\.\))?$"))
            {
                elementType = ScriptElementType.Character;
                return true;
            }

            // Default: Action
            elementType = ScriptElementType.Action;
            return true;
        }
    }
}
