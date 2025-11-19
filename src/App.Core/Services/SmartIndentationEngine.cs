using System;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Smart indentation based on screenplay element types
    /// Provides proper margins and spacing per industry standards
    /// </summary>
    public class SmartIndentationEngine
    {
        private const int SPACES_PER_INCH = 10;

        /// <summary>
        /// Get indentation string (spaces) for element type
        /// </summary>
        public string GetIndentation(ScriptElementType elementType)
        {
            double marginInches = GetLeftMarginInches(elementType);
            int spaces = (int)(marginInches * SPACES_PER_INCH);
            return new string(' ', spaces);
        }

        /// <summary>
        /// Get left margin in inches for element type
        /// </summary>
        public double GetLeftMarginInches(ScriptElementType elementType)
        {
            return elementType switch
            {
                ScriptElementType.SceneHeading => 1.5,
                ScriptElementType.Action => 1.5,
                ScriptElementType.Character => 3.5,
                ScriptElementType.Dialogue => 2.5,
                ScriptElementType.Parenthetical => 3.0,
                ScriptElementType.Transition => 2.0,
                ScriptElementType.Shot => 1.5,
                ScriptElementType.CenteredText => 0.0,
                _ => 1.5
            };
        }

        /// <summary>
        /// Get right margin in inches for element type
        /// </summary>
        public double GetRightMarginInches(ScriptElementType elementType)
        {
            return elementType switch
            {
                ScriptElementType.SceneHeading => 1.0,
                ScriptElementType.Action => 1.0,
                ScriptElementType.Character => 1.0,
                ScriptElementType.Dialogue => 1.5,
                ScriptElementType.Parenthetical => 2.0,
                ScriptElementType.Transition => 1.0,
                ScriptElementType.Shot => 1.0,
                ScriptElementType.CenteredText => 2.5,
                _ => 1.0
            };
        }

        /// <summary>
        /// Get alignment for element type
        /// </summary>
        public string GetAlignment(ScriptElementType elementType)
        {
            return elementType switch
            {
                ScriptElementType.Character => "CENTER",
                ScriptElementType.Transition => "RIGHT",
                ScriptElementType.CenteredText => "CENTER",
                _ => "LEFT"
            };
        }

        /// <summary>
        /// Get auto-indent for new line based on previous element type
        /// </summary>
        public string GetAutoIndentForNewLine(ScriptElementType? previousElementType)
        {
            if (previousElementType == null)
                return string.Empty;

            if (previousElementType == ScriptElementType.SceneHeading)
                return GetIndentation(ScriptElementType.Action);

            if (previousElementType == ScriptElementType.Character)
                return GetIndentation(ScriptElementType.Dialogue);

            if (previousElementType == ScriptElementType.Action)
                return GetIndentation(ScriptElementType.Action);

            return string.Empty;
        }
    }
}
