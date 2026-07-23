using LogicExporter.Logic;

namespace LogicExporter.Tests;

public sealed class SymbolNormalizerTests
{
    [Theory]
    [InlineData("~P & Q", "¬P ∧ Q")]
    [InlineData("!P | Q", "¬P ∨ Q")]
    [InlineData("P -> Q", "P → Q")]
    [InlineData("P <-> Q", "P ↔ Q")]
    [InlineData("[]F(x)", "□F(x)")]
    [InlineData("<>F(x)", "◇F(x)")]
    [InlineData("forall x exists y R(x,y)", "∀ x ∃ y R(x,y)")]
    [InlineData("x != a", "x ≠ a")]
    public void Normalize_ConvertsAsciiAliases(string input, string expected)
    {
        Assert.Equal(expected, SymbolNormalizer.Normalize(input));
    }
}
