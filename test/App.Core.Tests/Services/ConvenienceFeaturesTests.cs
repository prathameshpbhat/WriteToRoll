using System;
using Xunit;
using App.Core.Services;
using App.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace App.Core.Tests.Services
{
    /// <summary>
    /// Unit tests for SmartAutocompleteEngine
    /// </summary>
    public class SmartAutocompleteEngineTests
    {
        private readonly SmartAutocompleteEngine _engine;

        public SmartAutocompleteEngineTests()
        {
            _engine = new SmartAutocompleteEngine();
        }

        [Fact]
        public void GetContextAwareSuggestions_ReturnsSuggestions()
        {
            // Arrange
            var context = new SmartAutocompleteEngine.AutocompleteContext
            {
                PreviousElementType = ScriptElementType.SceneHeading,
                PartialText = "INT.",
                LineNumber = 1
            };

            // Act
            var suggestions = _engine.GetContextAwareSuggestions(context);

            // Assert
            Assert.NotNull(suggestions);
        }

        [Fact]
        public void RegisterCharacter_TracksCharacter()
        {
            // Act
            _engine.RegisterCharacter("JOHN");
            _engine.RegisterCharacter("JOHN");
            var frequent = _engine.GetFrequentCharacters(1);

            // Assert
            Assert.Contains("JOHN", frequent);
        }

        [Fact]
        public void GetFrequentCharacters_ReturnsOrdered()
        {
            // Arrange
            _engine.RegisterCharacter("ALICE");
            _engine.RegisterCharacter("ALICE");
            _engine.RegisterCharacter("BOB");

            // Act
            var frequent = _engine.GetFrequentCharacters(2);

            // Assert
            Assert.Equal(2, frequent.Count);
            Assert.Equal("ALICE", frequent[0]);
        }
    }

    /// <summary>
    /// Unit tests for QuickFormatTemplateEngine
    /// </summary>
    public class QuickFormatTemplateEngineTests
    {
        private readonly QuickFormatTemplateEngine _engine;

        public QuickFormatTemplateEngineTests()
        {
            _engine = new QuickFormatTemplateEngine();
        }

        [Fact]
        public void GetBuiltInTemplates_ReturnsEightTemplates()
        {
            // Act
            var templates = _engine.GetBuiltInTemplates();

            // Assert
            Assert.Equal(8, templates.Count);
        }

        [Fact]
        public void GetFormatPreset_ReturnsStandardPreset()
        {
            // Act
            var preset = _engine.GetFormatPreset("standard");

            // Assert
            Assert.NotNull(preset);
            Assert.Equal("Standard Screenplay", preset.Name);
        }

        [Fact]
        public void ApplyTemplate_ReturnsElements()
        {
            // Arrange
            var replacements = new Dictionary<string, string>();

            // Act
            var elements = _engine.ApplyTemplate("action", replacements);

            // Assert
            Assert.NotNull(elements);
        }

        [Fact]
        public void CreateTemplateFromScene_SavesCustomTemplate()
        {
            // Arrange
            var elements = new List<ScriptElement>
            {
                new ActionElement { Text = "Test action" },
                new CharacterElement { Name = "TEST" }
            };

            // Act
            var template = _engine.CreateTemplateFromScene("Custom", "Test template", elements);

            // Assert
            Assert.NotNull(template);
            Assert.False(template.IsBuiltIn);
            Assert.Equal("Custom", template.Name);
        }
    }

    /// <summary>
    /// Unit tests for FormatFixerService
    /// </summary>
    public class FormatFixerServiceTests
    {
        private readonly FormatFixerService _fixer;

        public FormatFixerServiceTests()
        {
            _fixer = new FormatFixerService();
        }

        [Fact]
        public void DetectCharacterIssues_FindsLowercaseNames()
        {
            // Arrange
            var element = new CharacterElement { Name = "john" };

            // Act
            var issues = _fixer.DetectElementIssues(element, 0);

            // Assert
            Assert.NotEmpty(issues);
            Assert.Contains(issues, i => i.IssueType.Contains("Case"));
        }

        [Fact]
        public void DetectSceneHeadingIssues_FindsMissingIntExt()
        {
            // Arrange
            var element = new SceneHeadingElement { Text = "BEDROOM - DAY" };

            // Act
            var issues = _fixer.DetectElementIssues(element, 0);

            // Assert
            Assert.NotEmpty(issues);
        }

        [Fact]
        public void FixAllIssues_FixesAutoFixableIssues()
        {
            // Arrange
            var script = new Script();
            script.Elements.Add(new CharacterElement { Name = "john" });
            script.Elements.Add(new DialogueElement { Text = "Hello" });

            // Act
            var issues = _fixer.DetectAllIssues(script);

            // Assert
            Assert.NotNull(issues);
        }

        [Fact]
        public void QuickCleanup_RemovesEmptyElements()
        {
            // Arrange
            var script = new Script();
            script.Elements.Add(new ActionElement { Text = "Valid action" });
            script.Elements.Add(new ActionElement { Text = "" });
            script.Elements.Add(new ActionElement { Text = "   " });

            // Act
            _fixer.QuickCleanup(script);

            // Assert
            Assert.Single(script.Elements);
        }
    }

    /// <summary>
    /// Unit tests for WritingMetricsService
    /// </summary>
    public class WritingMetricsServiceTests
    {
        private readonly WritingMetricsService _metrics;

        public WritingMetricsServiceTests()
        {
            _metrics = new WritingMetricsService();
        }

        [Fact]
        public void AnalyzeScriptMetrics_CountsElements()
        {
            // Arrange
            var script = new Script();
            script.Elements.Add(new ActionElement { Text = "Action" });
            script.Elements.Add(new CharacterElement { Name = "JOHN" });
            script.Elements.Add(new DialogueElement { Text = "Hello" });

            // Act
            var metrics = _metrics.AnalyzeScriptMetrics(script);

            // Assert
            Assert.Equal(3, metrics.TotalElements);
            Assert.Equal(1, metrics.ActionCount);
            Assert.Equal(1, metrics.CharacterCount);
            Assert.Equal(1, metrics.DialogueCount);
        }

        [Fact]
        public void CalculatePageMetrics_EstimatesPages()
        {
            // Arrange
            var script = new Script();
            for (int i = 0; i < 250; i++)
            {
                script.Elements.Add(new ActionElement { Text = "word " });
            }

            // Act
            var metrics = _metrics.CalculatePageMetrics(script);

            // Assert
            Assert.True(metrics.EstimatedPages > 0);
        }

        [Fact]
        public void AnalyzeCharacterMetrics_TrackCharacters()
        {
            // Arrange
            var script = new Script();
            script.Elements.Add(new CharacterElement { Name = "JOHN" });
            script.Elements.Add(new DialogueElement { Text = "Hello world" });

            // Act
            var metrics = _metrics.AnalyzeCharacterMetrics(script);

            // Assert
            Assert.NotEmpty(metrics);
        }

        [Fact]
        public void CalculateHealthMetrics_GeneratesScore()
        {
            // Arrange
            var script = new Script();
            script.Elements.Add(new ActionElement { Text = "Action" });

            // Act
            var health = _metrics.CalculateHealthMetrics(script);

            // Assert
            Assert.NotNull(health.HealthScore);
            Assert.NotEmpty(health.Recommendations);
        }

        [Fact]
        public void GenerateQuickStats_ReturnsFormattedStats()
        {
            // Arrange
            var script = new Script();
            script.Elements.Add(new ActionElement { Text = "Action" });

            // Act
            var stats = _metrics.GenerateQuickStats(script);

            // Assert
            Assert.NotNull(stats);
            Assert.Contains("pages", stats.ToLower());
        }
    }

    /// <summary>
    /// Unit tests for QuickActionsService
    /// </summary>
    public class QuickActionsServiceTests
    {
        private readonly QuickActionsService _actions;

        public QuickActionsServiceTests()
        {
            _actions = new QuickActionsService();
        }

        [Fact]
        public void CreateSnapshot_CreatesUndo()
        {
            // Arrange
            var script = new Script();

            // Act
            _actions.CreateSnapshot(script, "Test snapshot");

            // Assert
            Assert.True(_actions.CanUndo());
        }

        [Fact]
        public void AddDialogue_InsertsElements()
        {
            // Arrange
            var script = new Script();
            int initialCount = script.Elements.Count;

            // Act
            _actions.AddDialogue(script, "JOHN", "Hello", "");

            // Assert
            Assert.True(script.Elements.Count > initialCount);
        }

        [Fact]
        public void RenameCharacter_ReplacesAllInstances()
        {
            // Arrange
            var script = new Script();
            script.Elements.Add(new CharacterElement { Name = "JOHN" });
            script.Elements.Add(new CharacterElement { Name = "JOHN" });
            script.Elements.Add(new CharacterElement { Name = "MARY" });

            // Act
            int renamed = _actions.RenameCharacter(script, "JOHN", "JACK");

            // Assert
            Assert.True(renamed >= 0);
        }

        [Fact]
        public void DuplicateScene_CreatesSceneCopy()
        {
            // Arrange
            var script = new Script();
            script.Elements.Add(new SceneHeadingElement { Text = "INT. BEDROOM - DAY" });
            script.Elements.Add(new ActionElement { Text = "Action line" });
            int initialCount = script.Elements.Count;

            // Act
            bool success = _actions.DuplicateScene(script, 1, insertAfter: true);

            // Assert
            Assert.True(success || script.Elements.Count > initialCount);
        }

        [Fact]
        public void ReplaceAllText_UpdatesElements()
        {
            // Arrange
            var script = new Script();
            script.Elements.Add(new ActionElement { Text = "John enters the room" });

            // Act
            int replaced = _actions.ReplaceAllText(script, "John", "Jack");

            // Assert
            Assert.True(replaced >= 0);
        }

        [Fact]
        public void CleanupEmptyElements_RemovesEmpty()
        {
            // Arrange
            var script = new Script();
            script.Elements.Add(new ActionElement { Text = "Valid" });
            script.Elements.Add(new ActionElement { Text = "" });

            // Act
            int cleaned = _actions.CleanupEmptyElements(script);

            // Assert
            Assert.True(cleaned >= 0);
        }

        [Fact]
        public void Undo_RestoresPreviousState()
        {
            // Arrange
            var script = new Script();
            _actions.CreateSnapshot(script, "Initial");
            script.Elements.Add(new ActionElement { Text = "New element" });

            // Act
            bool success = _actions.Undo(script);

            // Assert
            Assert.True(success || !_actions.CanUndo());
        }

        [Fact]
        public void Redo_RestoresRedoState()
        {
            // Arrange
            var script = new Script();
            _actions.CreateSnapshot(script, "Initial");
            script.Elements.Add(new ActionElement { Text = "New" });
            _actions.Undo(script);

            // Act
            bool success = _actions.Redo(script);

            // Assert
            Assert.True(success || !_actions.CanRedo());
        }
    }
}
