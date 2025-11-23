using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Fade-In Feature: Professional Report Generation
    /// 
    /// Generates industry-standard script breakdowns and reports:
    /// - Scene reports with descriptions and page counts
    /// - Cast reports with character line counts
    /// - Location reports with interior/exterior breakdown
    /// - Production reports for budgeting and scheduling
    /// </summary>
    public class ReportGenerator
    {
        public class SceneReport
        {
            public int SceneNumber { get; set; }
            public string SceneHeading { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int PageStart { get; set; }
            public int PageEnd { get; set; }
            public double PageCount { get; set; }
            public List<string> Characters { get; set; } = new();
            public List<string> Locations { get; set; } = new();
            public List<string> Props { get; set; } = new();
            public List<string> Effects { get; set; } = new();
        }

        public class CastReport
        {
            public string CharacterName { get; set; } = string.Empty;
            public int Appearances { get; set; }
            public int DialogueLines { get; set; }
            public int WordCount { get; set; }
            public double ScreenTime { get; set; }
            public List<int> SceneNumbers { get; set; } = new();
            public bool IsMainCharacter { get; set; }
        }

        public class LocationReport
        {
            public string Location { get; set; } = string.Empty;
            public bool IsInterior { get; set; }
            public List<string> Times { get; set; } = new();
            public int Appearances { get; set; }
            public List<int> SceneNumbers { get; set; } = new();
        }

        public class ProductionReport
        {
            public string Title { get; set; } = string.Empty;
            public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;
            public int TotalScenes { get; set; }
            public int TotalPages { get; set; }
            public double EstimatedMinutes { get; set; }
            public int UniqueCasts { get; set; }
            public int UniqueLocations { get; set; }
            public List<SceneReport> Scenes { get; set; } = new();
            public List<CastReport> Cast { get; set; } = new();
            public List<LocationReport> Locations { get; set; } = new();
            public Dictionary<string, int> SpecialEffects { get; set; } = new();
            public Dictionary<string, int> Props { get; set; } = new();
        }

        /// <summary>
        /// Generate comprehensive production report
        /// </summary>
        public ProductionReport GenerateProductionReport(Script script, List<ScriptElement> elements)
        {
            var report = new ProductionReport
            {
                Title = script.Title,
                TotalPages = script.GetEstimatedPageCount(),
                TotalScenes = script.Scenes.Count
            };

            // Build scene reports
            report.Scenes = GenerateSceneReports(script.Scenes, elements);
            
            // Build cast reports
            report.Cast = GenerateCastReports(script, elements);
            
            // Build location reports
            report.Locations = GenerateLocationReports(script.Scenes, elements);
            
            // Calculate totals
            report.UniqueCasts = report.Cast.Count;
            report.UniqueLocations = report.Locations.Count;
            report.EstimatedMinutes = report.TotalPages * 1.0;  // ~1 min per page

            return report;
        }

        /// <summary>
        /// Generate scene-by-scene reports
        /// </summary>
        private List<SceneReport> GenerateSceneReports(List<Scene> scenes, List<ScriptElement> elements)
        {
            var reports = new List<SceneReport>();
            int pageCounter = 1;

            foreach (var scene in scenes)
            {
                var sceneElements = scene.Content ?? new List<ScriptElement>();
                var pageCount = Math.Ceiling(sceneElements.Sum(e => e.GetLineCount()) / 55.0);

                var report = new SceneReport
                {
                    SceneNumber = scenes.IndexOf(scene) + 1,
                    SceneHeading = scene.Slugline,
                    Description = scene.IndexCard.Summary,
                    PageStart = pageCounter,
                    PageEnd = pageCounter + (int)pageCount - 1,
                    PageCount = pageCount,
                    Characters = ExtractCharactersFromScene(sceneElements),
                    Locations = ExtractLocationsFromScene(scene),
                    Props = ExtractPropsFromScene(sceneElements),
                    Effects = ExtractEffectsFromScene(sceneElements)
                };

                reports.Add(report);
                pageCounter += (int)pageCount;
            }

            return reports;
        }

        /// <summary>
        /// Generate cast report (character breakdown)
        /// </summary>
        private List<CastReport> GenerateCastReports(Script script, List<ScriptElement> elements)
        {
            var castReports = new Dictionary<string, CastReport>(StringComparer.OrdinalIgnoreCase);

            foreach (var element in elements.OfType<DialogueElement>())
            {
                if (castReports.TryGetValue(element.SpeakerName, out var report))
                {
                    report.DialogueLines++;
                    report.WordCount += element.Text.Split().Length;
                }
            }

            // Add character appearances
            foreach (var character in script.Characters)
            {
                if (!castReports.ContainsKey(character.Name))
                {
                    castReports[character.Name] = new CastReport
                    {
                        CharacterName = character.Name,
                        Appearances = character.AppearanceCount,
                        ScreenTime = character.AppearanceCount * 1.0  // Simplified
                    };
                }
                else
                {
                    castReports[character.Name].Appearances = character.AppearanceCount;
                }
            }

            // Determine main characters (more than X appearances)
            const int mainCharacterThreshold = 5;
            foreach (var report in castReports.Values)
            {
                report.IsMainCharacter = report.Appearances >= mainCharacterThreshold;
            }

            return castReports.Values.OrderByDescending(c => c.Appearances).ToList();
        }

        /// <summary>
        /// Generate location report
        /// </summary>
        private List<LocationReport> GenerateLocationReports(List<Scene> scenes, List<ScriptElement> elements)
        {
            var locationReports = new Dictionary<string, LocationReport>(StringComparer.OrdinalIgnoreCase);

            foreach (var scene in scenes)
            {
                var location = scene.Heading.Location;
                var time = scene.Heading.Time;
                var isInterior = scene.Heading.IsInterior;

                if (!locationReports.ContainsKey(location))
                {
                    locationReports[location] = new LocationReport
                    {
                        Location = location,
                        IsInterior = isInterior
                    };
                }

                var report = locationReports[location];
                report.Appearances++;
                report.SceneNumbers.Add(scenes.IndexOf(scene) + 1);
                if (!report.Times.Contains(time))
                    report.Times.Add(time);
            }

            return locationReports.Values.OrderByDescending(l => l.Appearances).ToList();
        }

        /// <summary>
        /// Generate formatted text report
        /// </summary>
        public string ExportSceneReport(List<SceneReport> scenes)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== SCENE BREAKDOWN ===\n");

            foreach (var scene in scenes)
            {
                sb.AppendLine($"SCENE {scene.SceneNumber}: {scene.SceneHeading}");
                sb.AppendLine($"Pages: {scene.PageStart}-{scene.PageEnd} ({scene.PageCount:F1} pages)");
                
                if (scene.Characters.Count > 0)
                    sb.AppendLine($"Characters: {string.Join(", ", scene.Characters)}");
                
                if (scene.Locations.Count > 0)
                    sb.AppendLine($"Locations: {string.Join(", ", scene.Locations)}");
                
                if (scene.Props.Count > 0)
                    sb.AppendLine($"Props: {string.Join(", ", scene.Props)}");
                
                if (scene.Effects.Count > 0)
                    sb.AppendLine($"Effects: {string.Join(", ", scene.Effects)}");
                
                if (!string.IsNullOrEmpty(scene.Description))
                    sb.AppendLine($"Description: {scene.Description}");
                
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Export cast report as CSV
        /// </summary>
        public string ExportCastReportCSV(List<CastReport> cast)
        {
            var sb = new StringBuilder();
            sb.AppendLine("CHARACTER,APPEARANCES,DIALOGUE_LINES,WORD_COUNT,SCREEN_TIME,IS_MAIN");

            foreach (var character in cast.OrderByDescending(c => c.Appearances))
            {
                sb.AppendLine($"\"{character.CharacterName}\",{character.Appearances},{character.DialogueLines}," +
                             $"{character.WordCount},{character.ScreenTime:F1},{character.IsMainCharacter}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Export location report as CSV
        /// </summary>
        public string ExportLocationReportCSV(List<LocationReport> locations)
        {
            var sb = new StringBuilder();
            sb.AppendLine("LOCATION,TYPE,APPEARANCES,TIMES,SCENES");

            foreach (var location in locations.OrderByDescending(l => l.Appearances))
            {
                var type = location.IsInterior ? "INT" : "EXT";
                var times = string.Join("|", location.Times);
                var scenes = string.Join("|", location.SceneNumbers);
                sb.AppendLine($"\"{location.Location}\",{type},{location.Appearances},\"{times}\",\"{scenes}\"");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Export full production report as text
        /// </summary>
        public string ExportFullReport(ProductionReport report)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("╔═════════════════════════════════════════════════════════╗");
            sb.AppendLine($"║  PRODUCTION REPORT: {report.Title.PadRight(38)}║");
            sb.AppendLine("╚═════════════════════════════════════════════════════════╝\n");

            sb.AppendLine($"Generated: {report.GeneratedDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Total Scenes: {report.TotalScenes}");
            sb.AppendLine($"Total Pages: {report.TotalPages}");
            sb.AppendLine($"Estimated Duration: {report.EstimatedMinutes:F0} minutes (~{report.EstimatedMinutes / 60:F1} hours)");
            sb.AppendLine($"Unique Cast: {report.UniqueCasts}");
            sb.AppendLine($"Unique Locations: {report.UniqueLocations}");
            sb.AppendLine();

            sb.AppendLine("=== TOP CAST ===");
            foreach (var character in report.Cast.Take(10))
            {
                var mainMarker = character.IsMainCharacter ? " [MAIN]" : "";
                sb.AppendLine($"  {character.CharacterName,-30} {character.Appearances,3} scenes  {character.DialogueLines,3} lines{mainMarker}");
            }

            sb.AppendLine("\n=== LOCATIONS ===");
            foreach (var location in report.Locations.Take(15))
            {
                var typeMarker = location.IsInterior ? "INT" : "EXT";
                sb.AppendLine($"  {location.Location,-40} {typeMarker} x{location.Appearances}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Extract characters from scene elements
        /// </summary>
        private List<string> ExtractCharactersFromScene(List<ScriptElement> elements)
        {
            var characters = new HashSet<string>();
            foreach (var element in elements.OfType<CharacterElement>())
            {
                characters.Add(element.Name);
            }
            return characters.ToList();
        }

        /// <summary>
        /// Extract locations from scene
        /// </summary>
        private List<string> ExtractLocationsFromScene(Scene scene)
        {
            return new List<string> { scene.Heading.Location };
        }

        /// <summary>
        /// Extract props mentioned in scene
        /// </summary>
        private List<string> ExtractPropsFromScene(List<ScriptElement> elements)
        {
            var props = new HashSet<string>();
            var propKeywords = new[] { "gun", "phone", "car", "door", "window", "desk", "chair", "table", "bottle", "knife", "gun", "sword" };
            
            foreach (var element in elements.OfType<ActionElement>())
            {
                var text = element.Text.ToLower();
                foreach (var prop in propKeywords)
                {
                    if (text.Contains(prop))
                        props.Add(prop);
                }
            }

            return props.ToList();
        }

        /// <summary>
        /// Extract special effects mentioned in scene
        /// </summary>
        private List<string> ExtractEffectsFromScene(List<ScriptElement> elements)
        {
            var effects = new HashSet<string>();
            var effectKeywords = new[] { "explosion", "fire", "crash", "gunshot", "scream", "wind", "thunder", "lightning", "rain", "snow" };
            
            foreach (var element in elements.OfType<ActionElement>())
            {
                var text = element.Text.ToLower();
                foreach (var effect in effectKeywords)
                {
                    if (text.Contains(effect))
                        effects.Add(effect);
                }
            }

            return effects.ToList();
        }
    }
}
