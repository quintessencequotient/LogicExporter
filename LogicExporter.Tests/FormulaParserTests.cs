using LogicExporter.Logic;

namespace LogicExporter.Tests;

public sealed class FormulaParserTests
{
    [Theory]
    [InlineData("P")]
    [InlineData("¬P")]
    [InlineData("P ∧ Q")]
    [InlineData("P ∨ Q ∧ R")]
    [InlineData("(P ∨ Q) ∧ R")]
    [InlineData("P → Q → R")]
    [InlineData("¬(P ↔ Q)")]
    [InlineData("~P & Q")]
    [InlineData("P -> Q")]
    [InlineData("P <-> Q")]
    public void Parse_ReturnsSuccessForValidPropositionalFormulas(string input)
    {
        var result = FormulaParser.Parse(input, LogicSystem.Propositional);

        Assert.True(result.IsValid, result.ErrorMessage);
        Assert.NotNull(result.Root);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorPosition);
    }

    [Theory]
    [InlineData("∧P")]
    [InlineData("P Q")]
    [InlineData("P →")]
    [InlineData("¬")]
    [InlineData("(P ∨ Q")]
    [InlineData("P ∧ ∨ Q")]
    [InlineData("()")]
    public void Parse_ReturnsFailureForInvalidPropositionalFormulas(string input)
    {
        var result = FormulaParser.Parse(input, LogicSystem.Propositional);

        Assert.False(result.IsValid);
        Assert.Null(result.Root);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotNull(result.ErrorPosition);
    }

    [Fact]
    public void Parse_UsesConjunctionBeforeDisjunction()
    {
        var root = AssertBinary(ParseValid("P ∨ Q ∧ R"), BinaryOperator.Disjunction);

        AssertSentence(root.Left, "P");
        var right = AssertBinary(root.Right, BinaryOperator.Conjunction);
        AssertSentence(right.Left, "Q");
        AssertSentence(right.Right, "R");
    }

    [Fact]
    public void Parse_PreservesGroupedDisjunctionBeforeConjunction()
    {
        var root = AssertBinary(ParseValid("(P ∨ Q) ∧ R"), BinaryOperator.Conjunction);

        var left = AssertBinary(root.Left, BinaryOperator.Disjunction);
        AssertSentence(left.Left, "P");
        AssertSentence(left.Right, "Q");
        AssertSentence(root.Right, "R");
    }

    [Fact]
    public void Parse_ConditionalAssociatesRight()
    {
        var root = AssertBinary(ParseValid("P → Q → R"), BinaryOperator.Conditional);

        AssertSentence(root.Left, "P");
        var right = AssertBinary(root.Right, BinaryOperator.Conditional);
        AssertSentence(right.Left, "Q");
        AssertSentence(right.Right, "R");
    }

    [Fact]
    public void Parse_BiconditionalAssociatesLeft()
    {
        var root = AssertBinary(ParseValid("P ↔ Q ↔ R"), BinaryOperator.Biconditional);

        var left = AssertBinary(root.Left, BinaryOperator.Biconditional);
        AssertSentence(left.Left, "P");
        AssertSentence(left.Right, "Q");
        AssertSentence(root.Right, "R");
    }

    private static FormulaNode ParseValid(string input)
    {
        var result = FormulaParser.Parse(input, LogicSystem.Propositional);

        Assert.True(result.IsValid, result.ErrorMessage);
        Assert.NotNull(result.Root);
        return result.Root!;
    }

    private static BinaryFormulaNode AssertBinary(FormulaNode node, BinaryOperator expectedOperator)
    {
        var binary = Assert.IsType<BinaryFormulaNode>(node);
        Assert.Equal(expectedOperator, binary.Operator);
        return binary;
    }

    private static void AssertSentence(FormulaNode node, string expectedName)
    {
        var sentence = Assert.IsType<SentenceLetterNode>(node);
        Assert.Equal(expectedName, sentence.Name);
    }
}
