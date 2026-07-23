namespace LogicExporter.Logic;

public abstract record FormulaNode;

public sealed record SentenceLetterNode(string Name) : FormulaNode;

public sealed record NegationNode(FormulaNode Operand) : FormulaNode;

public sealed record ModalNode(
    ModalOperator Operator,
    FormulaNode Operand) : FormulaNode;

public sealed record BinaryFormulaNode(
    BinaryOperator Operator,
    FormulaNode Left,
    FormulaNode Right) : FormulaNode;

public sealed record PredicateFormulaNode(
    string Name,
    IReadOnlyList<TermNode> Arguments) : FormulaNode;

public sealed record IdentityFormulaNode(
    IdentityOperator Operator,
    TermNode Left,
    TermNode Right) : FormulaNode;

public sealed record QuantifiedFormulaNode(
    Quantifier Quantifier,
    string Variable,
    FormulaNode Operand) : FormulaNode;

public enum BinaryOperator
{
    Conjunction,
    Disjunction,
    Conditional,
    Biconditional
}

public enum ModalOperator
{
    Box,
    Diamond
}

public enum Quantifier
{
    Universal,
    Existential
}

public enum IdentityOperator
{
    Identity,
    NonIdentity
}

public abstract record TermNode;

public sealed record VariableTermNode(string Name) : TermNode;

public sealed record ConstantTermNode(string Name) : TermNode;
