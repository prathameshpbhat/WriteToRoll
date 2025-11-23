using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Fade-In Feature: Character Sides & Actor Briefing Documents
    /// 
    /// Features:
    /// - Extract individual actor sections from screenplay
    /// - Character sides show only their scenes and dialogue
    /// - Generate actor briefing documents
    /// - Include scene context and other characters
    /// - Professional formatting for actors
    /// </summary>
    public class CharacterSidesGenerator
    {
        public class CharacterSide
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string CharacterName { get; set; } = string.Empty;
            public string ActorName { get; set; } = string.Empty;
            public List<SceneSide> Scenes { get; set; } = new();
            public int TotalScenes { get; set; }
            public int TotalLines { get; set; }
            public double EstimatedMinutes { get; set; }
            public List<string> CoActors { get; set; } = new();
            public Dictionary<string, string> CostumeNotes { get; set; } = new();
            public List<string> Props { get; set; } = new();
        }

        public class SceneSide
        {
            public int SceneNumber { get; set; }
            public string SceneHeading { get; set; } = string.Empty;
            public List<DialogueBlock> DialogueBlocks { get; set; } = new();
            public string SceneNotes { get; set; } = string.Empty;
            public int PageStart { get; set; }
            public int PageEnd { get; set; }
            public List<string> OtherCharactersInScene { get; set; } = new();
            public List<string> ActionBeforeDialogue { get; set; } = new();
            public List<string> ActionAfterDialogue { get; set; } = new();
        }

        public class DialogueBlock
        {
            public int LineNumber { get; set; }
            public string PrecedingAction { get; set; } = string.Empty;
            public string Character { get; set; } = string.Empty;
            public string Parenthetical { get; set; } = string.Empty;
            public string DialogueText { get; set; } = string.Empty;
            public string FollowingAction { get; set; } = string.Empty;
            public List<string> CueLines { get; set; } = new();  // Last few words from other character
        }

        /// <summary>
        /// Generate character sides for a specific character
        /// </summary>
        public CharacterSide GenerateCharacterSide(string characterName, List<Scene> scenes, List<ScriptElement> elements)
        {
            var side = new CharacterSide { CharacterName = characterName };
            var coActors = new HashSet<string>();

            foreach (var scene in scenes)
            {
                var sceneSide = ExtractSceneSide(characterName, scene, elements);
                if (sceneSide.DialogueBlocks.Count > 0 || 
                    (sceneSide.OtherCharactersInScene.Count > 0 && sceneSide.ActionBeforeDialogue.Count > 0))
                {
                    side.Scenes.Add(sceneSide);
                    side.TotalLines += sceneSide.DialogueBlocks.Sum(d => 1);  // Count dialogue blocks
                    side.TotalScenes++;

                    foreach (var actor in sceneSide.OtherCharactersInScene)
                    {
                        if (!actor.Equals(characterName, StringComparison.OrdinalIgnoreCase))
                            coActors.Add(actor);
                    }
                }
            }

            side.CoActors = coActors.ToList();
            side.EstimatedMinutes = side.TotalScenes * 0.5;  // Rough estimate
            return side;
        }

        /// <summary>
        /// Extract dialogue and context for character in a specific scene
        /// </summary>
        private SceneSide ExtractSceneSide(string characterName, Scene scene, List<ScriptElement> elements)
        {
            var side = new SceneSide
            {
                SceneHeading = scene.Slugline,
                SceneNotes = scene.Notes
            };

            var characterDialogueElements = new List<int>();
            var sceneElements = scene.Content ?? new List<ScriptElement>();

            // Find all dialogue elements for this character
            for (int i = 0; i < sceneElements.Count; i++)
            {
                if (sceneElements[i] is CharacterElement charElem && 
                    charElem.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase))
                {
                    characterDialogueElements.Add(i);
                }
            }

            // Extract dialogue blocks with context
            foreach (var idx in characterDialogueElements)
            {
                var block = ExtractDialogueBlock(characterName, sceneElements, idx);
                side.DialogueBlocks.Add(block);
            }

            // Extract other characters in scene
            foreach (var element in sceneElements.OfType<CharacterElement>())
            {
                if (!element.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase) &&
                    !side.OtherCharactersInScene.Contains(element.Name))
                {
                    side.OtherCharactersInScene.Add(element.Name);
                }
            }

            // Extract action before first dialogue
            if (characterDialogueElements.Count > 0)
            {
                var firstDialogueIdx = characterDialogueElements[0];
                for (int i = 0; i < firstDialogueIdx; i++)
                {
                    if (sceneElements[i] is ActionElement action)
                        side.ActionBeforeDialogue.Add(action.Text);
                }
            }

            // Extract action after last dialogue
            if (characterDialogueElements.Count > 0)
            {
                var lastDialogueIdx = characterDialogueElements[characterDialogueElements.Count - 1];
                for (int i = lastDialogueIdx + 1; i < sceneElements.Count; i++)
                {
                    if (sceneElements[i] is ActionElement action)
                        side.ActionAfterDialogue.Add(action.Text);
                }
            }

            return side;
        }

        /// <summary>
        /// Extract individual dialogue block with surrounding context
        /// </summary>
        private DialogueBlock ExtractDialogueBlock(string characterName, List<ScriptElement> sceneElements, int characterIndex)
        {
            var block = new DialogueBlock { Character = characterName, LineNumber = characterIndex };

            // Get parenthetical after character name
            if (characterIndex + 1 < sceneElements.Count && sceneElements[characterIndex + 1] is ParentheticalElement paren)
            {
                block.Parenthetical = paren.Text;
            }

            // Get dialogue
            var dialogueIdx = characterIndex + 1;
            if (block.Parenthetical != string.Empty)
                dialogueIdx++;  // Skip parenthetical

            if (dialogueIdx < sceneElements.Count && sceneElements[dialogueIdx] is DialogueElement dialogue)
            {
                block.DialogueText = dialogue.Text;
            }

            // Get preceding action (immediately before character name)
            if (characterIndex > 0 && sceneElements[characterIndex - 1] is ActionElement precedingAction)
            {
                block.PrecedingAction = precedingAction.Text;
            }

            // Get following action (after dialogue, before next character or transition)
            var followingIdx = dialogueIdx + 1;
            if (followingIdx < sceneElements.Count && sceneElements[followingIdx] is ActionElement followingAction)
            {
                block.FollowingAction = followingAction.Text;
            }

            // Get cue lines from other characters
            for (int i = characterIndex - 1; i >= Math.Max(0, characterIndex - 5); i--)
            {
                if (sceneElements[i] is DialogueElement cueDialogue)
                {
                    var cueLine = cueDialogue.Text;
                    var words = cueLine.Split(' ');
                    var lastWords = string.Join(" ", words.TakeLast(Math.Min(3, words.Length)));
                    block.CueLines.Add(lastWords);
                }
            }

            return block;
        }

        /// <summary>
        /// Generate formatted character sides document (text)
        /// </summary>
        public string GenerateCharacterSideDocument(CharacterSide side)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("╔════════════════════════════════════════════════════════════════════════════════╗");
            sb.AppendLine($"║  CHARACTER SIDES: {side.CharacterName.ToUpper().PadRight(60)}║");
            if (!string.IsNullOrEmpty(side.ActorName))
                sb.AppendLine($"║  ACTOR: {side.ActorName.PadRight(71)}║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════════════════════╝\n");

            // Summary
            sb.AppendLine($"Total Scenes: {side.TotalScenes}");
            sb.AppendLine($"Total Lines: {side.TotalLines}");
            sb.AppendLine($"Estimated Screen Time: {side.EstimatedMinutes:F1} minutes");
            if (side.CoActors.Count > 0)
                sb.AppendLine($"Co-Stars: {string.Join(", ", side.CoActors)}");
            sb.AppendLine();

            // Scenes
            foreach (var scene in side.Scenes)
            {
                sb.AppendLine($"\n{'═' * 80}");
                sb.AppendLine($"SCENE {scene.SceneNumber}");
                sb.AppendLine($"{scene.SceneHeading}");
                sb.AppendLine($"{'═' * 80}\n");

                if (scene.ActionBeforeDialogue.Count > 0)
                {
                    sb.AppendLine("--- SCENE SETUP ---");
                    foreach (var action in scene.ActionBeforeDialogue.Take(2))
                    {
                        sb.AppendLine(action);
                    }
                    sb.AppendLine();
                }

                foreach (var dialogue in scene.DialogueBlocks)
                {
                    if (!string.IsNullOrEmpty(dialogue.PrecedingAction))
                        sb.AppendLine(dialogue.PrecedingAction);

                    // Cue lines (other character's words)
                    if (dialogue.CueLines.Count > 0)
                    {
                        sb.AppendLine($"  [...{dialogue.CueLines[0]}...]");
                    }

                    sb.AppendLine($"{dialogue.Character}");
                    if (!string.IsNullOrEmpty(dialogue.Parenthetical))
                        sb.AppendLine($"  {dialogue.Parenthetical}");
                    sb.AppendLine($"  {dialogue.DialogueText}");

                    if (!string.IsNullOrEmpty(dialogue.FollowingAction))
                        sb.AppendLine(dialogue.FollowingAction);

                    sb.AppendLine();
                }

                if (!string.IsNullOrEmpty(scene.SceneNotes))
                {
                    sb.AppendLine($"NOTES: {scene.SceneNotes}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate actor briefing document with character background
        /// </summary>
        public string GenerateActorBriefing(CharacterSide side, string characterBackground = "", string motivations = "")
        {
            var sb = new StringBuilder();

            sb.AppendLine("╔════════════════════════════════════════════════════════════════════════════════╗");
            sb.AppendLine($"║  ACTOR BRIEFING: {side.CharacterName.ToUpper().PadRight(59)}║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════════════════════╝\n");

            sb.AppendLine("CHARACTER INFORMATION");
            sb.AppendLine(new string('─', 80));

            if (!string.IsNullOrEmpty(characterBackground))
            {
                sb.AppendLine("BACKGROUND:");
                sb.AppendLine(characterBackground);
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(motivations))
            {
                sb.AppendLine("KEY MOTIVATIONS:");
                sb.AppendLine(motivations);
                sb.AppendLine();
            }

            sb.AppendLine($"SCENE BREAKDOWN: {side.TotalScenes} scenes");
            sb.AppendLine($"TOTAL DIALOGUE: {side.TotalLines} lines");

            if (side.CoActors.Count > 0)
            {
                sb.AppendLine($"\nINTERACTIONS WITH:");
                foreach (var actor in side.CoActors)
                {
                    sb.AppendLine($"  - {actor}");
                }
            }

            sb.AppendLine("\n" + new string('═', 80));
            sb.AppendLine("CHARACTER SIDES");
            sb.AppendLine(new string('═', 80));

            sb.Append(GenerateCharacterSideDocument(side));

            return sb.ToString();
        }

        /// <summary>
        /// Generate all character sides for distribution
        /// </summary>
        public Dictionary<string, CharacterSide> GenerateAllCharacterSides(List<Character> characters, List<Scene> scenes, List<ScriptElement> elements)
        {
            var sides = new Dictionary<string, CharacterSide>();

            foreach (var character in characters)
            {
                var side = GenerateCharacterSide(character.Name, scenes, elements);
                sides[character.Name] = side;
            }

            return sides;
        }

        /// <summary>
        /// Generate batch export of all character sides
        /// </summary>
        public Dictionary<string, string> ExportAllCharacterSidesDocuments(Dictionary<string, CharacterSide> allSides)
        {
            var exports = new Dictionary<string, string>();

            foreach (var kvp in allSides)
            {
                exports[kvp.Key] = GenerateCharacterSideDocument(kvp.Value);
            }

            return exports;
        }

        /// <summary>
        /// Get character interaction matrix
        /// </summary>
        public Dictionary<string, List<string>> GetCharacterInteractions(List<CharacterSide> sides)
        {
            var interactions = new Dictionary<string, List<string>>();

            foreach (var side in sides)
            {
                interactions[side.CharacterName] = new List<string>(side.CoActors);
            }

            return interactions;
        }
    }
}
