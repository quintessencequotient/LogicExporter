using LogicExporter.Logic;

namespace LogicExporter.Tests;

public sealed class ModalParserTests
{
    [Theory]
    [InlineData("□F(x)")]
    [InlineData("◇(F(x) → G(x))")]
    [InlineData("□∀xF(x)")]
    [InlineData("∀x◇F(x)")]
    [InlineData("□(a = b)")]
    [InlineData("◇∃x(F(x) ∧ x ≠ a)")]
    [InlineData("[]F(x)")]
    [InlineData("<>F(x)")]
    public void Parse_ReturnsSuccessForValidModalFormulas(string input)
    {
        var result = FormulaParser.Parse(input, LogicSystem.Modal);

        Assert.True(result.IsValid, result.ErrorMessage);
        Assert.NotNull(result.Root);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorPosition);
    }

    [Theory]
    [InlineData("□□F(x)")]
    [InlineData("◇□∀xF(x)")]
    [InlineData("¬◇□F(x)")]
    public void Parse_ReturnsSuccessForNestedModalOperators(string input)
    {
        var result = FormulaParser.Parse(input, LogicSystem.Modal);

        Assert.True(result.IsValid, result.ErrorMessage);
        Assert.NotNull(result.Root);
    }

    [Theory]
    [InlineData("□")]
    [InlineData("◇∧P")]
    [InlineData("□(P →)")]
    [InlineData("P")]
    [InlineData("□P")]
    [InlineData("◇(P → Q)")]
    public void Parse_ReturnsFailureForInvalidModalFormulas(string input)
    {
        var result = FormulaParser.Parse(input, LogicSystem.Modal);

        Assert.False(result.IsValid);
        Assert.Null(result.Root);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotNull(result.ErrorPosition);
    }

    [Fact]
    public void Parse_RejectsModalSyntaxOutsideModalSystem()
    {
        var result = FormulaParser.Parse("□F(x)", LogicSystem.FirstOrderWithIdentity);

        Assert.False(result.IsValid);
        Assert.Null(result.Root);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Parse_ModalOperatorsBindBeforeConjunction()
    {
        var root = AssertBinary(ParseValid("□F(x) ∧ G(x)"), BinaryOperator.Conjunction);

        var left = AssertModal(root.Left, ModalOperator.Box);
        AssertPredicate(left.Operand, "F");
        AssertPredicate(root.Right, "G");
    }

    [Fact]
    public void Parse_ModalOperatorsBindBeforeConditional()
    {
        var root = AssertBinary(ParseValid("◇F(x) → G(x)"), BinaryOperator.Conditional);

        var left = AssertModal(root.Left, ModalOperator.Diamond);
        AssertPredicate(left.Operand, "F");
        AssertPredicate(root.Right, "G");
    }

    [Fact]
    public void Parse_ModalOperatorPreservesGroupedBinaryOperand()
    {
        var root = AssertModal(ParseValid("□(F(x) → G(x))"), ModalOperator.Box);
        var operand = AssertBinary(root.Operand, BinaryOperator.Conditional);

        AssertPredicate(operand.Left, "F");
        AssertPredicate(operand.Right, "G");
    }

    [Fact]
    public void Parse_AcceptsConsistentPredicateAritiesInsideModalScope()
    {
        var result = FormulaParser.Parse("□(R(x) ∧ R(y))", LogicSystem.Modal);

        Assert.True(result.IsValid, result.ErrorMessage);
    }

    [Fact]
    public void Parse_RejectsInconsistentPredicateArityInsideModalScope()
    {
        var result = FormulaParser.Parse("□(R(x) ∧ R(x,y))", LogicSystem.Modal);

        Assert.False(result.IsValid);
        Assert.Contains("arity", result.ErrorMessage);
        Assert.NotNull(result.ErrorPosition);
    }

    [Fact]
    public void Parse_ReportsFreeVariablesThroughModalOperators()
    {
        var result = FormulaParser.Parse("∀x◇R(x,y)", LogicSystem.Modal);

        Assert.True(result.IsValid, result.ErrorMessage);
        Assert.False(result.IsSentence);
        Assert.Equal(["y"], result.FreeVariables);
    }

    private static FormulaNode ParseValid(string input)
    {
        var result = FormulaParser.Parse(input, LogicSystem.Modal);

        Assert.True(result.IsValid, result.ErrorMessage);
        Assert.NotNull(result.Root);
        return result.Root!;
    }

    private static ModalNode AssertModal(FormulaNode node, ModalOperator expectedOperator)
    {
        var modal = Assert.IsType<ModalNode>(node);
        Assert.Equal(expectedOperator, modal.Operator);
        return modal;
    }

    private static BinaryFormulaNode AssertBinary(FormulaNode node, BinaryOperator expectedOperator)
    {
        var binary = Assert.IsType<BinaryFormulaNode>(node);
        Assert.Equal(expectedOperator, binary.Operator);
        return binary;
    }

    private static void AssertPredicate(FormulaNode node, string expectedName)
    {
        var predicate = Assert.IsType<PredicateFormulaNode>(node);
        Assert.Equal(expectedName, predicate.Name);
    }
}
