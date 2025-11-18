using System;
using System.Threading.Tasks;
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
        private Script currentScript;

        [ObservableProperty]
        private string windowTitle = "ScriptWriter - Untitled";

        [ObservableProperty]
        private bool isLoading;

        public MainViewModel(IScriptService scriptService)
        {
            _scriptService = scriptService ?? throw new ArgumentNullException(nameof(scriptService));
            CurrentScript = new Script { Title = "Untitled" };
            UpdateWindowTitle();
        }

        [RelayCommand]
        private async Task NewScript()
        {
            if (await CheckUnsavedChanges())
            {
                CurrentScript = new Script { Title = "Untitled" };
                UpdateWindowTitle();
            }
        }

        [RelayCommand]
        private async Task OpenScript()
        {
            if (await CheckUnsavedChanges())
            {
                IsLoading = true;
                try
                {
                    var result = await _scriptService.OpenFileDialogAsync();
                    if (result.Success && result.Script != null)
                    {
                        CurrentScript = result.Script;
                        UpdateWindowTitle();
                    }
                }
                finally
                {
                    IsLoading = false;
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

            IsLoading = true;
            try
            {
                await _scriptService.SaveAsync(CurrentScript);
                UpdateWindowTitle();
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SaveScriptAs()
        {
            IsLoading = true;
            try
            {
                var result = await _scriptService.SaveFileDialogAsync(CurrentScript);
                if (result.Success && result.Script != null)
                {
                    CurrentScript = result.Script;
                    UpdateWindowTitle();
                }
            }
            finally
            {
                IsLoading = false;
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

        private void UpdateWindowTitle()
        {
            string dirtyMarker = CurrentScript.IsDirty ? "*" : "";
            string fileName = string.IsNullOrEmpty(CurrentScript.FilePath)
                ? CurrentScript.Title
                : System.IO.Path.GetFileNameWithoutExtension(CurrentScript.FilePath);

            WindowTitle = $"ScriptWriter - {fileName}{dirtyMarker}";
        }

        partial void OnCurrentScriptChanged(Script value)
        {
            if (value != null)
            {
                UpdateWindowTitle();
            }
        }
    }
}