using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace App.UI.Controls
{
    public class ScriptEditor : RichTextBox
    {
    private ScriptElementType _currentElementType = ScriptElementType.Action;
    private bool _isHandlingTextChange = false;
    private bool _isUndoRedoing = false;
    private bool _isFormatting = false;

    static ScriptEditor()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ScriptEditor), 
            new FrameworkPropertyMetadata(typeof(ScriptEditor)));
    }

    private void UpdateUndoRedoState()
    {
        _isUndoRedoing = true;
        try
        {
            // Reapply formatting after undo/redo
            if (CaretPosition?.Paragraph != null)
            {
                var text = new TextRange(CaretPosition.Paragraph.ContentStart, CaretPosition.Paragraph.ContentEnd).Text;
                if (ScriptElementFormats.DetectElementType(text, out var detectedType))
                {
                    _currentElementType = detectedType;
                    ApplyElementFormatting(detectedType);
                }
            }
        }
        finally
        {
            _isUndoRedoing = false;
        }
    }

    private ScriptElementType GetCurrentElementType()
    {
        var caretPosition = CaretPosition;
        var paragraph = caretPosition?.Paragraph;
        
        if (paragraph == null)
            return ScriptElementType.Action;

        // Try to detect from content
        var text = new TextRange(paragraph.ContentStart, paragraph.ContentEnd).Text;
        if (ScriptElementFormats.DetectElementType(text, out var detectedType))
            return detectedType;

        // If not detected from content, use the current type
        return _currentElementType;
    }

    public ScriptEditor()
    {
        try
        {
            // Configure screenplay formatting defaults
            AcceptsReturn = true;
            AcceptsTab = true;
            SpellCheck.IsEnabled = true;
            FontFamily = new System.Windows.Media.FontFamily("Courier New");
            FontSize = 12;
            Padding = new Thickness(0);
            
            // Enable undo/redo
            IsUndoEnabled = true;
            UndoLimit = 100;
            
            // Set page margins
            Document.PagePadding = new Thickness(
                ScriptElementFormats.Margins.LEFT,
                ScriptElementFormats.Margins.TOP,
                ScriptElementFormats.Margins.RIGHT,
                ScriptElementFormats.Margins.BOTTOM
            );
            
            // Initialize with action element
            _currentElementType = ScriptElementType.Action;
            
            // Handle key events for smart formatting
            PreviewKeyDown += ScriptEditor_PreviewKeyDown;
            TextChanged += ScriptEditor_TextChanged;

            // Set up accessibility
            AccessibilityHelper.SetupAccessibility(
                this, 
                "Screenplay Editor", 
                "Edit your screenplay with professional formatting. Use Tab to cycle between element types."
            );
            AccessibilityHelper.SetLiveRegion(this, true);

            // Add error handling
            Dispatcher.UnhandledException += (s, e) =>
            {
                MessageBox.Show($"An error occurred: {e.Exception.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error initializing editor: {ex.Message}", "Initialization Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ScriptEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isHandlingTextChange || _isUndoRedoing || _isFormatting) return;

        try
        {
            _isHandlingTextChange = true;
            _isFormatting = true;

            var caretPosition = CaretPosition;
            if (caretPosition?.Paragraph == null) return;

            var currentParagraph = caretPosition.Paragraph;
            var text = new TextRange(currentParagraph.ContentStart, currentParagraph.ContentEnd).Text.TrimEnd();
            
            // Prevent processing empty text
            if (string.IsNullOrWhiteSpace(text)) return;
            
            // Get caret offset before any changes
            var caretOffset = caretPosition.GetOffsetToPosition(currentParagraph.ContentEnd);
            
            // Special handling for INT/EXT while typing
            var upperText = text.ToUpper();
            if (upperText == "INT" || upperText == "EXT" || upperText.StartsWith("INT.") || upperText.StartsWith("EXT."))
            {
                if (_currentElementType != ScriptElementType.SceneHeading)
                {
                    _currentElementType = ScriptElementType.SceneHeading;
                    ApplyElementFormatting(ScriptElementType.SceneHeading);
                }
                
                // Convert to uppercase only if needed
                if (text != upperText)
                {
                    var range = new TextRange(currentParagraph.ContentStart, currentParagraph.ContentEnd);
                    range.Text = upperText;
                    
                    // Restore caret position
                    var newCaretPos = currentParagraph.ContentStart.GetPositionAtOffset(caretOffset);
                    if (newCaretPos != null)
                    {
                        Selection.Select(newCaretPos, newCaretPos);
                    }
                }
                return;
            }

            // Auto-detect element type from content
            if (ScriptElementFormats.DetectElementType(text, out var detectedType))
            {
                if (_currentElementType != detectedType)
                {
                    _currentElementType = detectedType;
                    ApplyElementFormatting(detectedType);
                    
                    // Convert to uppercase for scene headings
                    if (detectedType == ScriptElementType.SceneHeading && text != upperText)
                    {
                        var range = new TextRange(currentParagraph.ContentStart, currentParagraph.ContentEnd);
                        range.Text = upperText;
                        
                        // Restore caret position
                        var newCaretPos = currentParagraph.ContentStart.GetPositionAtOffset(caretOffset);
                        if (newCaretPos != null)
                        {
                            Selection.Select(newCaretPos, newCaretPos);
                        }
                    }
                    
                    // Announce changes for screen readers
                    AccessibilityHelper.AnnounceScreenReaderMessage($"Current element: {detectedType}");
                }
            }
        }
        finally
        {
            _isHandlingTextChange = false;
            _isFormatting = false;
        }
    }

    private void ScriptEditor_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case System.Windows.Input.Key.Enter:
                HandleEnterKey(e);
                break;
            case System.Windows.Input.Key.Tab:
                HandleTabKey(e);
                break;
        }
    }

    private void HandleEnterKey(System.Windows.Input.KeyEventArgs e)
    {
        var currentParagraph = CaretPosition?.Paragraph;
        if (currentParagraph == null) return;

        // Get the current text
        var text = new TextRange(currentParagraph.ContentStart, currentParagraph.ContentEnd).Text.Trim();

        ScriptElementType nextType = _currentElementType;

        // Determine next element type based on current
        switch (_currentElementType)
        {
            case ScriptElementType.SceneHeading:
                nextType = ScriptElementType.Action;
                break;
            case ScriptElementType.Character:
                nextType = ScriptElementType.Dialogue;
                break;
            case ScriptElementType.Dialogue:
                nextType = ScriptElementType.Character;
                break;
            case ScriptElementType.Parenthetical:
                nextType = ScriptElementType.Dialogue;
                break;
            case ScriptElementType.Transition:
                nextType = ScriptElementType.SceneHeading;
                break;
            // Default to Action for other types
            default:
                nextType = ScriptElementType.Action;
                break;
        }

        _currentElementType = nextType;
        e.Handled = true;
    }

    private void HandleTabKey(System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;

        // Cycle through element types
        _currentElementType = e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift) 
            ? GetPreviousElementType(_currentElementType)
            : GetNextElementType(_currentElementType);

        ApplyElementFormatting(_currentElementType);
    }

    private ScriptElementType GetNextElementType(ScriptElementType current)
    {
        return current switch
        {
            ScriptElementType.Action => ScriptElementType.SceneHeading,
            ScriptElementType.SceneHeading => ScriptElementType.Character,
            ScriptElementType.Character => ScriptElementType.Parenthetical,
            ScriptElementType.Parenthetical => ScriptElementType.Dialogue,
            ScriptElementType.Dialogue => ScriptElementType.Transition,
            ScriptElementType.Transition => ScriptElementType.Shot,
            ScriptElementType.Shot => ScriptElementType.Action,
            _ => ScriptElementType.Action
        };
    }

    private ScriptElementType GetPreviousElementType(ScriptElementType current)
    {
        return current switch
        {
            ScriptElementType.Action => ScriptElementType.Shot,
            ScriptElementType.Shot => ScriptElementType.Transition,
            ScriptElementType.Transition => ScriptElementType.Dialogue,
            ScriptElementType.Dialogue => ScriptElementType.Parenthetical,
            ScriptElementType.Parenthetical => ScriptElementType.Character,
            ScriptElementType.Character => ScriptElementType.SceneHeading,
            ScriptElementType.SceneHeading => ScriptElementType.Action,
            _ => ScriptElementType.Action
        };
    }

    public void ApplyElementFormatting(ScriptElementType elementType)
    {
        // Get screenplay margins for this element type
        var margins = GetElementMargins(elementType);

        // Create paragraph formatting
        var paraFormat = new Paragraph
        {
            TextAlignment = GetElementAlignment(elementType),
            Margin = margins
        };

        // Apply formatting to selection or current paragraph
        var selection = Selection;
        if (selection.IsEmpty)
        {
            var caretPosition = CaretPosition;
            var currentBlock = caretPosition.Paragraph;
            if (currentBlock != null)
            {
                // Replace current paragraph formatting
                var replacementPara = new Paragraph();
                replacementPara.Inlines.AddRange(currentBlock.Inlines.ToList());
                replacementPara.TextAlignment = paraFormat.TextAlignment;
                replacementPara.Margin = paraFormat.Margin;
                
                var doc = Document;
                doc.Blocks.InsertBefore(currentBlock, replacementPara);
                doc.Blocks.Remove(currentBlock);
                
                CaretPosition = replacementPara.ContentStart;
            }
        }
        else
        {
            // Apply formatting to selection
            // TODO: Handle multi-paragraph selections
        }
    }

    private static Thickness GetElementMargins(ScriptElementType elementType)
    {
        // Standard screenplay margins in pixels (at 96 DPI)
        return elementType switch
        {
            ScriptElementType.SceneHeading => new Thickness(160, 12, 160, 0),  // 1.5" left, 1.5" right
            ScriptElementType.Action => new Thickness(160, 12, 160, 0),        // 1.5" left, 1.5" right
            ScriptElementType.Character => new Thickness(400, 12, 160, 0),     // 3.7" left, 1.5" right
            ScriptElementType.Parenthetical => new Thickness(320, 0, 320, 0),  // 3.0" left, 3.0" right
            ScriptElementType.Dialogue => new Thickness(250, 0, 250, 12),      // 2.3" left, 2.3" right
            ScriptElementType.Transition => new Thickness(480, 12, 160, 12),   // 4.5" left, 1.5" right
            ScriptElementType.Shot => new Thickness(160, 12, 160, 12),         // 1.5" left, 1.5" right
            ScriptElementType.CenteredText => new Thickness(160, 12, 160, 12), // 1.5" margins, centered
            _ => new Thickness(160, 0, 160, 0)                                 // Default margins
        };
    }

    private static TextAlignment GetElementAlignment(ScriptElementType elementType)
    {
        return elementType switch
        {
            ScriptElementType.CenteredText => TextAlignment.Center,
            ScriptElementType.Transition => TextAlignment.Right,
            _ => TextAlignment.Left
        };
    }
}

public enum ScriptElementType
{
    SceneHeading,
    Action,
    Character,
    Parenthetical,
    Dialogue,
    Transition,
    Shot,
    CenteredText
}
}