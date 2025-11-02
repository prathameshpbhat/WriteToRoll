namespace App.Core.Models;

public abstract class ScriptElement
{
    public string Text { get; set; } = string.Empty;
    public FormattingMeta Formatting { get; set; } = new();
    public int LineNumber { get; set; }
}

public class FormattingMeta
{
    public string FontFamily { get; set; } = "Courier New";
    public bool ForcedUppercase { get; set; }
    public bool ContinuationFlag { get; set; }
}

public class Action : ScriptElement
{
}

public class Character : ScriptElement
{
    public List<string> AlternateNames { get; set; } = new();
    public int DialogueCount { get; set; }
    public string ColorTag { get; set; } = string.Empty;
}

public class Parenthetical : ScriptElement
{
}

public class Dialogue : ScriptElement
{
    public Character? Speaker { get; set; }
}

public class Transition : ScriptElement
{
}

public class Shot : ScriptElement
{
}

public class CenteredText : ScriptElement
{
}