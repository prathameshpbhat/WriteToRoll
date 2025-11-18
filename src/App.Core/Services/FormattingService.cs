using System;
using System.Collections.Generic;
using App.Core.Models;

namespace App.Core.Services
{
    public class FormattingService
    {
        private const int CharsPerInch = 10;
        private const int PageWidth = 85;

        public string GetIndentation(ScriptElementType elementType)
        {
            var formatting = FormattingMeta.GetDefaults(elementType);
            return GetIndentationFromMargin(formatting.LeftMargin);
        }

        public string GetIndentationFromMargin(string margin)
        {
            if (string.IsNullOrEmpty(margin))
                return string.Empty;

            var cleanMargin = margin.Replace("\"", "").Trim();
            if (double.TryParse(cleanMargin, out var inches))
            {
                int spaces = (int)(inches * CharsPerInch);
                return new string(' ', Math.Max(0, spaces));
            }

            return string.Empty;
        }

        public string FormatForDisplay(ScriptElement element)
        {
            if (element == null) return string.Empty;

            string indent = GetIndentation(element.ElementType);
            string content = element.GetFormattedOutput();

            return element.ElementType switch
            {
                ScriptElementType.Character => CenterText(content, 60),
                ScriptElementType.Transition => RightAlignText(content, 60),
                ScriptElementType.CenteredText => CenterText(content, 60),
                _ => indent + content
            };
        }

        public List<string> WrapText(string text, int lineWidth = 60)
        {
            var lines = new List<string>();
            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var currentLine = "";

            foreach (var word in words)
            {
                if ((currentLine + " " + word).Length > lineWidth)
                {
                    if (!string.IsNullOrEmpty(currentLine))
                        lines.Add(currentLine);
                    currentLine = word;
                }
                else
                {
                    currentLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
                lines.Add(currentLine);

            return lines;
        }

        private string CenterText(string text, int width)
        {
            int spaces = Math.Max(0, (width - text.Length) / 2);
            return new string(' ', spaces) + text;
        }

        private string RightAlignText(string text, int width)
        {
            int spaces = Math.Max(0, width - text.Length);
            return new string(' ', spaces) + text;
        }

        public int EstimateLineHeight(string text, int width = 60)
        {
            if (string.IsNullOrEmpty(text))
                return 1;

            var lines = WrapText(text, width);
            return Math.Max(1, lines.Count);
        }
    }
}
