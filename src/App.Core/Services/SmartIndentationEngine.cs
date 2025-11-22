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
            var profile = ScreenplayElementProfiles.GetProfile(elementType);
            return profile.LeftMarginInches;
        }

        /// <summary>
        /// Get right margin in inches for element type
        /// </summary>
        public double GetRightMarginInches(ScriptElementType elementType)
        {
            var profile = ScreenplayElementProfiles.GetProfile(elementType);
            return profile.RightMarginInches;
        }

        /// <summary>
        /// Get alignment for element type
        /// </summary>
        public string GetAlignment(ScriptElementType elementType)
        {
            var profile = ScreenplayElementProfiles.GetProfile(elementType);
            return profile.Alignment switch
            {
                App.Core.Models.ElementAlignment.Center => "CENTER",
                App.Core.Models.ElementAlignment.Right => "RIGHT",
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
