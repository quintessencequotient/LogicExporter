using LogicExporter.Logic;

namespace LogicExporter.Tests;

public sealed class LatexConverterTests
{
    [Theory]
    [InlineData("¬P", @"\neg P")]
    [InlineData("P ∧ Q", @"P \land Q")]
    [InlineData("P ∨ Q", @"P \lor Q")]
    [InlineData("P → Q", @"P \to Q")]
    [InlineData("P ↔ Q", @"P \leftrightarrow Q")]
    [InlineData("□F(x)", @"\Box F(x)")]
    [InlineData("◇F(x)", @"\Diamond F(x)")]
    [InlineData("∀x∃yR(x,y)", @"\forall x\exists yR(x,y)")]
    [InlineData("x ≠ a", @"x \neq a")]
    public void Convert_MapsUnicodeSymbolsToLatex(string input, string expected)
    {
        Assert.Equal(expected, LatexConverter.Convert(input));
    }

    [Fact]
    public void Convert_NormalizesAliasesBeforeLatexConversion()
    {
        var latex = LatexConverter.Convert("~P & Q | R -> F <-> G [] <> forall x exists y x != a");

        Assert.Equal(@"\neg P \land Q \lor R \to F \leftrightarrow G \Box \Diamond \forall x \exists y x \neq a", latex);
    }

    [Fact]
    public void Convert_UsesParsedAstToPreserveRequiredGrouping()
    {
        var latex = LatexConverter.Convert("(P ∨ Q) ∧ R");

        Assert.Equal(@"(P \lor Q) \land R", latex);
    }

    [Theory]
    [InlineData("∀xF(x)", @"\forall x F(x)")]
    [InlineData("∃x(F(x) ∧ G(x))", @"\exists x (F(x) \land G(x))")]
    [InlineData("x ≠ a", @"x \neq a")]
    [InlineData("R(x,y)", @"R(x,y)")]
    [InlineData("∀x_0F_1(x_0)", @"\forall x_{0} F_{1}(x_{0})")]
    public void Convert_UsesFirstOrderAstWhenSystemIsFirstOrder(string input, string expected)
    {
        Assert.Equal(expected, LatexConverter.Convert(input, LogicSystem.FirstOrderWithIdentity));
    }

    [Theory]
    [InlineData("□F(x)", @"\Box F(x)")]
    [InlineData("◇(F(x) → G(x))", @"\Diamond (F(x) \to G(x))")]
    [InlineData("□∀xF(x)", @"\Box \forall x F(x)")]
    [InlineData("◇∃x(F(x) ∧ x ≠ a)", @"\Diamond \exists x (F(x) \land x \neq a)")]
    public void Convert_UsesModalAstWhenSystemIsModal(string input, string expected)
    {
        Assert.Equal(expected, LatexConverter.Convert(input, LogicSystem.Modal));
    }
}
