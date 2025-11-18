using System;
using System.Collections.Generic;

namespace App.Core.Models
{
    public class FormattingMeta
    {
        public string LeftMargin { get; set; } = "1.5\"";
        public string RightMargin { get; set; } = "1.0\"";
        public string FontName { get; set; } = "Courier New";
        public int FontSize { get; set; } = 12;
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public bool IsUnderlined { get; set; }
        public string Alignment { get; set; } = "Left";
        public double LineHeight { get; set; } = 1.5;
    }

    public abstract class ScriptElement
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ScriptElementType ElementType { get; set; }
        public string Text { get; set; } = string.Empty;
        public int PageNumber { get; set; } = 1;
        public int LineNumber { get; set; }
        public int OrderIndex { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
        public string Notes { get; set; } = string.Empty;
        public FormattingMeta Formatting { get; set; } = new();
        public bool IsUserForced { get; set; }

        public abstract int GetLineCount();
        public abstract string GetFormattedOutput();
        public abstract ScriptElement Clone();
    }

    public class SceneHeadingElement : ScriptElement
    {
        public string Location { get; set; } = string.Empty;
        public string Time { get; set; } = "DAY";
        public bool IsInterior { get; set; } = true;
        public int? SceneNumber { get; set; }
        public List<string> Characters { get; set; } = new();

        public SceneHeadingElement()
        {
            ElementType = ScriptElementType.SceneHeading;
            Formatting.IsBold = true;
            Formatting.LeftMargin = "1.5\"";
        }

        public override int GetLineCount() => 1;
        public override string GetFormattedOutput()
        {
            string prefix = IsInterior ? "INT." : "EXT.";
            return $"{prefix} {Location} - {Time}".ToUpper();
        }
        public override ScriptElement Clone() => new SceneHeadingElement
        {
            Location = Location,
            Time = Time,
            IsInterior = IsInterior,
            SceneNumber = SceneNumber,
            Text = Text,
            Notes = Notes
        };
    }

    public class ActionElement : ScriptElement
    {
        public bool IsForcedAction { get; set; }
        public List<string> MentionedCharacters { get; set; } = new();

        public ActionElement()
        {
            ElementType = ScriptElementType.Action;
            Formatting.LeftMargin = "1.5\"";
            Formatting.RightMargin = "1.0\"";
        }

        public override int GetLineCount()
        {
            int words = Text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
            int estimatedLines = (int)Math.Ceiling(words / 10.0);
            return Math.Max(1, estimatedLines);
        }

        public override string GetFormattedOutput() => Text;
        public override ScriptElement Clone() => new ActionElement
        {
            Text = Text,
            IsForcedAction = IsForcedAction,
            MentionedCharacters = new(MentionedCharacters),
            Notes = Notes
        };
    }

    public class CharacterElement : ScriptElement
    {
        public string Name { get; set; } = string.Empty;
        public string Modifier { get; set; } = string.Empty;
        public bool IsForcedCharacter { get; set; }
        public List<string> AlternateNames { get; set; } = new();
        public int DialogueCount { get; set; }
        public int AppearanceCount { get; set; }
        public string ColorTag { get; set; } = string.Empty;

        public CharacterElement()
        {
            ElementType = ScriptElementType.Character;
            Formatting.Alignment = "Center";
            Formatting.LeftMargin = "3.5\"";
            Formatting.IsBold = true;
        }

        public override int GetLineCount() => 1;
        public override string GetFormattedOutput()
        {
            return string.IsNullOrEmpty(Modifier) ? Name.ToUpper() : $"{Name.ToUpper()} ({Modifier})";
        }
        public override ScriptElement Clone() => new CharacterElement
        {
            Name = Name,
            Modifier = Modifier,
            IsForcedCharacter = IsForcedCharacter,
            AlternateNames = new(AlternateNames),
            Notes = Notes
        };
    }

    public class DialogueElement : ScriptElement
    {
        public string SpeakerId { get; set; } = string.Empty;
        public string SpeakerName { get; set; } = string.Empty;
        public bool IsContinued { get; set; }
        public bool IsOffScreen { get; set; }
        public bool IsVoiceOver { get; set; }

        public DialogueElement()
        {
            ElementType = ScriptElementType.Dialogue;
            Formatting.LeftMargin = "2.5\"";
            Formatting.RightMargin = "1.5\"";
        }

        public override int GetLineCount() => Math.Max(1, Text.Split('\n').Length);
        public override string GetFormattedOutput() => Text;
        public override ScriptElement Clone() => new DialogueElement
        {
            SpeakerId = SpeakerId,
            SpeakerName = SpeakerName,
            IsContinued = IsContinued,
            IsOffScreen = IsOffScreen,
            IsVoiceOver = IsVoiceOver,
            Text = Text,
            Notes = Notes
        };
    }

    public class ParentheticalElement : ScriptElement
    {
        public ParentheticalElement()
        {
            ElementType = ScriptElementType.Parenthetical;
            Formatting.LeftMargin = "3.0\"";
            Formatting.RightMargin = "2.0\"";
            Formatting.IsItalic = true;
        }

        public override int GetLineCount() => 1;
        public override string GetFormattedOutput() => $"({Text})";
        public override ScriptElement Clone() => new ParentheticalElement { Text = Text, Notes = Notes };
    }

    public class TransitionElement : ScriptElement
    {
        public TransitionElement()
        {
            ElementType = ScriptElementType.Transition;
            Formatting.Alignment = "Right";
            Formatting.LeftMargin = "6.0\"";
            Formatting.RightMargin = "1.0\"";
            Formatting.IsBold = true;
        }

        public override int GetLineCount() => 1;
        public override string GetFormattedOutput()
        {
            string output = Text.ToUpper();
            if (!output.EndsWith(":")) output += ":";
            return output;
        }
        public override ScriptElement Clone() => new TransitionElement { Text = Text, Notes = Notes };
    }

    public class ShotElement : ScriptElement
    {
        public string ShotType { get; set; } = string.Empty;

        public ShotElement()
        {
            ElementType = ScriptElementType.Shot;
            Formatting.LeftMargin = "1.5\"";
        }

        public override int GetLineCount() => 1;
        public override string GetFormattedOutput() => $"{ShotType} {Text}".ToUpper().Trim();
        public override ScriptElement Clone() => new ShotElement
        {
            ShotType = ShotType,
            Text = Text,
            Notes = Notes
        };
    }

    public class CenteredTextElement : ScriptElement
    {
        public CenteredTextElement()
        {
            ElementType = ScriptElementType.CenteredText;
            Formatting.Alignment = "Center";
        }

        public override int GetLineCount() => 1;
        public override string GetFormattedOutput() => Text;
        public override ScriptElement Clone() => new CenteredTextElement { Text = Text, Notes = Notes };
    }

    public class SectionElement : ScriptElement
    {
        public int Level { get; set; } = 1;
        public string Title { get; set; } = string.Empty;

        public SectionElement()
        {
            ElementType = ScriptElementType.Section;
            Formatting.IsBold = true;
        }

        public override int GetLineCount() => 1;
        public override string GetFormattedOutput() => new string('#', Level) + " " + Title;
        public override ScriptElement Clone() => new SectionElement
        {
            Level = Level,
            Title = Title,
            Text = Text,
            Notes = Notes
        };
    }

    public class SynopsisElement : ScriptElement
    {
        public SynopsisElement()
        {
            ElementType = ScriptElementType.Synopsis;
        }

        public override int GetLineCount() => 0;
        public override string GetFormattedOutput() => string.Empty;
        public override ScriptElement Clone() => new SynopsisElement { Text = Text, Notes = Notes };
    }

    public class NoteElement : ScriptElement
    {
        public NoteElement()
        {
            ElementType = ScriptElementType.Note;
        }

        public override int GetLineCount() => 0;
        public override string GetFormattedOutput() => string.Empty;
        public override ScriptElement Clone() => new NoteElement { Text = Text, Notes = Notes };
    }

    public class DualDialogueElement : ScriptElement
    {
        public string LeftCharacter { get; set; } = string.Empty;
        public string RightCharacter { get; set; } = string.Empty;
        public string LeftDialogue { get; set; } = string.Empty;
        public string RightDialogue { get; set; } = string.Empty;

        public DualDialogueElement()
        {
            ElementType = ScriptElementType.DualDialogue;
            Formatting.LeftMargin = "1.5\"";
            Formatting.RightMargin = "1.5\"";
        }

        public override int GetLineCount()
        {
            int leftLines = LeftDialogue.Split('\n').Length;
            int rightLines = RightDialogue.Split('\n').Length;
            return Math.Max(leftLines, rightLines);
        }

        public override string GetFormattedOutput() => $"{LeftCharacter}|{RightCharacter}";
        public override ScriptElement Clone() => new DualDialogueElement
        {
            LeftCharacter = LeftCharacter,
            RightCharacter = RightCharacter,
            LeftDialogue = LeftDialogue,
            RightDialogue = RightDialogue,
            Notes = Notes
        };
    }

    public class PageBreakElement : ScriptElement
    {
        public PageBreakElement()
        {
            ElementType = ScriptElementType.PageBreak;
        }

        public override int GetLineCount() => 0;
        public override string GetFormattedOutput() => "===";
        public override ScriptElement Clone() => new PageBreakElement { Notes = Notes };
    }

    public class LyricLineElement : ScriptElement
    {
        public LyricLineElement()
        {
            ElementType = ScriptElementType.LyricLine;
            Formatting.LeftMargin = "1.5\"";
        }

        public override int GetLineCount() => 1;
        public override string GetFormattedOutput() => Text;
        public override ScriptElement Clone() => new LyricLineElement { Text = Text, Notes = Notes };
    }

    public class ExtendedNoteElement : ScriptElement
    {
        public string Author { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsResolved { get; set; }

        public ExtendedNoteElement()
        {
            ElementType = ScriptElementType.ExtendedNote;
        }

        public override int GetLineCount() => 0;
        public override string GetFormattedOutput() => string.Empty;
        public override ScriptElement Clone() => new ExtendedNoteElement
        {
            Author = Author,
            Category = Category,
            IsResolved = IsResolved,
            Text = Text,
            Notes = Notes
        };
    }

    public class TitlePageElement : ScriptElement
    {
        public string Title { get; set; } = string.Empty;
        public string Credit { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTime? DraftDate { get; set; }
        public string Contact { get; set; } = string.Empty;
        public string Copyright { get; set; } = string.Empty;

        public TitlePageElement()
        {
            ElementType = ScriptElementType.TitlePage;
        }

        public override int GetLineCount() => 0;
        public override string GetFormattedOutput() => Title;
        public override ScriptElement Clone() => new TitlePageElement
        {
            Title = Title,
            Credit = Credit,
            Author = Author,
            Source = Source,
            DraftDate = DraftDate,
            Contact = Contact,
            Copyright = Copyright,
            Notes = Notes
        };
    }
}
