using System.Text;
using System.Text.RegularExpressions;

namespace LogicExporter.Logic;

public static class LatexConverter
{
    public static string Convert(string input)
    {
        return Convert(input, LogicSystem.Propositional);
    }

    public static string Convert(string input, LogicSystem logicSystem, ParserOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var parseResult = FormulaParser.Parse(input, logicSystem, options);
        if (parseResult.IsValid && parseResult.Root is not null)
        {
            return Convert(parseResult.Root);
        }

        var normalized = SymbolNormalizer.Normalize(input);
        return ConvertSymbols(normalized);
    }

    public static string Convert(FormulaNode root)
    {
        return WriteFormula(root, parentPrecedence: 0, ChildSide.None, parentOperator: null);
    }

    private static string ConvertSymbols(string normalized)
    {
        var builder = new StringBuilder(normalized.Length * 2);

        foreach (var character in normalized)
        {
            builder.Append(character switch
            {
                '¬' => @"\neg ",
                '∧' => @" \land ",
                '∨' => @" \lor ",
                '→' => @" \to ",
                '↔' => @" \leftrightarrow ",
                '□' => @"\Box ",
                '◇' => @"\Diamond ",
                '∀' => @"\forall ",
                '∃' => @"\exists ",
                '≠' => @" \neq ",
                _ => character
            });
        }

        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }

    private static string WriteFormula(
        FormulaNode node,
        int parentPrecedence,
        ChildSide side,
        BinaryOperator? parentOperator)
    {
        return node switch
        {
            SentenceLetterNode sentenceLetter => WriteSymbol(sentenceLetter.Name),
            NegationNode negation => WriteNegation(negation, parentPrecedence),
            ModalNode modal => WriteModal(modal, parentPrecedence),
            BinaryFormulaNode binary => WriteBinary(binary, parentPrecedence, side, parentOperator),
            PredicateFormulaNode predicate => WritePredicate(predicate),
            IdentityFormulaNode identity => WriteIdentity(identity),
            QuantifiedFormulaNode quantified => WriteQuantifiedFormula(quantified, parentPrecedence),
            _ => throw new ArgumentOutOfRangeException(nameof(node), node, "Unknown formula node.")
        };
    }

    private static string WriteNegation(NegationNode node, int parentPrecedence)
    {
        const int precedence = 5;
        var operand = WriteFormula(node.Operand, precedence, ChildSide.Right, parentOperator: null);
        var text = $@"\neg {operand}";

        return precedence < parentPrecedence ? $"({text})" : text;
    }

    private static string WriteModal(ModalNode node, int parentPrecedence)
    {
        const int precedence = 5;
        var modalOperator = node.Operator == ModalOperator.Box ? @"\Box" : @"\Diamond";
        var operand = node.Operand is BinaryFormulaNode
            ? $"({WriteFormula(node.Operand, parentPrecedence: 0, ChildSide.None, parentOperator: null)})"
            : WriteFormula(node.Operand, precedence, ChildSide.Right, parentOperator: null);

        var text = $"{modalOperator} {operand}";
        return precedence < parentPrecedence ? $"({text})" : text;
    }

    private static string WritePredicate(PredicateFormulaNode node)
    {
        return $"{WriteSymbol(node.Name)}({string.Join(",", node.Arguments.Select(WriteTerm))})";
    }

    private static string WriteIdentity(IdentityFormulaNode node)
    {
        var identityOperator = node.Operator == IdentityOperator.Identity ? "=" : @"\neq";
        return $"{WriteTerm(node.Left)} {identityOperator} {WriteTerm(node.Right)}";
    }

    private static string WriteQuantifiedFormula(QuantifiedFormulaNode node, int parentPrecedence)
    {
        const int precedence = 5;
        var quantifier = node.Quantifier == Quantifier.Universal ? @"\forall" : @"\exists";
        var operand = WriteFormula(node.Operand, parentPrecedence: 0, ChildSide.None, parentOperator: null);

        if (node.Operand is BinaryFormulaNode)
        {
            operand = $"({operand})";
        }

        var text = $"{quantifier} {WriteSymbol(node.Variable)} {operand}";
        return precedence < parentPrecedence ? $"({text})" : text;
    }

    private static string WriteBinary(
        BinaryFormulaNode node,
        int parentPrecedence,
        ChildSide side,
        BinaryOperator? parentOperator)
    {
        var precedence = GetPrecedence(node.Operator);
        var left = WriteFormula(node.Left, precedence, ChildSide.Left, node.Operator);
        var right = WriteFormula(node.Right, precedence, ChildSide.Right, node.Operator);
        var text = $"{left} {GetLatexOperator(node.Operator)} {right}";

        return ShouldParenthesize(precedence, parentPrecedence, side, parentOperator, node.Operator)
            ? $"({text})"
            : text;
    }

    private static bool ShouldParenthesize(
        int childPrecedence,
        int parentPrecedence,
        ChildSide side,
        BinaryOperator? parentOperator,
        BinaryOperator childOperator)
    {
        if (side == ChildSide.None)
        {
            return false;
        }

        if (childPrecedence < parentPrecedence)
        {
            return true;
        }

        if (childPrecedence > parentPrecedence)
        {
            return false;
        }

        if (parentOperator is null)
        {
            return false;
        }

        return parentOperator.Value switch
        {
            BinaryOperator.Conditional => side == ChildSide.Left,
            BinaryOperator.Biconditional => side == ChildSide.Right,
            BinaryOperator.Conjunction => side == ChildSide.Right && childOperator == BinaryOperator.Conjunction,
            BinaryOperator.Disjunction => side == ChildSide.Right && childOperator == BinaryOperator.Disjunction,
            _ => false
        };
    }

    private static int GetPrecedence(BinaryOperator binaryOperator)
    {
        return binaryOperator switch
        {
            BinaryOperator.Conjunction => 4,
            BinaryOperator.Disjunction => 3,
            BinaryOperator.Conditional => 2,
            BinaryOperator.Biconditional => 1,
            _ => throw new ArgumentOutOfRangeException(nameof(binaryOperator), binaryOperator, null)
        };
    }

    private static string GetLatexOperator(BinaryOperator binaryOperator)
    {
        return binaryOperator switch
        {
            BinaryOperator.Conjunction => @"\land",
            BinaryOperator.Disjunction => @"\lor",
            BinaryOperator.Conditional => @"\to",
            BinaryOperator.Biconditional => @"\leftrightarrow",
            _ => throw new ArgumentOutOfRangeException(nameof(binaryOperator), binaryOperator, null)
        };
    }

    private static string WriteTerm(TermNode node)
    {
        return node switch
        {
            VariableTermNode variable => WriteSymbol(variable.Name),
            ConstantTermNode constant => WriteSymbol(constant.Name),
            _ => throw new ArgumentOutOfRangeException(nameof(node), node, "Unknown term node.")
        };
    }

    private static string WriteSymbol(string symbol)
    {
        var underscoreIndex = symbol.IndexOf('_', StringComparison.Ordinal);

        if (underscoreIndex < 0 || underscoreIndex == symbol.Length - 1)
        {
            return symbol;
        }

        return $"{symbol[..underscoreIndex]}_{{{symbol[(underscoreIndex + 1)..]}}}";
    }

    private enum ChildSide
    {
        None,
        Left,
        Right
    }
}
