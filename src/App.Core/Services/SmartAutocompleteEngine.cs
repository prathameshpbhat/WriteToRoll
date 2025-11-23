using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Fade-In Feature: Smart Autocomplete & Writing Suggestions
    /// 
    /// Features:
    /// - Intelligent context-aware autocomplete
    /// - Suggest next screenplay element
    /// - Character name autocomplete
    /// - Location and time of day suggestions
    /// - Transition suggestions
    /// - Common dialogue phrases
    /// - Smart formatting suggestions
    /// </summary>
    public class SmartAutocompleteEngine
    {
        public class AutocompleteContext
        {
            public ScriptElementType PreviousElementType { get; set; }
            public string LastCharacterName { get; set; } = string.Empty;
            public string CurrentLocation { get; set; } = string.Empty;
            public string CurrentTime { get; set; } = string.Empty;
            public int LineNumber { get; set; }
            public string PartialText { get; set; } = string.Empty;
        }

        public class AutocompleteSuggestion
        {
            public string Text { get; set; } = string.Empty;
            public string DisplayText { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public ScriptElementType SuggestedType { get; set; }
            public int Priority { get; set; }  // Higher = more relevant
            public string Icon { get; set; } = string.Empty;
        }

        private Dictionary<string, int> _characterFrequency = new();
        private Dictionary<string, int> _locationFrequency = new();
        private HashSet<string> _usedCharacters = new();
        private HashSet<string> _usedLocations = new();

        public SmartAutocompleteEngine()
        {
            InitializeCommonSuggestions();
        }

        /// <summary>
        /// Initialize common screenplay suggestions
        /// </summary>
        private void InitializeCommonSuggestions()
        {
            // Common character names in screenplays
            _usedCharacters = new HashSet<string>
            {
                "JOHN", "SARAH", "MICHAEL", "JAMES", "DAVID", "ROBERT", "WILLIAM",
                "MARY", "PATRICIA", "JENNIFER", "LINDA", "BARBARA", "ELIZABETH"
            };

            // Common locations
            _usedLocations = new HashSet<string>
            {
                "BEDROOM", "KITCHEN", "LIVING ROOM", "BATHROOM", "OFFICE",
                "STREET", "PARK", "CAR", "BUILDING", "HOUSE", "APARTMENT",
                "RESTAURANT", "BAR", "COFFEE SHOP", "HOSPITAL", "POLICE STATION"
            };
        }

        /// <summary>
        /// Get smart autocomplete suggestions based on context
        /// </summary>
        public List<AutocompleteSuggestion> GetContextAwareSuggestions(AutocompleteContext context)
        {
            var suggestions = new List<AutocompleteSuggestion>();

            switch (context.PreviousElementType)
            {
                case ScriptElementType.SceneHeading:
                    suggestions.AddRange(GetActionSuggestions(context));
                    break;

                case ScriptElementType.Action:
                    suggestions.AddRange(GetCharacterSuggestions(context));
                    suggestions.AddRange(GetTransitionSuggestions(context));
                    break;

                case ScriptElementType.Character:
                    suggestions.AddRange(GetParentheticalSuggestions(context));
                    break;

                case ScriptElementType.Parenthetical:
                    suggestions.AddRange(GetDialogueSuggestions(context));
                    break;

                case ScriptElementType.Dialogue:
                    suggestions.AddRange(GetFollowUpSuggestions(context));
                    break;

                case ScriptElementType.Transition:
                    suggestions.AddRange(GetSceneHeadingSuggestions(context));
                    break;
            }

            return suggestions.OrderByDescending(s => s.Priority).ToList();
        }

        /// <summary>
        /// Get action line suggestions
        /// </summary>
        private List<AutocompleteSuggestion> GetActionSuggestions(AutocompleteContext context)
        {
            var suggestions = new List<AutocompleteSuggestion>
            {
                new() 
                { 
                    Text = "", 
                    DisplayText = "Start typing action...", 
                    Description = "Describe what happens in this scene",
                    SuggestedType = ScriptElementType.Action,
                    Priority = 1
                }
            };

            // If user starts typing, suggest common action phrases
            if (!string.IsNullOrEmpty(context.PartialText))
            {
                var commonActions = GetCommonActionPhrases();
                var matching = commonActions
                    .Where(a => a.ToUpper().StartsWith(context.PartialText.ToUpper()))
                    .Take(5);

                foreach (var action in matching)
                {
                    suggestions.Add(new()
                    {
                        Text = action,
                        DisplayText = action,
                        Description = "Common action phrase",
                        SuggestedType = ScriptElementType.Action,
                        Priority = 3
                    });
                }
            }

            return suggestions;
        }

        /// <summary>
        /// Get character name suggestions
        /// </summary>
        private List<AutocompleteSuggestion> GetCharacterSuggestions(AutocompleteContext context)
        {
            var suggestions = new List<AutocompleteSuggestion>();

            if (string.IsNullOrEmpty(context.PartialText))
            {
                // Suggest continuing with same character
                if (!string.IsNullOrEmpty(context.LastCharacterName))
                {
                    suggestions.Add(new()
                    {
                        Text = context.LastCharacterName,
                        DisplayText = $"{context.LastCharacterName} (Same character)",
                        Description = "Continue dialogue with same character",
                        SuggestedType = ScriptElementType.Character,
                        Priority = 5
                    });
                }

                // Suggest other characters
                var otherCharacters = _usedCharacters.Where(c => c != context.LastCharacterName).Take(3);
                foreach (var character in otherCharacters)
                {
                    suggestions.Add(new()
                    {
                        Text = character,
                        DisplayText = character,
                        Description = "Previously used character",
                        SuggestedType = ScriptElementType.Character,
                        Priority = 4
                    });
                }
            }
            else
            {
                // Filter based on typed text
                var matching = _usedCharacters
                    .Where(c => c.StartsWith(context.PartialText.ToUpper()))
                    .Take(5);

                foreach (var character in matching)
                {
                    suggestions.Add(new()
                    {
                        Text = character,
                        DisplayText = character,
                        Description = "Character name",
                        SuggestedType = ScriptElementType.Character,
                        Priority = 4
                    });
                }
            }

            return suggestions;
        }

        /// <summary>
        /// Get parenthetical suggestions (V.O., O.S., etc.)
        /// </summary>
        private List<AutocompleteSuggestion> GetParentheticalSuggestions(AutocompleteContext context)
        {
            var suggestions = new List<AutocompleteSuggestion>
            {
                new()
                {
                    Text = "",
                    DisplayText = "Skip (No parenthetical)",
                    Description = "Press Tab to skip to dialogue",
                    SuggestedType = ScriptElementType.Parenthetical,
                    Priority = 2
                },
                new()
                {
                    Text = "(V.O.)",
                    DisplayText = "(V.O.)",
                    Description = "Voice over - character not on screen",
                    SuggestedType = ScriptElementType.Parenthetical,
                    Priority = 5
                },
                new()
                {
                    Text = "(O.S.)",
                    DisplayText = "(O.S.)",
                    Description = "Off screen - heard but not seen",
                    SuggestedType = ScriptElementType.Parenthetical,
                    Priority = 5
                },
                new()
                {
                    Text = "(O.C.)",
                    DisplayText = "(O.C.)",
                    Description = "Off camera - similar to O.S.",
                    SuggestedType = ScriptElementType.Parenthetical,
                    Priority = 4
                },
                new()
                {
                    Text = "(CONT'D)",
                    DisplayText = "(CONT'D)",
                    Description = "Continuation - character continues from previous",
                    SuggestedType = ScriptElementType.Parenthetical,
                    Priority = 4
                },
                new()
                {
                    Text = "(whispering)",
                    DisplayText = "(whispering)",
                    Description = "Character speaks softly",
                    SuggestedType = ScriptElementType.Parenthetical,
                    Priority = 3
                },
                new()
                {
                    Text = "(shouting)",
                    DisplayText = "(shouting)",
                    Description = "Character speaks loudly",
                    SuggestedType = ScriptElementType.Parenthetical,
                    Priority = 3
                }
            };

            return suggestions;
        }

        /// <summary>
        /// Get dialogue suggestions based on context
        /// </summary>
        private List<AutocompleteSuggestion> GetDialogueSuggestions(AutocompleteContext context)
        {
            var suggestions = new List<AutocompleteSuggestion>
            {
                new()
                {
                    Text = "",
                    DisplayText = "Start typing dialogue...",
                    Description = "Enter what the character says",
                    SuggestedType = ScriptElementType.Dialogue,
                    Priority = 1
                }
            };

            // Suggest common dialogue phrases
            var commonPhrases = GetCommonDialoguePhrases();
            foreach (var phrase in commonPhrases.Take(3))
            {
                suggestions.Add(new()
                {
                    Text = phrase,
                    DisplayText = phrase,
                    Description = "Common dialogue",
                    SuggestedType = ScriptElementType.Dialogue,
                    Priority = 2
                });
            }

            return suggestions;
        }

        /// <summary>
        /// Get transition suggestions
        /// </summary>
        private List<AutocompleteSuggestion> GetTransitionSuggestions(AutocompleteContext context)
        {
            var transitions = new List<AutocompleteSuggestion>
            {
                new() { Text = "CUT TO", DisplayText = "CUT TO", Description = "Abrupt cut to next scene", SuggestedType = ScriptElementType.Transition, Priority = 5 },
                new() { Text = "FADE TO", DisplayText = "FADE TO", Description = "Scene fades to black", SuggestedType = ScriptElementType.Transition, Priority = 4 },
                new() { Text = "DISSOLVE TO", DisplayText = "DISSOLVE TO", Description = "Scene dissolves to next", SuggestedType = ScriptElementType.Transition, Priority = 4 },
                new() { Text = "SMASH CUT TO", DisplayText = "SMASH CUT TO", Description = "Hard, impactful cut", SuggestedType = ScriptElementType.Transition, Priority = 3 },
                new() { Text = "MATCH CUT TO", DisplayText = "MATCH CUT TO", Description = "Cut with matching visuals", SuggestedType = ScriptElementType.Transition, Priority = 3 }
            };

            return transitions;
        }

        /// <summary>
        /// Get scene heading suggestions
        /// </summary>
        private List<AutocompleteSuggestion> GetSceneHeadingSuggestions(AutocompleteContext context)
        {
            var suggestions = new List<AutocompleteSuggestion>();

            // Suggest recent locations
            var recentLocations = _usedLocations.Take(3);
            foreach (var location in recentLocations)
            {
                var times = new[] { "DAY", "NIGHT", "MORNING", "EVENING" };
                foreach (var time in times)
                {
                    suggestions.Add(new()
                    {
                        Text = $"INT. {location} - {time}",
                        DisplayText = $"INT. {location} - {time}",
                        Description = "Interior scene",
                        SuggestedType = ScriptElementType.SceneHeading,
                        Priority = 3
                    });
                }
            }

            return suggestions;
        }

        /// <summary>
        /// Get follow-up suggestions after dialogue
        /// </summary>
        private List<AutocompleteSuggestion> GetFollowUpSuggestions(AutocompleteContext context)
        {
            var suggestions = new List<AutocompleteSuggestion>
            {
                new()
                {
                    Text = "",
                    DisplayText = "Continue with same character",
                    Description = "More dialogue from same character",
                    SuggestedType = ScriptElementType.Character,
                    Priority = 3
                },
                new()
                {
                    Text = "",
                    DisplayText = "New character speaks",
                    Description = "Switch to different character",
                    SuggestedType = ScriptElementType.Character,
                    Priority = 3
                },
                new()
                {
                    Text = "",
                    DisplayText = "Add action",
                    Description = "Describe what happens",
                    SuggestedType = ScriptElementType.Action,
                    Priority = 2
                }
            };

            return suggestions;
        }

        /// <summary>
        /// Common action phrases for suggestions
        /// </summary>
        private List<string> GetCommonActionPhrases()
        {
            return new List<string>
            {
                "He enters the room", "She looks around", "They walk down the street",
                "The camera pans across", "A moment of silence", "Suddenly,",
                "Later that day", "As time passes", "In the distance",
                "Close on", "Wide shot of", "We see", "The door opens",
                "Footsteps approach", "Lights fade", "Music swells"
            };
        }

        /// <summary>
        /// Common dialogue phrases for quick entry
        /// </summary>
        private List<string> GetCommonDialoguePhrases()
        {
            return new List<string>
            {
                "What do you want?", "I don't know.", "That's not right.", "Listen to me.",
                "We need to go.", "I'm sorry.", "You're right.", "What happened?",
                "I don't understand.", "Tell me the truth.", "Get out of here.", "Help me.",
                "I can't believe it.", "This is crazy.", "What now?", "Let's go."
            };
        }

        /// <summary>
        /// Register character usage for learning
        /// </summary>
        public void RegisterCharacter(string characterName)
        {
            if (!string.IsNullOrEmpty(characterName))
            {
                _usedCharacters.Add(characterName);
                if (_characterFrequency.ContainsKey(characterName))
                    _characterFrequency[characterName]++;
                else
                    _characterFrequency[characterName] = 1;
            }
        }

        /// <summary>
        /// Register location usage for learning
        /// </summary>
        public void RegisterLocation(string location)
        {
            if (!string.IsNullOrEmpty(location))
            {
                _usedLocations.Add(location);
                if (_locationFrequency.ContainsKey(location))
                    _locationFrequency[location]++;
                else
                    _locationFrequency[location] = 1;
            }
        }

        /// <summary>
        /// Get most frequently used characters
        /// </summary>
        public List<string> GetFrequentCharacters(int count = 5)
        {
            return _characterFrequency
                .OrderByDescending(x => x.Value)
                .Take(count)
                .Select(x => x.Key)
                .ToList();
        }

        /// <summary>
        /// Get most frequently used locations
        /// </summary>
        public List<string> GetFrequentLocations(int count = 5)
        {
            return _locationFrequency
                .OrderByDescending(x => x.Value)
                .Take(count)
                .Select(x => x.Key)
                .ToList();
        }
    }
}
