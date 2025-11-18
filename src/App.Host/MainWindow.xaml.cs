using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private readonly IScreenwritingLogic _screenwritingLogic;
        private readonly IFountainParser _fountainParser;
        private readonly FormattingService _formattingService;
        private readonly AutoFormattingService _autoFormattingService;
        private readonly SmartIndentationService _indentationService;
        private Script _currentScript;
        private bool _isUpdatingUI = false;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                _screenwritingLogic = new ScreenwritingLogic();
                _fountainParser = new FountainParser();
                _formattingService = new FormattingService();
                _indentationService = new SmartIndentationService();
                _autoFormattingService = new AutoFormattingService(_formattingService, _screenwritingLogic);
                _currentScript = new Script();

                Title = "ScriptWriter Pro - Untitled";
                ScriptEditor.Focus();
                UpdateLineNumbers();
                StatusText.Text = "Ready - Start typing! (Auto-formatting enabled)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initialization error: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void ScriptEditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingUI) return;

            try
            {
                // Get current line and position
                int caretIndex = ScriptEditor.CaretIndex;
                string currentLine = GetCurrentLine();
                int lineStartIndex = GetCurrentLineStartIndex();
                int caretLinePos = caretIndex - lineStartIndex;

                // Trim the current line for analysis
                string trimmedLine = currentLine.Trim();

                // Only format if there's content
                if (!string.IsNullOrWhiteSpace(trimmedLine))
                {
                    // Get previous element type for context
                    var previousType = GetPreviousElementType();

                    // Check for auto-formatting patterns
                    var shouldFormat = CheckFormatPatterns(trimmedLine);

                    if (shouldFormat)
                    {
                        // Get formatting result
                        var formattingResult = _autoFormattingService.FormatAsYouType(trimmedLine, previousType, caretLinePos);

                        // If formatting changed the text, update it
                        if (formattingResult.WasChanged)
                        {
                            _isUpdatingUI = true;
                            try
                            {
                                // Find the current line boundaries
                                int lineLength = GetCurrentLineLength();

                                // Select the entire line (without newline)
                                ScriptEditor.Select(lineStartIndex, lineLength);

                                // Replace with formatted text
                                ScriptEditor.SelectedText = formattingResult.FormattedText;

                                // Restore caret position
                                int newCaretPos = lineStartIndex + formattingResult.CaretPosition;
                                ScriptEditor.CaretIndex = Math.Min(newCaretPos, ScriptEditor.Text.Length);

                                // Show what was formatted
                                StatusText.Text = $"Auto-formatted: {formattingResult.FormattedText.Substring(0, Math.Min(30, formattingResult.FormattedText.Length))}...";
                            }
                            finally
                            {
                                _isUpdatingUI = false;
                            }
                        }
                    }
                }

                // Update UI
                UpdateLineNumbers();
                UpdateStatistics();
                UpdateCurrentElementDisplay(currentLine);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Check if the current line matches any auto-formatting patterns
        /// </summary>
        private bool CheckFormatPatterns(string trimmedLine)
        {
            var upper = trimmedLine.ToUpperInvariant();

            // INT/EXT patterns
            if (upper.Equals("INT") || upper.Equals("EXT")) return true;
            if (upper.StartsWith("INT/") || upper.StartsWith("EXT/")) return true;

            // Parenthetical patterns
            if (trimmedLine.StartsWith("(") && !trimmedLine.EndsWith(")")) return true;

            // Quote patterns
            if (trimmedLine.StartsWith("\"") && !trimmedLine.EndsWith("\"")) return true;

            // Centered text patterns
            if (trimmedLine.StartsWith(">") && !trimmedLine.EndsWith("<")) return true;

            // Transition patterns
            if (upper.StartsWith("FADE IN") || upper.StartsWith("FADE OUT")) return true;
            if (upper.StartsWith("CUT TO")) return true;

            // Character modifier patterns
            if (trimmedLine.Contains(" VO") || trimmedLine.Contains(" vo") || trimmedLine.Contains(" V O")) return true;
            if (trimmedLine.Contains(" OS") || trimmedLine.Contains(" os") || trimmedLine.Contains(" O S")) return true;

            return false;
        }

        private void ScriptEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
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

                var currentLine = GetCurrentLine();
                var trimmed = currentLine.Trim();

                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    // On empty line, apply smart indentation
                    var prevType = GetPreviousElementType();
                    var indent = _indentationService.GetAutoIndentForNewLine(prevType);

                    if (!string.IsNullOrEmpty(indent))
                    {
                        int caretIndex = ScriptEditor.CaretIndex;
                        ScriptEditor.Text = ScriptEditor.Text.Insert(caretIndex, indent);
                        ScriptEditor.CaretIndex = caretIndex + indent.Length;
                        StatusText.Text = "Auto-indent applied";
                    }
                }
                else
                {
                    // Insert regular tab
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

                var currentLine = GetCurrentLine();
                var trimmed = currentLine.Trim();

                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    // Empty line - just insert newline
                    int caretIndex = ScriptEditor.CaretIndex;
                    ScriptEditor.Text = ScriptEditor.Text.Insert(caretIndex, Environment.NewLine);
                    ScriptEditor.CaretIndex = caretIndex + Environment.NewLine.Length;
                    return;
                }

                // Get context
                var previousType = GetPreviousElementType();
                var context = new ScriptContext(previousType, true);

                // Detect element type
                var result = _screenwritingLogic.DetectAndNormalize(trimmed, context);

                // Format the line
                var formattingResult = _autoFormattingService.FormatOnBlockEnd(trimmed, previousType);

                // Replace current line with formatted version
                int lineStartIndex = GetCurrentLineStartIndex();
                int lineLength = GetCurrentLineLength();
                ScriptEditor.Select(lineStartIndex, lineLength);
                ScriptEditor.SelectedText = formattingResult.FormattedText;

                // Move caret to end of formatted line
                int caretPos = lineStartIndex + formattingResult.FormattedText.Length;
                ScriptEditor.CaretIndex = caretPos;

                // Insert newline
                ScriptEditor.Text = ScriptEditor.Text.Insert(caretPos, Environment.NewLine);
                ScriptEditor.CaretIndex = caretPos + Environment.NewLine.Length;

                // Add blank line after certain elements
                if (result.ElementType == ScriptElementType.SceneHeading ||
                    result.ElementType == ScriptElementType.Action ||
                    result.ElementType == ScriptElementType.Transition)
                {
                    ScriptEditor.Text = ScriptEditor.Text.Insert(ScriptEditor.CaretIndex, Environment.NewLine);
                    ScriptEditor.CaretIndex += Environment.NewLine.Length;
                }

                // Auto-indent next line
                if (result.ElementType == ScriptElementType.SceneHeading)
                {
                    var actionIndent = _indentationService.GetIndentationString(ScriptElementType.Action);
                    ScriptEditor.Text = ScriptEditor.Text.Insert(ScriptEditor.CaretIndex, actionIndent);
                    ScriptEditor.CaretIndex += actionIndent.Length;
                }

                StatusText.Text = $"{result.ElementType} - {GetElementDescription(result.ElementType)}";
            }
            finally
            {
                _isUpdatingUI = false;
            }
        }

        private void ScriptEditor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                int caretIndex = ScriptEditor.CaretIndex;
                int lineNumber = ScriptEditor.GetLineIndexFromCharacterIndex(caretIndex) + 1;
                int lineStartIndex = ScriptEditor.GetCharacterIndexFromLineIndex(lineNumber - 1);
                int columnNumber = caretIndex - lineStartIndex + 1;

                CaretPosText.Text = $"Ln {lineNumber}, Col {columnNumber}";
            }
            catch
            {
                // Silently ignore
            }
        }

        private void UpdateCurrentElementDisplay(string currentLine)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(currentLine))
                {
                    var context = new ScriptContext(GetPreviousElementType(), false);
                    var result = _screenwritingLogic.DetectAndNormalize(currentLine.Trim(), context);
                    CurrentElementText.Text = result.ElementType.ToString();
                }
            }
            catch
            {
                // Silently ignore
            }
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

                // Search backwards for last non-empty line
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
                            var result = _screenwritingLogic.DetectAndNormalize(line, context);
                            return result.ElementType;
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        private void ReplaceCurrentLine(string newText)
        {
            try
            {
                int lineStartIndex = GetCurrentLineStartIndex();
                int lineLength = GetCurrentLineLength();

                if (lineLength > 0)
                {
                    ScriptEditor.Select(lineStartIndex, lineLength);
                    ScriptEditor.SelectedText = newText;
                }
            }
            catch { }
        }

        private void UpdateLineNumbers()
        {
            try
            {
                int lineCount = ScriptEditor.LineCount;
                var lineNumbers = new List<string>();

                for (int i = 1; i <= lineCount; i++)
                {
                    lineNumbers.Add(i.ToString());
                }

                LineNumbers.Text = string.Join(Environment.NewLine, lineNumbers);
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
                int pageCount = 1;

                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        elementCount++;
                        wordCount += line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    }
                }

                pageCount = Math.Max(1, (elementCount / 55) + 1);

                ElementCountText.Text = $"Elements: {elementCount}";
                WordCountText.Text = $"Words: {wordCount}";
                PageCountText.Text = $"Page: {pageCount}";
            }
            catch { }
        }

        private string GetElementDescription(ScriptElementType type)
        {
            return type switch
            {
                ScriptElementType.SceneHeading => "Scene Heading (INT./EXT.)",
                ScriptElementType.Action => "Action/Narrative",
                ScriptElementType.Character => "Character Name",
                ScriptElementType.Dialogue => "Dialogue",
                ScriptElementType.Parenthetical => "Parenthetical (beat/pause)",
                ScriptElementType.Transition => "Transition (CUT TO:, FADE OUT:, etc.)",
                ScriptElementType.Shot => "Shot Description",
                ScriptElementType.CenteredText => "Centered Text",
                _ => type.ToString()
            };
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            if (ScriptEditor.Text.Length > 0)
            {
                var result = MessageBox.Show("Create new script? Any unsaved changes will be lost.", 
                    "New Script", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    return;
            }

            _currentScript = new Script();
            ScriptEditor.Clear();
            Title = "ScriptWriter Pro - Untitled";
            StatusText.Text = "New script created - Start typing!";
            UpdateLineNumbers();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Open functionality coming soon...";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _currentScript.MarkModified();
            StatusText.Text = "Script saved!";
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
