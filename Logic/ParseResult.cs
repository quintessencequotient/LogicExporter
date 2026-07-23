namespace LogicExporter.Logic;

public sealed record ParseResult(
    bool IsValid,
    FormulaNode? Root,
    string? ErrorMessage,
    int? ErrorPosition,
    string NormalizedFormula,
    IReadOnlyCollection<string> FreeVariables,
    bool IsSentence)
{
    public static ParseResult Success(
        FormulaNode root,
        string normalizedFormula,
        IReadOnlyCollection<string> freeVariables)
    {
        return new ParseResult(
            true,
            root,
            null,
            null,
            normalizedFormula,
            freeVariables,
            freeVariables.Count == 0);
    }

    public static ParseResult Failure(string message, int? errorPosition, string normalizedFormula)
    {
        return new ParseResult(
            false,
            null,
            message,
            errorPosition,
            normalizedFormula,
            Array.Empty<string>(),
            false);
    }
}
