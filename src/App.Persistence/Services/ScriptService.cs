using App.Core.Models;
using App.Core.Services;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace App.Persistence.Services
{
    public class ScriptService : IScriptService
    {
        private readonly FountainParser _fountainParser;

        public ScriptService(FountainParser fountainParser)
        {
            _fountainParser = fountainParser;
        }

        public async Task<OpenScriptResult> OpenFileDialogAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Fountain Files (*.fountain)|*.fountain|All Files (*.*)|*.*",
                FilterIndex = 1
            };

            if (dialog.ShowDialog() == true)
            {
                var text = await File.ReadAllTextAsync(dialog.FileName);
                var script = _fountainParser.ParseFountain(text);
                script.FilePath = dialog.FileName;
                return new OpenScriptResult { Success = true, Script = script };
            }

            return new OpenScriptResult { Success = false };
        }

        public async Task SaveAsync(Script script)
        {
            if (string.IsNullOrEmpty(script.FilePath))
                throw new InvalidOperationException("FilePath not set");

            var text = _fountainParser.ConvertToFountain(script);
            await File.WriteAllTextAsync(script.FilePath, text);
            script.IsDirty = false;
        }

        public async Task<SaveScriptResult> SaveFileDialogAsync(Script script)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Fountain Files (*.fountain)|*.fountain|All Files (*.*)|*.*",
                FilterIndex = 1,
                DefaultExt = ".fountain"
            };

            if (dialog.ShowDialog() == true)
            {
                script.FilePath = dialog.FileName;
                await SaveAsync(script);
                return new SaveScriptResult { Success = true, Script = script };
            }

            return new SaveScriptResult { Success = false };
        }

        public Task<SaveChangesResult> ShowSaveChangesDialogAsync()
        {
            var result = MessageBox.Show(
                "Do you want to save changes?",
                "Save Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            var saveResult = result switch
            {
                MessageBoxResult.Yes => SaveChangesResult.Save,
                MessageBoxResult.No => SaveChangesResult.DontSave,
                _ => SaveChangesResult.Cancel
            };

            return Task.FromResult(saveResult);
        }
    }
}