using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Tracks characters and locations in a screenplay
    /// Helps with maintaining consistency and providing suggestions
    /// </summary>
    public interface IScreenplayTracker
    {
        void ScanScript(string scriptText);
        List<string> GetAllCharacters();
        List<string> GetAllLocations();
        int GetCharacterLineCount(string characterName);
        List<string> GetCharacterDialogue(string characterName, int maxLines = 10);
        Dictionary<string, int> GetCharacterScreenTime();
        bool IsKnownCharacter(string name);
        bool IsKnownLocation(string location);
    }

    public class ScreenplayTracker : IScreenplayTracker
    {
        private readonly HashSet<string> _characters = new();
        private readonly HashSet<string> _locations = new();
        private readonly Dictionary<string, int> _characterLineCount = new();
        private readonly Dictionary<string, List<string>> _characterDialogue = new();
        private readonly PaginationEngine _pagination;

        public ScreenplayTracker(PageFormatting pageFormat)
        {
            _pagination = new PaginationEngine(pageFormat ?? PageFormatting.StandardLetter());
        }

        /// <summary>
        /// Scans script and extracts all characters and locations
        /// </summary>
        public void ScanScript(string scriptText)
        {
            _characters.Clear();
            _locations.Clear();
            _characterLineCount.Clear();
            _characterDialogue.Clear();

            if (string.IsNullOrEmpty(scriptText)) return;

            var lines = scriptText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                // Extract scene headings (locations)
                if (Regex.IsMatch(line, @"^(INT|EXT|INT/EXT)", RegexOptions.IgnoreCase))
                {
                    string? location = ExtractLocation(line);
                    if (!string.IsNullOrEmpty(location))
                        _locations.Add(location);
                }

                // Extract character names (all caps lines that aren't scene headings)
                if (Regex.IsMatch(line, @"^[A-Z\s]+(\s+\(V\.O\.\)|\s+\(O\.S\.\))?$") && 
                    !line.Contains("INT") && !line.Contains("EXT"))
                {
                    string? characterName = ExtractCharacterName(line);
                    if (!string.IsNullOrEmpty(characterName))
                    {
                        _characters.Add(characterName);

                        // Track line count for this character
                        if (!_characterLineCount.ContainsKey(characterName))
                            _characterLineCount[characterName] = 0;
                        _characterLineCount[characterName]++;

                        // Track dialogue if next line is not blank
                        if (i + 1 < lines.Length && !string.IsNullOrWhiteSpace(lines[i + 1]))
                        {
                            string dialogue = lines[i + 1].Trim();
                            if (!string.IsNullOrEmpty(dialogue) && !dialogue.StartsWith("("))
                            {
                                if (!_characterDialogue.ContainsKey(characterName))
                                    _characterDialogue[characterName] = new List<string>();
                                _characterDialogue[characterName].Add(dialogue);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets all characters found in script
        /// </summary>
        public List<string> GetAllCharacters()
        {
            return _characters.OrderBy(c => c).ToList();
        }

        /// <summary>
        /// Gets all locations found in script
        /// </summary>
        public List<string> GetAllLocations()
        {
            return _locations.OrderBy(l => l).ToList();
        }

        /// <summary>
        /// Gets how many lines a character has
        /// </summary>
        public int GetCharacterLineCount(string characterName)
        {
            string key = characterName.ToUpper();
            return _characterLineCount.ContainsKey(key) ? _characterLineCount[key] : 0;
        }

        /// <summary>
        /// Gets sample dialogue lines for a character
        /// </summary>
        public List<string> GetCharacterDialogue(string characterName, int maxLines = 10)
        {
            string key = characterName.ToUpper();
            if (!_characterDialogue.ContainsKey(key))
                return new List<string>();

            return _characterDialogue[key].Take(maxLines).ToList();
        }

        /// <summary>
        /// Gets estimated screen time for each character
        /// Rough estimate: character line count / 5 ≈ minutes on screen
        /// </summary>
        public Dictionary<string, int> GetCharacterScreenTime()
        {
            var screenTime = new Dictionary<string, int>();

            foreach (var kvp in _characterLineCount)
            {
                // Rough estimate: 5 character appearances ≈ 1 minute
                int estimatedMinutes = Math.Max(1, kvp.Value / 5);
                screenTime[kvp.Key] = estimatedMinutes;
            }

            return screenTime.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// Checks if character name is known in script
        /// </summary>
        public bool IsKnownCharacter(string name)
        {
            return _characters.Contains(name.ToUpper());
        }

        /// <summary>
        /// Checks if location is known in script
        /// </summary>
        public bool IsKnownLocation(string location)
        {
            return _locations.Any(l => l.Contains(location, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Extracts character name from line
        /// </summary>
        private string? ExtractCharacterName(string line)
        {
            // Remove extensions like (V.O.), (O.S.), (CONT'D)
            string cleaned = Regex.Replace(line, @"\s*\(V\.O\.\)\s*|\s*\(O\.S\.\)\s*|\s*\(CONT'D\)\s*", string.Empty);
            cleaned = cleaned.Trim().ToUpper();

            // Verify it's a valid character name (no numbers, no special chars except spaces)
            if (Regex.IsMatch(cleaned, @"^[A-Z\s]+$") && cleaned.Length > 0)
                return cleaned;

            return null;
        }

        /// <summary>
        /// Extracts location from scene heading
        /// </summary>
        private string? ExtractLocation(string line)
        {
            // Pattern: INT/EXT. LOCATION - TIME
            var match = Regex.Match(line, @"(INT|EXT|INT/EXT)\.\s+(.+?)\s*-\s*", RegexOptions.IgnoreCase);
            
            if (match.Success)
            {
                return match.Groups[2].Value.Trim();
            }

            return null;
        }
    }
}
