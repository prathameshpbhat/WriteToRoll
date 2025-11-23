using System;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Generates professional screenplay title pages per industry standards
    /// Title page is typically page 1 (unnumbered)
    /// Body pages start at page 2
    /// </summary>
    public interface ITitlePageGenerator
    {
        string GenerateTitlePage(string title, string author, string email, string phone = "");
        string GenerateTitlePageWithContact(string title, string author, string email, string phone, string website = "");
        bool ValidateTitlePageInput(string title, string author);
    }

    public class TitlePageGenerator : ITitlePageGenerator
    {
        private const int TitlePageHeight = 55; // Lines on title page (same as body page)
        private const int CenterLine = 28; // Approximate center line for page

        /// <summary>
        /// Generates a professional screenplay title page
        /// Format: Centered title, "Written by", Author name, Contact info
        /// </summary>
        public string GenerateTitlePage(string title, string author, string email, string phone = "")
        {
            if (!ValidateTitlePageInput(title, author))
                return GenerateBlankTitlePage();

            title = title.Trim();
            author = author.Trim();
            email = email.Trim();
            phone = phone.Trim();

            // Title page structure (professional format):
            // 1. Blank lines (top padding)
            // 2. Title (centered, bold/underlined in print, but plain text here)
            // 3. Blank line
            // 4. "Written by"
            // 5. Author name
            // 6. Blank lines (bottom padding)
            // 7. Contact info (bottom right)

            var lines = new System.Collections.Generic.List<string>();

            // Top padding (~10 blank lines)
            for (int i = 0; i < 10; i++)
                lines.Add("");

            // Title - centered (approximated with spaces for Courier 12pt)
            string centeredTitle = CenterText(title.ToUpper(), 60);
            lines.Add(centeredTitle);
            lines.Add("");

            // "Written by" - centered
            string writtenBy = CenterText("Written by", 60);
            lines.Add(writtenBy);
            lines.Add("");

            // Author name - centered
            string centeredAuthor = CenterText(author, 60);
            lines.Add(centeredAuthor);

            // Bottom padding (blank lines to push contact info to bottom)
            int currentLines = lines.Count;
            int paddingNeeded = TitlePageHeight - currentLines - 4; // Leave 4 lines for contact
            for (int i = 0; i < paddingNeeded; i++)
                lines.Add("");

            // Contact info - bottom right (simple right-align with tabs)
            if (!string.IsNullOrEmpty(email))
            {
                lines.Add("");
                string emailLine = RightAlignText(email, 60);
                lines.Add(emailLine);
            }

            if (!string.IsNullOrEmpty(phone))
            {
                string phoneLine = RightAlignText(phone, 60);
                lines.Add(phoneLine);
            }

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Generates title page with additional contact fields
        /// </summary>
        public string GenerateTitlePageWithContact(string title, string author, string email, string phone, string website = "")
        {
            if (!ValidateTitlePageInput(title, author))
                return GenerateBlankTitlePage();

            title = title.Trim();
            author = author.Trim();
            email = email.Trim();
            phone = phone.Trim();
            website = website.Trim();

            var lines = new System.Collections.Generic.List<string>();

            // Top padding
            for (int i = 0; i < 10; i++)
                lines.Add("");

            // Title
            string centeredTitle = CenterText(title.ToUpper(), 60);
            lines.Add(centeredTitle);
            lines.Add("");

            // "Written by"
            string writtenBy = CenterText("Written by", 60);
            lines.Add(writtenBy);
            lines.Add("");

            // Author
            string centeredAuthor = CenterText(author, 60);
            lines.Add(centeredAuthor);

            // Bottom padding
            int currentLines = lines.Count;
            int paddingNeeded = TitlePageHeight - currentLines - 5; // Leave space for contact
            for (int i = 0; i < paddingNeeded; i++)
                lines.Add("");

            // Contact info - bottom right
            lines.Add("");
            if (!string.IsNullOrEmpty(email))
                lines.Add(RightAlignText(email, 60));
            if (!string.IsNullOrEmpty(phone))
                lines.Add(RightAlignText(phone, 60));
            if (!string.IsNullOrEmpty(website))
                lines.Add(RightAlignText(website, 60));

            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Validates title page input
        /// </summary>
        public bool ValidateTitlePageInput(string title, string author)
        {
            return !string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(author);
        }

        /// <summary>
        /// Generates blank title page
        /// </summary>
        private string GenerateBlankTitlePage()
        {
            var lines = new System.Collections.Generic.List<string>();
            for (int i = 0; i < TitlePageHeight; i++)
                lines.Add("");
            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>
        /// Centers text for Courier 12pt (approx 60 chars per line)
        /// </summary>
        private string CenterText(string text, int lineWidth)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            text = text.Trim();
            int padding = (lineWidth - text.Length) / 2;
            if (padding < 0) padding = 0;

            return new string(' ', padding) + text;
        }

        /// <summary>
        /// Right-aligns text (for contact info at bottom right)
        /// </summary>
        private string RightAlignText(string text, int lineWidth)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            text = text.Trim();
            int padding = lineWidth - text.Length;
            if (padding < 0) padding = 0;

            return new string(' ', padding) + text;
        }
    }
}
