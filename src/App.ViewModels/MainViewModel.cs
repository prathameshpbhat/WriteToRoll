using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using App.Core.Models;
using App.Core.Services;

namespace App.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IScriptService _scriptService;
        
        [ObservableProperty]
        private Script _currentScript;

        [ObservableProperty]
        private Scene? _selectedScene;

        public MainViewModel(IScriptService scriptService)
        {
            _scriptService = scriptService;
            CurrentScript = new Script();
        }

        [RelayCommand]
        private async Task NewScript()
        {
            if (await CheckUnsavedChanges())
            {
                CurrentScript = new Script();
            }
        }

        [RelayCommand]
        private async Task OpenScript()
        {
            if (await CheckUnsavedChanges())
            {
                var result = await _scriptService.OpenFileDialogAsync();
                if (result.Success)
                {
                    CurrentScript = result.Script;
                }
            }
        }

        [RelayCommand]
        private async Task SaveScript()
        {
            if (string.IsNullOrEmpty(CurrentScript.FilePath))
            {
                await SaveScriptAs();
                return;
            }

            await _scriptService.SaveAsync(CurrentScript);
        }

        [RelayCommand]
        private async Task SaveScriptAs()
        {
            var result = await _scriptService.SaveFileDialogAsync(CurrentScript);
            if (result.Success)
            {
                CurrentScript = result.Script;
            }
        }

        private async Task<bool> CheckUnsavedChanges()
        {
            if (!CurrentScript.IsDirty)
                return true;

            var result = await _scriptService.ShowSaveChangesDialogAsync();
            if (result == SaveChangesResult.Cancel)
                return false;

            if (result == SaveChangesResult.Save)
            {
                await SaveScript();
            }

            return true;
        }
    }
}