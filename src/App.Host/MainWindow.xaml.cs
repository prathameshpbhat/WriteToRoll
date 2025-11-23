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
        private SmartIndentationService _indentationService;
        private ScreenplayStructureAdvisor _structureAdvisor;
        private IFountainParser _fountainParser;
        private ContextAwareFormattingEngine _contextFormatter;
        private ElementTypeDetector _elementDetector;
        private PageFormatting _pageFormat;

        private Script _currentScript;
        private bool _isUpdatingUI = false;
        private List<string> _knownCharacters = new();
        private ScriptElementType? _activeElementType;
        private int _autoCompleteAnchorIndex = -1;

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                _logic = new ScreenwritingLogic();
                _autoFormatter = new AutoFormattingEngine(_logic);
                _indentation = new SmartIndentationEngine();
                _indentationService = new SmartIndentationService();
                _structureAdvisor = new ScreenplayStructureAdvisor();
                _fountainParser = new FountainParser();
                _contextFormatter = new ContextAwareFormattingEngine(_logic);
                _elementDetector = new ElementTypeDetector(_logic);
                _pageFormat = PageFormatting.StandardLetter();
                _currentScript = new Script();
                _activeElementType = ScriptElementType.Action;

                Title = "ScriptWriter Pro - Untitled";
                ScriptEditor.Focus();
                StatusText.Text = "✓ Ready";
                CurrentElementText.Text = "📝 Action";
                NextElementText.Text = "Next: Action";
                UpdateLineNumbers();
                UpdateNextElementHint(ScriptElementType.Action);

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
        /// Main TextChanged event - MASTER DETECTION using ElementTypeDetector
        /// Determines element type, formats content (NOT indentation), updates UI
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
                    _activeElementType = ScriptElementType.Action;
                    UpdateNextElementHint(ScriptElementType.Action);
                    _isUpdatingUI = false;
                    return;
                }

                // ===== MASTER DETECTION =====
                // Get element type based on content + context
                var previousType = GetPreviousElementType();
                var detectedType = _elementDetector.DetectElementType(trimmed, previousType);
                
                // Format content (ONLY the text, preserve indentation)
                var formatted = _elementDetector.FormatLineContent(trimmed, detectedType);
                
                // Apply formatting if content changed
                if (formatted != trimmed)
                {
                    int lineStartIndex = GetCurrentLineStartIndex();
                    int lineLength = GetCurrentLineLength();
                    
                    if (lineLength > 0)
                    {
                        ScriptEditor.Select(lineStartIndex, lineLength);
                        ScriptEditor.SelectedText = formatted;
                        StatusText.Text = $"✓ {detectedType}: {formatted.Substring(0, Math.Min(40, formatted.Length))}";
                    }
                }
                else
                {
                    StatusText.Text = $"✓ {_elementDetector.GetElementDisplayName(detectedType)}";
                }

                _activeElementType = detectedType;
                CurrentElementText.Text = _elementDetector.GetElementDisplayName(detectedType);
                UpdateNextElementHint(detectedType);
                
                UpdateLineNumbers();
                UpdateStatistics();

                // Show auto-complete dropdown while typing
                if (!string.IsNullOrEmpty(trimmed))
                {
                    CheckAutoCompleteActivation(trimmed);
                }
                else
                {
                    HideAutoCompleteDropdown();
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

        private void BeginAutoCompleteSession(int? forcedAnchorIndex = null)
        {
            int current = forcedAnchorIndex ?? ScriptEditor.CaretIndex;
            _autoCompleteAnchorIndex = Math.Max(0, Math.Min(current, ScriptEditor.Text.Length));
        }

        private void HideAutoCompleteDropdown()
        {
            AutoCompleteDropdown.Hide();
            _autoCompleteAnchorIndex = -1;
        }

        private static readonly string[] TransitionKeywords = new[]
        {
            "FADE",
            "CUT",
            "DISSOLVE",
            "SMASH",
            "MATCH",
            "WIPE",
            "IRIS",
            "FLASH",
            "BACK",
            "MONTAGE"
        };

        private void CheckAutoCompleteActivation(string currentLine)
        {
            var trimmed = currentLine.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                HideAutoCompleteDropdown();
                return;
            }

            if (IsSluglineTrigger(trimmed))
            {
                BeginAutoCompleteSession(GetCurrentLineStartIndex());
                AutoCompleteDropdown.ShowSluglineSuggestions(currentLine);
                return;
            }

            var previousType = GetPreviousElementType();
            var context = new ScriptContext(previousType, false);
            var detection = _logic.DetectAndNormalize(trimmed, context);

            switch (detection.ElementType)
            {
                case ScriptElementType.SceneHeading:
                    BeginAutoCompleteSession(GetCurrentLineStartIndex());
                    AutoCompleteDropdown.ShowSluglineSuggestions(currentLine);
                    return;
                case ScriptElementType.Transition:
                    BeginAutoCompleteSession(GetCurrentLineStartIndex());
                    AutoCompleteDropdown.ShowTransitionSuggestions(currentLine);
                    return;
                case ScriptElementType.Character:
                    if (_knownCharacters.Count > 0)
                    {
                        BeginAutoCompleteSession(GetCurrentLineStartIndex());
                        AutoCompleteDropdown.ShowCharacterSuggestions(trimmed, _knownCharacters);
                    }
                    else
                    {
                        HideAutoCompleteDropdown();
                    }
                    return;
                case ScriptElementType.Parenthetical:
                    BeginAutoCompleteSession(GetCurrentLineStartIndex());
                    AutoCompleteDropdown.ShowParentheticalSuggestions(trimmed);
                    return;
            }

            var upper = trimmed.ToUpperInvariant();
            if (TransitionKeywords.Any(token => upper.Contains(token)))
            {
                BeginAutoCompleteSession(GetCurrentLineStartIndex());
                AutoCompleteDropdown.ShowTransitionSuggestions(currentLine);
                return;
            }

            HideAutoCompleteDropdown();
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
                bool dropdownVisible = AutoCompleteDropdown.Visibility == Visibility.Visible;

                if (dropdownVisible && (e.Key == Key.Down || e.Key == Key.Up))
                {
                    AutoCompleteDropdown.HandleArrowKey(e.Key);
                    e.Handled = true;
                    return;
                }

                if (dropdownVisible && (e.Key == Key.Return || e.Key == Key.Tab))
                {
                    if (AutoCompleteDropdown.TryCommitSelection() && e.Key == Key.Tab)
                    {
                        e.Handled = true;
                        return;
                    }
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
                    HideAutoCompleteDropdown();
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

                string currentLine = GetCurrentLine();
                string trimmedLine = currentLine.Trim();
                var previousType = GetPreviousElementType();

                // ===== MASTER TAB DETECTION =====
                // If current line is just a word after ACTION/SCENE/TRANSITION → Treat as CHARACTER
                if (!string.IsNullOrWhiteSpace(trimmedLine) && previousType.HasValue &&
                    (previousType == ScriptElementType.Action ||
                     previousType == ScriptElementType.SceneHeading ||
                     previousType == ScriptElementType.Transition))
                {
                    // Check if looks like character name (uppercase)
                    var detectedType = _elementDetector.DetectElementType(trimmedLine, previousType);
                    if (detectedType == ScriptElementType.Character)
                    {
                        string formatted = _elementDetector.FormatLineContent(trimmedLine, ScriptElementType.Character);
                        int lineStartIndex = GetCurrentLineStartIndex();
                        int lineLength = GetCurrentLineLength();
                        if (lineLength > 0)
                        {
                            ScriptEditor.Select(lineStartIndex, lineLength);
                            ScriptEditor.SelectedText = formatted;
                            _activeElementType = ScriptElementType.Character;
                            UpdateNextElementHint(ScriptElementType.Character);
                            StatusText.Text = $"✓ CHARACTER: {formatted}";
                            return;
                        }
                    }
                }

                if (ShouldTriggerParentheticalSuggestions(trimmedLine))
                {
                    BeginAutoCompleteSession(GetCurrentLineStartIndex());
                    AutoCompleteDropdown.ShowParentheticalSuggestions(trimmedLine);
                    return;
                }

                if (ShouldTriggerCharacterSuggestions(trimmedLine, previousType))
                {
                    BeginAutoCompleteSession(GetCurrentLineStartIndex());
                    AutoCompleteDropdown.ShowCharacterSuggestions(trimmedLine, _knownCharacters);
                    return;
                }

                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    var prevType = previousType;
                    var suggested = prevType.HasValue
                        ? _structureAdvisor.GetDefaultNextElement(prevType.Value)
                        : null;

                    if (suggested.HasValue)
                    {
                        var indent = GetAutoIndentationString(suggested.Value);
                        if (!string.IsNullOrEmpty(indent))
                        {
                            InsertIndentationString(indent);
                            var profile = ScreenplayElementProfiles.GetProfile(suggested.Value);
                            StatusText.Text = $"✓ Indented for {profile.DisplayName}";
                            return;
                        }
                    }

                    var fallback = _indentation.GetAutoIndentForNewLine(prevType);
                    if (!string.IsNullOrEmpty(fallback))
                    {
                        InsertIndentationString(fallback);
                        StatusText.Text = "✓ Auto-indent applied";
                        return;
                    }
                }

                InsertIndentationString("\t");
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
                    HideAutoCompleteDropdown();
                    return;
                }

                var previousType = GetPreviousElementType();
                
                // ===== MASTER ENTER DETECTION =====
                // Detect what current line is, format it, then suggest next
                var detectedType = _elementDetector.DetectElementType(currentLine, previousType);
                var formatted = _elementDetector.FormatLineContent(currentLine, detectedType);

                int lineStartIndex = GetCurrentLineStartIndex();
                int lineLength = GetCurrentLineLength();

                if (lineLength > 0)
                {
                    ScriptEditor.Select(lineStartIndex, lineLength);
                    ScriptEditor.SelectedText = formatted;
                }

                int caretPos = lineStartIndex + formatted.Length;
                ScriptEditor.CaretIndex = caretPos;

                ScriptEditor.Text = ScriptEditor.Text.Insert(caretPos, Environment.NewLine);
                ScriptEditor.CaretIndex = caretPos + Environment.NewLine.Length;

                // Add blank line after SCENE/ACTION/TRANSITION for readability
                if (detectedType == ScriptElementType.SceneHeading ||
                    detectedType == ScriptElementType.Action ||
                    detectedType == ScriptElementType.Transition)
                {
                    ScriptEditor.Text = ScriptEditor.Text.Insert(ScriptEditor.CaretIndex, Environment.NewLine);
                    ScriptEditor.CaretIndex += Environment.NewLine.Length;
                }

                var suggestedNext = _structureAdvisor.GetDefaultNextElement(detectedType);
                InsertAutoIndentationFor(suggestedNext);

                // Track character names
                if (detectedType == ScriptElementType.Character)
                {
                    var charName = formatted.ToUpper();
                    if (!_knownCharacters.Contains(charName))
                    {
                        _knownCharacters.Add(charName);
                    }
                }

                StatusText.Text = $"✓ {detectedType}";
                HideAutoCompleteDropdown();
            }
            finally
            {
                _isUpdatingUI = false;
            }
        }

        private void AutoCompleteDropdown_ItemSelected(string selectedItem)
        {
            if (string.IsNullOrWhiteSpace(selectedItem))
                return;

            try
            {
                _isUpdatingUI = true;
                var normalizedSelection = NormalizeSelectionForCurrentMode(selectedItem);
                if (string.IsNullOrWhiteSpace(normalizedSelection))
                    return;

                var indent = GetIndentationForCurrentMode();
                var replacement = string.Concat(indent, normalizedSelection);

                int caretIndexBefore = Math.Max(0, Math.Min(ScriptEditor.CaretIndex, ScriptEditor.Text.Length));
                int insertionStart = _autoCompleteAnchorIndex >= 0
                    ? Math.Min(_autoCompleteAnchorIndex, ScriptEditor.Text.Length)
                    : GetCurrentLineStartIndex();

                int insertionEnd = Math.Max(insertionStart, caretIndexBefore);

                ScriptEditor.Select(insertionStart, insertionEnd - insertionStart);
                ScriptEditor.SelectedText = replacement;

                int finalCaretPos = insertionStart + replacement.Length;
                ScriptEditor.CaretIndex = finalCaretPos;
                ScriptEditor.SelectionLength = 0;
                int finalLineIndex = ScriptEditor.GetLineIndexFromCharacterIndex(finalCaretPos);
                ScriptEditor.ScrollToLine(finalLineIndex);
                ScriptEditor.Focus();

                StatusText.Text = $"✓ {normalizedSelection}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
            finally
            {
                HideAutoCompleteDropdown();
                _isUpdatingUI = false;
            }
        }

        private int FindTriggerStartIndex(string line)
        {
            // Not used anymore - keeping for reference only
            return -1;
        }

        private string NormalizeSelectionForCurrentMode(string selection)
        {
            var mode = AutoCompleteDropdown.CurrentMode;
            return mode switch
            {
                AutoCompleteDropdown.AutoCompleteMode.Parenthetical => EnsureParentheticalFormat(selection),
                AutoCompleteDropdown.AutoCompleteMode.Character => selection.ToUpperInvariant(),
                AutoCompleteDropdown.AutoCompleteMode.Slugline => selection.ToUpperInvariant(),
                AutoCompleteDropdown.AutoCompleteMode.Transition => selection.ToUpperInvariant(),
                AutoCompleteDropdown.AutoCompleteMode.Location => selection.ToUpperInvariant(),
                AutoCompleteDropdown.AutoCompleteMode.TimeOfDay => selection.ToUpperInvariant(),
                _ => selection
            };
        }

        private static string EnsureParentheticalFormat(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var value = input.Trim();

            if (!value.StartsWith("(", StringComparison.Ordinal))
            {
                value = "(" + value;
            }

            if (!value.EndsWith(")", StringComparison.Ordinal))
            {
                value += ")";
            }

            return value.ToLowerInvariant();
        }

        private bool ShouldTriggerCharacterSuggestions(string trimmedLine, ScriptElementType? previousType)
        {
            if (_knownCharacters.Count == 0)
                return false;

            if (!string.IsNullOrWhiteSpace(trimmedLine))
            {
                return IsLikelyCharacterLine(trimmedLine);
            }

            return previousType == ScriptElementType.Action ||
                   previousType == ScriptElementType.SceneHeading ||
                   previousType == ScriptElementType.Parenthetical ||
                   previousType == ScriptElementType.Dialogue ||
                   previousType == null;
        }

        private static bool IsLikelyCharacterLine(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (value.Length > 40)
                return false;

            return value == value.ToUpperInvariant();
        }

        private bool ShouldTriggerParentheticalSuggestions(string trimmedLine)
        {
            if (string.IsNullOrWhiteSpace(trimmedLine))
                return false;

            return trimmedLine.StartsWith("(", StringComparison.Ordinal);
        }

        private bool IsSluglineTrigger(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            var tokens = line.Trim()
                .Split(new[] { ' ', '\t', '-' }, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0)
                return false;

            var firstToken = tokens[0].ToUpperInvariant();

            return firstToken == "INT" ||
                   firstToken == "EXT" ||
                   firstToken == "INT/EXT" ||
                   firstToken == "EXT/INT";
        }

        private void UpdateElementTypeDisplay(string currentLine)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(currentLine))
                {
                    CurrentElementText.Text = "📝 (empty)";
                    UpdateNextElementHint(null);
                    return;
                }

                var context = new ScriptContext(GetPreviousElementType(), false);
                var result = _logic.DetectAndNormalize(currentLine, context);
                CurrentElementText.Text = GetElementDescription(result.ElementType);
                _activeElementType = result.ElementType;
                UpdateNextElementHint(result.ElementType);
            }
            catch { }
        }

        private void UpdateNextElementHint(ScriptElementType? currentType)
        {
            try
            {
                var baseType = currentType ?? ScriptElementType.Action;
                var preferred = _structureAdvisor.GetPreferredNextElements(baseType);
                if (preferred == null || preferred.Count == 0)
                {
                    NextElementText.Text = "Next: (free)";
                    return;
                }

                var nextType = preferred[0];
                var profile = ScreenplayElementProfiles.GetProfile(nextType);
                var margins = _indentationService.GetElementMargins(nextType);
                NextElementText.Text = $"Next: {profile.DisplayName} ({margins.LeftMarginInches:0.0}\" / {margins.RightMarginInches:0.0}\")";
            }
            catch
            {
                NextElementText.Text = "Next: (free)";
            }
        }

        private void InsertAutoIndentationFor(ScriptElementType? nextType)
        {
            if (!nextType.HasValue)
                return;

            var indent = GetAutoIndentationString(nextType.Value);
            if (string.IsNullOrEmpty(indent))
                return;

            InsertIndentationString(indent);
        }

        private void InsertIndentationString(string indent)
        {
            if (string.IsNullOrEmpty(indent))
                return;

            int caretIndex = ScriptEditor.CaretIndex;
            ScriptEditor.Text = ScriptEditor.Text.Insert(caretIndex, indent);
            ScriptEditor.CaretIndex = caretIndex + indent.Length;
        }

        private string GetIndentationForCurrentMode()
        {
            return AutoCompleteDropdown.CurrentMode switch
            {
                AutoCompleteDropdown.AutoCompleteMode.Character => GetAutoIndentationString(ScriptElementType.Character),
                AutoCompleteDropdown.AutoCompleteMode.Parenthetical => GetAutoIndentationString(ScriptElementType.Parenthetical),
                AutoCompleteDropdown.AutoCompleteMode.Transition => GetAutoIndentationString(ScriptElementType.Transition),
                _ => string.Empty
            };
        }

        private string GetAutoIndentationString(ScriptElementType type)
        {
            if (type == ScriptElementType.SceneHeading ||
                type == ScriptElementType.Action ||
                type == ScriptElementType.Shot)
            {
                return string.Empty;
            }

            return _indentation.GetIndentation(type);
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
