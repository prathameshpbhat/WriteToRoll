using System.Linq;
using System.Windows;
using System.Windows.Documents;
using App.Core.Models;

namespace App.UI.Controls;

public static class ScriptElementFormats
{
    private const double DPI = 96.0;
    private const double POINTS_TO_PIXELS = DPI / 72.0;
    private const double COURIER_CHAR_WIDTH = 7.2; // Courier at 12pt

    // Convert inches to pixels
    public static double InchesToPixels(double inches) => inches * DPI;
    
    // Convert character count to pixels for Courier 12pt
    public static double CharsToPixels(int chars) => chars * COURIER_CHAR_WIDTH;

    public static class Margins
    {
        
        // Standard margins
        public static readonly double TOP = InchesToPixels(1.0);
        public static readonly double BOTTOM = InchesToPixels(1.0);
        public static readonly double LEFT = InchesToPixels(1.5);
        public static readonly double RIGHT = InchesToPixels(1.0);
        
        // Element-specific left margins in pixels
        public static readonly double ACTION_LEFT = InchesToPixels(1.5);
        public static readonly double CHARACTER_LEFT = InchesToPixels(3.5);
        public static readonly double CHARACTER_RIGHT = InchesToPixels(1.0);
        public static readonly double PARENTHETICAL_LEFT = InchesToPixels(3.0);
        public static readonly double PARENTHETICAL_RIGHT = InchesToPixels(2.0);
        public static readonly double DIALOGUE_LEFT = InchesToPixels(2.5);
        public static readonly double DIALOGUE_RIGHT = InchesToPixels(1.5);
        public static readonly double TRANSITION_LEFT = CharsToPixels(50);    // Right aligned
    }

    public static class LineWidths
    {
        public static readonly double CHARACTER_WIDTH = CharsToPixels(15);
        public static readonly double DIALOGUE_WIDTH = CharsToPixels(35);
        public static readonly double PARENTHETICAL_WIDTH = CharsToPixels(25);
    }

    public static Thickness GetElementMargins(ScriptElementType elementType)
    {
        return elementType switch
        {
            ScriptElementType.SceneHeading => new Thickness(Margins.LEFT, 24, Margins.RIGHT, 12),
            ScriptElementType.Action => new Thickness(Margins.LEFT, 12, Margins.RIGHT, 12),
            ScriptElementType.Character => new Thickness(Margins.CHARACTER_LEFT, 24, Margins.CHARACTER_RIGHT, 0),
            ScriptElementType.Parenthetical => new Thickness(Margins.PARENTHETICAL_LEFT, 0, Margins.PARENTHETICAL_RIGHT, 0),
            ScriptElementType.Dialogue => new Thickness(Margins.DIALOGUE_LEFT, 0, Margins.DIALOGUE_RIGHT, 12),
            ScriptElementType.Transition => new Thickness(Margins.TRANSITION_LEFT, 24, Margins.RIGHT, 24),
            ScriptElementType.Shot => new Thickness(Margins.LEFT, 12, Margins.RIGHT, 12),
            _ => new Thickness(Margins.LEFT, 12, Margins.RIGHT, 12)
        };
    }

    private static readonly string[] TransitionWords = new[]
    {
        "FADE", "CUT", "DISSOLVE", "SMASH", "WIPE", "IRIS"
    };

    private static readonly string[] ShotWords = new[]
    {
        "ANGLE", "CLOSE", "WIDE", "SHOT", "POV", "VIEW"
    };

    public static bool DetectElementType(string text, out ScriptElementType elementType)
    {
        text = text.Trim();
        var upperText = text.ToUpper();
        
        // Scene Heading detection - INT./EXT./INT./EXT.
        if ((upperText.StartsWith("INT.") || upperText.StartsWith("EXT.") || 
             upperText.StartsWith("INT./EXT.") || upperText.StartsWith("EXT./INT.")) &&
            !upperText.StartsWith("INTO") && !upperText.StartsWith("INTERIOR"))
        {
            elementType = ScriptElementType.SceneHeading;
            return true;
        }
        
        // Transition detection - FADE IN/OUT, CUT TO:, etc.
        if (upperText == text && text.EndsWith(":") && text.Length <= 25 &&
            TransitionWords.Any(w => upperText.StartsWith(w)))
        {
            elementType = ScriptElementType.Transition;
            return true;
        }
        
        // Parenthetical - (beat), (pause), (angry), etc.
        if (text.StartsWith("(") && text.EndsWith(")"))
        {
            elementType = ScriptElementType.Parenthetical;
            return true;
        }
        
        // Shot detection - ANGLE ON:, CLOSE UP:, etc.
        if (text.EndsWith(":") && upperText == text && text.Length <= 20 &&
            ShotWords.Any(w => upperText.Contains(w)))
        {
            elementType = ScriptElementType.Shot;
            return true;
        }
        
        // Character detection (all caps, 2-30 chars, no punctuation except O.S./V.O./CONT'D, not a transition)
        if (upperText == text && text.Length >= 2 && text.Length <= 30 && 
            !text.Contains(":") && !TransitionWords.Any(w => upperText.StartsWith(w)) &&
            !ShotWords.Any(w => upperText.Contains(w)) && !upperText.StartsWith("EXTRA"))
        {
            elementType = ScriptElementType.Character;
            return true;
        }

        // Default to Action
        elementType = ScriptElementType.Action;
        return false;
    }
}