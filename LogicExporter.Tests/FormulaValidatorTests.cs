using LogicExporter.Logic;

namespace LogicExporter.Tests;

public sealed class FormulaValidatorTests
{
    [Fact]
    public void Validate_ReturnsInvalidForEmptyInput()
    {
        var result = FormulaValidator.Validate("   ", LogicSystem.Propositional);

        Assert.Equal(ValidationStatus.Invalid, result.Status);
        Assert.False(result.IsValid);
        Assert.Null(result.ErrorPosition);
        Assert.Empty(result.FreeVariables);
        Assert.Equal(string.Empty, result.NormalizedFormula);
    }

    [Fact]
    public void Validate_ReturnsValidForWellFormedPropositionalInput()
    {
        var result = FormulaValidator.Validate("P -> Q", LogicSystem.Propositional);

        Assert.Equal(ValidationStatus.Valid, result.Status);
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorPosition);
        Assert.Empty(result.FreeVariables);
        Assert.True(result.IsSentence);
        Assert.Equal("P → Q", result.NormalizedFormula);
        Assert.Equal("Well-formed formula.", result.Message);
    }

    [Theory]
    [InlineData("∧P")]
    [InlineData("P Q")]
    [InlineData("P →")]
    [InlineData("¬")]
    [InlineData("(P ∨ Q")]
    [InlineData("P ∧ ∨ Q")]
    [InlineData("()")]
    public void Validate_ReturnsInvalidForMalformedPropositionalInput(string input)
    {
        var result = FormulaValidator.Validate(input, LogicSystem.Propositional);

        Assert.Equal(ValidationStatus.Invalid, result.Status);
        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorPosition);
        Assert.Empty(result.FreeVariables);
        Assert.False(result.IsSentence);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Fact]
    public void Validate_ReturnsValidForFirstOrderSentence()
    {
        var result = FormulaValidator.Validate("forall x F(x)", LogicSystem.FirstOrderWithIdentity);

        Assert.Equal(ValidationStatus.Valid, result.Status);
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorPosition);
        Assert.Empty(result.FreeVariables);
        Assert.True(result.IsSentence);
        Assert.Equal("∀ x F(x)", result.NormalizedFormula);
        Assert.Equal("Well-formed sentence.", result.Message);
    }

    [Fact]
    public void Validate_ReturnsValidForFirstOrderOpenFormula()
    {
        var result = FormulaValidator.Validate("F(x)", LogicSystem.FirstOrderWithIdentity);

        Assert.Equal(ValidationStatus.Valid, result.Status);
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorPosition);
        Assert.Equal(["x"], result.FreeVariables);
        Assert.False(result.IsSentence);
        Assert.Equal("Well-formed formula, but not a sentence.", result.Message);
    }

    [Fact]
    public void Validate_ReturnsValidForModalSentence()
    {
        var result = FormulaValidator.Validate("◇∃x(F(x) ∧ x ≠ a)", LogicSystem.Modal);

        Assert.Equal(ValidationStatus.Valid, result.Status);
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorPosition);
        Assert.Empty(result.FreeVariables);
        Assert.True(result.IsSentence);
        Assert.Equal("Well-formed sentence.", result.Message);
    }

    [Fact]
    public void Validate_ReturnsValidForModalOpenFormula()
    {
        var result = FormulaValidator.Validate("□F(x)", LogicSystem.Modal);

        Assert.Equal(ValidationStatus.Valid, result.Status);
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorPosition);
        Assert.Equal(["x"], result.FreeVariables);
        Assert.False(result.IsSentence);
        Assert.Equal("Well-formed formula, but not a sentence.", result.Message);
    }
}
