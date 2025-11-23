using System;
using System.Collections.Generic;
using System.Linq;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Fade-In Feature: Real-time Writing Metrics & Statistics
    /// 
    /// Features:
    /// - Script length tracking
    /// - Page count estimation
    /// - Character/dialogue analysis
    /// - Scene statistics
    /// - Writing pace analysis
    /// - Character appearance tracking
    /// - Screenplay health metrics
    /// </summary>
    public class WritingMetricsService
    {
        public class ScriptMetrics
        {
            public int TotalElements { get; set; }
            public int ActionCount { get; set; }
            public int DialogueCount { get; set; }
            public int CharacterCount { get; set; }
            public int ParentheticalCount { get; set; }
            public int TransitionCount { get; set; }
            public int SceneHeadingCount { get; set; }
            public int TitlePageCount { get; set; }
        }

        public class PageMetrics
        {
            public double EstimatedPages { get; set; }
            public double EstimatedMinutes { get; set; }
            public string ScreenplayFormat { get; set; } = "Feature";
            public double WordCount { get; set; }
            public double LineCount { get; set; }
        }

        public class CharacterMetrics
        {
            public string CharacterName { get; set; } = string.Empty;
            public int TotalAppearances { get; set; }
            public int LineCount { get; set; }
            public double WordsSpoken { get; set; }
            public List<int> SceneAppearances { get; set; } = new();
            public bool IsProtagionist { get; set; }
            public double PercentageOfDialogue { get; set; }
        }

        public class SceneMetrics
        {
            public int SceneNumber { get; set; }
            public string Heading { get; set; } = string.Empty;
            public int ElementCount { get; set; }
            public int CharacterCount { get; set; }
            public int DialogueLineCount { get; set; }
            public int ActionWordCount { get; set; }
            public double EstimatedDuration { get; set; }
            public string TimeOfDay { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
        }

        public class HealthMetrics
        {
            public double DialogueActionRatio { get; set; }
            public double AverageSceneLength { get; set; }
            public int LongestScene { get; set; }
            public int ShortestScene { get; set; }
            public double CharacterDistribution { get; set; }
            public string HealthScore { get; set; } = string.Empty;
            public List<string> Recommendations { get; set; } = new();
        }

        private const double WORDS_PER_PAGE = 250.0;
        private const double MINUTES_PER_PAGE = 1.0;
        private const double WORDS_PER_MINUTE = 130.0;

        /// <summary>
        /// Analyze entire script metrics
        /// </summary>
        public ScriptMetrics AnalyzeScriptMetrics(Script script)
        {
            var metrics = new ScriptMetrics();

            if (script?.Elements == null)
                return metrics;

            metrics.TotalElements = script.Elements.Count;
            metrics.ActionCount = script.Elements.OfType<ActionElement>().Count();
            metrics.DialogueCount = script.Elements.OfType<DialogueElement>().Count();
            metrics.CharacterCount = script.Elements.OfType<CharacterElement>().Count();
            metrics.ParentheticalCount = script.Elements.OfType<ParentheticalElement>().Count();
            metrics.TransitionCount = script.Elements.OfType<TransitionElement>().Count();
            metrics.SceneHeadingCount = script.Elements.OfType<SceneHeadingElement>().Count();
            metrics.TitlePageCount = script.Elements.OfType<TitlePageElement>().Count();

            return metrics;
        }

        /// <summary>
        /// Calculate page metrics
        /// </summary>
        public PageMetrics CalculatePageMetrics(Script script)
        {
            var metrics = new PageMetrics();
            var wordCount = 0.0;
            var lineCount = 0;

            if (script?.Elements == null)
                return metrics;

            foreach (var element in script.Elements)
            {
                var text = element switch
                {
                    ActionElement ae => ae.Text,
                    DialogueElement de => de.Text,
                    CharacterElement ce => ce.Name,
                    TransitionElement te => te.Text,
                    SceneHeadingElement she => she.Text,
                    _ => string.Empty
                };

                if (!string.IsNullOrEmpty(text))
                {
                    wordCount += text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                    lineCount += text.Split('\n').Length;
                }
            }

            metrics.WordCount = wordCount;
            metrics.LineCount = lineCount;
            metrics.EstimatedPages = Math.Round(wordCount / WORDS_PER_PAGE, 1);
            metrics.EstimatedMinutes = Math.Round(metrics.EstimatedPages * MINUTES_PER_PAGE, 1);

            return metrics;
        }

        /// <summary>
        /// Analyze character metrics
        /// </summary>
        public List<CharacterMetrics> AnalyzeCharacterMetrics(Script script)
        {
            var characterMetrics = new Dictionary<string, CharacterMetrics>();
            var totalDialogueWords = 0.0;

            if (script?.Elements == null)
                return new List<CharacterMetrics>();

            // Count dialogue
            foreach (var element in script.Elements)
            {
                if (element is CharacterElement ce && !string.IsNullOrWhiteSpace(ce.Name))
                {
                    var name = ce.Name.Trim().ToUpper();
                    
                    if (!characterMetrics.ContainsKey(name))
                    {
                        characterMetrics[name] = new CharacterMetrics { CharacterName = name };
                    }

                    characterMetrics[name].TotalAppearances++;
                }
                else if (element is DialogueElement de && !string.IsNullOrWhiteSpace(de.Text))
                {
                    // Assign to last character mentioned
                    CharacterElement? lastCharacter = null;
                    for (int i = script.Elements.IndexOf(de) - 1; i >= 0; i--)
                    {
                        if (script.Elements[i] is CharacterElement charElem)
                        {
                            lastCharacter = charElem;
                            break;
                        }
                    }

                    if (lastCharacter != null)
                    {
                        var name = lastCharacter.Name?.Trim().ToUpper() ?? "UNKNOWN";
                        if (characterMetrics.ContainsKey(name))
                        {
                            var words = de.Text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
                            characterMetrics[name].LineCount++;
                            characterMetrics[name].WordsSpoken += words;
                            totalDialogueWords += words;
                        }
                    }
                }
            }

            // Calculate percentages
            if (totalDialogueWords > 0)
            {
                foreach (var metric in characterMetrics.Values)
                {
                    metric.PercentageOfDialogue = Math.Round((metric.WordsSpoken / totalDialogueWords) * 100, 1);
                    metric.IsProtagionist = metric.PercentageOfDialogue > 25;
                }
            }

            return characterMetrics.Values.OrderByDescending(m => m.WordsSpoken).ToList();
        }

        /// <summary>
        /// Analyze individual scene metrics
        /// </summary>
        public List<SceneMetrics> AnalyzeSceneMetrics(Script script)
        {
            var sceneMetrics = new List<SceneMetrics>();
            var currentScene = new SceneMetrics { SceneNumber = 1 };
            var sceneIndex = 0;

            if (script?.Elements == null)
                return sceneMetrics;

            foreach (var element in script.Elements)
            {
                if (element is SceneHeadingElement she)
                {
                    if (currentScene.ElementCount > 0)
                    {
                        sceneMetrics.Add(currentScene);
                    }

                    sceneIndex++;
                    currentScene = new SceneMetrics
                    {
                        SceneNumber = sceneIndex,
                        Heading = she.Text
                    };

                    // Parse heading for location and time
                    ParseSceneHeading(she.Text, currentScene);
                }
                else
                {
                    currentScene.ElementCount++;

                    if (element is ActionElement ae)
                    {
                        currentScene.ActionWordCount += ae.Text?.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length ?? 0;
                    }
                    else if (element is CharacterElement ce && !string.IsNullOrWhiteSpace(ce.Name))
                    {
                        currentScene.CharacterCount++;
                    }
                    else if (element is DialogueElement de)
                    {
                        currentScene.DialogueLineCount++;
                    }
                }
            }

            // Add last scene
            if (currentScene.ElementCount > 0)
            {
                sceneMetrics.Add(currentScene);
            }

            // Calculate duration estimates
            foreach (var scene in sceneMetrics)
            {
                scene.EstimatedDuration = scene.ActionWordCount / WORDS_PER_MINUTE;
            }

            return sceneMetrics;
        }

        /// <summary>
        /// Calculate screenplay health metrics
        /// </summary>
        public HealthMetrics CalculateHealthMetrics(Script script)
        {
            var health = new HealthMetrics();
            var scriptMetrics = AnalyzeScriptMetrics(script);
            var characterMetrics = AnalyzeCharacterMetrics(script);
            var sceneMetrics = AnalyzeSceneMetrics(script);

            // Calculate ratios
            if (scriptMetrics.ActionCount > 0 && scriptMetrics.DialogueCount > 0)
            {
                health.DialogueActionRatio = Math.Round((double)scriptMetrics.DialogueCount / scriptMetrics.ActionCount, 2);
            }

            // Average scene length
            if (sceneMetrics.Count > 0)
            {
                health.AverageSceneLength = Math.Round(sceneMetrics.Average(s => s.ElementCount), 1);
                health.LongestScene = sceneMetrics.Max(s => s.ElementCount);
                health.ShortestScene = sceneMetrics.Min(s => s.ElementCount);
            }

            // Character distribution
            if (characterMetrics.Count > 0)
            {
                var mainCharacterLines = characterMetrics.Where(c => c.IsProtagionist).Sum(c => c.LineCount);
                var totalLines = characterMetrics.Sum(c => c.LineCount);
                if (totalLines > 0)
                {
                    health.CharacterDistribution = Math.Round((double)mainCharacterLines / totalLines * 100, 1);
                }
            }

            // Generate health score and recommendations
            GenerateHealthAssessment(health, scriptMetrics, characterMetrics, sceneMetrics);

            return health;
        }

        /// <summary>
        /// Generate health score and recommendations
        /// </summary>
        private void GenerateHealthAssessment(HealthMetrics health, ScriptMetrics scriptMetrics, 
            List<CharacterMetrics> characters, List<SceneMetrics> scenes)
        {
            var score = 100;
            health.Recommendations = new List<string>();

            // Check dialogue/action balance
            if (health.DialogueActionRatio < 0.5)
            {
                score -= 10;
                health.Recommendations.Add("Add more dialogue for character development");
            }
            else if (health.DialogueActionRatio > 2.0)
            {
                score -= 10;
                health.Recommendations.Add("Add more action to break up dialogue");
            }

            // Check scene variety
            if (health.LongestScene > 50)
            {
                score -= 15;
                health.Recommendations.Add("Consider breaking up longer scenes");
            }

            if (health.ShortestScene < 3)
            {
                score -= 5;
                health.Recommendations.Add("Extend very short scenes for better pacing");
            }

            // Check character development
            if (characters.Count > 0)
            {
                var topCharacter = characters.First();
                if (topCharacter.PercentageOfDialogue > 50)
                {
                    score -= 15;
                    health.Recommendations.Add("Develop supporting characters more");
                }

                if (characters.Count < 3)
                {
                    score -= 10;
                    health.Recommendations.Add("Introduce more characters for variety");
                }
            }

            // Check script length
            if (scriptMetrics.SceneHeadingCount < 5)
            {
                score -= 10;
                health.Recommendations.Add("Script may be too short; add more scenes");
            }

            health.HealthScore = score switch
            {
                >= 90 => "EXCELLENT",
                >= 80 => "GOOD",
                >= 70 => "FAIR",
                >= 60 => "NEEDS WORK",
                _ => "POOR"
            };
        }

        /// <summary>
        /// Parse scene heading for location and time
        /// </summary>
        private void ParseSceneHeading(string heading, SceneMetrics metrics)
        {
            var parts = heading.Split('-');
            if (parts.Length > 0)
            {
                metrics.Location = parts[0].Trim();
            }

            if (parts.Length > 1)
            {
                var timeText = parts[1].Trim().ToUpper();
                metrics.TimeOfDay = timeText switch
                {
                    "DAY" => "DAY",
                    "NIGHT" => "NIGHT",
                    "DUSK" => "DUSK",
                    "DAWN" => "DAWN",
                    "MORNING" => "MORNING",
                    "AFTERNOON" => "AFTERNOON",
                    "EVENING" => "EVENING",
                    _ => "UNSPECIFIED"
                };
            }
        }

        /// <summary>
        /// Generate comprehensive metrics report
        /// </summary>
        public string GenerateMetricsReport(Script script)
        {
            var sb = new System.Text.StringBuilder();
            var scriptMetrics = AnalyzeScriptMetrics(script);
            var pageMetrics = CalculatePageMetrics(script);
            var characters = AnalyzeCharacterMetrics(script);
            var scenes = AnalyzeSceneMetrics(script);
            var health = CalculateHealthMetrics(script);

            sb.AppendLine("=== SCREENPLAY METRICS REPORT ===");
            sb.AppendLine();

            sb.AppendLine("📊 SCRIPT OVERVIEW");
            sb.AppendLine($"  Total Elements: {scriptMetrics.TotalElements}");
            sb.AppendLine($"  Total Scenes: {scriptMetrics.SceneHeadingCount}");
            sb.AppendLine();

            sb.AppendLine("📄 PAGE METRICS");
            sb.AppendLine($"  Word Count: {pageMetrics.WordCount:F0}");
            sb.AppendLine($"  Estimated Pages: {pageMetrics.EstimatedPages}");
            sb.AppendLine($"  Estimated Runtime: {pageMetrics.EstimatedMinutes} minutes");
            sb.AppendLine();

            sb.AppendLine("🎬 CONTENT BREAKDOWN");
            sb.AppendLine($"  Action Lines: {scriptMetrics.ActionCount}");
            sb.AppendLine($"  Dialogue Lines: {scriptMetrics.DialogueCount}");
            sb.AppendLine($"  Character Appearances: {scriptMetrics.CharacterCount}");
            sb.AppendLine($"  Transitions: {scriptMetrics.TransitionCount}");
            sb.AppendLine();

            sb.AppendLine("👥 TOP CHARACTERS");
            foreach (var character in characters.Take(5))
            {
                sb.AppendLine($"  {character.CharacterName}: {character.LineCount} lines ({character.PercentageOfDialogue}%)");
            }
            sb.AppendLine();

            sb.AppendLine("🎞️ SCENE ANALYSIS");
            sb.AppendLine($"  Average Scene Length: {health.AverageSceneLength} elements");
            sb.AppendLine($"  Longest Scene: {health.LongestScene} elements");
            sb.AppendLine($"  Shortest Scene: {health.ShortestScene} elements");
            sb.AppendLine();

            sb.AppendLine("💪 SCREENPLAY HEALTH");
            sb.AppendLine($"  Health Score: {health.HealthScore}");
            sb.AppendLine($"  Dialogue/Action Ratio: {health.DialogueActionRatio}");
            sb.AppendLine();

            if (health.Recommendations.Any())
            {
                sb.AppendLine("💡 RECOMMENDATIONS");
                foreach (var rec in health.Recommendations)
                {
                    sb.AppendLine($"  • {rec}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get character appearance timeline
        /// </summary>
        public Dictionary<string, List<int>> GetCharacterAppearanceTimeline(Script script)
        {
            var timeline = new Dictionary<string, List<int>>();
            var sceneNumber = 0;

            if (script?.Elements == null)
                return timeline;

            foreach (var element in script.Elements)
            {
                if (element is SceneHeadingElement)
                {
                    sceneNumber++;
                }
                else if (element is CharacterElement ce)
                {
                    var name = ce.Name?.Trim().ToUpper() ?? "UNKNOWN";
                    if (!timeline.ContainsKey(name))
                    {
                        timeline[name] = new List<int>();
                    }

                    if (!timeline[name].Contains(sceneNumber))
                    {
                        timeline[name].Add(sceneNumber);
                    }
                }
            }

            return timeline;
        }

        /// <summary>
        /// Generate quick statistics summary
        /// </summary>
        public string GenerateQuickStats(Script script)
        {
            var pageMetrics = CalculatePageMetrics(script);
            var characters = AnalyzeCharacterMetrics(script);
            var scenes = AnalyzeSceneMetrics(script);

            return $"📄 {pageMetrics.EstimatedPages} pages | ⏱️ {pageMetrics.EstimatedMinutes}min | 👥 {characters.Count} chars | 🎬 {scenes.Count} scenes";
        }
    }
}
