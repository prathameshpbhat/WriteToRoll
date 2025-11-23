using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace App.Core.Services
{
    /// <summary>
    /// Fade-In Feature: Watermarking and Security
    /// 
    /// Features:
    /// - Secure watermark generation for script distribution
    /// - Batch watermarking capability (watermark multiple scripts at once)
    /// - Prevent unauthorized copying/distribution
    /// - Watermark entire projects
    /// - Generate unique tracking watermarks
    /// </summary>
    public class ScriptWatermarkManager
    {
        public class WatermarkInfo
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string RecipientName { get; set; } = string.Empty;
            public string RecipientEmail { get; set; } = string.Empty;
            public string ScriptTitle { get; set; } = string.Empty;
            public DateTime IssuedDate { get; set; } = DateTime.UtcNow;
            public DateTime? ExpirationDate { get; set; }
            public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value < DateTime.UtcNow;
            public string WatermarkText { get; set; } = string.Empty;
            public string WatermarkHash { get; set; } = string.Empty;
            public List<string> AllowedIPs { get; set; } = new();
            public List<string> AllowedDevices { get; set; } = new();
            public bool IsPrintable { get; set; } = false;
            public bool IsDownloadable { get; set; } = false;
            public bool IsShareable { get; set; } = false;
            public int MaxPrintCopies { get; set; } = 0;
            public int PrintCopiesMade { get; set; } = 0;
            public bool TrackingEnabled { get; set; } = true;
            public Dictionary<string, DateTime> AccessLog { get; set; } = new();
        }

        public class WatermarkBatch
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public string ScriptTitle { get; set; } = string.Empty;
            public string ScriptPath { get; set; } = string.Empty;
            public List<WatermarkInfo> Watermarks { get; set; } = new();
            public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
            public int TotalRecipients { get; set; }
            public int SuccessfulWatermarks { get; set; }
            public int FailedWatermarks { get; set; }
            public string Status { get; set; } = "Pending";  // Pending, Processing, Completed, Failed
        }

        private Dictionary<string, WatermarkInfo> _watermarks = new();
        private Dictionary<string, WatermarkBatch> _batches = new();
        private List<string> _watermarkHistory = new();

        /// <summary>
        /// Generate a unique watermark for a recipient
        /// </summary>
        public WatermarkInfo GenerateWatermark(string recipientName, string recipientEmail, string scriptTitle, 
                                             DateTime? expirationDate = null, bool printable = false, bool downloadable = true)
        {
            var watermark = new WatermarkInfo
            {
                RecipientName = recipientName,
                RecipientEmail = recipientEmail,
                ScriptTitle = scriptTitle,
                ExpirationDate = expirationDate,
                IsPrintable = printable,
                IsDownloadable = downloadable,
                WatermarkText = GenerateWatermarkText(recipientName, recipientEmail, scriptTitle)
            };

            watermark.WatermarkHash = HashWatermark(watermark.WatermarkText);
            _watermarks[watermark.Id] = watermark;
            _watermarkHistory.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Watermark generated for {recipientName} ({watermark.Id})");

            return watermark;
        }

        /// <summary>
        /// Generate watermark text with recipient and timestamp info
        /// </summary>
        private string GenerateWatermarkText(string recipientName, string recipientEmail, string scriptTitle)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var uniqueId = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            return $"CONFIDENTIAL - {recipientName.ToUpper()} - {recipientEmail} - {scriptTitle} - {timestamp} - {uniqueId}";
        }

        /// <summary>
        /// Hash watermark for verification
        /// </summary>
        private string HashWatermark(string watermarkText)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(watermarkText));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        /// <summary>
        /// Verify watermark integrity
        /// </summary>
        public bool VerifyWatermark(string watermarkId, string watermarkText)
        {
            if (!_watermarks.ContainsKey(watermarkId))
                return false;

            var watermark = _watermarks[watermarkId];
            var computedHash = HashWatermark(watermarkText);
            return watermark.WatermarkHash == computedHash && !watermark.IsExpired;
        }

        /// <summary>
        /// Create batch watermarks for multiple recipients
        /// </summary>
        public WatermarkBatch CreateWatermarkBatch(string scriptTitle, string scriptPath, 
                                                  List<(string Name, string Email)> recipients,
                                                  DateTime? expirationDate = null,
                                                  bool printable = false)
        {
            var batch = new WatermarkBatch
            {
                ScriptTitle = scriptTitle,
                ScriptPath = scriptPath,
                TotalRecipients = recipients.Count,
                Status = "Processing"
            };

            foreach (var (name, email) in recipients)
            {
                try
                {
                    var watermark = GenerateWatermark(name, email, scriptTitle, expirationDate, printable);
                    batch.Watermarks.Add(watermark);
                    batch.SuccessfulWatermarks++;
                }
                catch (Exception ex)
                {
                    batch.FailedWatermarks++;
                    _watermarkHistory.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] FAILED: Watermark for {name}: {ex.Message}");
                }
            }

            batch.Status = batch.FailedWatermarks == 0 ? "Completed" : "CompletedWithErrors";
            _batches[batch.Id] = batch;
            _watermarkHistory.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Batch created: {batch.Id} ({batch.SuccessfulWatermarks}/{batch.TotalRecipients} successful)");

            return batch;
        }

        /// <summary>
        /// Log access to watermarked script
        /// </summary>
        public void LogAccess(string watermarkId, string accessPoint = "Unknown")
        {
            if (_watermarks.ContainsKey(watermarkId))
            {
                var watermark = _watermarks[watermarkId];
                if (watermark.TrackingEnabled)
                {
                    watermark.AccessLog[DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")] = DateTime.UtcNow;
                    _watermarkHistory.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Access logged for {watermarkId} from {accessPoint}");
                }
            }
        }

        /// <summary>
        /// Log print action
        /// </summary>
        public bool LogPrint(string watermarkId)
        {
            if (!_watermarks.ContainsKey(watermarkId))
                return false;

            var watermark = _watermarks[watermarkId];
            
            // Check if printing is allowed
            if (!watermark.IsPrintable)
            {
                _watermarkHistory.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] DENIED: Print attempted on non-printable watermark {watermarkId}");
                return false;
            }

            // Check print limit
            if (watermark.MaxPrintCopies > 0 && watermark.PrintCopiesMade >= watermark.MaxPrintCopies)
            {
                _watermarkHistory.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] DENIED: Print limit exceeded for {watermarkId}");
                return false;
            }

            watermark.PrintCopiesMade++;
            LogAccess(watermarkId, $"Print (Copy {watermark.PrintCopiesMade})");
            return true;
        }

        /// <summary>
        /// Get watermark details
        /// </summary>
        public WatermarkInfo GetWatermark(string watermarkId)
        {
            return _watermarks.ContainsKey(watermarkId) ? _watermarks[watermarkId] : null;
        }

        /// <summary>
        /// Get all watermarks for a recipient
        /// </summary>
        public List<WatermarkInfo> GetWatermarksForRecipient(string recipientEmail)
        {
            return _watermarks.Values
                .Where(w => w.RecipientEmail.Equals(recipientEmail, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Get all active (non-expired) watermarks
        /// </summary>
        public List<WatermarkInfo> GetActiveWatermarks()
        {
            return _watermarks.Values
                .Where(w => !w.IsExpired)
                .ToList();
        }

        /// <summary>
        /// Get batch details
        /// </summary>
        public WatermarkBatch GetBatch(string batchId)
        {
            return _batches.ContainsKey(batchId) ? _batches[batchId] : null;
        }

        /// <summary>
        /// Revoke a watermark
        /// </summary>
        public bool RevokeWatermark(string watermarkId, string reason = "")
        {
            if (!_watermarks.ContainsKey(watermarkId))
                return false;

            _watermarks.Remove(watermarkId);
            _watermarkHistory.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Watermark revoked: {watermarkId}. Reason: {reason}");
            return true;
        }

        /// <summary>
        /// Extend watermark expiration
        /// </summary>
        public bool ExtendWatermarkExpiration(string watermarkId, DateTime newExpirationDate)
        {
            if (!_watermarks.ContainsKey(watermarkId))
                return false;

            _watermarks[watermarkId].ExpirationDate = newExpirationDate;
            _watermarkHistory.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Watermark {watermarkId} expiration extended to {newExpirationDate:yyyy-MM-dd}");
            return true;
        }

        /// <summary>
        /// Get access report for a watermark
        /// </summary>
        public string GenerateAccessReport(string watermarkId)
        {
            if (!_watermarks.ContainsKey(watermarkId))
                return "Watermark not found.";

            var watermark = _watermarks[watermarkId];
            var report = new StringBuilder();
            report.AppendLine($"=== Watermark Access Report ===");
            report.AppendLine($"ID: {watermark.Id}");
            report.AppendLine($"Recipient: {watermark.RecipientName} ({watermark.RecipientEmail})");
            report.AppendLine($"Script: {watermark.ScriptTitle}");
            report.AppendLine($"Issued: {watermark.IssuedDate:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Expires: {(watermark.ExpirationDate.HasValue ? watermark.ExpirationDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Never")}");
            report.AppendLine($"Status: {(watermark.IsExpired ? "EXPIRED" : "ACTIVE")}");
            report.AppendLine($"Printable: {watermark.IsPrintable}");
            report.AppendLine($"Prints Made: {watermark.PrintCopiesMade}" + (watermark.MaxPrintCopies > 0 ? $" / {watermark.MaxPrintCopies}" : ""));
            report.AppendLine($"Total Accesses: {watermark.AccessLog.Count}");
            report.AppendLine();
            report.AppendLine("=== Access Log ===");
            foreach (var access in watermark.AccessLog.OrderBy(x => x.Value))
            {
                report.AppendLine($"{access.Key}");
            }

            return report.ToString();
        }

        /// <summary>
        /// Get complete watermark history
        /// </summary>
        public List<string> GetWatermarkHistory()
        {
            return _watermarkHistory.OrderBy(x => x).ToList();
        }

        /// <summary>
        /// Generate security audit report
        /// </summary>
        public string GenerateSecurityAudit()
        {
            var report = new StringBuilder();
            report.AppendLine("=== Watermark Security Audit Report ===");
            report.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();
            report.AppendLine($"Total Watermarks: {_watermarks.Count}");
            report.AppendLine($"Active Watermarks: {GetActiveWatermarks().Count}");
            report.AppendLine($"Expired Watermarks: {_watermarks.Count - GetActiveWatermarks().Count}");
            report.AppendLine($"Total Batches: {_batches.Count}");
            report.AppendLine();
            report.AppendLine("=== Watermark Summary ===");
            foreach (var watermark in _watermarks.Values.OrderBy(w => w.IssuedDate))
            {
                var status = watermark.IsExpired ? "EXPIRED" : "ACTIVE";
                report.AppendLine($"  {watermark.RecipientEmail} - {watermark.ScriptTitle} - {status} - {watermark.AccessLog.Count} accesses");
            }

            return report.ToString();
        }
    }
}
