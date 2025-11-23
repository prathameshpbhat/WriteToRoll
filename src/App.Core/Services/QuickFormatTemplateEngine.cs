using System;
using System.Collections.Generic;
using System.Linq;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Fade-In Feature: Quick Format Templates & Scene Templates
    /// 
    /// Features:
    /// - Pre-built scene templates (phone call, action sequence, dialogue scene)
    /// - One-click formatting
    /// - Customizable templates
    /// - Save frequent formatting patterns
    /// - Quick element insertion
    /// </summary>
    public class QuickFormatTemplateEngine
    {
        public class SceneTemplate
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public List<TemplateElement> Elements { get; set; } = new();
            public int Uses { get; set; }
            public bool IsBuiltIn { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }

        public class TemplateElement
        {
            public ScriptElementType Type { get; set; }
            public string Content { get; set; } = string.Empty;
            public string Placeholder { get; set; } = string.Empty;  // For user to fill in
            public int Order { get; set; }
        }

        public class FormatPreset
        {
            public string Name { get; set; } = string.Empty;
            public FormattingMeta Formatting { get; set; } = new();
            public bool IsDefault { get; set; }
        }

        private Dictionary<string, SceneTemplate> _templates = new();
        private Dictionary<string, FormatPreset> _formatPresets = new();

        public QuickFormatTemplateEngine()
        {
            InitializeBuiltInTemplates();
            InitializeFormatPresets();
        }

        /// <summary>
        /// Initialize built-in scene templates
        /// </summary>
        private void InitializeBuiltInTemplates()
        {
            // Phone Call Template
            _templates["phone_call"] = new SceneTemplate
            {
                Name = "Phone Call Scene",
                Description = "Two characters having a phone conversation",
                IsBuiltIn = true,
                Elements = new List<TemplateElement>
                {
                    new() { Type = ScriptElementType.Action, Content = "[CHARACTER] sits at desk, phone in hand. [CHARACTER2] on the other end.", Order = 1 },
                    new() { Type = ScriptElementType.Character, Content = "[CHARACTER]", Order = 2 },
                    new() { Type = ScriptElementType.Dialogue, Content = "[Type dialogue here]", Order = 3 },
                    new() { Type = ScriptElementType.Character, Content = "[CHARACTER2] (V.O.)", Order = 4 },
                    new() { Type = ScriptElementType.Dialogue, Content = "[Type response here]", Order = 5 }
                }
            };

            // Action Sequence Template
            _templates["action_sequence"] = new SceneTemplate
            {
                Name = "Action Sequence",
                Description = "Fast-paced action with stunts and movement",
                IsBuiltIn = true,
                Elements = new List<TemplateElement>
                {
                    new() { Type = ScriptElementType.Action, Content = "[DESCRIBE THE ACTION IN PRESENT TENSE]", Order = 1 },
                    new() { Type = ScriptElementType.Action, Content = "[ACTION CONTINUES - describe movement and choreography]", Order = 2 },
                    new() { Type = ScriptElementType.Character, Content = "[CHARACTER]", Order = 3 },
                    new() { Type = ScriptElementType.Dialogue, Content = "[Dialogue during action]", Order = 4 },
                    new() { Type = ScriptElementType.Action, Content = "[ACTION CLIMAX - describe the payoff]", Order = 5 }
                }
            };

            // Dialogue Heavy Scene Template
            _templates["dialogue_scene"] = new SceneTemplate
            {
                Name = "Dialogue Heavy Scene",
                Description = "Character-focused conversation with multiple participants",
                IsBuiltIn = true,
                Elements = new List<TemplateElement>
                {
                    new() { Type = ScriptElementType.Action, Content = "[SETTING THE SCENE - describe where characters are and what they're doing]", Order = 1 },
                    new() { Type = ScriptElementType.Character, Content = "[CHARACTER1]", Order = 2 },
                    new() { Type = ScriptElementType.Dialogue, Content = "[First line of dialogue]", Order = 3 },
                    new() { Type = ScriptElementType.Character, Content = "[CHARACTER2]", Order = 4 },
                    new() { Type = ScriptElementType.Dialogue, Content = "[Response]", Order = 5 },
                    new() { Type = ScriptElementType.Character, Content = "[CHARACTER1]", Order = 6 },
                    new() { Type = ScriptElementType.Dialogue, Content = "[Next line]", Order = 7 }
                }
            };

            // Montage Template
            _templates["montage"] = new SceneTemplate
            {
                Name = "Montage Sequence",
                Description = "Series of short scenes showing passage of time",
                IsBuiltIn = true,
                Elements = new List<TemplateElement>
                {
                    new() { Type = ScriptElementType.Transition, Content = "MONTAGE - [DESCRIBE THEME]", Order = 1 },
                    new() { Type = ScriptElementType.Action, Content = "[SCENE 1 - brief description]", Order = 2 },
                    new() { Type = ScriptElementType.Action, Content = "[SCENE 2 - brief description]", Order = 3 },
                    new() { Type = ScriptElementType.Action, Content = "[SCENE 3 - brief description]", Order = 4 },
                    new() { Type = ScriptElementType.Action, Content = "[SCENE 4 - brief description]", Order = 5 },
                    new() { Type = ScriptElementType.Transition, Content = "END MONTAGE", Order = 6 }
                }
            };

            // Flashback Template
            _templates["flashback"] = new SceneTemplate
            {
                Name = "Flashback Scene",
                Description = "Jump to the past with visual style change",
                IsBuiltIn = true,
                Elements = new List<TemplateElement>
                {
                    new() { Type = ScriptElementType.Transition, Content = "FLASHBACK", Order = 1 },
                    new() { Type = ScriptElementType.Action, Content = "[PAST LOCATION AND TIME DESCRIPTION]", Order = 2 },
                    new() { Type = ScriptElementType.Character, Content = "[YOUNGER CHARACTER / PAST VERSION]", Order = 3 },
                    new() { Type = ScriptElementType.Dialogue, Content = "[Dialogue in flashback]", Order = 4 },
                    new() { Type = ScriptElementType.Action, Content = "[Continue flashback action]", Order = 5 },
                    new() { Type = ScriptElementType.Transition, Content = "BACK TO PRESENT", Order = 6 }
                }
            };

            // Entrance/Exit Template
            _templates["entrance_exit"] = new SceneTemplate
            {
                Name = "Character Entrance/Exit",
                Description = "Quick character introduction or exit",
                IsBuiltIn = true,
                Elements = new List<TemplateElement>
                {
                    new() { Type = ScriptElementType.Action, Content = "[CHARACTER] enters. [BRIEF DESCRIPTION OF APPEARANCE/MOOD].", Order = 1 },
                    new() { Type = ScriptElementType.Character, Content = "[CHARACTER]", Order = 2 },
                    new() { Type = ScriptElementType.Dialogue, Content = "[Greeting or opening line]", Order = 3 }
                }
            };

            // Reaction/Emotion Template
            _templates["reaction"] = new SceneTemplate
            {
                Name = "Character Reaction",
                Description = "Show character's emotional response",
                IsBuiltIn = true,
                Elements = new List<TemplateElement>
                {
                    new() { Type = ScriptElementType.Action, Content = "[CHARACTER] [DESCRIBES REACTION - shocked, happy, angry, etc.]", Order = 1 },
                    new() { Type = ScriptElementType.Character, Content = "[CHARACTER]", Order = 2 },
                    new() { Type = ScriptElementType.Parenthetical, Content = "[EMOTION]", Order = 3 },
                    new() { Type = ScriptElementType.Dialogue, Content = "[Emotional dialogue or exclamation]", Order = 4 }
                }
            };

            // Narration Template
            _templates["narration"] = new SceneTemplate
            {
                Name = "Narration/Voiceover",
                Description = "Narrator provides background or insight",
                IsBuiltIn = true,
                Elements = new List<TemplateElement>
                {
                    new() { Type = ScriptElementType.Action, Content = "[VISUAL DESCRIPTION - what we see]", Order = 1 },
                    new() { Type = ScriptElementType.Character, Content = "[NARRATOR]", Order = 2 },
                    new() { Type = ScriptElementType.Parenthetical, Content = "(V.O.)", Order = 3 },
                    new() { Type = ScriptElementType.Dialogue, Content = "[Narration - provide context or insight]", Order = 4 }
                }
            };
        }

        /// <summary>
        /// Initialize format presets
        /// </summary>
        private void InitializeFormatPresets()
        {
            // Standard screenplay format
            _formatPresets["standard"] = new FormatPreset
            {
                Name = "Standard Screenplay",
                IsDefault = true,
                Formatting = new FormattingMeta
                {
                    LeftMargin = "1.5\"",
                    RightMargin = "1.0\"",
                    FontName = "Courier New",
                    FontSize = 12,
                    LineHeight = 1.5
                }
            };

            // European format (A4 paper)
            _formatPresets["european"] = new FormatPreset
            {
                Name = "European (A4)",
                Formatting = new FormattingMeta
                {
                    LeftMargin = "2.0\"",
                    RightMargin = "1.5\"",
                    FontName = "Courier New",
                    FontSize = 11,
                    LineHeight = 1.5
                }
            };

            // TV Movie format (tighter spacing)
            _formatPresets["tv_movie"] = new FormatPreset
            {
                Name = "TV Movie",
                Formatting = new FormattingMeta
                {
                    LeftMargin = "1.25\"",
                    RightMargin = "0.75\"",
                    FontName = "Courier New",
                    FontSize = 11,
                    LineHeight = 1.35
                }
            };

            // Stage play format
            _formatPresets["stage_play"] = new FormatPreset
            {
                Name = "Stage Play",
                Formatting = new FormattingMeta
                {
                    LeftMargin = "1.0\"",
                    RightMargin = "1.0\"",
                    FontName = "Courier New",
                    FontSize = 12,
                    LineHeight = 1.5
                }
            };
        }

        /// <summary>
        /// Get all available templates
        /// </summary>
        public List<SceneTemplate> GetAllTemplates()
        {
            return _templates.Values.OrderBy(t => t.Name).ToList();
        }

        /// <summary>
        /// Get template by ID
        /// </summary>
        public SceneTemplate GetTemplate(string templateId)
        {
            return _templates.ContainsKey(templateId) ? _templates[templateId] : null;
        }

        /// <summary>
        /// Get built-in templates only
        /// </summary>
        public List<SceneTemplate> GetBuiltInTemplates()
        {
            return _templates.Values.Where(t => t.IsBuiltIn).OrderBy(t => t.Name).ToList();
        }

        /// <summary>
        /// Get custom templates (user-created)
        /// </summary>
        public List<SceneTemplate> GetCustomTemplates()
        {
            return _templates.Values.Where(t => !t.IsBuiltIn).OrderByDescending(t => t.CreatedAt).ToList();
        }

        /// <summary>
        /// Create custom template from existing scene
        /// </summary>
        public SceneTemplate CreateTemplateFromScene(string name, string description, List<ScriptElement> elements)
        {
            var template = new SceneTemplate
            {
                Name = name,
                Description = description,
                IsBuiltIn = false,
                CreatedAt = DateTime.UtcNow
            };

            int order = 1;
            foreach (var element in elements)
            {
                template.Elements.Add(new TemplateElement
                {
                    Type = element.ElementType,
                    Content = element.Text,
                    Order = order++
                });
            }

            _templates[template.Id] = template;
            return template;
        }

        /// <summary>
        /// Apply template to create scene elements
        /// </summary>
        public List<ScriptElement> ApplyTemplate(string templateId, Dictionary<string, string> replacements = null)
        {
            replacements ??= new Dictionary<string, string>();
            var elements = new List<ScriptElement>();

            if (!_templates.ContainsKey(templateId))
                return elements;

            var template = _templates[templateId];
            template.Uses++;

            foreach (var templateElement in template.Elements.OrderBy(e => e.Order))
            {
                var content = templateElement.Content;

                // Replace placeholders
                foreach (var replacement in replacements)
                {
                    content = content.Replace($"[{replacement.Key}]", replacement.Value);
                }

                // Remove any remaining placeholders with empty strings
                content = System.Text.RegularExpressions.Regex.Replace(content, @"\[[^\]]+\]", "");

                // Create appropriate script element
                var element = CreateScriptElement(templateElement.Type, content);
                elements.Add(element);
            }

            return elements;
        }

        /// <summary>
        /// Get format preset
        /// </summary>
        public FormatPreset GetFormatPreset(string presetName)
        {
            return _formatPresets.ContainsKey(presetName) ? _formatPresets[presetName] : _formatPresets["standard"];
        }

        /// <summary>
        /// Get all format presets
        /// </summary>
        public List<FormatPreset> GetAllFormatPresets()
        {
            return _formatPresets.Values.OrderBy(p => p.Name).ToList();
        }

        /// <summary>
        /// Get most used templates
        /// </summary>
        public List<SceneTemplate> GetMostUsedTemplates(int count = 5)
        {
            return _templates.Values
                .OrderByDescending(t => t.Uses)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Delete custom template
        /// </summary>
        public bool DeleteTemplate(string templateId)
        {
            if (_templates.ContainsKey(templateId) && !_templates[templateId].IsBuiltIn)
            {
                _templates.Remove(templateId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Helper: Create script element from template element
        /// </summary>
        private ScriptElement CreateScriptElement(ScriptElementType type, string content)
        {
            return type switch
            {
                ScriptElementType.Action => new ActionElement { Text = content },
                ScriptElementType.Character => new CharacterElement { Name = content },
                ScriptElementType.Dialogue => new DialogueElement { Text = content },
                ScriptElementType.Parenthetical => new ParentheticalElement { Text = content },
                ScriptElementType.Transition => new TransitionElement { Text = content },
                ScriptElementType.SceneHeading => new SceneHeadingElement { Text = content },
                _ => new ActionElement { Text = content }
            };
        }

        /// <summary>
        /// Get template statistics
        /// </summary>
        public string GenerateTemplateStats()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== TEMPLATE STATISTICS ===");
            sb.AppendLine($"Total Templates: {_templates.Count}");
            sb.AppendLine($"Built-in Templates: {_templates.Values.Count(t => t.IsBuiltIn)}");
            sb.AppendLine($"Custom Templates: {_templates.Values.Count(t => !t.IsBuiltIn)}");
            sb.AppendLine();
            sb.AppendLine("Most Used Templates:");
            foreach (var template in GetMostUsedTemplates(5))
            {
                sb.AppendLine($"  {template.Name}: {template.Uses} uses");
            }
            return sb.ToString();
        }
    }
}
