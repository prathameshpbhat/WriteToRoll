using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using App.Core.Services;

namespace ScriptWriter
{
    public partial class AutoCompleteDropdown : UserControl
    {
        public ObservableCollection<string> Suggestions { get; set; } = new();

        public enum AutoCompleteMode
        {
            Transition,
            Slugline,
            Location,
            TimeOfDay,
            Character
        }

        private AutoCompleteMode _currentMode = AutoCompleteMode.Transition;

        public event Action<string> ItemSelected;

        public AutoCompleteDropdown()
        {
            InitializeComponent();
            ResultsList.ItemsSource = Suggestions;
        }

        /// <summary>
        /// Show suggestions for transitions
        /// </summary>
        public void ShowTransitionSuggestions(string query = "")
        {
            _currentMode = AutoCompleteMode.Transition;
            SearchBox.Text = query;
            UpdateSuggestions(query);
            this.Visibility = Visibility.Visible;
            // Don't focus SearchBox - keep focus on ScriptEditor
        }

        /// <summary>
        /// Show suggestions for sluglines
        /// </summary>
        public void ShowSluglineSuggestions(string sluglineQuery = "")
        {
            _currentMode = AutoCompleteMode.Slugline;
            SearchBox.Text = sluglineQuery;
            var suggestions = AutoCompleteEngine.GenerateSluglineSuggestions(sluglineQuery);
            UpdateSuggestionsFromList(suggestions);
            this.Visibility = Visibility.Visible;
            // Don't focus SearchBox - keep focus on ScriptEditor to prevent cursor from disappearing
        }

        /// <summary>
        /// Show suggestions for locations
        /// </summary>
        public void ShowLocationSuggestions(string query = "")
        {
            _currentMode = AutoCompleteMode.Location;
            SearchBox.Text = query;
            UpdateSuggestions(query);
            this.Visibility = Visibility.Visible;
            // Don't focus SearchBox - keep focus on ScriptEditor
        }

        /// <summary>
        /// Show suggestions for time-of-day
        /// </summary>
        public void ShowTimeOfDaySuggestions(string query = "")
        {
            _currentMode = AutoCompleteMode.TimeOfDay;
            SearchBox.Text = query;
            UpdateSuggestions(query);
            this.Visibility = Visibility.Visible;
            // Don't focus SearchBox - keep focus on ScriptEditor
        }

        /// <summary>
        /// Show suggestions for characters
        /// </summary>
        public void ShowCharacterSuggestions(string query, List<string> knownCharacters)
        {
            _currentMode = AutoCompleteMode.Character;
            SearchBox.Text = query;
            
            var filtered = string.IsNullOrEmpty(query)
                ? knownCharacters.Take(10).ToList()
                : knownCharacters.Where(c => c.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(c => c.IndexOf(query, StringComparison.OrdinalIgnoreCase))
                    .Take(10)
                    .ToList();

            UpdateSuggestionsFromList(filtered);
            this.Visibility = Visibility.Visible;
            SearchBox.Focus();
            SearchBox.SelectAll();
        }

        private void UpdateSuggestions(string query)
        {
            List<string> results = _currentMode switch
            {
                AutoCompleteMode.Transition => AutoCompleteEngine.SearchTransitions(query),
                AutoCompleteMode.Location => AutoCompleteEngine.SearchLocations(query),
                AutoCompleteMode.TimeOfDay => AutoCompleteEngine.SearchTimeOfDay(query),
                _ => new List<string>()
            };

            UpdateSuggestionsFromList(results);
        }

        private void UpdateSuggestionsFromList(List<string> results)
        {
            Suggestions.Clear();
            foreach (var result in results)
            {
                Suggestions.Add(result);
            }

            // Hide dropdown if no suggestions
            if (Suggestions.Count == 0)
            {
                this.Visibility = Visibility.Collapsed;
            }
            else if (Suggestions.Count > 0)
            {
                ResultsList.SelectedIndex = 0;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSuggestions(SearchBox.Text);
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                // Move to first item in ResultsList
                if (Suggestions.Count > 0)
                {
                    if (ResultsList.SelectedIndex < 0)
                    {
                        ResultsList.SelectedIndex = 0;
                    }
                    else if (ResultsList.SelectedIndex < Suggestions.Count - 1)
                    {
                        ResultsList.SelectedIndex++;
                    }
                    // Don't focus ResultsList - keep focus on ScriptEditor
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                if (ResultsList.SelectedIndex > 0)
                {
                    ResultsList.SelectedIndex--;
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                if (ResultsList.SelectedIndex >= 0 && ResultsList.SelectedIndex < Suggestions.Count)
                {
                    SelectItem(Suggestions[ResultsList.SelectedIndex]);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                this.Visibility = Visibility.Collapsed;
                e.Handled = true;
            }
        }

        private void ResultsList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                e.Handled = true;
                if (ResultsList.SelectedIndex < Suggestions.Count - 1)
                {
                    ResultsList.SelectedIndex++;
                }
                return;
            }
            else if (e.Key == Key.Up)
            {
                e.Handled = true;
                if (ResultsList.SelectedIndex > 0)
                {
                    ResultsList.SelectedIndex--;
                }
                // Don't go back to SearchBox - keep focus on ScriptEditor
                return;
            }
            else if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (ResultsList.SelectedIndex >= 0 && ResultsList.SelectedIndex < Suggestions.Count)
                {
                    SelectItem(Suggestions[ResultsList.SelectedIndex]);
                }
                return;
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                this.Visibility = Visibility.Collapsed;
                return;
            }
        }

        private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ResultsList.SelectedIndex >= 0)
            {
                SelectItem(Suggestions[ResultsList.SelectedIndex]);
            }
        }

        private void SelectItem(string item)
        {
            ItemSelected?.Invoke(item);
            this.Visibility = Visibility.Collapsed;
            ResultsList.SelectedIndex = -1;
        }

        public void Hide()
        {
            this.Visibility = Visibility.Collapsed;
            ResultsList.SelectedIndex = -1;
        }

        public void HandleArrowKey(Key key)
        {
            if (key == Key.Down)
            {
                if (ResultsList.SelectedIndex < Suggestions.Count - 1)
                {
                    ResultsList.SelectedIndex++;
                }
            }
            else if (key == Key.Up)
            {
                if (ResultsList.SelectedIndex > 0)
                {
                    ResultsList.SelectedIndex--;
                }
            }
        }
    }
}
