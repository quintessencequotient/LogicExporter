namespace LogicExporter.Logic;

public static class FormulaValidator
{
    public static FormulaValidationResult Validate(string input, LogicSystem system)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new FormulaValidationResult(
                ValidationStatus.Invalid,
                "Enter a formula before validating.",
                null,
                Array.Empty<string>(),
                string.Empty);
        }

        var parseResult = FormulaParser.Parse(input, system);

        if (parseResult.IsValid)
        {
            return new FormulaValidationResult(
                ValidationStatus.Valid,
                GetSuccessMessage(system, parseResult.IsSentence),
                null,
                parseResult.FreeVariables,
                parseResult.NormalizedFormula)
            {
                IsSentence = parseResult.IsSentence
            };
        }

        return new FormulaValidationResult(
            ValidationStatus.Invalid,
            parseResult.ErrorMessage ?? "The formula is not well formed.",
            parseResult.ErrorPosition,
            parseResult.FreeVariables,
            parseResult.NormalizedFormula);
    }

    private static string GetSuccessMessage(LogicSystem system, bool isSentence)
    {
        if (system is LogicSystem.FirstOrderWithIdentity or LogicSystem.Modal)
        {
            return isSentence
                ? "Well-formed sentence."
                : "Well-formed formula, but not a sentence.";
        }

        return "Well-formed formula.";
    }
}

public enum ValidationStatus
{
    Valid,
    Invalid,
    ParserPending
}

public sealed record FormulaValidationResult(
    ValidationStatus Status,
    string Message,
    int? ErrorPosition,
    IReadOnlyCollection<string> FreeVariables,
    string NormalizedFormula)
{
    public bool IsValid => Status == ValidationStatus.Valid;

    public bool IsSentence { get; init; }
}
