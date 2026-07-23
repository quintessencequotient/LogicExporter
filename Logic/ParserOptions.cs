namespace LogicExporter.Logic;

public sealed record ParserOptions(FirstOrderLanguage FirstOrderLanguage)
{
    public static ParserOptions Default { get; } = new(FirstOrderLanguage.Default);
}

public sealed record FirstOrderLanguage(
    IReadOnlySet<string> Variables,
    IReadOnlySet<string> Constants,
    IReadOnlySet<string> Predicates)
{
    public static FirstOrderLanguage Default { get; } = Create(
        variables: WithSubscripts(["x", "y", "z"]),
        constants: ["a", "b", "c", "d", "e"],
        predicates: WithSubscripts(["P", "Q", "R", "F", "G", "H"]));

    public static FirstOrderLanguage Create(
        IEnumerable<string> variables,
        IEnumerable<string> constants,
        IEnumerable<string> predicates)
    {
        var variableSet = ToSymbolSet(variables, nameof(variables));
        var constantSet = ToSymbolSet(constants, nameof(constants));
        var predicateSet = ToSymbolSet(predicates, nameof(predicates));

        if (variableSet.Overlaps(constantSet))
        {
            throw new ArgumentException("Variable and constant symbols must not overlap.");
        }

        return new FirstOrderLanguage(variableSet, constantSet, predicateSet);
    }

    public bool IsVariable(string symbol)
    {
        return Variables.Contains(symbol);
    }

    public bool IsConstant(string symbol)
    {
        return Constants.Contains(symbol);
    }

    public bool IsPredicate(string symbol)
    {
        return Predicates.Contains(symbol);
    }

    private static IReadOnlySet<string> ToSymbolSet(IEnumerable<string> symbols, string parameterName)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);

        foreach (var symbol in symbols)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException("Symbols must not be empty.", parameterName);
            }

            set.Add(symbol);
        }

        return set;
    }

    private static IEnumerable<string> WithSubscripts(IEnumerable<string> baseSymbols)
    {
        foreach (var symbol in baseSymbols)
        {
            yield return symbol;

            for (var index = 0; index <= 5; index++)
            {
                yield return $"{symbol}_{index}";
            }
        }
    }
}
