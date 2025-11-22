using System;
using System.Text.RegularExpressions;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Real-time auto-formatting engine - handles formatting as user types
    /// Implements all user-friendly auto-formatting from screenwriting_logic_expanded.txt
    /// </summary>
    public class AutoFormattingEngine
    {
        private readonly IScreenwritingLogic _logic;

        public AutoFormattingEngine(IScreenwritingLogic logic)
        {
            _logic = logic;
        }

        /// <summary>
        /// Format text in real-time as user types each character
        /// Includes proper margins on both sides per screenplay standards
        /// </summary>
        public AutoFormatResult FormatAsYouType(string input)
        {
            if (string.IsNullOrEmpty(input))
                return new AutoFormatResult(input, 0, false);

            var trimmed = input.Trim();

            // SCENE HEADINGS: 1.0" left, 1.0" right
            if (input.Equals("INT", StringComparison.OrdinalIgnoreCase))
                return new AutoFormatResult("INT. ", 5, true, ConvertInchesToPixels(1.0), ConvertInchesToPixels(1.0));

            if (input.Equals("EXT", StringComparison.OrdinalIgnoreCase))
                return new AutoFormatResult("EXT. ", 5, true, ConvertInchesToPixels(1.0), ConvertInchesToPixels(1.0));

            if (input.Equals("INT/", StringComparison.OrdinalIgnoreCase))
                return new AutoFormatResult("INT./EXT. ", 10, true, ConvertInchesToPixels(1.0), ConvertInchesToPixels(1.0));

            if (input.Equals("EXT/", StringComparison.OrdinalIgnoreCase))
                return new AutoFormatResult("EXT./INT. ", 10, true, ConvertInchesToPixels(1.0), ConvertInchesToPixels(1.0));

            // PARENTHETICAL: 3.1" left, 2.4" right
            if (input.StartsWith("(") && !input.EndsWith(")") && input.Length > 1)
                return new AutoFormatResult(input + ")", input.Length, true, ConvertInchesToPixels(3.1), ConvertInchesToPixels(2.4));

            // QUOTED TEXT: reuse dialogue margins 2.5" both sides
            if (input.StartsWith("\"") && !input.EndsWith("\"") && input.Length > 1)
                return new AutoFormatResult(input + "\"", input.Length, true, ConvertInchesToPixels(2.5), ConvertInchesToPixels(2.5));

            // CENTERED TEXT: 2.5" both sides (equal margins = centered)
            if (input.StartsWith(">") && !input.EndsWith("<") && input.Length > 1)
                return new AutoFormatResult(input + " <", input.Length, true, ConvertInchesToPixels(2.5), ConvertInchesToPixels(2.5));

            // TRANSITIONS: 6.0" left (right-aligned), 1.0" right
            if (Regex.IsMatch(input, @"^FADE\s+(IN|OUT)$", RegexOptions.IgnoreCase) && !input.EndsWith(":"))
                return new AutoFormatResult(input.ToUpper() + ":", input.Length, true, ConvertInchesToPixels(6.0), ConvertInchesToPixels(1.0));

            if (Regex.IsMatch(input, @"^CUT\s+TO$", RegexOptions.IgnoreCase) && !input.EndsWith(":"))
                return new AutoFormatResult(input.ToUpper() + ":", input.Length, true, ConvertInchesToPixels(6.0), ConvertInchesToPixels(1.0));

            if (Regex.IsMatch(input, @"^DISSOLVE\s+TO$", RegexOptions.IgnoreCase) && !input.EndsWith(":"))
                return new AutoFormatResult(input.ToUpper() + ":", input.Length, true, ConvertInchesToPixels(6.0), ConvertInchesToPixels(1.0));

            // CHARACTER MODIFIERS: align to character block 3.7" left, 1.0" right
            var normalized = NormalizeCharacterModifiers(input);
            if (normalized != input)
                return new AutoFormatResult(normalized, normalized.Length, true, ConvertInchesToPixels(3.7), ConvertInchesToPixels(1.0));

            return new AutoFormatResult(input, input.Length, false);
        }

        /// <summary>
        /// Convert inches to pixel values for margin calculation
        /// Standard: 1 inch = 100 pixels in WPF
        /// </summary>
        private int ConvertInchesToPixels(double inches)
        {
            return (int)(inches * 100);
        }

        /// <summary>
        /// Format complete line when user presses Enter with proper margins
        /// </summary>
        public AutoFormatResult FormatOnEnter(string input, ScriptElementType? previousType)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new AutoFormatResult(input, 0, false);

            var context = new ScriptContext(previousType, true);
            var result = _logic.DetectAndNormalize(input.Trim(), context);

            var profile = ScreenplayElementProfiles.GetProfile(result.ElementType);
            var normalizedText = ApplyCaseStyle(result.Text, profile.CaseStyle);
            var (leftMargin, rightMargin) = GetMarginsForElementType(result.ElementType);

            return new AutoFormatResult(
                normalizedText,
                normalizedText.Length,
                result.TextWasChanged,
                leftMargin,
                rightMargin
            );
        }

        /// <summary>
        /// Get proper margins for each screenplay element type
        /// Based on screenwriting_logic_expanded.txt specifications
        /// </summary>
        private (int leftMargin, int rightMargin) GetMarginsForElementType(ScriptElementType type)
        {
            var profile = ScreenplayElementProfiles.GetProfile(type);
            return (
                ConvertInchesToPixels(profile.LeftMarginInches),
                ConvertInchesToPixels(profile.RightMarginInches));
        }

        /// <summary>
        /// Normalize character modifiers (V.O., O.S., CONT'D, O.C.)
        /// </summary>
        private string NormalizeCharacterModifiers(string input)
        {
            var upper = input.ToUpperInvariant();

            // V.O. variations
            upper = Regex.Replace(upper, @"\s+(VO|V\.O|V O)\s*$", " V.O.", RegexOptions.IgnoreCase);
            upper = Regex.Replace(upper, @"\s+(VO|V\.O|V O)(?=\s|$)", " V.O.", RegexOptions.IgnoreCase);

            // O.S. variations
            upper = Regex.Replace(upper, @"\s+(OS|O\.S|O S)\s*$", " O.S.", RegexOptions.IgnoreCase);
            upper = Regex.Replace(upper, @"\s+(OS|O\.S|O S)(?=\s|$)", " O.S.", RegexOptions.IgnoreCase);

            // CONT'D variations
            upper = Regex.Replace(upper, @"\s+(CONTD|CONT\'D|CONT D)\s*$", " CONT'D", RegexOptions.IgnoreCase);
            upper = Regex.Replace(upper, @"\s+(CONTD|CONT\'D|CONT D)(?=\s|$)", " CONT'D", RegexOptions.IgnoreCase);

            // O.C. variations
            upper = Regex.Replace(upper, @"\s+(OC|O\.C|O C)\s*$", " O.C.", RegexOptions.IgnoreCase);
            upper = Regex.Replace(upper, @"\s+(OC|O\.C|O C)(?=\s|$)", " O.C.", RegexOptions.IgnoreCase);

            return upper != input.ToUpperInvariant() ? upper : input;
        }

        private string ApplyCaseStyle(string text, ElementCaseStyle caseStyle)
        {
            return caseStyle switch
            {
                ElementCaseStyle.Uppercase => text.ToUpperInvariant(),
                ElementCaseStyle.Lowercase => text.ToLowerInvariant(),
                ElementCaseStyle.Sentence => ToSentenceCase(text),
                _ => text
            };
        }

        private static string ToSentenceCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            var trimmed = input.Trim();
            return char.ToUpperInvariant(trimmed[0]) + (trimmed.Length > 1 ? trimmed.Substring(1) : string.Empty);
        }
    }

    public class AutoFormatResult
    {
        public string FormattedText { get; set; }
        public int CaretPosition { get; set; }
        public bool WasChanged { get; set; }
        public int LeftMargin { get; set; }
        public int RightMargin { get; set; }

        public AutoFormatResult(string text, int caret, bool changed, int leftMargin = 0, int rightMargin = 0)
        {
            FormattedText = text;
            CaretPosition = caret;
            WasChanged = changed;
            LeftMargin = leftMargin;
            RightMargin = rightMargin;
        }
    }
}
