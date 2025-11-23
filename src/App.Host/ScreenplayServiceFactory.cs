using System;
using App.Core.Models;
using App.Core.Services;

namespace ScriptWriter
{
    /// <summary>
    /// Helper class to initialize and manage all screenplay services
    /// Tier 1 & 2 enhanced features for professional screenplay development
    /// </summary>
    public class ScreenplayServiceFactory
    {
        private PageFormatting _pageFormat;

        public ScreenplayServiceFactory(PageSize pageSize = PageSize.Letter)
        {
            _pageFormat = pageSize == PageSize.A4 
                ? PageFormatting.StandardA4() 
                : PageFormatting.StandardLetter();
        }

        /// <summary>
        /// Creates all professional screenplay services
        /// </summary>
        public ScreenplayServices CreateServices()
        {
            return new ScreenplayServices
            {
                PageFormat = _pageFormat,
                PaginationEngine = new PaginationEngine(_pageFormat),
                FormattingRules = new ScreenplayFormattingRules(_pageFormat),
                TitlePageGenerator = new TitlePageGenerator(),
                PageBreakVisualizer = new PageBreakVisualizer(55),
                ScreenplayTracker = new ScreenplayTracker(_pageFormat)
            };
        }

        /// <summary>
        /// Gets page format configuration
        /// </summary>
        public PageFormatting GetPageFormat()
        {
            return _pageFormat;
        }

        /// <summary>
        /// Switches between A4 and Letter format
        /// </summary>
        public void SwitchPageFormat(PageSize newSize)
        {
            _pageFormat = newSize == PageSize.A4 
                ? PageFormatting.StandardA4() 
                : PageFormatting.StandardLetter();
        }
    }

    /// <summary>
    /// Container for all screenplay services
    /// </summary>
    public class ScreenplayServices
    {
        public required PageFormatting PageFormat { get; set; }
        public required IPaginationEngine PaginationEngine { get; set; }
        public required IScreenplayFormattingRules FormattingRules { get; set; }
        public required ITitlePageGenerator TitlePageGenerator { get; set; }
        public required IPageBreakVisualizer PageBreakVisualizer { get; set; }
        public required IScreenplayTracker ScreenplayTracker { get; set; }

        /// <summary>
        /// Example: Generate a complete screenplay with title page
        /// </summary>
        public string GenerateCompleteScreenplay(
            string title, 
            string author, 
            string email,
            string scriptBody,
            string phone = "")
        {
            var titlePage = TitlePageGenerator.GenerateTitlePage(title, author, email, phone);
            var pageBreaks = PageBreakVisualizer.InsertPageBreakMarkers(scriptBody, "--- PAGE {0} ---");
            
            return titlePage + Environment.NewLine + Environment.NewLine + pageBreaks;
        }

        /// <summary>
        /// Analyze screenplay for statistics
        /// </summary>
        public ScreenplayStatistics AnalyzeScreenplay(string scriptText)
        {
            ScreenplayTracker.ScanScript(scriptText);

            var stats = new ScreenplayStatistics
            {
                TotalPages = PaginationEngine.GetTotalPageCount(scriptText),
                EstimatedMinutes = PaginationEngine.GetEstimatedScreenMinutes(scriptText),
                Characters = ScreenplayTracker.GetAllCharacters(),
                Locations = ScreenplayTracker.GetAllLocations(),
                CharacterScreenTime = ScreenplayTracker.GetCharacterScreenTime(),
                PageRanges = PageBreakVisualizer.GetPageRanges(scriptText)
            };

            return stats;
        }
    }

    /// <summary>
    /// Screenplay analysis results
    /// </summary>
    public class ScreenplayStatistics
    {
        public int TotalPages { get; set; }
        public double EstimatedMinutes { get; set; }
        public System.Collections.Generic.List<string>? Characters { get; set; }
        public System.Collections.Generic.List<string>? Locations { get; set; }
        public System.Collections.Generic.Dictionary<string, int>? CharacterScreenTime { get; set; }
        public System.Collections.Generic.List<(int startLine, int endLine, int pageNumber)>? PageRanges { get; set; }

        public override string ToString()
        {
            var charCount = Characters?.Count ?? 0;
            var locCount = Locations?.Count ?? 0;
            return $"Screenplay: {TotalPages} pages (~{EstimatedMinutes:F0} min), " +
                   $"{charCount} characters, {locCount} locations";
        }
    }
}
