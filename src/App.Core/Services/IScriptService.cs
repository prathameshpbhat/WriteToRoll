using System.Threading.Tasks;
using App.Core.Models;

namespace App.Core.Services
{
    public interface IScriptService
    {
        Task<OpenScriptResult> OpenFileDialogAsync();
        Task<SaveScriptResult> SaveFileDialogAsync(Script script);
        Task SaveAsync(Script script);
        Task<SaveChangesResult> ShowSaveChangesDialogAsync();
    }

    public enum SaveChangesResult
    {
        Save,
        DontSave,
        Cancel
    }

    public class OpenScriptResult
    {
        public bool Success { get; set; }
        public Script? Script { get; set; }
    }

    public class SaveScriptResult 
    {
        public bool Success { get; set; }
        public Script? Script { get; set; }
    }
}