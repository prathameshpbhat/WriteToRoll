using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using App.Core.Models;
using App.Core.Services;

namespace App.ViewModels
{
    public partial class ScriptEditorViewModel : ObservableObject
    {
        private readonly IScreenwritingLogic _screenwritingLogic;
        private readonly FormattingService _formattingService;
        private Script _currentScript = new();

        [ObservableProperty]
        private ObservableCollection<ScriptElement> elements = new();

        [ObservableProperty]
        private string currentLineText = string.Empty;

        [ObservableProperty]
        private int pageCount = 1;

        [ObservableProperty]
        private int elementCount = 0;

        [ObservableProperty]
        private int wordCount = 0;

        [ObservableProperty]
        private string statusMessage = "Ready";

        public ScriptEditorViewModel()
        {
            _screenwritingLogic = new ScreenwritingLogic();
            _formattingService = new FormattingService();
        }

        [RelayCommand]
        public void ProcessLineInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return;

            var previousType = Elements.LastOrDefault()?.ElementType;
            var context = new ScriptContext(previousType, false);
            var result = _screenwritingLogic.DetectAndNormalize(input, context);

            var element = CreateElementFromDetection(result);
            if (element != null)
            {
                Elements.Add(element);
                _currentScript.Elements.Add(element);
                UpdateStatistics();
                StatusMessage = $"{result.ElementType} added";
            }

            CurrentLineText = string.Empty;
        }

        [RelayCommand]
        public void FinalizeLineInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return;

            var previousType = Elements.LastOrDefault()?.ElementType;
            var context = new ScriptContext(previousType, true);
            var result = _screenwritingLogic.DetectAndNormalize(input, context);

            var element = CreateElementFromDetection(result);
            if (element != null)
            {
                Elements.Add(element);
                _currentScript.Elements.Add(element);

                if (result.SuggestAppendTimeDash && !string.IsNullOrEmpty(result.AutoSuggestion))
                {
                    element.Text += result.AutoSuggestion;
                    StatusMessage = $"Auto-appended: {result.AutoSuggestion}";
                }

                UpdateStatistics();
            }

            CurrentLineText = string.Empty;
        }

        [RelayCommand]
        public void DeleteElement(int index)
        {
            if (index >= 0 && index < Elements.Count)
            {
                Elements.RemoveAt(index);
                _currentScript.Elements.RemoveAt(index);
                UpdateStatistics();
                StatusMessage = "Element deleted";
            }
        }

        private ScriptElement? CreateElementFromDetection(DetectionResult result)
        {
            return result.ElementType switch
            {
                ScriptElementType.SceneHeading => new SceneHeadingElement { Text = result.Text },
                ScriptElementType.Action => new ActionElement { Text = result.Text },
                ScriptElementType.Character => new CharacterElement { Name = result.Text, Text = result.Text },
                ScriptElementType.Dialogue => new DialogueElement { Text = result.Text },
                ScriptElementType.Parenthetical => new ParentheticalElement { Text = result.Text },
                ScriptElementType.Transition => new TransitionElement { Text = result.Text },
                ScriptElementType.Shot => new ShotElement { Text = result.Text },
                ScriptElementType.CenteredText => new CenteredTextElement { Text = result.Text },
                _ => null
            };
        }

        private void UpdateStatistics()
        {
            PageCount = _currentScript.GetEstimatedPageCount();
            ElementCount = _currentScript.GetElementCount();
            WordCount = _currentScript.GetWordCount();
        }
    }
}
