using App.Core.Models;

namespace App.Core.Services
{
    public record DetectionResult(
        string Text,
        ScriptElementType ElementType,
        bool MoveCaretToEnd,
        bool TextWasChanged,
        bool SuggestAppendTimeDash = false,
        string? AutoSuggestion = null,
        string? WarningMessage = null
    );

    public record ScriptContext(
        ScriptElementType? PreviousElementType = null,
        bool OnFinalize = false,
        string? PreviousText = null,
        int LineCount = 1
    );

    public interface IScreenwritingLogic
    {
        DetectionResult DetectAndNormalize(string rawLine, ScriptContext context);
        ValidationResult Validate(ScriptElement element);
        string ApplySmartCorrections(string text, ScriptElementType type);
    }

    public record ValidationResult(
        bool IsValid,
        List<string> Errors = null,
        List<string> Warnings = null
    )
    {
        public ValidationResult(bool isValid) : this(isValid, new(), new()) { }
    }
}
