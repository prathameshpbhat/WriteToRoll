using System;
using System.Collections.Generic;
using System.Linq;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Provides visual page break indicators for editor display
    /// Shows where pages break in the screenplay for visual reference
    /// </summary>
    public interface IPageBreakVisualizer
    {
        List<int> GetPageBreakPositions(string scriptText);
        List<(int startLine, int endLine, int pageNumber)> GetPageRanges(string scriptText);
        string InsertPageBreakMarkers(string scriptText, string markerFormat = "PAGE {0}");
        bool IsLineAtPageBreak(string scriptText, int lineNumber);
    }

    public class PageBreakVisualizer : IPageBreakVisualizer
    {
        private readonly int _linesPerPage;

        public PageBreakVisualizer(int linesPerPage = 55)
        {
            _linesPerPage = linesPerPage;
        }

        /// <summary>
        /// Gets character positions where page breaks occur
        /// </summary>
        public List<int> GetPageBreakPositions(string scriptText)
        {
            var positions = new List<int>();
            if (string.IsNullOrEmpty(scriptText)) return positions;

            var lines = scriptText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int charPos = 0;
            int lineCount = 0;

            foreach (var line in lines)
            {
                if (lineCount > 0 && lineCount % _linesPerPage == 0)
                {
                    positions.Add(charPos);
                }

                charPos += line.Length + Environment.NewLine.Length;
                lineCount++;
            }

            return positions;
        }

        /// <summary>
        /// Gets page ranges (start line, end line, page number)
        /// Useful for highlighting page boundaries in editor
        /// </summary>
        public List<(int startLine, int endLine, int pageNumber)> GetPageRanges(string scriptText)
        {
            var ranges = new List<(int, int, int)>();
            if (string.IsNullOrEmpty(scriptText)) return ranges;

            var lines = scriptText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int pageNumber = 1;
            int pageStartLine = 0;
            int currentLine = 0;

            foreach (var line in lines)
            {
                if (currentLine > 0 && currentLine % _linesPerPage == 0)
                {
                    // End previous page
                    ranges.Add((pageStartLine, currentLine - 1, pageNumber));
                    
                    // Start new page
                    pageNumber++;
                    pageStartLine = currentLine;
                }

                currentLine++;
            }

            // Add final page
            if (pageStartLine < currentLine)
            {
                ranges.Add((pageStartLine, currentLine - 1, pageNumber));
            }

            return ranges;
        }

        /// <summary>
        /// Inserts visual page break markers (soft breaks, not saved to file)
        /// Marker format: "PAGE {0}" where {0} is the page number
        /// </summary>
        public string InsertPageBreakMarkers(string scriptText, string markerFormat = "PAGE {0}")
        {
            if (string.IsNullOrEmpty(scriptText)) return scriptText;

            var lines = scriptText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
            int pageNumber = 1;

            for (int i = _linesPerPage; i < lines.Count; i += _linesPerPage + 1)
            {
                pageNumber++;
                string marker = string.Format(markerFormat, pageNumber);
                string markerLine = "--- " + marker + " ---";
                lines.Insert(i, markerLine);
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Checks if a given line is at a page break boundary
        /// </summary>
        public bool IsLineAtPageBreak(string scriptText, int lineNumber)
        {
            if (string.IsNullOrEmpty(scriptText)) return false;
            return lineNumber > 0 && lineNumber % _linesPerPage == 0;
        }
    }
}
