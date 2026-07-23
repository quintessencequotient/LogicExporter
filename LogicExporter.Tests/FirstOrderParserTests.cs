using LogicExporter.Logic;

namespace LogicExporter.Tests;

public sealed class FirstOrderParserTests
{
    [Theory]
    [InlineData("F(x)")]
    [InlineData("R(x,y)")]
    [InlineData("x = a")]
    [InlineData("x ≠ y")]
    [InlineData("d = e")]
    [InlineData("∀xF(x)")]
    [InlineData("F_1(x_0)")]
    [InlineData("∀x_0F_2(x_0)")]
    [InlineData("R_5(x_1,y_2)")]
    [InlineData("∃x(F(x) ∧ G(x))")]
    [InlineData("∀x(F(x) → ∃yR(x,y))")]
    [InlineData("∃x∀xF(x)")]
    public void Parse_ReturnsSuccessForValidFirstOrderWffs(string input)
    {
        var result = FormulaParser.Parse(input, LogicSystem.FirstOrderWithIdentity);

        Assert.True(result.IsValid, result.ErrorMessage);
        Assert.NotNull(result.Root);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorPosition);
    }

    [Theory]
    [InlineData("∀F(x)")]
    [InlineData("∃x")]
    [InlineData("R(x,)")]
    [InlineData("x =")]
    [InlineData("∀x(F(x) ∧)")]
    [InlineData("P")]
    [InlineData("S(x)")]
    [InlineData("w = a")]
    [InlineData("F_6(x)")]
    [InlineData("x_6 = a")]
    public void Parse_ReturnsFailureForInvalidFirstOrderWffs(string input)
    {
        var result = FormulaParser.Parse(input, LogicSystem.FirstOrderWithIdentity);

        Assert.False(result.IsValid);
        Assert.Null(result.Root);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotNull(result.ErrorPosition);
    }

    [Theory]
    [InlineData("F(x)", false, "x")]
    [InlineData("F_1(x_0)", false, "x_0")]
    [InlineData("∀xF(x)", true)]
    [InlineData("∀x_0F_1(x_0)", true)]
    [InlineData("∀xR(x,y)", false, "y")]
    [InlineData("R(a,b)", true)]
    [InlineData("R(d,e)", true)]
    [InlineData("∃x∀xF(x)", true)]
    public void Parse_ReportsSentenceStatusAndFreeVariables(
        string input,
        bool expectedIsSentence,
        params string[] expectedFreeVariables)
    {
        var result = FormulaParser.Parse(input, LogicSystem.FirstOrderWithIdentity);

        Assert.True(result.IsValid, result.ErrorMessage);
        Assert.Equal(expectedIsSentence, result.IsSentence);
        Assert.Equal(expectedFreeVariables, result.FreeVariables);
    }

    [Theory]
    [InlineData("R(x) ∧ R(y)")]
    [InlineData("R_1(x) ∧ R_1(y)")]
    [InlineData("R(x,y) ∧ R(a,b)")]
    [InlineData("F(x) ∧ R(x,y)")]
    public void Parse_AcceptsConsistentPredicateArities(string input)
    {
        var result = FormulaParser.Parse(input, LogicSystem.FirstOrderWithIdentity);

        Assert.True(result.IsValid, result.ErrorMessage);
    }

    [Fact]
    public void Parse_RejectsInconsistentPredicateArity()
    {
        var result = FormulaParser.Parse("R(x) ∧ R(x,y)", LogicSystem.FirstOrderWithIdentity);

        Assert.False(result.IsValid);
        Assert.Contains("arity", result.ErrorMessage);
        Assert.NotNull(result.ErrorPosition);
    }

    [Fact]
    public void Parse_UsesCustomFirstOrderLanguage()
    {
        var options = new ParserOptions(FirstOrderLanguage.Create(
            variables: ["x", "u"],
            constants: ["a", "b", "c", "d", "e", "k"],
            predicates: ["F", "S"]));

        var result = FormulaParser.Parse("S(u) ∧ k = a", LogicSystem.FirstOrderWithIdentity, options);

        Assert.True(result.IsValid, result.ErrorMessage);
        Assert.Equal(["u"], result.FreeVariables);
    }

    [Fact]
    public void Parse_RejectsOverlappingVariablesAndConstants()
    {
        Assert.Throws<ArgumentException>(() => FirstOrderLanguage.Create(
            variables: ["x"],
            constants: ["x"],
            predicates: ["F"]));
    }
}
