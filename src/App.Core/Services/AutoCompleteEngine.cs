using System;
using System.Collections.Generic;
using System.Linq;

namespace App.Core.Services
{
    /// <summary>
    /// Provides auto-complete suggestions for screenplay elements
    /// Includes transitions, sluglines, time-of-day, locations, and characters
    /// </summary>
    public class AutoCompleteEngine
    {
        public static readonly List<string> Transitions = new()
        {
            "CUT TO",
            "FADE IN",
            "FADE OUT",
            "DISSOLVE TO",
            "SMASH CUT TO",
            "MATCH CUT TO",
            "WIPE TO",
            "JUMP CUT TO",
            "HARD CUT TO",
            "BACK TO",
            "BACK TO SCENE",
            "BACK TO PRESENT",
            "BACK TO REALITY",
            "IRIS IN",
            "IRIS OUT",
            "FREEZE FRAME",
            "END ON",
            "ON BLACK",
            "CRASH CUT TO",
            "WHIP PAN TO",
            "RIPPLE DISSOLVE TO",
            "FLASH TO",
            "FLASH CUT TO",
            "CUT BACK TO",
            "CUT TO PRESENT",
            "CUT TO FLASHBACK",
            "END OF SCENE",
            "END ACT ONE",
            "END ACT TWO",
            "FADE TO BLACK",
            "FADE UP",
            "FADE UP ON",
            "MONTAGE TO",
            "FADE TO WHITE",
            "FADE FROM BLACK"
        };

        public static readonly List<string> TimeOfDay = new()
        {
            "MORNING",
            "AFTERNOON",
            "EVENING",
            "DUSK",
            "DAWN",
            "DAY",
            "NIGHT",
            "CONTINUOUS",
            "MOMENTS LATER",
            "LATER",
            "SAME TIME",
            "SECONDS LATER",
            "IMMEDIATELY AFTER",
            "NEXT",
            "INTERCUT"
        };

        public static readonly List<string> CommonLocations = new()
        {
            "BEDROOM",
            "KITCHEN",
            "LIVING ROOM",
            "BATHROOM",
            "OFFICE",
            "STREET",
            "PARK",
            "CAR",
            "RESTAURANT",
            "BAR",
            "HOSPITAL",
            "SCHOOL",
            "HOUSE",
            "APARTMENT",
            "STORE",
            "BUILDING",
            "BASEMENT",
            "ROOFTOP",
            "HALLWAY",
            "LOBBY"
        };

        /// <summary>
        /// Search for matching transitions
        /// </summary>
        public static List<string> SearchTransitions(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Transitions.Take(10).ToList();

            var upper = query.ToUpperInvariant();
            return Transitions
                .Where(t => t.Contains(upper, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.IndexOf(upper, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();
        }

        /// <summary>
        /// Search for matching time-of-day
        /// </summary>
        public static List<string> SearchTimeOfDay(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return TimeOfDay.Take(10).ToList();

            var upper = query.ToUpperInvariant();
            return TimeOfDay
                .Where(t => t.Contains(upper, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.IndexOf(upper, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();
        }

        /// <summary>
        /// Search for matching locations
        /// </summary>
        public static List<string> SearchLocations(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return CommonLocations.Take(10).ToList();

            var upper = query.ToUpperInvariant();
            return CommonLocations
                .Where(l => l.Contains(upper, StringComparison.OrdinalIgnoreCase))
                .OrderBy(l => l.IndexOf(upper, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();
        }

        /// <summary>
        /// Get matching slugline suggestions (INT./EXT. LOCATION - TIME)
        /// </summary>
        public static List<string> GenerateSluglineSuggestions(string sluglineQuery = "")
        {
            var suggestions = new List<string>();
            var upper = sluglineQuery.ToUpperInvariant().Trim();

            // Remove the dot if user has already typed it (auto-formatter adds it)
            if (upper.EndsWith("."))
            {
                upper = upper.Substring(0, upper.Length - 1);
            }

            // If user just typed "INT", show base options first
            if (upper == "INT")
            {
                suggestions.Add("INT.");
                suggestions.Add("INT./EXT.");
            }
            else if (upper == "EXT")
            {
                suggestions.Add("EXT.");
                suggestions.Add("EXT./INT.");
            }
            // If user typed "INT." or more, show locations
            else if (upper.StartsWith("INT") && !upper.Contains("-"))
            {
                // Show INT. base option first
                if (!upper.Contains("."))
                {
                    suggestions.Add("INT.");
                }
                
                // Then show common locations
                var locations = CommonLocations.Take(8).ToList();
                foreach (var location in locations)
                {
                    suggestions.Add($"INT. {location}");
                }
            }
            else if (upper.StartsWith("EXT") && !upper.Contains("-"))
            {
                // Show EXT. base option first
                if (!upper.Contains("."))
                {
                    suggestions.Add("EXT.");
                }
                
                // Then show common locations
                var locations = CommonLocations.Take(8).ToList();
                foreach (var location in locations)
                {
                    suggestions.Add($"EXT. {location}");
                }
            }
            // If user has added a dash, show times
            else if (upper.Contains("-"))
            {
                var times = TimeOfDay.Take(10).ToList();
                suggestions.AddRange(times);
            }

            return suggestions;
        }

        private static bool IsInteriorLocation(string location)
        {
            var interiorKeywords = new[] 
            { 
                "BEDROOM", "KITCHEN", "LIVING", "BATHROOM", "OFFICE", 
                "RESTAURANT", "BAR", "HOSPITAL", "SCHOOL", "APARTMENT", 
                "STORE", "BUILDING", "BASEMENT", "HALLWAY", "LOBBY", "HOUSE"
            };

            return interiorKeywords.Any(k => location.Contains(k, StringComparison.OrdinalIgnoreCase));
        }
    }
}
