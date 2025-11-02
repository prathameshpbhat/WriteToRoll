using System.Text.RegularExpressions;
using System.Windows.Input;

namespace App.UI.Controls;

public class ScriptEditorKeyboardHandler
{
    private readonly ScriptEditor _editor;
    private static readonly Regex SceneHeadingRegex = new(@"^(INT\.|EXT\.|INT\/EXT\.)", RegexOptions.Compiled);
    private static readonly Regex CharacterRegex = new(@"^[A-Z\s]+$", RegexOptions.Compiled);
    private static readonly Regex TransitionRegex = new(@"^.*TO:$|^FADE (IN|OUT)$|^DISSOLVE TO:$", RegexOptions.Compiled);

    public ScriptEditorKeyboardHandler(ScriptEditor editor)
    {
        _editor = editor;
    }

    public bool HandleKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            return HandleEnterKey(e);
        }
        else if (e.Key == Key.Tab)
        {
            return HandleTabKey(e);
        }

        return false;
    }

    private bool HandleEnterKey(KeyEventArgs e)
    {
        var currentText = GetCurrentLineText();
        var currentType = DetectElementType(currentText);

        // Continue same element type for dialogue and action
        if (currentType == ScriptElementType.Dialogue || currentType == ScriptElementType.Action)
        {
            _editor.ApplyElementFormatting(currentType);
            return false; // Let the Enter keystroke through
        }

        // For other types, go back to Action
        _editor.ApplyElementFormatting(ScriptElementType.Action);
        return false;
    }

    private bool HandleTabKey(KeyEventArgs e)
    {
        var currentType = GetCurrentElementType();
        var nextType = GetNextElementType(currentType, e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift));
        
        _editor.ApplyElementFormatting(nextType);
        e.Handled = true;
        return true;
    }

    private string GetCurrentLineText()
    {
        // TODO: Get text of current paragraph
        return string.Empty;
    }

    private ScriptElementType DetectElementType(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ScriptElementType.Action;

        if (SceneHeadingRegex.IsMatch(text))
            return ScriptElementType.SceneHeading;

        if (CharacterRegex.IsMatch(text))
            return ScriptElementType.Character;

        if (TransitionRegex.IsMatch(text))
            return ScriptElementType.Transition;

        if (text.StartsWith("(") && text.EndsWith(")"))
            return ScriptElementType.Parenthetical;

        return ScriptElementType.Action;
    }

    private ScriptElementType GetCurrentElementType()
    {
        // TODO: Determine current element type based on formatting
        return ScriptElementType.Action;
    }

    private static ScriptElementType GetNextElementType(ScriptElementType current, bool reverse)
    {
        var types = new[]
        {
            ScriptElementType.SceneHeading,
            ScriptElementType.Action,
            ScriptElementType.Character,
            ScriptElementType.Parenthetical,
            ScriptElementType.Dialogue,
            ScriptElementType.Transition
        };

        var index = Array.IndexOf(types, current);
        if (index == -1) index = 0;

        if (reverse)
        {
            index--;
            if (index < 0) index = types.Length - 1;
        }
        else
        {
            index++;
            if (index >= types.Length) index = 0;
        }

        return types[index];
    }
}