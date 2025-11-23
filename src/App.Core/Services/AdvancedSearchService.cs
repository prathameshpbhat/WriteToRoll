using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Fade-In Feature: Advanced Find & Replace
    /// 
    /// Features:
    /// - Find and replace text throughout document
    /// - Search for specific screenplay elements
    /// - Navigate through search results
    /// - Pattern matching with regular expressions
    /// - Replace across scenes or specific characters
    /// - Case sensitivity options
    /// - Whole word matching
    /// </summary>
    public class AdvancedSearchService
    {
        public class SearchResult
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string ElementId { get; set; } = string.Empty;
            public int PageNumber { get; set; }
            public int LineNumber { get; set; }
            public string FoundText { get; set; } = string.Empty;
            public int StartPosition { get; set; }
            public int EndPosition { get; set; }
            public string Context { get; set; } = string.Empty;  // Surrounding text for display
            public ScriptElementType ElementType { get; set; }
            public string ElementContent { get; set; } = string.Empty;
        }

        public class SearchOptions
        {
            public string SearchPattern { get; set; } = string.Empty;
            public bool IsCaseSensitive { get; set; } = false;
            public bool IsWholeWordOnly { get; set; } = false;
            public bool IsRegexPattern { get; set; } = false;
            public bool SearchInDialogue { get; set; } = true;
            public bool SearchInAction { get; set; } = true;
            public bool SearchInSceneHeadings { get; set; } = true;
            public bool SearchInCharacterNames { get; set; } = true;
            public bool SearchInParenthetical { get; set; } = true;
            public string SpecificCharacter { get; set; } = string.Empty;  // Limit to character
            public ScriptElementType? SpecificElementType { get; set; }  // Limit to type
        }

        public class ReplaceResult
        {
            public int TotalMatches { get; set; }
            public int Replaced { get; set; }
            public int Skipped { get; set; }
            public List<string> ModifiedElementIds { get; set; } = new();
            public List<string> ReplacementLog { get; set; } = new();
        }

        /// <summary>
        /// Search for text in script
        /// </summary>
        public List<SearchResult> Search(List<ScriptElement> elements, SearchOptions options)
        {
            var results = new List<SearchResult>();

            var regex = CreateSearchRegex(options);

            foreach (var element in elements)
            {
                // Check if we should search this element type
                if (!ShouldSearchElement(element, options))
                    continue;

                var searchResults = SearchElement(element, regex, options);
                results.AddRange(searchResults);
            }

            return results.OrderBy(r => r.PageNumber).ThenBy(r => r.LineNumber).ToList();
        }

        /// <summary>
        /// Find and replace with various options
        /// </summary>
        public ReplaceResult FindAndReplace(List<ScriptElement> elements, string findPattern, string replacePattern, SearchOptions options)
        {
            var result = new ReplaceResult();
            var regex = CreateSearchRegex(options);
            var modifiedIds = new HashSet<string>();

            foreach (var element in elements)
            {
                if (!ShouldSearchElement(element, options))
                    continue;

                var matches = regex.Matches(GetElementSearchText(element, options));
                if (matches.Count > 0)
                {
                    result.TotalMatches += matches.Count;
                    var newText = regex.Replace(GetElementSearchText(element, options), replacePattern);
                    SetElementText(element, newText);
                    result.Replaced += matches.Count;
                    modifiedIds.Add(element.Id);
                    result.ReplacementLog.Add($"Element {element.Id}: Replaced {matches.Count} occurrence(s)");
                }
            }

            result.ModifiedElementIds = modifiedIds.ToList();
            return result;
        }

        /// <summary>
        /// Search for character name and optionally replace
        /// </summary>
        public ReplaceResult RenameCharacter(List<ScriptElement> elements, string oldName, string newName)
        {
            var result = new ReplaceResult();
            var modifiedIds = new HashSet<string>();

            foreach (var element in elements)
            {
                var modified = false;

                // Update character elements
                if (element is CharacterElement charElement && 
                    charElement.Name.Equals(oldName, StringComparison.OrdinalIgnoreCase))
                {
                    charElement.Name = newName;
                    modified = true;
                    result.Replaced++;
                }

                if (modified)
                {
                    modifiedIds.Add(element.Id);
                    result.ReplacementLog.Add($"Renamed character: {oldName} -> {newName} (Element {element.Id})");
                }
            }

            result.ModifiedElementIds = modifiedIds.ToList();
            result.TotalMatches = result.Replaced;
            return result;
        }

        /// <summary>
        /// Create regex from search options
        /// </summary>
        private Regex CreateSearchRegex(SearchOptions options)
        {
            var pattern = options.SearchPattern;

            if (!options.IsRegexPattern)
            {
                // Escape regex special characters for literal search
                pattern = Regex.Escape(pattern);

                if (options.IsWholeWordOnly)
                {
                    pattern = @"\b" + pattern + @"\b";
                }
            }

            var regexOptions = RegexOptions.Compiled;
            if (!options.IsCaseSensitive)
                regexOptions |= RegexOptions.IgnoreCase;

            return new Regex(pattern, regexOptions);
        }

        /// <summary>
        /// Determine if element should be searched based on options
        /// </summary>
        private bool ShouldSearchElement(ScriptElement element, SearchOptions options)
        {
            // Check element type filters
            if (options.SpecificElementType.HasValue && element.ElementType != options.SpecificElementType)
                return false;

            // Check what types to search
            switch (element.ElementType)
            {
                case ScriptElementType.Dialogue:
                    return options.SearchInDialogue;
                case ScriptElementType.Action:
                    return options.SearchInAction;
                case ScriptElementType.SceneHeading:
                    return options.SearchInSceneHeadings;
                case ScriptElementType.Character:
                    return options.SearchInCharacterNames;
                case ScriptElementType.Parenthetical:
                    return options.SearchInParenthetical;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Get text to search from element based on options
        /// </summary>
        private string GetElementSearchText(ScriptElement element, SearchOptions options)
        {
            return element.Text;
        }

        /// <summary>
        /// Set element text (handles different element types)
        /// </summary>
        private void SetElementText(ScriptElement element, string newText)
        {
            element.Text = newText;
            element.ModifiedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Search individual element
        /// </summary>
        private List<SearchResult> SearchElement(ScriptElement element, Regex regex, SearchOptions options)
        {
            var results = new List<SearchResult>();
            var searchText = GetElementSearchText(element, options);

            var matches = regex.Matches(searchText);
            foreach (Match match in matches)
            {
                var context = GetContext(searchText, match.Index, 40);
                var result = new SearchResult
                {
                    ElementId = element.Id,
                    PageNumber = element.PageNumber,
                    LineNumber = element.LineNumber,
                    FoundText = match.Value,
                    StartPosition = match.Index,
                    EndPosition = match.Index + match.Length,
                    Context = context,
                    ElementType = element.ElementType,
                    ElementContent = searchText
                };
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// Get surrounding context for search result display
        /// </summary>
        private string GetContext(string text, int matchIndex, int contextLength)
        {
            var startIndex = Math.Max(0, matchIndex - contextLength);
            var endIndex = Math.Min(text.Length, matchIndex + contextLength);

            var prefix = startIndex > 0 ? "..." : "";
            var suffix = endIndex < text.Length ? "..." : "";

            return prefix + text.Substring(startIndex, endIndex - startIndex) + suffix;
        }

        /// <summary>
        /// Search for scene headings with specific location
        /// </summary>
        public List<SearchResult> FindScenesWithLocation(List<ScriptElement> elements, string location)
        {
            var results = new List<SearchResult>();
            var regex = new Regex(Regex.Escape(location), RegexOptions.IgnoreCase);

            foreach (var element in elements.OfType<SceneHeadingElement>())
            {
                var matches = regex.Matches(element.Text);
                foreach (Match match in matches)
                {
                    results.Add(new SearchResult
                    {
                        ElementId = element.Id,
                        PageNumber = element.PageNumber,
                        FoundText = location,
                        ElementType = ScriptElementType.SceneHeading,
                        ElementContent = element.Text
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Search for dialogue patterns (e.g., find all questions)
        /// </summary>
        public List<SearchResult> FindDialoguePatterns(List<ScriptElement> elements, DialoguePattern pattern)
        {
            var regex = GetDialoguePatternRegex(pattern);
            var results = new List<SearchResult>();

            foreach (var element in elements.OfType<DialogueElement>())
            {
                var matches = regex.Matches(element.Text);
                foreach (Match match in matches)
                {
                    results.Add(new SearchResult
                    {
                        ElementId = element.Id,
                        PageNumber = element.PageNumber,
                        FoundText = match.Value,
                        ElementType = ScriptElementType.Dialogue,
                        ElementContent = element.Text
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// Get regex for dialogue patterns
        /// </summary>
        private Regex GetDialoguePatternRegex(DialoguePattern pattern)
        {
            return pattern switch
            {
                DialoguePattern.Questions => new Regex(@".*\?.*", RegexOptions.Compiled),
                DialoguePattern.Exclamations => new Regex(@".*!.*", RegexOptions.Compiled),
                DialoguePattern.AllCaps => new Regex(@"^[A-Z\s]+$", RegexOptions.Compiled),
                DialoguePattern.OneWord => new Regex(@"^\w+$", RegexOptions.Compiled),
                DialoguePattern.Parenthetical => new Regex(@"\([^)]*\)", RegexOptions.Compiled),
                _ => new Regex(@".*", RegexOptions.Compiled)
            };
        }

        /// <summary>
        /// Get statistics on search results
        /// </summary>
        public string GenerateSearchStats(List<SearchResult> results)
        {
            var sb = new System.Text.StringBuilder();
            var byElementType = results.GroupBy(r => r.ElementType);
            var byPage = results.GroupBy(r => r.PageNumber);

            sb.AppendLine("=== SEARCH STATISTICS ===");
            sb.AppendLine($"Total Matches: {results.Count}");
            sb.AppendLine();
            sb.AppendLine("By Element Type:");
            foreach (var group in byElementType.OrderByDescending(g => g.Count()))
            {
                sb.AppendLine($"  {group.Key}: {group.Count()}");
            }
            sb.AppendLine();
            sb.AppendLine($"Pages with matches: {byPage.Count()}");

            return sb.ToString();
        }
    }

    public enum DialoguePattern
    {
        Questions,
        Exclamations,
        AllCaps,
        OneWord,
        Parenthetical
    }
}
