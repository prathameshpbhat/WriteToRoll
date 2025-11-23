using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Fade-In Feature: Dialogue Tuner
    /// Advanced rewriting tool for analyzing and editing character consistency
    /// 
    /// Features:
    /// - View all dialogue for a single character in one place
    /// - Edit character dialogue collectively for consistency
    /// - Analyze character voice (word frequency, patterns)
    /// - Identify overused words by character
    /// - Ensure character consistency across entire screenplay
    /// </summary>
    public class DialogueTuner
    {
        public class CharacterDialogueAnalysis
        {
            public string CharacterName { get; set; } = string.Empty;
            public int TotalDialogueSections { get; set; }
            public int TotalWordCount { get; set; }
            public int TotalLineCount { get; set; }
            public double AverageWordsPerLine { get; set; }
            public List<DialogueSegment> DialogueSegments { get; set; } = new();
            public Dictionary<string, int> WordFrequency { get; set; } = new();
            public List<string> OverusedWords { get; set; } = new();
            public List<string> UniqueWords { get; set; } = new();
            public Dictionary<string, int> CommonPhrases { get; set; } = new();
            public double UniquenessScore { get; set; }  // 0-1, higher = more unique
        }

        public class DialogueSegment
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string CharacterName { get; set; } = string.Empty;
            public string DialogueText { get; set; } = string.Empty;
            public int LineNumber { get; set; }
            public int PageNumber { get; set; }
            public int SceneIndex { get; set; }
            public string SceneHeading { get; set; } = string.Empty;
            public string Parenthetical { get; set; } = string.Empty;
            public int WordCount { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public bool IsFlagged { get; set; }
            public string Flag { get; set; } = string.Empty;
        }

        private const int OverusedWordThreshold = 3;  // Words appearing 3+ times
        private const int CommonPhraseMinWords = 2;
        private const int CommonPhraseMinOccurrences = 2;

        /// <summary>
        /// Analyze all dialogue for a specific character
        /// </summary>
        public CharacterDialogueAnalysis AnalyzeCharacterDialogue(string characterName, List<ScriptElement> elements)
        {
            var analysis = new CharacterDialogueAnalysis { CharacterName = characterName };
            var dialogueSegments = new List<DialogueSegment>();

            // Find all dialogue segments for this character
            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i] is DialogueElement dialogue && 
                    IsCharacterDialogue(dialogue, characterName, elements, i))
                {
                    var parenthetical = GetAssociatedParenthetical(elements, i);
                    var segment = new DialogueSegment
                    {
                        CharacterName = characterName,
                        DialogueText = dialogue.Text,
                        LineNumber = dialogue.LineNumber,
                        PageNumber = dialogue.PageNumber,
                        SceneIndex = i,
                        SceneHeading = GetAssociatedSceneHeading(elements, i),
                        Parenthetical = parenthetical,
                        WordCount = CountWords(dialogue.Text)
                    };
                    dialogueSegments.Add(segment);
                }
            }

            analysis.DialogueSegments = dialogueSegments;
            analysis.TotalDialogueSections = dialogueSegments.Count;
            analysis.TotalWordCount = dialogueSegments.Sum(s => s.WordCount);
            analysis.TotalLineCount = dialogueSegments.Sum(s => CountLines(s.DialogueText));
            analysis.AverageWordsPerLine = analysis.TotalLineCount > 0 
                ? (double)analysis.TotalWordCount / analysis.TotalLineCount 
                : 0;

            // Analyze word frequency
            AnalyzeWordFrequency(analysis);
            
            // Find overused words
            FindOverusedWords(analysis);
            
            // Extract unique words
            analysis.UniqueWords = analysis.WordFrequency
                .Where(x => x.Value == 1)
                .Select(x => x.Key)
                .ToList();

            // Find common phrases
            FindCommonPhrases(analysis);

            // Calculate uniqueness score
            analysis.UniquenessScore = CalculateUniquenessScore(analysis);

            return analysis;
        }

        /// <summary>
        /// Check if dialogue belongs to character (handles V.O., O.S., CONT'D)
        /// </summary>
        private bool IsCharacterDialogue(DialogueElement dialogue, string characterName, List<ScriptElement> elements, int index)
        {
            // Look backwards to find the character name element
            for (int i = index - 1; i >= 0; i--)
            {
                if (elements[i] is CharacterElement charElement)
                {
                    return charElement.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase);
                }
                if (elements[i] is SceneHeadingElement)
                    break;  // Stop searching if we hit another scene heading
            }
            return false;
        }

        /// <summary>
        /// Get parenthetical associated with dialogue (V.O., O.S., etc.)
        /// </summary>
        private string GetAssociatedParenthetical(List<ScriptElement> elements, int dialogueIndex)
        {
            // Look backwards for parenthetical immediately before dialogue
            for (int i = dialogueIndex - 1; i >= 0; i--)
            {
                if (elements[i] is ParentheticalElement paren)
                    return paren.Text;
                if (!(elements[i] is CharacterElement))
                    break;
            }
            return string.Empty;
        }

        /// <summary>
        /// Get scene heading associated with dialogue
        /// </summary>
        private string GetAssociatedSceneHeading(List<ScriptElement> elements, int index)
        {
            // Look backwards for most recent scene heading
            for (int i = index; i >= 0; i--)
            {
                if (elements[i] is SceneHeadingElement scene)
                    return scene.Text;
            }
            return "Unknown Scene";
        }

        /// <summary>
        /// Analyze word frequency for character
        /// </summary>
        private void AnalyzeWordFrequency(CharacterDialogueAnalysis analysis)
        {
            var wordFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var commonStopWords = GetStopWords();

            foreach (var segment in analysis.DialogueSegments)
            {
                var words = segment.DialogueText.ToLower()
                    .Split(new[] { ' ', '\t', '\n', '\r', ',', '.', '!', '?', ';', ':', '"', '-' }, 
                           StringSplitOptions.RemoveEmptyEntries);

                foreach (var word in words)
                {
                    if (word.Length > 2 && !commonStopWords.Contains(word))  // Skip short & common words
                    {
                        if (wordFrequency.ContainsKey(word))
                            wordFrequency[word]++;
                        else
                            wordFrequency[word] = 1;
                    }
                }
            }

            analysis.WordFrequency = wordFrequency.OrderByDescending(x => x.Value)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// Find words that appear too frequently
        /// </summary>
        private void FindOverusedWords(CharacterDialogueAnalysis analysis)
        {
            analysis.OverusedWords = analysis.WordFrequency
                .Where(x => x.Value >= OverusedWordThreshold)
                .Select(x => $"{x.Key} ({x.Value}x)")
                .ToList();
        }

        /// <summary>
        /// Find common phrases (2+ word sequences appearing 2+ times)
        /// </summary>
        private void FindCommonPhrases(CharacterDialogueAnalysis analysis)
        {
            var phraseFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var segment in analysis.DialogueSegments)
            {
                var words = segment.DialogueText.ToLower()
                    .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                // Find 2-word and 3-word phrases
                for (int i = 0; i < words.Length - 1; i++)
                {
                    var phrase = $"{words[i]} {words[i + 1]}";
                    if (phraseFrequency.ContainsKey(phrase))
                        phraseFrequency[phrase]++;
                    else
                        phraseFrequency[phrase] = 1;
                }
            }

            analysis.CommonPhrases = phraseFrequency
                .Where(x => x.Value >= CommonPhraseMinOccurrences)
                .OrderByDescending(x => x.Value)
                .ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// Calculate how unique/distinctive character's dialogue is (0-1 scale)
        /// </summary>
        private double CalculateUniquenessScore(CharacterDialogueAnalysis analysis)
        {
            if (analysis.UniqueWords.Count == 0)
                return 0;

            // Higher ratio of unique words = more distinctive
            double uniqueWordRatio = (double)analysis.UniqueWords.Count / 
                                   (analysis.WordFrequency.Count > 0 ? analysis.WordFrequency.Count : 1);
            
            // Penalize if too many overused words
            double overusedPenalty = (double)analysis.OverusedWords.Count / Math.Max(1, analysis.WordFrequency.Count);
            
            return Math.Clamp(uniqueWordRatio - overusedPenalty, 0, 1);
        }

        /// <summary>
        /// Check for inconsistencies in character voice across dialogue
        /// </summary>
        public List<string> FindInconsistencies(CharacterDialogueAnalysis analysis)
        {
            var issues = new List<string>();

            // Check for dialogue that doesn't match character's typical style
            if (analysis.DialogueSegments.Count > 0)
            {
                var avgWords = analysis.AverageWordsPerLine;
                foreach (var segment in analysis.DialogueSegments)
                {
                    var wordCount = segment.WordCount;
                    // Flag if dialogue is significantly different from average
                    if (wordCount > avgWords * 2 || (wordCount < avgWords * 0.5 && avgWords > 5))
                    {
                        segment.IsFlagged = true;
                        segment.Flag = "Unusual line length";
                        issues.Add($"Line {segment.LineNumber}: Unusual length ({wordCount} words vs avg {avgWords:F1})");
                    }
                }
            }

            // Check for sudden changes in vocabulary
            if (analysis.DialogueSegments.Count > 10)
            {
                var firstHalf = analysis.DialogueSegments.Take(analysis.DialogueSegments.Count / 2).ToList();
                var secondHalf = analysis.DialogueSegments.Skip(analysis.DialogueSegments.Count / 2).ToList();
                
                var firstHalfVocabSize = ExtractUniqueWords(firstHalf).Count;
                var secondHalfVocabSize = ExtractUniqueWords(secondHalf).Count;
                
                if (firstHalfVocabSize > 0 && secondHalfVocabSize > 0)
                {
                    double vocabChange = Math.Abs((double)(secondHalfVocabSize - firstHalfVocabSize) / firstHalfVocabSize);
                    if (vocabChange > 0.5)  // More than 50% change
                    {
                        issues.Add($"Significant vocabulary shift between first and second half ({vocabChange:P})");
                    }
                }
            }

            return issues;
        }

        /// <summary>
        /// Get all words used in dialogue segments
        /// </summary>
        private HashSet<string> ExtractUniqueWords(List<DialogueSegment> segments)
        {
            var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var stopWords = GetStopWords();

            foreach (var segment in segments)
            {
                var segmentWords = segment.DialogueText.ToLower()
                    .Split(new[] { ' ', '\t', '\n', '\r', ',', '.', '!', '?', ';', ':', '"', '-' }, 
                           StringSplitOptions.RemoveEmptyEntries);

                foreach (var word in segmentWords)
                {
                    if (word.Length > 2 && !stopWords.Contains(word))
                        words.Add(word);
                }
            }

            return words;
        }

        /// <summary>
        /// Suggest word replacements for overused words
        /// </summary>
        public Dictionary<string, List<string>> GetAlternativeWords(CharacterDialogueAnalysis analysis)
        {
            var alternatives = new Dictionary<string, List<string>>();
            
            var wordThesaurus = GetSimpleThesaurus();

            foreach (var overusedWord in analysis.OverusedWords)
            {
                var word = overusedWord.Split('(')[0].Trim().ToLower();
                if (wordThesaurus.ContainsKey(word))
                {
                    alternatives[word] = wordThesaurus[word];
                }
            }

            return alternatives;
        }

        /// <summary>
        /// Count words in text
        /// </summary>
        private int CountWords(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        /// <summary>
        /// Count lines in text (paragraph breaks)
        /// </summary>
        private int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text)) return 1;
            return text.Split(new[] { '\n', '\r' }, StringSplitOptions.None).Length;
        }

        /// <summary>
        /// Get common stop words to exclude from analysis
        /// </summary>
        private static HashSet<string> GetStopWords()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by",
                "from", "up", "about", "into", "through", "during", "is", "are", "was", "be", "been",
                "being", "have", "has", "had", "do", "does", "did", "will", "would", "could", "should",
                "may", "might", "must", "can", "this", "that", "these", "those", "i", "you", "he", "she",
                "it", "we", "they", "what", "which", "who", "when", "where", "why", "how"
            };
        }

        /// <summary>
        /// Simple thesaurus for common words
        /// </summary>
        private static Dictionary<string, List<string>> GetSimpleThesaurus()
        {
            return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "good", new List<string> { "great", "excellent", "fine", "nice", "solid", "well" } },
                { "bad", new List<string> { "poor", "terrible", "awful", "dreadful", "horrible", "nasty" } },
                { "said", new List<string> { "mentioned", "stated", "explained", "noted", "remarked", "replied" } },
                { "very", new List<string> { "extremely", "incredibly", "remarkably", "exceptionally" } },
                { "look", new List<string> { "see", "watch", "observe", "view", "notice", "glance" } },
                { "think", new List<string> { "believe", "suppose", "consider", "ponder", "reflect", "contemplate" } },
                { "want", new List<string> { "desire", "wish", "need", "crave", "seek", "require" } },
                { "know", new List<string> { "understand", "realize", "comprehend", "recognize", "grasp" } },
                { "go", new List<string> { "move", "proceed", "head", "travel", "journey", "venture" } },
                { "get", new List<string> { "obtain", "acquire", "receive", "fetch", "grab", "secure" } }
            };
        }
    }
}
