using System;
using Xunit;
using App.Core.Services;
using App.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace App.Core.Tests.Services
{
    /// <summary>
    /// Unit tests for enterprise services - CORRECTED
    /// </summary>
    public class EnterpriseServicesTests
    {
        [Fact]
        public void RevisionColorManager_Initializes()
        {
            // Arrange & Act
            var manager = new RevisionColorManager();

            // Assert
            Assert.NotNull(manager);
        }

        [Fact]
        public void DialogueTuner_AnalyzesDialogue()
        {
            // Arrange
            var tuner = new DialogueTuner();
            var script = new Script();
            script.Elements.Add(new CharacterElement { Name = "JOHN" });
            script.Elements.Add(new DialogueElement { Text = "I'm heading to the store." });

            // Act
            var analysis = tuner.AnalyzeCharacterVoice(script, "JOHN");

            // Assert
            Assert.NotNull(analysis);
        }

        [Fact]
        public void ScriptWatermarkManager_CreatesWatermark()
        {
            // Arrange
            var watermark = new ScriptWatermarkManager();
            var script = new Script();

            // Act
            var wm = watermark.CreateWatermark("CONFIDENTIAL", "WM001");

            // Assert
            Assert.NotNull(wm);
        }

        [Fact]
        public void SceneOrganizationManager_Initializes()
        {
            // Arrange & Act
            var organizer = new SceneOrganizationManager();

            // Assert
            Assert.NotNull(organizer);
        }

        [Fact]
        public void ReportGenerator_GeneratesReport()
        {
            // Arrange
            var generator = new ReportGenerator();
            var script = new Script();
            script.Elements.Add(new SceneHeadingElement { Text = "INT. OFFICE - DAY" });
            script.Elements.Add(new ActionElement { Text = "John sits at desk" });

            // Act
            var report = generator.GenerateLocationReport(script);

            // Assert
            Assert.NotNull(report);
        }

        [Fact]
        public void AdvancedSearchService_SearchesText()
        {
            // Arrange
            var search = new AdvancedSearchService();
            var script = new Script();
            script.Elements.Add(new ActionElement { Text = "The door opens." });

            // Act
            var results = search.SearchElements(script, "door");

            // Assert
            Assert.NotNull(results);
        }

        [Fact]
        public void CharacterSidesGenerator_GeneratesSides()
        {
            // Arrange
            var generator = new CharacterSidesGenerator();
            var script = new Script();
            script.Elements.Add(new CharacterElement { Name = "JOHN" });
            script.Elements.Add(new DialogueElement { Text = "Hello world" });

            // Act
            var sides = generator.GenerateSidesForCharacter(script, "JOHN");

            // Assert
            Assert.NotNull(sides);
        }

        [Fact]
        public void BatchOperationEngine_TracksOperations()
        {
            // Arrange
            var batch = new BatchOperationEngine();
            var scripts = new List<Script>
            {
                new Script(),
                new Script()
            };

            // Act
            var tracking = batch.TrackBatchOperation("Test", "cleanup", scripts.Count);

            // Assert
            Assert.NotNull(tracking);
        }

        [Fact]
        public void ScriptComparisonEngine_ComparesScripts()
        {
            // Arrange
            var comparison = new ScriptComparisonEngine();
            var script1 = new Script();
            script1.Elements.Add(new ActionElement { Text = "John enters" });
            
            var script2 = new Script();
            script2.Elements.Add(new ActionElement { Text = "John exits" });

            // Act
            var diff = comparison.GenerateComparison(script1, script2);

            // Assert
            Assert.NotNull(diff);
        }
    }
}
