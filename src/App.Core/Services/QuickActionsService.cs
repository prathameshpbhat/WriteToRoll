using System;
using System.Collections.Generic;
using System.Linq;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Fade-In Feature: Quick Actions & Shortcuts for Common Tasks
    /// 
    /// Features:
    /// - One-click duplicate scene
    /// - Quick character rename across script
    /// - Batch element insertion
    /// - Quick formatting shortcuts
    /// - Undo/Redo stack
    /// - Common macro operations
    /// </summary>
    public class QuickActionsService
    {
        public class ActionCommand
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Shortcut { get; set; } = string.Empty;
            public Func<Script, bool>? Execute { get; set; }
        }

        public class ScriptSnapshot
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
            public List<ScriptElement> ElementsCopy { get; set; } = new();
            public string Description { get; set; } = string.Empty;
        }

        private Stack<ScriptSnapshot> _undoStack = new();
        private Stack<ScriptSnapshot> _redoStack = new();
        private Dictionary<string, ActionCommand> _commands = new();
        private int _maxUndoLevels = 50;

        public QuickActionsService()
        {
            InitializeQuickCommands();
        }

        /// <summary>
        /// Initialize quick action commands
        /// </summary>
        private void InitializeQuickCommands()
        {
            _commands["duplicate_scene"] = new ActionCommand
            {
                Name = "Duplicate Scene",
                Description = "Create a copy of the current scene",
                Shortcut = "Ctrl+D",
                Execute = (script) => true // Implementation in method
            };

            _commands["rename_character"] = new ActionCommand
            {
                Name = "Rename Character",
                Description = "Replace all instances of a character name",
                Shortcut = "Ctrl+H",
                Execute = (script) => true
            };

            _commands["add_action"] = new ActionCommand
            {
                Name = "Quick Add Action",
                Description = "Insert action line at cursor",
                Shortcut = "Shift+A",
                Execute = (script) => true
            };

            _commands["add_dialogue"] = new ActionCommand
            {
                Name = "Quick Add Dialogue",
                Description = "Insert dialogue line at cursor",
                Shortcut = "Shift+D",
                Execute = (script) => true
            };
        }

        /// <summary>
        /// Create snapshot for undo
        /// </summary>
        public void CreateSnapshot(Script script, string description = "")
        {
            // Clear redo stack on new action
            _redoStack.Clear();

            var snapshot = new ScriptSnapshot
            {
                Description = description,
                CreatedAt = DateTime.UtcNow,
                ElementsCopy = new List<ScriptElement>(script.Elements)
            };

            _undoStack.Push(snapshot);

            // Maintain max undo levels
            if (_undoStack.Count > _maxUndoLevels)
            {
                var items = _undoStack.ToList();
                _undoStack.Clear();
                foreach (var item in items.Take(_maxUndoLevels))
                {
                    _undoStack.Push(item);
                }
            }
        }

        /// <summary>
        /// Undo last action
        /// </summary>
        public bool Undo(Script script)
        {
            if (_undoStack.Count == 0)
                return false;

            // Save current state to redo stack
            var currentSnapshot = new ScriptSnapshot
            {
                ElementsCopy = new List<ScriptElement>(script.Elements),
                CreatedAt = DateTime.UtcNow
            };
            _redoStack.Push(currentSnapshot);

            // Restore from undo stack
            var previousSnapshot = _undoStack.Pop();
            script.Elements.Clear();
            script.Elements.AddRange(previousSnapshot.ElementsCopy);

            return true;
        }

        /// <summary>
        /// Redo last undone action
        /// </summary>
        public bool Redo(Script script)
        {
            if (_redoStack.Count == 0)
                return false;

            // Save current state to undo stack
            var currentSnapshot = new ScriptSnapshot
            {
                ElementsCopy = new List<ScriptElement>(script.Elements),
                CreatedAt = DateTime.UtcNow
            };
            _undoStack.Push(currentSnapshot);

            // Restore from redo stack
            var nextSnapshot = _redoStack.Pop();
            script.Elements.Clear();
            script.Elements.AddRange(nextSnapshot.ElementsCopy);

            return true;
        }

        /// <summary>
        /// Duplicate a scene
        /// </summary>
        public bool DuplicateScene(Script script, int sceneIndex, bool insertAfter = true)
        {
            if (sceneIndex < 0 || sceneIndex >= script.Elements.Count)
                return false;

            // Find scene heading
            int sceneStart = -1;
            int sceneEnd = script.Elements.Count;

            for (int i = sceneIndex; i >= 0; i--)
            {
                if (script.Elements[i] is SceneHeadingElement)
                {
                    sceneStart = i;
                    break;
                }
            }

            if (sceneStart == -1)
                return false;

            // Find scene end
            for (int i = sceneStart + 1; i < script.Elements.Count; i++)
            {
                if (script.Elements[i] is SceneHeadingElement)
                {
                    sceneEnd = i;
                    break;
                }
            }

            // Copy scene elements
            var sceneCopy = new List<ScriptElement>();
            for (int i = sceneStart; i < sceneEnd; i++)
            {
                sceneCopy.Add(script.Elements[i]);
            }

            // Insert copy
            int insertPosition = insertAfter ? sceneEnd : sceneStart;
            sceneCopy.Reverse();
            foreach (var element in sceneCopy)
            {
                script.Elements.Insert(insertPosition, CloneElement(element));
            }

            return true;
        }

        /// <summary>
        /// Rename all instances of a character
        /// </summary>
        public int RenameCharacter(Script script, string oldName, string newName)
        {
            int count = 0;

            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName))
                return 0;

            oldName = oldName.Trim();
            newName = newName.Trim();

            foreach (var element in script.Elements)
            {
                if (element is CharacterElement ce && ce.Name?.Trim().ToUpper() == oldName.ToUpper())
                {
                    ce.Name = newName;
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Quick add action element
        /// </summary>
        public void AddAction(Script script, string actionText, int insertIndex = -1)
        {
            var actionElement = new ActionElement { Text = actionText };

            if (insertIndex < 0 || insertIndex >= script.Elements.Count)
            {
                script.Elements.Add(actionElement);
            }
            else
            {
                script.Elements.Insert(insertIndex, actionElement);
            }
        }

        /// <summary>
        /// Quick add dialogue
        /// </summary>
        public void AddDialogue(Script script, string characterName, string dialogueText, 
            string parenthetical = "", int insertIndex = -1)
        {
            var insertPos = insertIndex < 0 ? script.Elements.Count : insertIndex;

            var charElement = new CharacterElement { Name = characterName };
            script.Elements.Insert(insertPos, charElement);

            if (!string.IsNullOrWhiteSpace(parenthetical))
            {
                var parenElement = new ParentheticalElement { Text = parenthetical };
                script.Elements.Insert(insertPos + 1, parenElement);
                insertPos++;
            }

            var dialogueElement = new DialogueElement { Text = dialogueText };
            script.Elements.Insert(insertPos + 1, dialogueElement);
        }

        /// <summary>
        /// Quick add scene heading
        /// </summary>
        public void AddScene(Script script, string location, string timeOfDay = "DAY", int insertIndex = -1)
        {
            var heading = $"INT. {location} - {timeOfDay}";
            var sceneElement = new SceneHeadingElement { Text = heading };

            if (insertIndex < 0 || insertIndex >= script.Elements.Count)
            {
                script.Elements.Add(sceneElement);
            }
            else
            {
                script.Elements.Insert(insertIndex, sceneElement);
            }
        }

        /// <summary>
        /// Remove all empty elements
        /// </summary>
        public int CleanupEmptyElements(Script script)
        {
            int count = 0;
            var emptyElements = new List<ScriptElement>();

            foreach (var element in script.Elements)
            {
                var text = element switch
                {
                    ActionElement ae => ae.Text,
                    DialogueElement de => de.Text,
                    CharacterElement ce => ce.Name,
                    TransitionElement te => te.Text,
                    SceneHeadingElement she => she.Text,
                    _ => null
                };

                if (string.IsNullOrWhiteSpace(text))
                {
                    emptyElements.Add(element);
                }
            }

            foreach (var element in emptyElements)
            {
                script.Elements.Remove(element);
                count++;
            }

            return count;
        }

        /// <summary>
        /// Replace all instances of text
        /// </summary>
        public int ReplaceAllText(Script script, string findText, string replaceText, bool caseSensitive = false)
        {
            int count = 0;

            foreach (var element in script.Elements)
            {
                var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                switch (element)
                {
                    case ActionElement ae:
                        if (!string.IsNullOrEmpty(ae.Text) && ae.Text.Contains(findText, comparison))
                        {
                            ae.Text = ae.Text.Replace(findText, replaceText, comparison);
                            count++;
                        }
                        break;
                    case DialogueElement de:
                        if (!string.IsNullOrEmpty(de.Text) && de.Text.Contains(findText, comparison))
                        {
                            de.Text = de.Text.Replace(findText, replaceText, comparison);
                            count++;
                        }
                        break;
                    case TransitionElement te:
                        if (!string.IsNullOrEmpty(te.Text) && te.Text.Contains(findText, comparison))
                        {
                            te.Text = te.Text.Replace(findText, replaceText, comparison);
                            count++;
                        }
                        break;
                    case SceneHeadingElement she:
                        if (!string.IsNullOrEmpty(she.Text) && she.Text.Contains(findText, comparison))
                        {
                            she.Text = she.Text.Replace(findText, replaceText, comparison);
                            count++;
                        }
                        break;
                }
            }

            return count;
        }

        /// <summary>
        /// Merge consecutive dialogue from same character
        /// </summary>
        public int MergeConsecutiveDialogue(Script script)
        {
            int count = 0;
            var elementsToRemove = new List<ScriptElement>();

            for (int i = 0; i < script.Elements.Count - 1; i++)
            {
                if (script.Elements[i] is CharacterElement ce1 && 
                    script.Elements[i + 1] is DialogueElement de1 &&
                    i + 2 < script.Elements.Count &&
                    script.Elements[i + 2] is CharacterElement ce2 &&
                    ce1.Name == ce2.Name)
                {
                    // Same character speaking twice in a row
                    if (i + 3 < script.Elements.Count && script.Elements[i + 3] is DialogueElement de2)
                    {
                        de1.Text += " " + de2.Text;
                        elementsToRemove.Add(script.Elements[i + 2]);
                        elementsToRemove.Add(script.Elements[i + 3]);
                        count++;
                    }
                }
            }

            foreach (var element in elementsToRemove)
            {
                script.Elements.Remove(element);
            }

            return count;
        }

        /// <summary>
        /// Add element at specific location
        /// </summary>
        public void InsertElementAfterCharacter(Script script, string characterName, ScriptElement newElement)
        {
            for (int i = 0; i < script.Elements.Count; i++)
            {
                if (script.Elements[i] is CharacterElement ce && ce.Name == characterName)
                {
                    // Insert after this character's dialogue
                    int insertPos = i + 1;
                    while (insertPos < script.Elements.Count &&
                           (script.Elements[insertPos] is ParentheticalElement || 
                            script.Elements[insertPos] is DialogueElement))
                    {
                        insertPos++;
                    }

                    script.Elements.Insert(insertPos, newElement);
                    return;
                }
            }

            // If not found, add at end
            script.Elements.Add(newElement);
        }

        /// <summary>
        /// Convert parenthetical text to proper format
        /// </summary>
        public int NormalizeParentheticals(Script script)
        {
            int count = 0;
            var standardParentheticals = new[] { "V.O.", "O.S.", "CONT'D", "INTERCUT", "B.G." };

            foreach (var element in script.Elements.OfType<ParentheticalElement>())
            {
                var normalized = element.Text?.ToUpper().Trim() ?? string.Empty;

                if (!normalized.StartsWith("("))
                    normalized = "(" + normalized;
                if (!normalized.EndsWith(")"))
                    normalized = normalized + ")";

                // Remove parentheses for comparison
                var inner = normalized.Substring(1, normalized.Length - 2).Trim();

                foreach (var standard in standardParentheticals)
                {
                    if (inner.Equals(standard, StringComparison.OrdinalIgnoreCase))
                    {
                        element.Text = standard;
                        count++;
                        break;
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Get undo count
        /// </summary>
        public int GetUndoCount() => _undoStack.Count;

        /// <summary>
        /// Get redo count
        /// </summary>
        public int GetRedoCount() => _redoStack.Count;

        /// <summary>
        /// Get all quick commands
        /// </summary>
        public List<ActionCommand> GetAllCommands()
        {
            return _commands.Values.ToList();
        }

        /// <summary>
        /// Get command by name
        /// </summary>
        public ActionCommand? GetCommand(string commandName)
        {
            return _commands.ContainsKey(commandName) ? _commands[commandName] : null;
        }

        /// <summary>
        /// Clone a script element
        /// </summary>
        private ScriptElement CloneElement(ScriptElement element)
        {
            return element switch
            {
                ActionElement ae => new ActionElement { Text = ae.Text },
                DialogueElement de => new DialogueElement { Text = de.Text },
                CharacterElement ce => new CharacterElement { Name = ce.Name },
                ParentheticalElement pe => new ParentheticalElement { Text = pe.Text },
                TransitionElement te => new TransitionElement { Text = te.Text },
                SceneHeadingElement she => new SceneHeadingElement { Text = she.Text },
                _ => new ActionElement { Text = "" }
            };
        }

        /// <summary>
        /// Generate quick actions menu
        /// </summary>
        public string GenerateQuickActionsMenu()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== QUICK ACTIONS MENU ===");
            sb.AppendLine();

            foreach (var command in GetAllCommands())
            {
                sb.AppendLine($"[{command.Shortcut}] {command.Name}");
                sb.AppendLine($"  {command.Description}");
            }

            return sb.ToString();
        }
    }
}
