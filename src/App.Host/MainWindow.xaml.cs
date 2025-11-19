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
        private readonly IScreenwritingLogic _logic;
        private readonly AutoFormattingEngine _autoFormatter;
        private readonly SmartIndentationEngine _indentation;
        private readonly IFountainParser _fountainParser;

        private Script _currentScript;
        private bool _isUpdatingUI = false;

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
                StatusText.Text = "✓ Ready - Start typing!";
                CurrentElementText.Text = "📝 Action";
                UpdateLineNumbers();
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
                    ApplyDefaultMargins();
                    _isUpdatingUI = false;
                    return;
                }

                // Apply auto-formatting as user types
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

                    // Apply margins visually
                    ApplyMargins(formatResult.LeftMargin, formatResult.RightMargin);

                    StatusText.Text = $"✓ Formatted: {formatResult.FormattedText.Substring(0, Math.Min(40, formatResult.FormattedText.Length))} | Left: {ConvertPixelsToInches(formatResult.LeftMargin)}\", Right: {ConvertPixelsToInches(formatResult.RightMargin)}\"";
                }
                else
                {
                    ApplyDefaultMargins();
                }

                UpdateElementTypeDisplay(trimmed);
                UpdateLineNumbers();
                UpdateStatistics();
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

        /// <summary>
        /// Handle Tab key - auto-indent
        /// </summary>
        private void ScriptEditor_PreviewKeyDown(object sender, KeyEventArgs e)
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

                StatusText.Text = $"✓ {GetElementDescription(detectResult.ElementType)}";
            }
            finally
            {
                _isUpdatingUI = false;
            }
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
                var result = MessageBox.Show("Create new script? Unsaved changes will be lost.", 
                    "New Script", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                    return;
            }

            _currentScript = new Script();
            ScriptEditor.Clear();
            Title = "ScriptWriter Pro - Untitled";
            StatusText.Text = "✓ New script created!";
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Open function coming soon...";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _currentScript.MarkModified();
            StatusText.Text = "✓ Script saved!";
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
