using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Handles real-time auto-formatting as user types
    /// Implements intelligent indentation, normalization, and formatting
    /// </summary>
    public class AutoFormattingService
    {
        private readonly FormattingService _formattingService;
        private readonly IScreenwritingLogic _screenwritingLogic;

        public AutoFormattingService(FormattingService formattingService, IScreenwritingLogic screenwritingLogic)
        {
            _formattingService = formattingService;
            _screenwritingLogic = screenwritingLogic;
        }

        /// <summary>
        /// Format text as user types (real-time)
        /// </summary>
        public FormattingResult FormatAsYouType(string input, ScriptElementType? previousElementType, int caretPosition)
        {
            if (string.IsNullOrEmpty(input))
                return new FormattingResult(input, 0, false, null);

            var trimmed = input.Trim();
            
            // Don't format empty input
            if (string.IsNullOrWhiteSpace(trimmed))
                return new FormattingResult(input, caretPosition, false, null);

            // Auto-format specific cases
            var formatted = AutoFormatSpecialCases(trimmed);
            
            return new FormattingResult(
                formatted.Text,
                formatted.CaretPosition,
                formatted.WasChanged,
                formatted.Suggestion
            );
        }

        /// <summary>
        /// Format text when user presses Enter/Tab (block finalization)
        /// </summary>
        public FormattingResult FormatOnBlockEnd(string input, ScriptElementType? previousElementType)
        {
            if (string.IsNullOrEmpty(input))
                return new FormattingResult(input, 0, false, null);

            var context = new ScriptContext(previousElementType, true);
            var result = _screenwritingLogic.DetectAndNormalize(input, context);

            // Apply formatting based on detected type
            var formatted = ApplyElementFormatting(result.Text, result.ElementType);

            return new FormattingResult(
                formatted,
                formatted.Length,
                result.TextWasChanged,
                result.AutoSuggestion
            );
        }

        /// <summary>
        /// Get proper indentation for element type
        /// </summary>
        public string GetElementIndentation(ScriptElementType elementType)
        {
            return _formattingService.GetIndentation(elementType);
        }

        /// <summary>
        /// Auto-format special cases as user types
        /// </summary>
        private AutoFormatData AutoFormatSpecialCases(string input)
        {
            var upper = input.ToUpperInvariant();
            var data = new AutoFormatData { Text = input, CaretPosition = input.Length, WasChanged = false };

            // Case 1: INT → INT. (scene heading start)
            if (input.Equals("INT", StringComparison.OrdinalIgnoreCase) && !input.EndsWith("."))
            {
                data.Text = "INT. ";
                data.CaretPosition = data.Text.Length;
                data.WasChanged = true;
                return data;
            }

            // Case 2: EXT → EXT. (scene heading start)
            if (input.Equals("EXT", StringComparison.OrdinalIgnoreCase) && !input.EndsWith("."))
            {
                data.Text = "EXT. ";
                data.CaretPosition = data.Text.Length;
                data.WasChanged = true;
                return data;
            }

            // Case 3: INT/ → INT./EXT. (dual location hint)
            if ((input.Equals("INT/", StringComparison.OrdinalIgnoreCase) ||
                 input.Equals("EXT/", StringComparison.OrdinalIgnoreCase)) &&
                !input.Contains("EXT"))
            {
                data.Text = input.TrimEnd('/') + "./EXT. ";
                data.CaretPosition = data.Text.Length;
                data.WasChanged = true;
                return data;
            }

            // Case 4: Auto-close parentheses in parentheticals
            if (input.StartsWith("(") && !input.EndsWith(")") && input.Length > 1)
            {
                data.Text = input + ")";
                data.CaretPosition = input.Length; // Keep caret before closing paren
                data.WasChanged = true;
                return data;
            }

            // Case 5: Auto-close quotes (if applicable)
            if (input.StartsWith("\"") && !input.EndsWith("\"") && input.Length > 1)
            {
                data.Text = input + "\"";
                data.CaretPosition = input.Length;
                data.WasChanged = true;
                return data;
            }

            // Case 6: Centered text > text < auto-close
            if (input.StartsWith(">") && !input.EndsWith("<"))
            {
                data.Text = input + " <";
                data.CaretPosition = input.Length; // Before closing <
                data.WasChanged = true;
                return data;
            }

            // Case 7: FADE IN: / OUT: auto-complete
            if ((upper.StartsWith("FADE IN") || upper.StartsWith("FADE OUT")) && !input.EndsWith(":"))
            {
                data.Text = input.TrimEnd() + ":";
                data.CaretPosition = data.Text.Length;
                data.WasChanged = true;
                return data;
            }

            // Case 8: CUT TO: auto-complete
            if (upper.StartsWith("CUT TO") && !input.EndsWith(":"))
            {
                data.Text = input.TrimEnd() + ":";
                data.CaretPosition = data.Text.Length;
                data.WasChanged = true;
                return data;
            }

            // Case 9: Normalize character modifiers
            if (input.Contains(" VO") || input.Contains(" vo") || input.Contains(" V O"))
            {
                data.Text = NormalizeCharacterModifiers(input);
                data.WasChanged = data.Text != input;
                data.CaretPosition = data.Text.Length;
                return data;
            }

            // Case 10: Normalize character modifiers - O.S.
            if (input.Contains(" OS") || input.Contains(" os") || input.Contains(" O S"))
            {
                data.Text = NormalizeCharacterModifiers(input);
                data.WasChanged = data.Text != input;
                data.CaretPosition = data.Text.Length;
                return data;
            }

            return data;
        }

        /// <summary>
        /// Apply element-specific formatting
        /// </summary>
        private string ApplyElementFormatting(string text, ScriptElementType elementType)
        {
            switch (elementType)
            {
                case ScriptElementType.SceneHeading:
                    return FormatSceneHeading(text);

                case ScriptElementType.Action:
                    return FormatAction(text);

                case ScriptElementType.Character:
                    return FormatCharacter(text);

                case ScriptElementType.Dialogue:
                    return FormatDialogue(text);

                case ScriptElementType.Parenthetical:
                    return FormatParenthetical(text);

                case ScriptElementType.Transition:
                    return FormatTransition(text);

                case ScriptElementType.Shot:
                    return FormatShot(text);

                case ScriptElementType.CenteredText:
                    return FormatCenteredText(text);

                default:
                    return text;
            }
        }

        private string FormatSceneHeading(string text)
        {
            var upper = text.ToUpperInvariant();

            // Ensure INT. or EXT. prefix
            if (!upper.StartsWith("INT.") && !upper.StartsWith("EXT.") &&
                !upper.StartsWith("INT./EXT.") && !upper.StartsWith("EXT./INT."))
            {
                if (upper.StartsWith("INT") || upper.StartsWith("EXT"))
                {
                    var prefix = upper.StartsWith("INT") ? "INT." : "EXT.";
                    text = prefix + text.Substring(3);
                    upper = text.ToUpperInvariant();
                }
            }

            // Ensure time element
            var hasTime = Regex.IsMatch(upper, @"(DAY|NIGHT|MORNING|EVENING|NOON|DAWN|DUSK|LATER|CONTINUOUS)");
            if (!hasTime && !upper.EndsWith(" - "))
            {
                text = text.TrimEnd() + " - DAY";
            }

            return upper;
        }

        private string FormatAction(string text)
        {
            // Sentence case for action
            return ToSentenceCase(text);
        }

        private string FormatCharacter(string text)
        {
            var upper = text.ToUpperInvariant();
            
            // Remove duplicate modifiers
            var modifiers = new[] { "V.O.", "O.S.", "CONT'D", "O.C.", "(V.O.)", "(O.S.)", "(O.C.)" };
            var normalized = upper;
            
            foreach (var mod in modifiers)
            {
                while (normalized.Contains(mod + " " + mod) || normalized.EndsWith(mod + " " + mod))
                {
                    normalized = normalized.Replace(mod + " " + mod, mod);
                }
            }

            return normalized;
        }

        private string FormatDialogue(string text)
        {
            // Trim and preserve case
            return text.Trim();
        }

        private string FormatParenthetical(string text)
        {
            var content = text.Trim('(', ')').Trim();
            // Apply sentence case to content
            content = ToSentenceCase(content);
            return $"({content})";
        }

        private string FormatTransition(string text)
        {
            var upper = text.ToUpperInvariant();
            if (!upper.EndsWith(":"))
                upper += ":";
            return upper;
        }

        private string FormatShot(string text)
        {
            return text.ToUpperInvariant();
        }

        private string FormatCenteredText(string text)
        {
            var inner = text.Trim('>', '<').Trim();
            return $"> {inner} <";
        }

        private string NormalizeCharacterModifiers(string text)
        {
            var result = text;
            
            // V.O. variations
            result = Regex.Replace(result, @"\s+(VO|V\.O|V O)(?=\s|$)", " V.O.", RegexOptions.IgnoreCase);
            
            // O.S. variations
            result = Regex.Replace(result, @"\s+(OS|O\.S|O S)(?=\s|$)", " O.S.", RegexOptions.IgnoreCase);
            
            // CONT'D variations
            result = Regex.Replace(result, @"\s+(CONTD|CONT\'D|CONT D)(?=\s|$)", " CONT'D", RegexOptions.IgnoreCase);
            
            // O.C. variations
            result = Regex.Replace(result, @"\s+(OC|O\.C|O C)(?=\s|$)", " O.C.", RegexOptions.IgnoreCase);

            return result.ToUpperInvariant();
        }

        private string ToSentenceCase(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            var lower = text.ToLowerInvariant();
            return char.ToUpperInvariant(lower[0]) + (lower.Length > 1 ? lower.Substring(1) : "");
        }
    }

    public class AutoFormatData
    {
        public string Text { get; set; } = string.Empty;
        public int CaretPosition { get; set; }
        public bool WasChanged { get; set; }
        public string? Suggestion { get; set; }
    }

    public class FormattingResult
    {
        public string FormattedText { get; set; }
        public int CaretPosition { get; set; }
        public bool WasChanged { get; set; }
        public string? Suggestion { get; set; }

        public FormattingResult(string text, int caretPos, bool changed, string? suggestion = null)
        {
            FormattedText = text;
            CaretPosition = caretPos;
            WasChanged = changed;
            Suggestion = suggestion;
        }
    }
}
