using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using App.Core.Models;
using App.Core.Services;
using App.Persistence.Services;

namespace ScriptWriter
{
    public partial class MainWindow : Window
    {
        private IScreenwritingLogic _logic;
        private AutoFormattingEngine _autoFormatter;
        private SmartIndentationEngine _indentation;
        private IFountainParser _fountainParser;

        private Script _currentScript;
        private bool _isUpdatingUI = false;
        private List<string> _knownCharacters = new();

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                _logic = new ScreenwritingLogic();
                _autoFormatter = new AutoFormattingEngine(_logic);
                _indentation = new SmartIndentationEngine();
                _fountainParser = new FountainParser();
                _currentScript = new Script();

                Title = "ScriptWriter Pro - Untitled";
                ScriptEditor.Focus();
                StatusText.Text = "✓ Ready";
                CurrentElementText.Text = "📝 Action";
                UpdateLineNumbers();

                // Wire up auto-complete dropdown
                AutoCompleteDropdown.ItemSelected += AutoCompleteDropdown_ItemSelected;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        /// <summary>
        /// Main TextChanged event - triggers real-time auto-formatting with margins
        /// </summary>
        private void ScriptEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUI) return;

            try
            {
                _isUpdatingUI = true;

                string currentLine = GetCurrentLine();
                string trimmed = currentLine.Trim();

                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    UpdateLineNumbers();
                    UpdateStatistics();
                    _isUpdatingUI = false;
                    return;
                }

                // Skip auto-formatting for INT/EXT/FADE/CUT - let dropdown handle it
                string upper = trimmed.ToUpperInvariant();
                if (!(upper.StartsWith("INT") || upper.StartsWith("EXT") || 
                      upper.Contains("FADE") || upper.Contains("CUT") || 
                      upper.Contains("DISSOLVE") || upper.Contains("SMASH") || 
                      upper.Contains("MATCH") || upper.Contains("WIPE") ||
                      upper.Contains("IRIS") || upper.Contains("FLASH") || 
                      upper.Contains("BACK") || upper.Contains("MONTAGE")))
                {
                    // Apply auto-formatting only for non-dropdown items
                    var formatResult = _autoFormatter.FormatAsYouType(trimmed);

                    if (formatResult.WasChanged)
                    {
                        int lineStartIndex = GetCurrentLineStartIndex();
                        int lineLength = GetCurrentLineLength();

                        if (lineLength > 0)
                        {
                            ScriptEditor.Select(lineStartIndex, lineLength);
                            ScriptEditor.SelectedText = formatResult.FormattedText;
                        }

                        int newCaretIndex = lineStartIndex + formatResult.CaretPosition;
                        ScriptEditor.CaretIndex = Math.Min(newCaretIndex, ScriptEditor.Text.Length);

                        StatusText.Text = $"✓ {formatResult.FormattedText.Substring(0, Math.Min(40, formatResult.FormattedText.Length))}";
                    }
                }

                UpdateElementTypeDisplay(trimmed);
                UpdateLineNumbers();
                UpdateStatistics();

                // Show auto-complete dropdown while typing
                if (!string.IsNullOrEmpty(trimmed) && trimmed.Length > 2)
                {
                    CheckAutoCompleteActivation(trimmed);
                }
                else
                {
                    AutoCompleteDropdown.Hide();
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
            finally
            {
                _isUpdatingUI = false;
            }
        }

        private void CheckAutoCompleteActivation(string currentLine)
        {
            var upper = currentLine.ToUpperInvariant();
            
            // Check if this is the first line (slugline/transition line)
            int currentLineIndex = GetCurrentLineNumber();
            bool isFirstLine = currentLineIndex == 1;
            
            // Only show slugline or transition suggestions on first line
            if (!isFirstLine)
            {
                AutoCompleteDropdown.Hide();
                return;
            }

            // Show slugline auto-complete for INT/EXT (check this first)
            if ((upper.StartsWith("INT") || upper.StartsWith("EXT")) && currentLine.Length > 2)
            {
                AutoCompleteDropdown.ShowSluglineSuggestions(currentLine);
            }
            // Show transition auto-complete when user types transition keywords
            else if ((upper.Contains("FADE") || upper.Contains("CUT") || upper.Contains("DISSOLVE") ||
                 upper.Contains("SMASH") || upper.Contains("MATCH") || upper.Contains("WIPE") ||
                 upper.Contains("IRIS") || upper.Contains("FLASH") || upper.Contains("BACK") ||
                 upper.Contains("MONTAGE")) && currentLine.Length > 2)
            {
                AutoCompleteDropdown.ShowTransitionSuggestions(currentLine);
            }
            else
            {
                AutoCompleteDropdown.Hide();
            }
        }

        /// <summary>
        /// Apply margin spacing to the editor
        /// Uses TextBox padding to simulate proper screenplay margins
        /// </summary>
        private void ApplyMargins(int leftMarginPixels, int rightMarginPixels)
        {
            try
            {
                // Convert to thickness for padding (WPF uses 1/96 DPI units by default)
                // Assuming 100 pixels = 1 inch, scale appropriately for display
                double leftPadding = (leftMarginPixels / 100.0) * 96;  // Convert to 96 DPI
                double rightPadding = (rightMarginPixels / 100.0) * 96;

                ScriptEditor.Padding = new Thickness(leftPadding, 5, rightPadding, 5);
            }
            catch { }
        }

        /// <summary>
        /// Apply default margins when line is empty
        /// </summary>
        private void ApplyDefaultMargins()
        {
            try
            {
                ScriptEditor.Padding = new Thickness(15, 5, 10, 5);  // Default 1.5" left, 1.0" right
            }
            catch { }
        }

        /// <summary>
        /// Convert pixel values back to inches for display
        /// </summary>
        private double ConvertPixelsToInches(int pixels)
        {
            return pixels / 100.0;
        }

        private void ScriptEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                // If dropdown is visible, pass arrow keys to it but keep focus on ScriptEditor
                if (AutoCompleteDropdown.Visibility == Visibility.Visible && (e.Key == Key.Down || e.Key == Key.Up))
                {
                    // Handle arrow navigation without changing focus
                    AutoCompleteDropdown.HandleArrowKey(e.Key);
                    e.Handled = true;
                    return;
                }
                
                if (e.Key == Key.Tab)
                {
                    HandleTabKey(e);
                    e.Handled = true;
                }
                else if (e.Key == Key.Return)
                {
                    HandleEnterKey(e);
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    AutoCompleteDropdown.Hide();
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }

        private void HandleTabKey(KeyEventArgs e)
        {
            try
            {
                _isUpdatingUI = true;

                string currentLine = GetCurrentLine().Trim();

                if (string.IsNullOrWhiteSpace(currentLine))
                {
                    var prevType = GetPreviousElementType();
                    string autoIndent = _indentation.GetAutoIndentForNewLine(prevType);

                    if (!string.IsNullOrEmpty(autoIndent))
                    {
                        int caretIndex = ScriptEditor.CaretIndex;
                        ScriptEditor.Text = ScriptEditor.Text.Insert(caretIndex, autoIndent);
                        ScriptEditor.CaretIndex = caretIndex + autoIndent.Length;
                        StatusText.Text = "✓ Auto-indent applied";
                    }
                }
                else
                {
                    int caretIndex = ScriptEditor.CaretIndex;
                    ScriptEditor.Text = ScriptEditor.Text.Insert(caretIndex, "\t");
                    ScriptEditor.CaretIndex = caretIndex + 1;
                }
            }
            finally
            {
                _isUpdatingUI = false;
            }
        }

        private void HandleEnterKey(KeyEventArgs e)
        {
            try
            {
                _isUpdatingUI = true;

                string currentLine = GetCurrentLine().Trim();

                if (string.IsNullOrWhiteSpace(currentLine))
                {
                    int caretIndex = ScriptEditor.CaretIndex;
                    ScriptEditor.Text = ScriptEditor.Text.Insert(caretIndex, Environment.NewLine);
                    ScriptEditor.CaretIndex = caretIndex + Environment.NewLine.Length;
                    AutoCompleteDropdown.Hide();
                    return;
                }

                var previousType = GetPreviousElementType();
                var formatResult = _autoFormatter.FormatOnEnter(currentLine, previousType);

                int lineStartIndex = GetCurrentLineStartIndex();
                int lineLength = GetCurrentLineLength();

                if (lineLength > 0)
                {
                    ScriptEditor.Select(lineStartIndex, lineLength);
                    ScriptEditor.SelectedText = formatResult.FormattedText;
                }

                int caretPos = lineStartIndex + formatResult.FormattedText.Length;
                ScriptEditor.CaretIndex = caretPos;

                ScriptEditor.Text = ScriptEditor.Text.Insert(caretPos, Environment.NewLine);
                ScriptEditor.CaretIndex = caretPos + Environment.NewLine.Length;

                var context = new ScriptContext(previousType, true);
                var detectResult = _logic.DetectAndNormalize(currentLine, context);

                if (detectResult.ElementType == ScriptElementType.SceneHeading ||
                    detectResult.ElementType == ScriptElementType.Action ||
                    detectResult.ElementType == ScriptElementType.Transition)
                {
                    ScriptEditor.Text = ScriptEditor.Text.Insert(ScriptEditor.CaretIndex, Environment.NewLine);
                    ScriptEditor.CaretIndex += Environment.NewLine.Length;
                }

                // Track character names
                if (detectResult.ElementType == ScriptElementType.Character)
                {
                    var charName = currentLine.ToUpper();
                    if (!_knownCharacters.Contains(charName))
                    {
                        _knownCharacters.Add(charName);
                    }
                }

                StatusText.Text = $"✓ {detectResult.ElementType}";
                AutoCompleteDropdown.Hide();
            }
            finally
            {
                _isUpdatingUI = false;
            }
        }

        private void AutoCompleteDropdown_ItemSelected(string selectedItem)
        {
            try
            {
                _isUpdatingUI = true;
                AutoCompleteDropdown.Hide();

                int lineStartIndex = GetCurrentLineStartIndex();
                int lineLength = GetCurrentLineLength();
                string currentLine = GetCurrentLine();

                // For time selection after dash (e.g., "INT. BEDROOM -" -> add "MORNING")
                if (currentLine.Contains("-"))
                {
                    int dashIndex = currentLine.LastIndexOf("-");
                    if (dashIndex >= 0)
                    {
                        // Keep everything before and including dash, append selected item
                        string beforeDash = currentLine.Substring(0, dashIndex + 1).TrimEnd();
                        string newLine = beforeDash + " " + selectedItem;
                        
                        // Replace entire line
                        ScriptEditor.Select(lineStartIndex, lineLength);
                        ScriptEditor.SelectedText = newLine;
                        
                        // Position cursor at end
                        ScriptEditor.CaretIndex = lineStartIndex + newLine.Length;
                        StatusText.Text = $"✓ {newLine}";
                        return;
                    }
                }

                // Normal case - replace entire line with selected item
                // Check if current line already ends with what we're adding
                string trimmedLine = currentLine.Trim();
                string trimmedItem = selectedItem.Trim();
                
                // If current line is "INT" and we're selecting "INT." - replace it
                // If current line is "INT." and we're selecting "INT." - don't add another dot
                string finalText;
                if (trimmedLine.EndsWith(".") && trimmedItem.StartsWith(trimmedLine))
                {
                    // Already has the dot, just use the selected item
                    finalText = trimmedItem;
                }
                else if (trimmedLine == trimmedItem)
                {
                    // Exact match, use as is
                    finalText = trimmedItem;
                }
                else if (trimmedLine.EndsWith(".") && trimmedItem.StartsWith(trimmedLine.TrimEnd('.')))
                {
                    // Line is "INT." and item is "INT. BEDROOM" - use item
                    finalText = trimmedItem;
                }
                else
                {
                    // Use selected item as is
                    finalText = trimmedItem;
                }
                
                // Replace entire line
                ScriptEditor.Select(lineStartIndex, lineLength);
                ScriptEditor.SelectedText = finalText;
                
                // Position cursor at the END of the inserted text
                int finalCaretPos = lineStartIndex + finalText.Length;
                ScriptEditor.CaretIndex = finalCaretPos;
                
                StatusText.Text = $"✓ {finalText}";
            }
            finally
            {
                _isUpdatingUI = false;
            }
        }

        private int FindTriggerStartIndex(string line)
        {
            // Not used anymore - keeping for reference only
            return -1;
        }

        private void UpdateElementTypeDisplay(string currentLine)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(currentLine))
                {
                    CurrentElementText.Text = "📝 (empty)";
                    return;
                }

                var context = new ScriptContext(GetPreviousElementType(), false);
                var result = _logic.DetectAndNormalize(currentLine, context);
                CurrentElementText.Text = GetElementDescription(result.ElementType);
            }
            catch { }
        }

        private string GetElementDescription(ScriptElementType type)
        {
            return type switch
            {
                ScriptElementType.SceneHeading => "📍 Scene Heading",
                ScriptElementType.Action => "📝 Action",
                ScriptElementType.Character => "👤 Character",
                ScriptElementType.Dialogue => "💬 Dialogue",
                ScriptElementType.Parenthetical => "🗨️ Parenthetical",
                ScriptElementType.Transition => "➡️ Transition",
                ScriptElementType.Shot => "📷 Shot",
                ScriptElementType.CenteredText => "🎬 Centered",
                _ => type.ToString()
            };
        }

        private void ScriptEditor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                int lineNumber = ScriptEditor.GetLineIndexFromCharacterIndex(ScriptEditor.CaretIndex) + 1;
                int lineStartIndex = ScriptEditor.GetCharacterIndexFromLineIndex(lineNumber - 1);
                int columnNumber = ScriptEditor.CaretIndex - lineStartIndex + 1;
                CaretPosText.Text = $"Ln {lineNumber}, Col {columnNumber}";
            }
            catch { }
        }

        private string GetCurrentLine()
        {
            try
            {
                int caretIndex = ScriptEditor.CaretIndex;
                int lineIndex = ScriptEditor.GetLineIndexFromCharacterIndex(caretIndex);
                int lineStartIndex = ScriptEditor.GetCharacterIndexFromLineIndex(lineIndex);
                int lineLength = ScriptEditor.GetLineLength(lineIndex);

                if (lineLength > 0)
                {
                    return ScriptEditor.Text.Substring(lineStartIndex, lineLength);
                }
            }
            catch { }

            return string.Empty;
        }

        private int GetCurrentLineNumber()
        {
            try
            {
                int caretIndex = ScriptEditor.CaretIndex;
                int lineIndex = ScriptEditor.GetLineIndexFromCharacterIndex(caretIndex);
                return lineIndex + 1;  // Return 1-based line number
            }
            catch { }

            return 0;
        }

        private int GetCurrentLineStartIndex()
        {
            try
            {
                int caretIndex = ScriptEditor.CaretIndex;
                int lineIndex = ScriptEditor.GetLineIndexFromCharacterIndex(caretIndex);
                return ScriptEditor.GetCharacterIndexFromLineIndex(lineIndex);
            }
            catch { }

            return 0;
        }

        private int GetCurrentLineLength()
        {
            try
            {
                int caretIndex = ScriptEditor.CaretIndex;
                int lineIndex = ScriptEditor.GetLineIndexFromCharacterIndex(caretIndex);
                return ScriptEditor.GetLineLength(lineIndex);
            }
            catch { }

            return 0;
        }

        private ScriptElementType? GetPreviousElementType()
        {
            try
            {
                int currentLineIndex = ScriptEditor.GetLineIndexFromCharacterIndex(ScriptEditor.CaretIndex);

                for (int i = currentLineIndex - 1; i >= 0; i--)
                {
                    int lineStart = ScriptEditor.GetCharacterIndexFromLineIndex(i);
                    int lineLength = ScriptEditor.GetLineLength(i);

                    if (lineLength > 0)
                    {
                        string line = ScriptEditor.Text.Substring(lineStart, lineLength).Trim();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            var context = new ScriptContext(null, false);
                            var result = _logic.DetectAndNormalize(line, context);
                            return result.ElementType;
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        private void UpdateLineNumbers()
        {
            try
            {
                int lineCount = ScriptEditor.LineCount;
                var lines = new List<string>();

                for (int i = 1; i <= lineCount; i++)
                {
                    lines.Add(i.ToString());
                }

                LineNumbers.Text = string.Join(Environment.NewLine, lines);
            }
            catch { }
        }

        private void UpdateStatistics()
        {
            try
            {
                var lines = ScriptEditor.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                int elementCount = 0;
                int wordCount = 0;

                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        elementCount++;
                        wordCount += line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    }
                }

                int pageCount = Math.Max(1, (elementCount / 55) + 1);

                ElementCountText.Text = $"Elements: {elementCount}";
                WordCountText.Text = $"Words: {wordCount}";
                PageCountText.Text = $"Page: {pageCount}";
            }
            catch { }
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            if (ScriptEditor.Text.Length > 0)
            {
                var result = MessageBox.Show("Discard changes?", "New", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes) return;
            }

            _currentScript = new Script();
            ScriptEditor.Clear();
            _knownCharacters.Clear();
            StatusText.Text = "✓ New script";
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Coming soon...";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "✓ Saved!";
        }
    }
}
