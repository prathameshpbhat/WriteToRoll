using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using App.Core.Models;

namespace App.Core.Services
{
    /// <summary>
    /// Service for file I/O and script management
    /// </summary>
    public interface IScriptService
    {
        Task<OpenScriptResult> OpenFileDialogAsync();
        Task<SaveScriptResult> SaveFileDialogAsync(Script script);
        Task SaveAsync(Script script);
        Task<SaveChangesResult> ShowSaveChangesDialogAsync();
        Task<ParseValidationResult> ValidateAsync(string content);
        Task<ScriptDiff> DiffAsync(Script before, Script after);
    }

    public enum SaveChangesResult
    {
        Save,
        DontSave,
        Cancel
    }

    public class ScriptDiff
    {
        public List<ScriptElement> AddedElements { get; set; } = new();
        public List<ScriptElement> RemovedElements { get; set; } = new();
        public List<ElementModification> ModifiedElements { get; set; } = new();
    }

    public class ElementModification
    {
        public string ElementId { get; set; } = string.Empty;
        public ScriptElement? Before { get; set; }
        public ScriptElement? After { get; set; }
        public List<string> ChangedProperties { get; set; } = new();
    }

    public class ParseValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class OpenScriptResult
    {
        public bool Success { get; set; }
        public string? Path { get; set; }
        public ParseValidationResult? Validation { get; set; }
        public Script? Script { get; set; }
    }

    public class SaveScriptResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Path { get; set; }
        public Script? Script { get; set; }
    }
}
