using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using App.Core.Models;

namespace App.Persistence.Services
{
    public class FountainParser : IFountainParser
    {
        public Script ParseFountain(string fountainText)
        {
            var script = new Script();
            if (string.IsNullOrEmpty(fountainText))
                return script;

            var lines = fountainText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int i = 0;

            while (i < lines.Length)
            {
                var line = lines[i];
                var trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    i++;
                    continue;
                }

                // Title page metadata
                if (trimmed.StartsWith("Title:"))
                {
                    script.Title = trimmed.Substring(6).Trim();
                    i++;
                    continue;
                }

                if (trimmed.StartsWith("Author:"))
                {
                    script.Author = trimmed.Substring(7).Trim();
                    i++;
                    continue;
                }

                // Scene heading
                if (IsSceneHeading(trimmed))
                {
                    var upper = trimmed.ToUpper();
                    bool isInterior = upper.StartsWith("INT");
                    string location = ExtractLocation(upper);
                    string time = ExtractTime(upper);

                    script.Elements.Add(new SceneHeadingElement
                    {
                        Location = location,
                        Time = time,
                        IsInterior = isInterior,
                        Text = trimmed
                    });
                    i++;
                    continue;
                }

                // Character
                if (IsCharacter(trimmed))
                {
                    script.Elements.Add(new CharacterElement
                    {
                        Name = trimmed,
                        Text = trimmed
                    });
                    i++;
                    continue;
                }

                // Transition
                if (IsTransition(trimmed))
                {
                    script.Elements.Add(new TransitionElement { Text = trimmed });
                    i++;
                    continue;
                }

                // Dialogue
                if (trimmed.StartsWith("(") && trimmed.EndsWith(")"))
                {
                    script.Elements.Add(new ParentheticalElement { Text = trimmed.Trim('(', ')') });
                    i++;
                    continue;
                }

                // Default to action
                script.Elements.Add(new ActionElement { Text = trimmed });
                i++;
            }

            return script;
        }

        public string ConvertToFountain(Script script)
        {
            var output = new StringBuilder();

            // Title page
            if (!string.IsNullOrEmpty(script.Title))
                output.AppendLine($"Title: {script.Title}");
            if (!string.IsNullOrEmpty(script.Author))
                output.AppendLine($"Author: {script.Author}");

            if (!string.IsNullOrEmpty(script.Title) || !string.IsNullOrEmpty(script.Author))
                output.AppendLine();

            // Elements
            foreach (var element in script.Elements)
            {
                switch (element)
                {
                    case SceneHeadingElement sh:
                        output.AppendLine(sh.GetFormattedOutput());
                        break;
                    case ActionElement a:
                        output.AppendLine(a.Text);
                        break;
                    case CharacterElement c:
                        output.AppendLine(c.GetFormattedOutput());
                        break;
                    case DialogueElement d:
                        output.AppendLine(d.Text);
                        break;
                    case ParentheticalElement p:
                        output.AppendLine(p.GetFormattedOutput());
                        break;
                    case TransitionElement t:
                        output.AppendLine(t.GetFormattedOutput());
                        break;
                    default:
                        output.AppendLine(element.GetFormattedOutput());
                        break;
                }
                output.AppendLine();
            }

            return output.ToString();
        }

        public (bool Success, string Message) ValidateFountain(string fountainText)
        {
            if (string.IsNullOrWhiteSpace(fountainText))
                return (false, "Fountain text cannot be empty");

            try
            {
                var script = ParseFountain(fountainText);
                if (!script.Elements.Any())
                    return (false, "No valid screenplay elements found");

                return (true, "Fountain format is valid");
            }
            catch (Exception ex)
            {
                return (false, $"Validation error: {ex.Message}");
            }
        }

        private bool IsSceneHeading(string line)
        {
            var upper = line.ToUpper();
            return Regex.IsMatch(upper, @"^\s*(INT|EXT|INT\.?/EXT\.?|EXT\.?/INT\.?)[\s\.]");
        }

        private bool IsCharacter(string line)
        {
            if (line.Length > 40 || line.Length < 2)
                return false;
            return Regex.IsMatch(line, @"^[A-Z0-9 ()''\.\-]+$");
        }

        private bool IsTransition(string line)
        {
            if (!line.ToUpper().EndsWith(":"))
                return false;
            return Regex.IsMatch(line, @"^[A-Z0-9 .''\-\(\)]+:$");
        }

        private string ExtractLocation(string sceneHeading)
        {
            var match = Regex.Match(sceneHeading, @"(?:INT|EXT|INT\.?/EXT\.?|EXT\.?/INT\.?)[./\s]+(.+?)(?:\s*-\s*|$)");
            return match.Success ? match.Groups[1].Value.Trim() : sceneHeading;
        }

        private string ExtractTime(string sceneHeading)
        {
            var match = Regex.Match(sceneHeading, @"-\s*(.+?)$");
            return match.Success ? match.Groups[1].Value.Trim() : "DAY";
        }
    }
}
