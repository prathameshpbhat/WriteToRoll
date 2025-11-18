using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Pure logic layer for screenplay block detection and normalization.
    /// Implements all rules from screenwriting_logic_expanded.txt spec
    /// </summary>
    public class ScreenwritingLogic : IScreenwritingLogic
    {
        // Regex patterns per Screenwriting_Software_Notes_Final.txt spec
        private static readonly Regex SceneHeadingRegex = new(
            @"^\s*(INT|EXT|INT\.?/EXT\.?|EXT\.?/INT\.?)(?=[\s./\-\)]|$)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );
        
        private static readonly Regex ShotRegex = new(
            @"^\s*(CLOSE|ANGLE|POV|WIDE|BACK|ON)\s",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );
        
        private static readonly Regex TransitionRegex = new(
            @"^[A-Z0-9 .''\-\(\)]+:$",
            RegexOptions.Compiled
        );
        
        private static readonly Regex AllCapsShortRegex = new(
            @"^[A-Z0-9 ()''\.\-]{1,40}$",
            RegexOptions.Compiled
        );
        
        private static readonly Regex ParentheticalRegex = new(
            @"^\s*\(.*\)\s*$",
            RegexOptions.Compiled
        );
        
        private static readonly Regex CenteredRegex = new(
            @"^\s*>\s*(.+?)\s*<\s*$",
            RegexOptions.Compiled
        );
        
        private static readonly Regex TimeTokenRegex = new(
            @"\b(DAY|NIGHT|MORNING|EVENING|NOON|DAWN|DUSK|LATER|CONTINUOUS)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );

        private static readonly string[] AllowedCharacterModifiers = new[] 
        { 
            "V.O.", "O.S.", "CONT'D", "O.C.", "(V.O.)", "(O.S.)", "(O.C.)" 
        };

        public DetectionResult DetectAndNormalize(string rawLine, ScriptContext context)
        {
            if (rawLine == null) rawLine = string.Empty;
            var trimmed = rawLine.Trim();
            var upper = trimmed.ToUpperInvariant();

            // Empty line handling
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return new DetectionResult(
                    string.Empty,
                    ScriptElementType.Action,
                    false,
                    false
                );
            }

            // 1) SCENE HEADING (INT./EXT./INT./EXT., etc.)
            if (SceneHeadingRegex.IsMatch(trimmed))
            {
                return ProcessSceneHeading(trimmed, upper, context);
            }

            // 2) PARENTHETICAL
            if (trimmed.StartsWith("(") || ParentheticalRegex.IsMatch(trimmed))
            {
                return ProcessParenthetical(trimmed);
            }

            // 3) TRANSITION (right-aligned, ALL CAPS, ends with :)
            if (trimmed.EndsWith(":") && TransitionRegex.IsMatch(trimmed))
            {
                return ProcessTransition(trimmed, upper);
            }

            // 4) SHOT HEADINGS
            if (ShotRegex.IsMatch(trimmed))
            {
                return ProcessShot(trimmed, upper);
            }

            // 5) CENTERED TEXT (> text <)
            var cm = CenteredRegex.Match(trimmed);
            if (cm.Success)
            {
                return ProcessCenteredText(cm);
            }

            // 6) CHARACTER detection (short, ALL CAPS, no trailing colon)
            if (IsLikelyCharacterLine(trimmed))
            {
                return ProcessCharacter(trimmed, upper);
            }

            // 7) DIALOGUE (follows Character)
            if (context.PreviousElementType == ScriptElementType.Character)
            {
                return new DetectionResult(
                    rawLine.TrimEnd(),
                    ScriptElementType.Dialogue,
                    false,
                    false
                );
            }

            // 8) Fallback: ACTION
            return ProcessAction(trimmed);
        }

        private DetectionResult ProcessSceneHeading(string trimmed, string upper, ScriptContext context)
        {
            var m = SceneHeadingRegex.Match(trimmed);
            string token = m.Groups[1].Value.ToUpperInvariant();

            // Canonicalize slug start
            string slugStart = token switch
            {
                "INT/EXT" or "INT./EXT." or "EXT/INT" or "EXT./INT." => "INT./EXT.",
                "INT" or "INT." => "INT.",
                "EXT" or "EXT." => "EXT.",
                _ => token.EndsWith(".") ? token : token + "."
            };

            // Preserve remainder (location/time)
            string remainder = trimmed.Substring(m.Length).TrimStart();
            string normalized = string.IsNullOrEmpty(remainder) 
                ? slugStart 
                : $"{slugStart} {NormalizeLocationAndTime(remainder)}";

            bool changed = !trimmed.StartsWith(slugStart, StringComparison.OrdinalIgnoreCase) 
                || trimmed != normalized;
            
            bool suggestAppend = context.OnFinalize && !TimeTokenRegex.IsMatch(normalized);

            return new DetectionResult(
                normalized,
                ScriptElementType.SceneHeading,
                true,
                changed,
                suggestAppend,
                suggestAppend ? " - DAY" : null
            );
        }

        private DetectionResult ProcessParenthetical(string trimmed)
        {
            string normalized = trimmed;
            if (!trimmed.EndsWith(")"))
            {
                normalized = "(" + trimmed.TrimStart('(') + ")";
            }

            return new DetectionResult(
                normalized,
                ScriptElementType.Parenthetical,
                true,
                normalized != trimmed
            );
        }

        private DetectionResult ProcessTransition(string trimmed, string upper)
        {
            var normalized = upper;
            return new DetectionResult(
                normalized,
                ScriptElementType.Transition,
                true,
                normalized != trimmed
            );
        }

        private DetectionResult ProcessShot(string trimmed, string upper)
        {
            var normalized = upper;
            return new DetectionResult(
                normalized,
                ScriptElementType.Shot,
                true,
                normalized != trimmed
            );
        }

        private DetectionResult ProcessCenteredText(Match cm)
        {
            var inner = cm.Groups[1].Value.Trim();
            return new DetectionResult(
                $"> {inner} <",
                ScriptElementType.CenteredText,
                false,
                true
            );
        }

        private DetectionResult ProcessCharacter(string trimmed, string upper)
        {
            var normalized = NormalizeCharacterLine(upper);
            return new DetectionResult(
                normalized,
                ScriptElementType.Character,
                true,
                normalized != trimmed
            );
        }

        private DetectionResult ProcessAction(string trimmed)
        {
            // Action block: apply sentence case (simple heuristic)
            var sentence = ToSentenceCase(trimmed);
            return new DetectionResult(
                sentence,
                ScriptElementType.Action,
                false,
                sentence != trimmed
            );
        }

        private static string NormalizeLocationAndTime(string remainder)
        {
            var parts = remainder.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return remainder.ToUpperInvariant();

            // Check if last token is a time indicator
            if (TimeTokenRegex.IsMatch(parts[^1]))
            {
                var time = parts[^1].ToUpperInvariant();
                var location = string.Join(' ', parts[..^1]).ToUpperInvariant();
                
                if (string.IsNullOrWhiteSpace(location))
                    return time;
                
                // Ensure em-dash separation
                return location.EndsWith(" -") || location.EndsWith("-") 
                    ? $"{location} {time}" 
                    : $"{location} - {time}";
            }

            return remainder.ToUpperInvariant();
        }

        private static bool IsLikelyCharacterLine(string trimmed)
        {
            if (trimmed.Length == 0 || trimmed.Length > 30) return false;
            
            // Must be all uppercase
            bool allCaps = trimmed == trimmed.ToUpperInvariant();
            if (!allCaps) return false;
            
            // Cannot end with colon (that's a transition)
            if (trimmed.EndsWith(":")) return false;
            
            // Must match ALL CAPS short regex pattern
            return AllCapsShortRegex.IsMatch(trimmed);
        }

        private static string NormalizeCharacterLine(string upper)
        {
            // Handle modifiers like V.O., O.S., etc.
            foreach (var mod in AllowedCharacterModifiers)
            {
                if (upper.EndsWith($" {mod}"))
                {
                    var name = upper.Substring(0, upper.Length - mod.Length - 1).Trim();
                    return $"{name} {mod}";
                }
                if (upper.EndsWith(mod) && !upper.StartsWith(mod))
                {
                    var name = upper.Substring(0, upper.Length - mod.Length).Trim();
                    return $"{name} {mod}";
                }
            }
            return upper.Trim();
        }

        private static string ToSentenceCase(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            
            var lower = text.ToLowerInvariant();
            return char.ToUpperInvariant(lower[0]) + (lower.Length > 1 ? lower.Substring(1) : string.Empty);
        }

        public ValidationResult Validate(ScriptElement element)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (element == null)
            {
                errors.Add("Element cannot be null");
                return new ValidationResult(false, errors, warnings);
            }

            // Validate by element type
            switch (element.ElementType)
            {
                case ScriptElementType.SceneHeading:
                    if (string.IsNullOrWhiteSpace(element.Text))
                        errors.Add("Scene heading cannot be empty");
                    break;

                case ScriptElementType.Character:
                    if (string.IsNullOrWhiteSpace(element.Text))
                        errors.Add("Character name cannot be empty");
                    if (element.Text.Length > 40)
                        warnings.Add("Character name is unusually long");
                    break;

                case ScriptElementType.Dialogue:
                    if (string.IsNullOrWhiteSpace(element.Text))
                        errors.Add("Dialogue cannot be empty");
                    if (element.Text.Length > 500)
                        warnings.Add("Dialogue is very long; consider breaking into multiple lines");
                    break;

                case ScriptElementType.Transition:
                    if (!element.Text.ToUpper().EndsWith(":"))
                        warnings.Add("Transition should end with a colon");
                    break;
            }

            return new ValidationResult(errors.Count == 0, errors, warnings);
        }

        public string ApplySmartCorrections(string text, ScriptElementType type)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            switch (type)
            {
                case ScriptElementType.SceneHeading:
                    // Auto-fix common scene heading issues
                    text = text.ToUpper();
                    if (!text.Contains("INT.") && !text.Contains("EXT."))
                        text = "INT. " + text;
                    // Auto-add time if missing
                    if (!Regex.IsMatch(text, @"(DAY|NIGHT|MORNING|EVENING)", RegexOptions.IgnoreCase))
                        text += " - DAY";
                    break;

                case ScriptElementType.Character:
                    // Auto-uppercase
                    text = text.ToUpper().Trim();
                    // Remove duplicate modifiers
                    foreach (var mod in AllowedCharacterModifiers)
                    {
                        if (text.EndsWith(mod))
                            text = text.Substring(0, text.Length - mod.Length).Trim();
                    }
                    break;

                case ScriptElementType.Parenthetical:
                    // Auto-close parentheses
                    if (text.StartsWith("(") && !text.EndsWith(")"))
                        text += ")";
                    else if (!text.StartsWith("("))
                        text = "(" + text + ")";
                    break;

                case ScriptElementType.Transition:
                    // Auto-uppercase and add colon
                    text = text.ToUpper().Trim();
                    if (!text.EndsWith(":"))
                        text += ":";
                    break;

                case ScriptElementType.Dialogue:
                    // Trim and fix common issues
                    text = text.Trim();
                    break;
            }

            return text;
        }
    }
}
