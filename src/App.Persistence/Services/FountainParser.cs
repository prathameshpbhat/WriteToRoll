using System;
using System.Collections.Generic;
using System.Text;
using ActionElement = App.Core.Models.Action;
using App.Core.Models;

namespace App.Persistence.Services;

public interface IFountainParser
{
    Script ParseFountain(string fountainText);
    string ConvertToFountain(Script script);
}

public class FountainParser : IFountainParser
{
    public Script ParseFountain(string fountainText)
    {
        var script = new Script();
        var currentScene = new Scene();
        var lines = fountainText.Split('\n');
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            if (IsSceneHeading(trimmedLine))
            {
                if (currentScene.Content.Any())
                {
                    script.Scenes.Add(currentScene);
                    currentScene = new Scene();
                }
                ParseSceneHeading(trimmedLine, currentScene);
            }
            else if (IsCharacter(trimmedLine))
            {
                currentScene.Content.Add(new Character { Text = trimmedLine });
            }
            else if (IsParenthetical(trimmedLine))
            {
                currentScene.Content.Add(new Parenthetical { Text = trimmedLine });
            }
            else if (IsTransition(trimmedLine))
            {
                currentScene.Content.Add(new Transition { Text = trimmedLine });
            }
            else if (IsCenteredText(trimmedLine))
            {
                currentScene.Content.Add(new CenteredText { Text = trimmedLine.Trim('>')});
            }
            else
            {
                // Default to Action
                currentScene.Content.Add(new ActionElement { Text = trimmedLine });
            }
        }

        // Add the last scene if it has content
        if (currentScene.Content.Any())
        {
            script.Scenes.Add(currentScene);
        }

        return script;
    }

    public string ConvertToFountain(Script script)
    {
        var output = new StringBuilder();

        // Add title page metadata
        output.AppendLine($"Title: {script.Title}");
        output.AppendLine($"Author: {script.Author}");
        output.AppendLine($"Draft: {script.DraftVersion}");
        output.AppendLine();

        foreach (var scene in script.Scenes)
        {
            // Scene heading
            output.AppendLine(scene.Heading.ToString());
            output.AppendLine();

            foreach (var element in scene.Content)
            {
                switch (element)
                {
                    case Character character:
                        output.AppendLine(character.Text.ToUpper());
                        break;
                    case Parenthetical parenthetical:
                        output.AppendLine($"({parenthetical.Text})");
                        break;
                    case Dialogue dialogue:
                        output.AppendLine(dialogue.Text);
                        output.AppendLine();
                        break;
                    case Transition transition:
                        output.AppendLine($"{transition.Text.ToUpper()} TO:");
                        output.AppendLine();
                        break;
                    case CenteredText centered:
                        output.AppendLine($">{centered.Text}<");
                        output.AppendLine();
                        break;
                    default:
                        output.AppendLine(element.Text);
                        output.AppendLine();
                        break;
                }
            }

            output.AppendLine();
        }

        return output.ToString();
    }

    private bool IsSceneHeading(string line)
    {
        return line.StartsWith("INT.") || line.StartsWith("EXT.") || 
               line.StartsWith("INT./EXT.") || line.StartsWith("INT/EXT");
    }

    private bool IsCharacter(string line)
    {
        return line.All(c => char.IsUpper(c) || char.IsWhiteSpace(c)) && 
               line.Length < 40 && !line.EndsWith("TO:");
    }

    private bool IsParenthetical(string line)
    {
        return line.StartsWith("(") && line.EndsWith(")");
    }

    private bool IsTransition(string line)
    {
        return line.EndsWith("TO:") || line.StartsWith("FADE") || line.StartsWith("DISSOLVE");
    }

    private bool IsCenteredText(string line)
    {
        return line.StartsWith(">") && line.EndsWith("<");
    }

    private void ParseSceneHeading(string line, Scene scene)
    {
        var separator = " - ";
        var parts = line.Split(new[] { separator }, 2, StringSplitOptions.None);
        var location = parts[0].Trim();
        var time = parts.Length > 1 ? parts[1].Trim() : string.Empty;

        scene.Heading = new SceneHeading
        {
            Location = location.Replace("INT.", "").Replace("EXT.", "").Trim(),
            Time = time,
            IsInterior = location.StartsWith("INT.")
        };
    }
}