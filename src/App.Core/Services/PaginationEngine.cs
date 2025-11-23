using System;
using System.Collections.Generic;
using System.Linq;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Tracks pagination state and calculates page breaks for screenplay formatting
    /// Per professional standard: ~55 lines per page in Courier 12pt with proper margins
    /// </summary>
    public interface IPaginationEngine
    {
        int GetCurrentPageNumber(int caretPosition);
        int GetTotalPageCount(string scriptText);
        int GetLinesOnCurrentPage(string scriptText, int caretPosition);
        List<int> GetPageBreakPositions(string scriptText);
        double GetEstimatedScreenMinutes(string scriptText);
    }

    public class PaginationEngine : IPaginationEngine
    {
        private readonly PageFormatting _pageFormat;

        public PaginationEngine(PageFormatting pageFormat)
        {
            _pageFormat = pageFormat ?? PageFormatting.StandardLetter();
        }

        /// <summary>
        /// Determines current page number based on caret position
        /// </summary>
        public int GetCurrentPageNumber(int caretPosition)
        {
            if (caretPosition < 0) return 1;
            return 1; // Simplified: returns 1 for now, can be enhanced with full text
        }

        /// <summary>
        /// Calculates total page count from script text
        /// Screenplay standard: 1 page = ~55 lines = ~1 minute screen time
        /// </summary>
        public int GetTotalPageCount(string scriptText)
        {
            if (string.IsNullOrEmpty(scriptText)) return 1;

            int lineCount = scriptText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Length;
            int pageCount = (lineCount + _pageFormat.LinesPerPage - 1) / _pageFormat.LinesPerPage;
            return Math.Max(1, pageCount);
        }

        /// <summary>
        /// Gets count of lines on the current page where caret is located
        /// </summary>
        public int GetLinesOnCurrentPage(string scriptText, int caretPosition)
        {
            if (string.IsNullOrEmpty(scriptText) || caretPosition < 0) return 0;

            // Count newlines up to caret position
            int linesBeforeCaret = scriptText.Substring(0, Math.Min(caretPosition, scriptText.Length))
                .Count(c => c == '\n') + 1;

            // Position within current page
            int lineInPage = linesBeforeCaret % _pageFormat.LinesPerPage;
            return lineInPage == 0 ? _pageFormat.LinesPerPage : lineInPage;
        }

        /// <summary>
        /// Identifies all page break positions in the script
        /// </summary>
        public List<int> GetPageBreakPositions(string scriptText)
        {
            var positions = new List<int>();
            if (string.IsNullOrEmpty(scriptText)) return positions;

            var lines = scriptText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            int currentPosition = 0;
            int lineCount = 0;

            foreach (var line in lines)
            {
                if (lineCount > 0 && lineCount % _pageFormat.LinesPerPage == 0)
                {
                    positions.Add(currentPosition);
                }

                currentPosition += line.Length + Environment.NewLine.Length;
                lineCount++;
            }

            return positions;
        }

        /// <summary>
        /// Estimates screen time in minutes
        /// Professional standard: 1 page ≈ 1 minute of screen time (Courier 12pt, proper spacing)
        /// Screenplay length typically 70-120 pages (average ~110 = ~110 minutes / 1hr 50 min)
        /// </summary>
        public double GetEstimatedScreenMinutes(string scriptText)
        {
            int pageCount = GetTotalPageCount(scriptText);
            return pageCount * 1.0; // 1 page = 1 minute
        }
    }
}
