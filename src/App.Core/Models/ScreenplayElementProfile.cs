using System;
using System.Collections.Generic;

namespace App.Core.Models
{
    public enum ElementCaseStyle
    {
        Preserve,
        Uppercase,
        Sentence,
        Lowercase
    }

    public class ScreenplayElementProfile
    {
        public ScriptElementType ElementType { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public ElementCaseStyle CaseStyle { get; init; } = ElementCaseStyle.Preserve;
        public double LeftMarginInches { get; init; }
        public double RightMarginInches { get; init; }
        public ElementAlignment Alignment { get; init; } = ElementAlignment.Left;
        public IReadOnlyList<ScriptElementType> PreferredNext { get; init; } = Array.Empty<ScriptElementType>();
        public IReadOnlyList<string> Examples { get; init; } = Array.Empty<string>();
        public string Notes { get; init; } = string.Empty;
    }

    public static class ScreenplayElementProfiles
    {
        private static readonly IReadOnlyDictionary<ScriptElementType, ScreenplayElementProfile> Profiles =
            new Dictionary<ScriptElementType, ScreenplayElementProfile>
            {
                [ScriptElementType.SceneHeading] = new()
                {
                    ElementType = ScriptElementType.SceneHeading,
                    DisplayName = "Slugline / Scene Heading",
                    CaseStyle = ElementCaseStyle.Uppercase,
                    LeftMarginInches = 1.0,
                    RightMarginInches = 1.0,
                    Alignment = ElementAlignment.Left,
                    PreferredNext = new[] { ScriptElementType.Action },
                    Examples = new[] { "INT. COFFEE SHOP - DAY" },
                    Notes = "INT./EXT. + LOCATION + TIME"
                },
                [ScriptElementType.Action] = new()
                {
                    ElementType = ScriptElementType.Action,
                    DisplayName = "Action / Description",
                    CaseStyle = ElementCaseStyle.Sentence,
                    LeftMarginInches = 1.0,
                    RightMarginInches = 1.0,
                    Alignment = ElementAlignment.Left,
                    PreferredNext = new[] { ScriptElementType.Action, ScriptElementType.Character, ScriptElementType.SceneHeading },
                    Examples = new[] { "John enters, soaked from the rain." },
                    Notes = "Visual description only"
                },
                [ScriptElementType.Character] = new()
                {
                    ElementType = ScriptElementType.Character,
                    DisplayName = "Character Name",
                    CaseStyle = ElementCaseStyle.Uppercase,
                    LeftMarginInches = 3.7,
                    RightMarginInches = 1.0,
                    Alignment = ElementAlignment.Center,
                    PreferredNext = new[] { ScriptElementType.Dialogue, ScriptElementType.Parenthetical },
                    Examples = new[] { "JOHN", "SARAH (O.S.)" },
                    Notes = "Label above dialogue"
                },
                [ScriptElementType.Parenthetical] = new()
                {
                    ElementType = ScriptElementType.Parenthetical,
                    DisplayName = "Parenthetical / Wryly",
                    CaseStyle = ElementCaseStyle.Lowercase,
                    LeftMarginInches = 3.1,
                    RightMarginInches = 2.4,
                    Alignment = ElementAlignment.Left,
                    PreferredNext = new[] { ScriptElementType.Dialogue },
                    Examples = new[] { "(whispering)", "(beat)" },
                    Notes = "Brief delivery cues"
                },
                [ScriptElementType.Dialogue] = new()
                {
                    ElementType = ScriptElementType.Dialogue,
                    DisplayName = "Dialogue",
                    CaseStyle = ElementCaseStyle.Sentence,
                    LeftMarginInches = 2.5,
                    RightMarginInches = 2.5,
                    Alignment = ElementAlignment.Left,
                    PreferredNext = new[] { ScriptElementType.Dialogue, ScriptElementType.Action, ScriptElementType.Character },
                    Examples = new[] { "I told you already, I'm not going back." },
                    Notes = "Spoken lines"
                },
                [ScriptElementType.Transition] = new()
                {
                    ElementType = ScriptElementType.Transition,
                    DisplayName = "Transition",
                    CaseStyle = ElementCaseStyle.Uppercase,
                    LeftMarginInches = 5.5,
                    RightMarginInches = 1.0,
                    Alignment = ElementAlignment.Right,
                    PreferredNext = new[] { ScriptElementType.SceneHeading },
                    Examples = new[] { "CUT TO:", "FADE OUT:" },
                    Notes = "Editing direction"
                },
                [ScriptElementType.Shot] = new()
                {
                    ElementType = ScriptElementType.Shot,
                    DisplayName = "Shot / Camera",
                    CaseStyle = ElementCaseStyle.Uppercase,
                    LeftMarginInches = 1.0,
                    RightMarginInches = 1.0,
                    Alignment = ElementAlignment.Left,
                    PreferredNext = new[] { ScriptElementType.Action },
                    Examples = new[] { "CLOSE ON JOHN'S HAND" },
                    Notes = "Use sparingly"
                },
                [ScriptElementType.CenteredText] = new()
                {
                    ElementType = ScriptElementType.CenteredText,
                    DisplayName = "Centered Text",
                    CaseStyle = ElementCaseStyle.Uppercase,
                    LeftMarginInches = 2.5,
                    RightMarginInches = 2.5,
                    Alignment = ElementAlignment.Center,
                    PreferredNext = new[] { ScriptElementType.Action },
                    Examples = new[] { "> TITLE <" },
                    Notes = "On-screen titles"
                }
            };

        public static ScreenplayElementProfile GetProfile(ScriptElementType type) =>
            Profiles.TryGetValue(type, out var profile) ? profile : Profiles[ScriptElementType.Action];
    }
}
