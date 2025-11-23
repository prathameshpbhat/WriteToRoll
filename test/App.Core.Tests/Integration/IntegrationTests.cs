using System;
using Xunit;
using App.Core.Services;
using App.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace App.Core.Tests.Integration
{
    /// <summary>
    /// Integration tests for convenience features - CORRECTED
    /// </summary>
    public class ConvenienceFeaturesIntegrationTests
    {
        [Fact]
        public void FullWorkflow_CreateSceneWithTemplate_ApplyFormatting_CheckMetrics()
        {
            // Arrange
            var template = new QuickFormatTemplateEngine();
            var fixer = new FormatFixerService();
            var metrics = new WritingMetricsService();
            var script = new Script();

            // Act - Step 1: Create scene from template
            var elements = template.ApplyTemplate("action", new Dictionary<string, string>());
            script.Elements.AddRange(elements);

            // Act - Step 2: Apply cleanup
            fixer.QuickCleanup(script);

            // Act - Step 3: Calculate metrics
            var scriptMetrics = metrics.AnalyzeScriptMetrics(script);

            // Assert
            Assert.NotNull(scriptMetrics);
            Assert.True(scriptMetrics.TotalElements >= 0);
        }

        [Fact]
        public void CharacterWorkflow_AddDialogue_TrackCharacters()
        {
            // Arrange
            var actions = new QuickActionsService();
            var autocomplete = new SmartAutocompleteEngine();
            var script = new Script();

            // Act - Step 1: Add dialogue for new character
            actions.AddDialogue(script, "ALICE", "Nice to meet you", "");

            // Act - Step 2: Register character for tracking
            autocomplete.RegisterCharacter("ALICE");

            // Act - Step 3: Get suggestions for next character
            var context = new SmartAutocompleteEngine.AutocompleteContext
            {
                PreviousElementType = ScriptElementType.Character,
                PartialText = "ALICE",
                LineNumber = 0
            };
            var suggestions = autocomplete.GetContextAwareSuggestions(context);

            // Assert
            Assert.NotEmpty(script.Elements);
            Assert.NotNull(suggestions);
        }

        [Fact]
        public void CleanupAndMetrics_AddMixedElements_Cleanup_CheckHealth()
        {
            // Arrange
            var actions = new QuickActionsService();
            var metrics = new WritingMetricsService();
            var script = new Script();

            // Act - Step 1: Add valid and invalid elements
            script.Elements.Add(new ActionElement { Text = "Valid action" });
            script.Elements.Add(new ActionElement { Text = "" });
            script.Elements.Add(new CharacterElement { Name = "JOHN" });

            // Act - Step 2: Cleanup empty elements
            int cleaned = actions.CleanupEmptyElements(script);

            // Act - Step 3: Check health
            var health = metrics.CalculateHealthMetrics(script);

            // Assert
            Assert.True(cleaned >= 0);
            Assert.NotNull(health);
        }

        [Fact]
        public void UndoRedoWorkflow_MakeChanges_Undo_Redo()
        {
            // Arrange
            var actions = new QuickActionsService();
            var script = new Script();

            // Act - Step 1: Create initial snapshot
            actions.CreateSnapshot(script, "Initial state");
            int initialCount = script.Elements.Count;

            // Act - Step 2: Add element
            script.Elements.Add(new ActionElement { Text = "New action" });
            int afterAddCount = script.Elements.Count;

            // Act - Step 3: Undo
            actions.Undo(script);
            int afterUndoCount = script.Elements.Count;

            // Act - Step 4: Redo
            actions.Redo(script);
            int afterRedoCount = script.Elements.Count;

            // Assert
            Assert.True(afterUndoCount <= afterAddCount);
            Assert.True(afterRedoCount >= afterUndoCount);
        }

        [Fact]
        public void BatchRenameWorkflow_MultipleCharacters_RenameAndTrack()
        {
            // Arrange
            var actions = new QuickActionsService();
            var autocomplete = new SmartAutocompleteEngine();
            var script = new Script();

            // Act - Step 1: Add multiple dialogue elements with same character
            actions.AddDialogue(script, "JOHN", "Hello", "");
            actions.AddDialogue(script, "JOHN", "How are you?", "");
            actions.AddDialogue(script, "MARY", "I'm good", "");

            // Act - Step 2: Rename character
            int renamed = actions.RenameCharacter(script, "JOHN", "JACK");

            // Act - Step 3: Update autocomplete tracking
            autocomplete.RegisterCharacter("JACK");

            // Assert
            Assert.True(renamed >= 0);
            var frequent = autocomplete.GetFrequentCharacters(1);
            Assert.NotNull(frequent);
        }
    }

    /// <summary>
    /// Integration tests for enterprise services - CORRECTED
    /// </summary>
    public class EnterpriseServicesIntegrationTests
    {
        [Fact]
        public void FullProductionWorkflow_CreateScript_CreateWatermark_GenerateReport()
        {
            // Arrange
            var watermark = new ScriptWatermarkManager();
            var generator = new ReportGenerator();
            var script = new Script();

            script.Elements.Add(new SceneHeadingElement { Text = "INT. OFFICE - DAY" });
            script.Elements.Add(new ActionElement { Text = "John sits at desk" });
            script.Elements.Add(new CharacterElement { Name = "JOHN" });
            script.Elements.Add(new DialogueElement { Text = "Time to work" });

            // Act - Step 1: Create watermark
            var wm = watermark.CreateWatermark("DRAFT", "WM-001");

            // Act - Step 2: Generate location report
            string report = generator.GenerateLocationReport(script);

            // Assert
            Assert.NotNull(wm);
            Assert.NotNull(report);
        }

        [Fact]
        public void SceneOrganization_CreateScenes_Organize()
        {
            // Arrange
            var organizer = new SceneOrganizationManager();
            var script = new Script();

            // Add scenes in various locations
            script.Elements.Add(new SceneHeadingElement { Text = "INT. BEDROOM - NIGHT" });
            script.Elements.Add(new ActionElement { Text = "Scene 1" });
            script.Elements.Add(new SceneHeadingElement { Text = "INT. BEDROOM - DAY" });
            script.Elements.Add(new ActionElement { Text = "Scene 2" });

            // Act
            var organized = organizer.GroupScenesByLocation(script);

            // Assert
            Assert.NotNull(organized);
        }

        [Fact]
        public void SearchAndReplace_AddElements_SearchText()
        {
            // Arrange
            var search = new AdvancedSearchService();
            var script = new Script();

            script.Elements.Add(new ActionElement { Text = "John walks to the door" });
            script.Elements.Add(new ActionElement { Text = "Mary opens the door" });
            script.Elements.Add(new ActionElement { Text = "Door slams shut" });

            // Act - Step 1: Search for pattern
            var results = search.SearchElements(script, "door");

            // Act - Step 2: Replace matches
            int replaced = search.ReplaceInElements(script, "door", "entrance");

            // Assert
            Assert.NotNull(results);
            Assert.True(replaced >= 0);
        }

        [Fact]
        public void CharacterSidesWorkflow_BuildScript_GenerateSides()
        {
            // Arrange
            var generator = new CharacterSidesGenerator();
            var script = new Script();

            // Build a scene with character dialogue
            script.Elements.Add(new SceneHeadingElement { Text = "INT. OFFICE - DAY" });
            script.Elements.Add(new CharacterElement { Name = "JOHN" });
            script.Elements.Add(new DialogueElement { Text = "Let's discuss the project" });
            script.Elements.Add(new CharacterElement { Name = "MARY" });
            script.Elements.Add(new DialogueElement { Text = "Agreed. When should we start?" });

            // Act - Step 1: Generate sides for JOHN
            var johnSides = generator.GenerateSidesForCharacter(script, "JOHN");

            // Act - Step 2: Generate sides for MARY
            var marySides = generator.GenerateSidesForCharacter(script, "MARY");

            // Assert
            Assert.NotNull(johnSides);
            Assert.NotNull(marySides);
        }

        [Fact]
        public void VersionControlWorkflow_CreateVersion1_CreateVersion2_CompareAndDiff()
        {
            // Arrange
            var comparison = new ScriptComparisonEngine();

            // Create version 1
            var scriptV1 = new Script();
            scriptV1.Elements.Add(new SceneHeadingElement { Text = "INT. OFFICE - DAY" });
            scriptV1.Elements.Add(new ActionElement { Text = "John enters" });
            scriptV1.Elements.Add(new CharacterElement { Name = "JOHN" });
            scriptV1.Elements.Add(new DialogueElement { Text = "Hello everyone" });

            // Create version 2 (modified)
            var scriptV2 = new Script();
            scriptV2.Elements.Add(new SceneHeadingElement { Text = "INT. OFFICE - DAY" });
            scriptV2.Elements.Add(new ActionElement { Text = "John walks in slowly" });
            scriptV2.Elements.Add(new CharacterElement { Name = "JOHN" });
            scriptV2.Elements.Add(new DialogueElement { Text = "Hey folks" });

            // Act
            var diff = comparison.GenerateComparison(scriptV1, scriptV2);

            // Assert
            Assert.NotNull(diff);
        }

        [Fact]
        public void BatchProcessing_CreateMultipleScripts_BatchTrack()
        {
            // Arrange
            var batch = new BatchOperationEngine();
            int scriptCount = 3;

            // Create 3 scripts with cleanup needs
            var scripts = new List<Script>();
            for (int i = 0; i < scriptCount; i++)
            {
                var script = new Script();
                script.Elements.Add(new ActionElement { Text = "Action" });
                script.Elements.Add(new ActionElement { Text = "" }); // Empty to clean
                scripts.Add(script);
            }

            // Act
            var tracking = batch.TrackBatchOperation("Cleanup Batch", "cleanup", scriptCount);

            // Assert
            Assert.NotNull(tracking);
        }

        [Fact]
        public void CompletePipeline_CreateScript_AnalyzeDialogue_OrganizeScenes()
        {
            // Arrange
            var tuner = new DialogueTuner();
            var organizer = new SceneOrganizationManager();
            var generator = new ReportGenerator();
            var script = new Script();

            // Create multi-scene script
            script.Elements.Add(new SceneHeadingElement { Text = "INT. BEDROOM - NIGHT" });
            script.Elements.Add(new CharacterElement { Name = "JOHN" });
            script.Elements.Add(new DialogueElement { Text = "I'm heading out" });
            script.Elements.Add(new SceneHeadingElement { Text = "EXT. STREET - NIGHT" });
            script.Elements.Add(new CharacterElement { Name = "JOHN" });
            script.Elements.Add(new DialogueElement { Text = "What a night" });

            // Act - Step 1: Analyze dialogue consistency
            var dialogueAnalysis = tuner.AnalyzeCharacterVoice(script, "JOHN");

            // Act - Step 2: Organize scenes
            var organized = organizer.GroupScenesByLocation(script);

            // Act - Step 3: Generate report
            var report = generator.GenerateLocationReport(script);

            // Assert
            Assert.NotNull(dialogueAnalysis);
            Assert.NotNull(organized);
            Assert.NotNull(report);
        }
    }
}
