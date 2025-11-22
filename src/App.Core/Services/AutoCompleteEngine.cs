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

        private static readonly string[] SluglinePrefixes = new[]
        {
            "INT.",
            "EXT.",
            "INT./EXT.",
            "EXT./INT."
        };

        private static readonly List<string> ParentheticalSuggestions = new()
        {
            "(WHISPER)",
            "(SHOUTS)",
            "(BEAT)",
            "(CONT'D)",
            "(V.O.)",
            "(O.S.)",
            "(O.C.)",
            "(SOFTLY)",
            "(INTO PHONE)",
            "(ON RADIO)",
            "(TO CAMERA)",
            "(ASIDE)"
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
            sluglineQuery ??= string.Empty;

            var upper = sluglineQuery.TrimStart().ToUpperInvariant();
            var suggestions = new List<string>();

            var prefixInfo = ResolveSluglinePrefix(upper);
            if (!prefixInfo.HasPrefix)
            {
                return BuildPrefixSuggestionList(upper);
            }

            var remainder = prefixInfo.Remainder.TrimStart();
            if (string.IsNullOrEmpty(remainder))
            {
                suggestions.Add(prefixInfo.Prefix);
                suggestions.AddRange(
                    FilterLocations(string.Empty)
                        .Select(location => $"{prefixInfo.Prefix} {location}")
                );

                return suggestions
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(10)
                    .ToList();
            }

            var dashIndex = remainder.IndexOf('-');
            var hasDash = dashIndex >= 0;
            var locationPart = hasDash
                ? remainder.Substring(0, dashIndex).Trim()
                : remainder.Trim();
            var timePart = hasDash
                ? remainder.Substring(dashIndex + 1).Trim()
                : string.Empty;

            if (!hasDash)
            {
                var locationMatches = FilterLocations(locationPart)
                    .Select(location => $"{prefixInfo.Prefix} {location}");

                suggestions.AddRange(locationMatches);

                if (!string.IsNullOrEmpty(locationPart))
                {
                    suggestions.Add($"{prefixInfo.Prefix} {locationPart} - ");
                }
            }
            else
            {
                if (string.IsNullOrEmpty(locationPart))
                {
                    locationPart = "LOCATION";
                }

                var timeMatches = FilterTimes(timePart)
                    .Select(time => $"{prefixInfo.Prefix} {locationPart} - {time}");

                suggestions.AddRange(timeMatches);
            }

            return suggestions
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();
        }

        private static List<string> BuildPrefixSuggestionList(string prefixSeed)
        {
            var seeds = new List<string>();

            if (string.IsNullOrWhiteSpace(prefixSeed))
            {
                seeds.AddRange(SluglinePrefixes);
            }
            else
            {
                var matches = SluglinePrefixes
                    .Where(p => p.StartsWith(prefixSeed, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matches.Count == 0)
                {
                    seeds.AddRange(SluglinePrefixes);
                }
                else
                {
                    seeds.AddRange(matches);
                }
            }

            return seeds
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(10)
                .ToList();
        }

        private static IEnumerable<string> FilterLocations(string userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput))
                return CommonLocations.Take(10);

            return CommonLocations
                .Where(location => location.Contains(userInput, StringComparison.OrdinalIgnoreCase))
                .OrderBy(location => location.StartsWith(userInput, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(location => location.IndexOf(userInput, StringComparison.OrdinalIgnoreCase))
                .Take(10);
        }

        private static IEnumerable<string> FilterTimes(string userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput))
                return TimeOfDay.Take(10);

            return TimeOfDay
                .Where(time => time.Contains(userInput, StringComparison.OrdinalIgnoreCase))
                .OrderBy(time => time.IndexOf(userInput, StringComparison.OrdinalIgnoreCase))
                .Take(10);
        }

        public static List<string> SearchParentheticals(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return ParentheticalSuggestions.Take(10).ToList();

            var trimmed = query.Trim();
            if (!trimmed.StartsWith("(", StringComparison.Ordinal))
            {
                trimmed = "(" + trimmed;
            }
            if (!trimmed.EndsWith(")", StringComparison.Ordinal))
            {
                trimmed += ")";
            }

            var token = trimmed.Trim('(', ')');

            return ParentheticalSuggestions
                .Prepend(trimmed.ToUpperInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(p => p.Contains(token, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();
        }

        private static (bool HasPrefix, string Prefix, string Remainder) ResolveSluglinePrefix(string upper)
        {
            if (string.IsNullOrWhiteSpace(upper))
                return (false, string.Empty, string.Empty);

            foreach (var prefix in SluglinePrefixes.OrderByDescending(p => p.Length))
            {
                if (upper.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return (true, prefix, upper.Substring(prefix.Length));
                }
            }

            if (upper.StartsWith("INT/EXT", StringComparison.OrdinalIgnoreCase))
            {
                var remainder = upper.Substring("INT/EXT".Length);
                return (true, "INT./EXT.", remainder);
            }

            if (upper.StartsWith("EXT/INT", StringComparison.OrdinalIgnoreCase))
            {
                var remainder = upper.Substring("EXT/INT".Length);
                return (true, "EXT./INT.", remainder);
            }

            if (upper.StartsWith("INT", StringComparison.OrdinalIgnoreCase))
            {
                var remainder = upper.Length > 3 ? upper.Substring(3) : string.Empty;
                return (true, "INT.", remainder);
            }

            if (upper.StartsWith("EXT", StringComparison.OrdinalIgnoreCase))
            {
                var remainder = upper.Length > 3 ? upper.Substring(3) : string.Empty;
                return (true, "EXT.", remainder);
            }

            return (false, string.Empty, string.Empty);
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
