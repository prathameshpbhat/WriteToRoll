using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// MASTER ELEMENT TYPE DETECTOR
    /// Definitive source for detecting screenplay element types
    /// Used everywhere for consistent, context-aware detection
    /// 
    /// Detection is 100% based on content patterns, not guessing
    /// </summary>
    public class ElementTypeDetector
    {
        private readonly IScreenwritingLogic _logic;

        public ElementTypeDetector(IScreenwritingLogic logic)
        {
            _logic = logic;
        }

        /// <summary>
        /// MASTER DETECTION: Analyze current line and determine its element type
        /// This is the SINGLE SOURCE OF TRUTH for all element detection
        /// </summary>
        public ScriptElementType DetectElementType(string line, ScriptElementType? previousElementType = null)
        {
            if (string.IsNullOrWhiteSpace(line))
                return ScriptElementType.Action;

            var trimmed = line.Trim();
            var upper = trimmed.ToUpperInvariant();

            // ========== RULE 1: SCENE HEADINGS (INT/EXT) ==========
            // Pattern: INT/EXT [./] LOCATION - TIME
            // Examples: INT. HOUSE - DAY, EXT/INT. ROAD - NIGHT
            if (IsSceneHeading(upper, trimmed))
                return ScriptElementType.SceneHeading;

            // ========== RULE 2: TRANSITIONS (RIGHT-ALIGNED) ==========
            // Pattern: TRANSITION_KEYWORD TO:
            // Examples: CUT TO:, DISSOLVE TO:, FADE IN:
            if (IsTransition(upper))
                return ScriptElementType.Transition;

            // ========== RULE 3: PARENTHETICALS ==========
            // Pattern: (content) - always in parentheses
            // Examples: (V.O.), (confused), (pause, then)
            if (IsParenthetical(trimmed))
                return ScriptElementType.Parenthetical;

            // ========== RULE 4: CHARACTER NAMES ==========
            // Context: Previous was SCENE/ACTION/TRANSITION
            // Pattern: UPPERCASE, 1-4 words, with optional modifiers
            // Examples: JOHN, JOHN V.O., OLD MAN, FEMALE OFFICER
            if (previousElementType == ScriptElementType.SceneHeading ||
                previousElementType == ScriptElementType.Action ||
                previousElementType == ScriptElementType.Transition)
            {
                if (IsCharacterName(upper, trimmed))
                    return ScriptElementType.Character;
            }

            // ========== RULE 5: DIALOGUE ==========
            // Context: Previous was CHARACTER or PARENTHETICAL (continuing character)
            // Pattern: Normal text, NOT all caps (except special codes)
            if (previousElementType == ScriptElementType.Character ||
                previousElementType == ScriptElementType.Parenthetical)
            {
                if (!upper.Equals(trimmed))  // If NOT all uppercase
                    return ScriptElementType.Dialogue;
            }

            // ========== RULE 6: DEFAULT TO ACTION ==========
            // Fallback for anything else
            return ScriptElementType.Action;
        }

        /// <summary>
        /// Check if line is a SCENE HEADING (slugline)
        /// Pattern: INT/EXT [./] LOCATION - TIME
        /// </summary>
        private bool IsSceneHeading(string upper, string original)
        {
            // Must start with INT or EXT
            if (!upper.StartsWith("INT") && !upper.StartsWith("EXT"))
                return false;

            // Check for valid scene heading patterns
            if (upper.StartsWith("INT.") || upper.StartsWith("INT "))
                return true;
            if (upper.StartsWith("EXT.") || upper.StartsWith("EXT "))
                return true;
            if (upper.StartsWith("INT/EXT") || upper.StartsWith("EXT/INT"))
                return true;

            return false;
        }

        /// <summary>
        /// Check if line is a TRANSITION (right-aligned action)
        /// Pattern: TRANSITION_KEYWORD TO: or IN: or similar
        /// </summary>
        private bool IsTransition(string upper)
        {
            // Transition keywords
            string[] transitionWords = {
                "FADE", "CUT", "DISSOLVE", "SMASH", "MATCH",
                "WIPE", "IRIS", "FLASH", "BACK", "MONTAGE",
                "TO BE CONTINUED", "END"
            };

            // Check if line contains any transition keyword
            foreach (var keyword in transitionWords)
            {
                if (upper.Contains(keyword))
                {
                    // Make sure it's not part of action text
                    // Transitions usually end with : or are full-line keywords
                    if (upper.EndsWith(":") || upper == keyword || upper.StartsWith(keyword))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if line is a PARENTHETICAL
        /// Pattern: (anything) - exact match with parentheses
        /// </summary>
        private bool IsParenthetical(string trimmed)
        {
            // Must start with ( and end with )
            return trimmed.StartsWith("(") && trimmed.EndsWith(")");
        }

        /// <summary>
        /// Check if line looks like a CHARACTER NAME
        /// Pattern: UPPERCASE, 1-4 words, allows modifiers (V.O., O.S., CONT'D, O.C.)
        /// </summary>
        private bool IsCharacterName(string upper, string original)
        {
            // Character names are uppercase with optional modifiers
            // V.O., O.S., CONT'D, O.C. are special modifiers
            
            // Must be uppercase (can have dots for modifiers)
            if (upper != original && original.ToUpperInvariant() != upper)
                return false;  // Has lowercase letters mixed in

            // Check word count (1-4 words)
            var words = upper.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length < 1 || words.Length > 4)
                return false;

            // Each word should be letters, dots, or apostrophes only
            foreach (var word in words)
            {
                if (!Regex.IsMatch(word, @"^[A-Z\.'-]+$"))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Get proper indentation for element type
        /// Returns indentation string (spaces or tabs) - NOT line content
        /// This ONLY handles indentation, never formats content
        /// </summary>
        public string GetIndentationForElementType(ScriptElementType elementType, PageFormatting pageFormat)
        {
            // Get profile for element type
            var profile = ScreenplayElementProfiles.GetProfile(elementType);
            if (profile == null)
                return "";

            // Convert inches to spaces (approximate: 1 inch = 10 spaces at 12pt Courier)
            int spacesNeeded = (int)(profile.LeftMarginInches * 10);
            return new string(' ', spacesNeeded);
        }

        /// <summary>
        /// Analyze full script and get element type for specific line number
        /// </summary>
        public ScriptElementType DetectLineElementType(string fullScript, int lineNumber)
        {
            var lines = fullScript.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            
            if (lineNumber < 0 || lineNumber >= lines.Length)
                return ScriptElementType.Action;

            // Get previous non-empty line's type
            ScriptElementType? previousType = null;
            for (int i = lineNumber - 1; i >= 0; i--)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    previousType = DetectElementType(lines[i], null);
                    break;
                }
            }

            return DetectElementType(lines[lineNumber], previousType);
        }

        /// <summary>
        /// Format line according to its detected element type
        /// ONLY formats the content, NEVER changes indentation
        /// </summary>
        public string FormatLineContent(string line, ScriptElementType elementType)
        {
            if (string.IsNullOrWhiteSpace(line))
                return line;

            var trimmed = line.Trim();

            return elementType switch
            {
                ScriptElementType.SceneHeading => FormatSceneHeading(trimmed),
                ScriptElementType.Character => FormatCharacter(trimmed),
                ScriptElementType.Dialogue => FormatDialogue(trimmed),
                ScriptElementType.Parenthetical => FormatParenthetical(trimmed),
                ScriptElementType.Transition => FormatTransition(trimmed),
                ScriptElementType.Action => FormatAction(trimmed),
                _ => trimmed
            };
        }

        // ===== FORMATTING FUNCTIONS (content only, no indentation) =====

        private string FormatSceneHeading(string text)
        {
            // Ensure INT/EXT. format
            var upper = text.ToUpperInvariant();
            if (upper.StartsWith("INT/"))
                return "INT./EXT. " + text.Substring(4).Trim();
            if (upper.StartsWith("EXT/"))
                return "EXT./INT. " + text.Substring(4).Trim();
            if (upper.StartsWith("INT"))
                return "INT. " + text.Substring(3).TrimStart('.', ' ');
            if (upper.StartsWith("EXT"))
                return "EXT. " + text.Substring(3).TrimStart('.', ' ');
            return upper;
        }

        private string FormatCharacter(string text)
        {
            // Character names are UPPERCASE with normalized modifiers
            string result = text.ToUpperInvariant();
            result = NormalizeModifiers(result);
            return result;
        }

        private string FormatDialogue(string text)
        {
            // Dialogue stays as-typed (no forced case changes)
            return text;
        }

        private string FormatParenthetical(string text)
        {
            // Parentheticals are lowercase except for special codes
            if (text.StartsWith("(") && text.EndsWith(")"))
            {
                var inner = text.Substring(1, text.Length - 2);

                // If contains special codes, keep uppercase
                if (inner.ToUpperInvariant().Contains("V.O.") ||
                    inner.ToUpperInvariant().Contains("O.S.") ||
                    inner.ToUpperInvariant().Contains("CONT'D") ||
                    inner.ToUpperInvariant().Contains("O.C."))
                {
                    return "(" + NormalizeModifiers(inner) + ")";
                }

                // Otherwise lowercase
                return "(" + inner.ToLowerInvariant() + ")";
            }
            return text.ToLowerInvariant();
        }

        private string FormatTransition(string text)
        {
            // Transitions are UPPERCASE and end with colon
            var upper = text.ToUpperInvariant();
            if (!upper.EndsWith(":"))
                upper += ":";
            return upper;
        }

        private string FormatAction(string text)
        {
            // Action text stays as-typed
            return text;
        }

        private string NormalizeModifiers(string text)
        {
            var result = text;
            result = Regex.Replace(result, @"\s+(VO|V\.O|V O)\s*", " V.O. ", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\s+(OS|O\.S|O S)\s*", " O.S. ", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\s+(CONTD|CONT\'D|CONT D)\s*", " CONT'D ", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, @"\s+(OC|O\.C|O C)\s*", " O.C. ", RegexOptions.IgnoreCase);
            return result.Trim();
        }

        /// <summary>
        /// Get display name for element type (for UI)
        /// </summary>
        public string GetElementDisplayName(ScriptElementType elementType)
        {
            return elementType switch
            {
                ScriptElementType.SceneHeading => "📍 Scene Heading",
                ScriptElementType.Action => "📝 Action",
                ScriptElementType.Character => "👤 Character",
                ScriptElementType.Dialogue => "💬 Dialogue",
                ScriptElementType.Parenthetical => "❗ Parenthetical",
                ScriptElementType.Transition => "➜ Transition",
                _ => "❓ Unknown"
            };
        }
    }
}
