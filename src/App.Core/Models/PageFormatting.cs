using System;

namespace App.Core.Models
{
    /// <summary>
    /// Defines page formatting standards for screenplay output
    /// </summary>
    public enum PageSize
    {
        Letter,  // 8.5" x 11"
        A4       // 8.27" x 11.69"
    }

    public class PageFormatting
    {
        public PageSize Size { get; set; }
        public double MarginLeft { get; set; }    // in inches
        public double MarginRight { get; set; }   // in inches
        public double MarginTop { get; set; }     // in inches
        public double MarginBottom { get; set; }  // in inches
        public int LinesPerPage { get; set; }
        public string FontFamily { get; set; } = "Courier New";
        public int FontSizePoints { get; set; }

        /// <summary>
        /// Standard Letter page formatting (1.5" left, 1" others)
        /// </summary>
        public static PageFormatting StandardLetter()
        {
            return new PageFormatting
            {
                Size = PageSize.Letter,
                MarginLeft = 1.5,
                MarginRight = 1.0,
                MarginTop = 1.0,
                MarginBottom = 1.0,
                LinesPerPage = 55,
                FontFamily = "Courier New",
                FontSizePoints = 12
            };
        }

        /// <summary>
        /// A4 page formatting (adjusted for 8.27" width)
        /// </summary>
        public static PageFormatting StandardA4()
        {
            return new PageFormatting
            {
                Size = PageSize.A4,
                MarginLeft = 1.5,
                MarginRight = 1.0,
                MarginTop = 1.0,
                MarginBottom = 1.0,
                LinesPerPage = 55,
                FontFamily = "Courier New",
                FontSizePoints = 12
            };
        }

        public double GetContentWidth()
        {
            double pageWidth = Size == PageSize.A4 ? 8.27 : 8.5;
            return pageWidth - MarginLeft - MarginRight;
        }

        public override string ToString()
        {
            return $"{Size}: {MarginLeft}\" left, {MarginRight}\" right, {MarginTop}\" top, {MarginBottom}\" bottom";
        }
    }
}
