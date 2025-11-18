using System;
using System.Collections.Generic;
using System.Linq;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Provides smart indentation based on screenplay rules
    /// Handles margin calculations and visual alignment
    /// </summary>
    public class SmartIndentationService
    {
        private const double DPI = 96.0;
        private const double POINTS_TO_PIXELS = DPI / 72.0;
        private const double COURIER_CHAR_WIDTH = 7.2; // Courier New 12pt average char width

        /// <summary>
        /// Get pixel-based indentation for element type
        /// </summary>
        public int GetPixelIndentation(ScriptElementType elementType)
        {
            var margins = GetElementMargins(elementType);
            return (int)InchesToPixels(margins.LeftMarginInches);
        }

        /// <summary>
        /// Get space-based indentation (for fixed-width display)
        /// </summary>
        public int GetSpaceIndentation(ScriptElementType elementType)
        {
            var margins = GetElementMargins(elementType);
            return (int)(margins.LeftMarginInches * 10); // 10 characters per inch for Courier
        }

        /// <summary>
        /// Get indentation string for element type
        /// </summary>
        public string GetIndentationString(ScriptElementType elementType)
        {
            int spaces = GetSpaceIndentation(elementType);
            return new string(' ', Math.Max(0, spaces));
        }

        /// <summary>
        /// Calculate right margin constraint for text wrapping
        /// </summary>
        public int GetLineWidth(ScriptElementType elementType)
        {
            var margins = GetElementMargins(elementType);
            double pageWidth = 8.5; // Standard letter width
            double usableWidth = pageWidth - margins.LeftMarginInches - margins.RightMarginInches;
            return (int)(usableWidth * 10); // Convert to character count for Courier
        }

        /// <summary>
        /// Get complete margin and alignment info for element type
        /// </summary>
        public ElementMargins GetElementMargins(ScriptElementType elementType)
        {
            return elementType switch
            {
                ScriptElementType.SceneHeading => new ElementMargins
                {
                    LeftMarginInches = 1.5,
                    RightMarginInches = 1.0,
                    Alignment = ElementAlignment.Left,
                    Description = "Scene Heading"
                },

                ScriptElementType.Action => new ElementMargins
                {
                    LeftMarginInches = 1.5,
                    RightMarginInches = 1.0,
                    Alignment = ElementAlignment.Left,
                    Description = "Action"
                },

                ScriptElementType.Character => new ElementMargins
                {
                    LeftMarginInches = 3.5,
                    RightMarginInches = 1.0,
                    Alignment = ElementAlignment.Center,
                    Description = "Character"
                },

                ScriptElementType.Dialogue => new ElementMargins
                {
                    LeftMarginInches = 2.5,
                    RightMarginInches = 1.5,
                    Alignment = ElementAlignment.Left,
                    Description = "Dialogue"
                },

                ScriptElementType.Parenthetical => new ElementMargins
                {
                    LeftMarginInches = 3.0,
                    RightMarginInches = 2.0,
                    Alignment = ElementAlignment.Left,
                    Description = "Parenthetical"
                },

                ScriptElementType.Transition => new ElementMargins
                {
                    LeftMarginInches = 6.0,
                    RightMarginInches = 1.0,
                    Alignment = ElementAlignment.Right,
                    Description = "Transition"
                },

                ScriptElementType.Shot => new ElementMargins
                {
                    LeftMarginInches = 1.5,
                    RightMarginInches = 1.0,
                    Alignment = ElementAlignment.Left,
                    Description = "Shot"
                },

                ScriptElementType.CenteredText => new ElementMargins
                {
                    LeftMarginInches = 2.5,
                    RightMarginInches = 2.5,
                    Alignment = ElementAlignment.Center,
                    Description = "Centered Text"
                },

                _ => new ElementMargins
                {
                    LeftMarginInches = 1.5,
                    RightMarginInches = 1.0,
                    Alignment = ElementAlignment.Left,
                    Description = "Default"
                }
            };
        }

        /// <summary>
        /// Apply proper indentation and alignment to text for display
        /// </summary>
        public string ApplyIndentationForDisplay(string text, ScriptElementType elementType, int displayWidth = 80)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var margins = GetElementMargins(elementType);
            string indent = GetIndentationString(elementType);

            return margins.Alignment switch
            {
                ElementAlignment.Center => CenterText(text, displayWidth, indent),
                ElementAlignment.Right => RightAlignText(text, displayWidth, indent),
                _ => indent + text
            };
        }

        /// <summary>
        /// Get character indentation (for auto-insert after scene heading)
        /// </summary>
        public string GetCharacterIndentationHint()
        {
            return "TAB or just start typing (auto-indent enabled)";
        }

        /// <summary>
        /// Smart indentation on newline based on previous element type
        /// </summary>
        public string GetAutoIndentForNewLine(ScriptElementType? previousElementType)
        {
            if (previousElementType == null)
                return string.Empty;

            // After Scene Heading, indent slightly for readability
            if (previousElementType == ScriptElementType.SceneHeading)
                return GetIndentationString(ScriptElementType.Action);

            // After Action, no extra indent (stays at action margin)
            if (previousElementType == ScriptElementType.Action)
                return GetIndentationString(ScriptElementType.Action);

            // After Character, indent for Dialogue
            if (previousElementType == ScriptElementType.Character)
                return GetIndentationString(ScriptElementType.Dialogue);

            // After Dialogue, possible parenthetical or new action
            if (previousElementType == ScriptElementType.Dialogue)
                return string.Empty; // User chooses action or parenthetical

            return string.Empty;
        }

        private string CenterText(string text, int width, string baseIndent = "")
        {
            int indentSpaces = baseIndent.Length;
            int availableWidth = width - indentSpaces;
            int contentWidth = Math.Min(text.Length, availableWidth);
            int padding = Math.Max(0, (availableWidth - contentWidth) / 2);

            return baseIndent + new string(' ', padding) + text;
        }

        private string RightAlignText(string text, int width, string baseIndent = "")
        {
            int indentSpaces = baseIndent.Length;
            int availableWidth = width - indentSpaces;
            int padding = Math.Max(0, availableWidth - text.Length);

            return new string(' ', padding) + text;
        }

        private double InchesToPixels(double inches)
        {
            return inches * DPI;
        }
    }

    public enum ElementAlignment
    {
        Left,
        Center,
        Right
    }

    public class ElementMargins
    {
        public double LeftMarginInches { get; set; }
        public double RightMarginInches { get; set; }
        public ElementAlignment Alignment { get; set; }
        public string Description { get; set; } = string.Empty;

        public override string ToString() =>
            $"{Description}: L={LeftMarginInches}\" R={RightMarginInches}\" {Alignment}";
    }
}
