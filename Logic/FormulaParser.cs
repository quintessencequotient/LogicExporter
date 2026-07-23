namespace LogicExporter.Logic;

public static class FormulaParser
{
    public static ParseResult Parse(string input, LogicSystem logicSystem, ParserOptions? options = null)
    {
        var normalized = SymbolNormalizer.Normalize(input);

        if (string.IsNullOrWhiteSpace(input))
        {
            return ParseResult.Failure("Enter a formula before parsing.", null, string.Empty);
        }

        var tokenization = Tokenize(input);
        if (tokenization.ErrorMessage is not null)
        {
            return ParseResult.Failure(tokenization.ErrorMessage, tokenization.ErrorPosition, normalized);
        }

        var parser = new Parser(
            tokenization.Tokens,
            normalized,
            logicSystem,
            (options ?? ParserOptions.Default).FirstOrderLanguage);

        return parser.Parse();
    }

    private static TokenizationResult Tokenize(string input)
    {
        var tokens = new List<Token>();

        for (var position = 0; position < input.Length;)
        {
            var character = input[position];

            if (char.IsWhiteSpace(character))
            {
                position++;
                continue;
            }

            if (StartsWith(input, position, "<->"))
            {
                tokens.Add(new Token(TokenType.Biconditional, "↔", position));
                position += 3;
                continue;
            }

            if (StartsWith(input, position, "->"))
            {
                tokens.Add(new Token(TokenType.Conditional, "→", position));
                position += 2;
                continue;
            }

            if (StartsWith(input, position, "[]"))
            {
                tokens.Add(new Token(TokenType.Box, "□", position));
                position += 2;
                continue;
            }

            if (StartsWith(input, position, "<>"))
            {
                tokens.Add(new Token(TokenType.Diamond, "◇", position));
                position += 2;
                continue;
            }

            if (StartsWith(input, position, "!="))
            {
                tokens.Add(new Token(TokenType.NotEqual, "≠", position));
                position += 2;
                continue;
            }

            if (MatchesWord(input, position, "forall"))
            {
                tokens.Add(new Token(TokenType.ForAll, "∀", position));
                position += "forall".Length;
                continue;
            }

            if (MatchesWord(input, position, "exists"))
            {
                tokens.Add(new Token(TokenType.Exists, "∃", position));
                position += "exists".Length;
                continue;
            }

            switch (character)
            {
                case '¬':
                case '~':
                case '!':
                    tokens.Add(new Token(TokenType.Not, "¬", position));
                    break;
                case '∧':
                case '&':
                    tokens.Add(new Token(TokenType.And, "∧", position));
                    break;
                case '∨':
                case '|':
                    tokens.Add(new Token(TokenType.Or, "∨", position));
                    break;
                case '→':
                    tokens.Add(new Token(TokenType.Conditional, "→", position));
                    break;
                case '↔':
                    tokens.Add(new Token(TokenType.Biconditional, "↔", position));
                    break;
                case '□':
                    tokens.Add(new Token(TokenType.Box, "□", position));
                    break;
                case '◇':
                    tokens.Add(new Token(TokenType.Diamond, "◇", position));
                    break;
                case '∀':
                    tokens.Add(new Token(TokenType.ForAll, "∀", position));
                    break;
                case '∃':
                    tokens.Add(new Token(TokenType.Exists, "∃", position));
                    break;
                case '=':
                    tokens.Add(new Token(TokenType.Equal, "=", position));
                    break;
                case '≠':
                    tokens.Add(new Token(TokenType.NotEqual, "≠", position));
                    break;
                case '(':
                    tokens.Add(new Token(TokenType.LeftParen, "(", position));
                    break;
                case ')':
                    tokens.Add(new Token(TokenType.RightParen, ")", position));
                    break;
                case ',':
                    tokens.Add(new Token(TokenType.Comma, ",", position));
                    break;
                default:
                    if (character is >= 'A' and <= 'Z')
                    {
                        var symbol = ReadSymbol(input, position);
                        tokens.Add(new Token(TokenType.SentenceLetter, symbol, position));
                        position += symbol.Length;
                        continue;
                    }

                    if (character is >= 'a' and <= 'z')
                    {
                        var symbol = ReadSymbol(input, position);
                        tokens.Add(new Token(TokenType.TermSymbol, symbol, position));
                        position += symbol.Length;
                        continue;
                    }

                    return TokenizationResult.Failure(
                        $"Unexpected character '{character}'.",
                        position);
            }

            position++;
        }

        tokens.Add(new Token(TokenType.EndOfInput, string.Empty, input.Length));
        return TokenizationResult.Success(tokens);
    }

    private static string ReadSymbol(string input, int position)
    {
        var end = position + 1;

        if (end + 1 < input.Length && input[end] == '_' && char.IsDigit(input[end + 1]))
        {
            end += 2;

            while (end < input.Length && char.IsDigit(input[end]))
            {
                end++;
            }
        }

        return input[position..end];
    }

    private static bool StartsWith(string input, int position, string value)
    {
        return input.AsSpan(position).StartsWith(value, StringComparison.Ordinal);
    }

    private static bool MatchesWord(string input, int position, string value)
    {
        if (!input.AsSpan(position).StartsWith(value, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var end = position + value.Length;
        return end == input.Length || !char.IsLetter(input[end]);
    }

    private sealed class Parser(
        IReadOnlyList<Token> tokens,
        string normalizedFormula,
        LogicSystem logicSystem,
        FirstOrderLanguage firstOrderLanguage)
    {
        private readonly Dictionary<string, int> _predicateArities = new(StringComparer.Ordinal);
        private int _current;

        public ParseResult Parse()
        {
            try
            {
                var root = ParseFormula();

                if (!Check(TokenType.EndOfInput))
                {
                    return Failure($"Unexpected token '{Current.Text}'.", Current.Position);
                }

                var freeVariables = CollectFreeVariables(root);
                return ParseResult.Success(root, normalizedFormula, freeVariables);
            }
            catch (ParseException exception)
            {
                return Failure(exception.Message, exception.Position);
            }
        }

        private FormulaNode ParseFormula()
        {
            return ParseBiconditional();
        }

        private FormulaNode ParseBiconditional()
        {
            var left = ParseConditional();

            while (Match(TokenType.Biconditional))
            {
                var operatorToken = Previous;
                EnsureFormulaStartsAfter(operatorToken);
                var right = ParseConditional();
                left = new BinaryFormulaNode(BinaryOperator.Biconditional, left, right);
            }

            return left;
        }

        private FormulaNode ParseConditional()
        {
            var left = ParseDisjunction();

            if (Match(TokenType.Conditional))
            {
                var operatorToken = Previous;
                EnsureFormulaStartsAfter(operatorToken);
                var right = ParseConditional();
                return new BinaryFormulaNode(BinaryOperator.Conditional, left, right);
            }

            return left;
        }

        private FormulaNode ParseDisjunction()
        {
            var left = ParseConjunction();

            while (Match(TokenType.Or))
            {
                var operatorToken = Previous;
                EnsureFormulaStartsAfter(operatorToken);
                var right = ParseConjunction();
                left = new BinaryFormulaNode(BinaryOperator.Disjunction, left, right);
            }

            return left;
        }

        private FormulaNode ParseConjunction()
        {
            var left = ParseUnary();

            while (Match(TokenType.And))
            {
                var operatorToken = Previous;
                EnsureFormulaStartsAfter(operatorToken);
                var right = ParseUnary();
                left = new BinaryFormulaNode(BinaryOperator.Conjunction, left, right);
            }

            return left;
        }

        private FormulaNode ParseUnary()
        {
            if (Match(TokenType.Not))
            {
                var operatorToken = Previous;
                EnsureFormulaStartsAfter(operatorToken);
                return new NegationNode(ParseUnary());
            }

            if (IsModal && Match(TokenType.Box))
            {
                var operatorToken = Previous;
                EnsureFormulaStartsAfter(operatorToken);
                return new ModalNode(ModalOperator.Box, ParseUnary());
            }

            if (IsModal && Match(TokenType.Diamond))
            {
                var operatorToken = Previous;
                EnsureFormulaStartsAfter(operatorToken);
                return new ModalNode(ModalOperator.Diamond, ParseUnary());
            }

            if (IsFirstOrderLike && (Check(TokenType.ForAll) || Check(TokenType.Exists)))
            {
                return ParseQuantifiedFormula();
            }

            if (Match(TokenType.LeftParen))
            {
                if (Check(TokenType.RightParen))
                {
                    throw Error("Expected a formula inside parentheses.", Current.Position);
                }

                var inner = ParseFormula();
                Expect(TokenType.RightParen);
                return inner;
            }

            return ParseAtomicFormula();
        }

        private FormulaNode ParseAtomicFormula()
        {
            if (!IsFirstOrderLike)
            {
                if (Match(TokenType.SentenceLetter))
                {
                    return new SentenceLetterNode(Previous.Text);
                }

                throw Error("Expected a sentence letter, negation, or '('.", Current.Position);
            }

            if (Check(TokenType.SentenceLetter))
            {
                return ParsePredicateApplication();
            }

            if (Check(TokenType.TermSymbol))
            {
                return ParseIdentityFormula();
            }

            throw Error("Expected a predicate, term, quantifier, negation, or '('.", Current.Position);
        }

        private FormulaNode ParseQuantifiedFormula()
        {
            var quantifierToken = Advance();
            var quantifier = quantifierToken.Type == TokenType.ForAll
                ? Quantifier.Universal
                : Quantifier.Existential;

            if (!Check(TokenType.TermSymbol))
            {
                throw Error($"Expected a variable after '{quantifierToken.Text}'.", Current.Position);
            }

            var variableToken = Advance();
            if (!firstOrderLanguage.IsVariable(variableToken.Text))
            {
                throw Error($"'{variableToken.Text}' is not a variable.", variableToken.Position);
            }

            EnsureFormulaStartsAfter(variableToken);
            return new QuantifiedFormulaNode(quantifier, variableToken.Text, ParseUnary());
        }

        private FormulaNode ParsePredicateApplication()
        {
            var predicateToken = Advance();

            if (!firstOrderLanguage.IsPredicate(predicateToken.Text))
            {
                throw Error(
                    $"Predicate '{predicateToken.Text}' is not in the first-order language.",
                    predicateToken.Position);
            }

            Expect(TokenType.LeftParen);

            if (Check(TokenType.RightParen))
            {
                throw Error("Expected at least one term in a predicate application.", Current.Position);
            }

            var arguments = new List<TermNode> { ParseTerm() };

            while (Match(TokenType.Comma))
            {
                if (Check(TokenType.RightParen))
                {
                    throw Error("Expected a term after ','.", Current.Position);
                }

                arguments.Add(ParseTerm());
            }

            Expect(TokenType.RightParen);
            CheckPredicateArity(predicateToken, arguments.Count);

            return new PredicateFormulaNode(predicateToken.Text, arguments);
        }

        private FormulaNode ParseIdentityFormula()
        {
            var left = ParseTerm();

            if (!Match(TokenType.Equal) && !Match(TokenType.NotEqual))
            {
                throw Error("Expected '=' or '≠' after term.", Current.Position);
            }

            var operatorToken = Previous;
            var identityOperator = operatorToken.Type == TokenType.Equal
                ? IdentityOperator.Identity
                : IdentityOperator.NonIdentity;

            if (!Check(TokenType.TermSymbol))
            {
                throw Error($"Expected a term after '{operatorToken.Text}'.", Current.Position);
            }

            return new IdentityFormulaNode(identityOperator, left, ParseTerm());
        }

        private TermNode ParseTerm()
        {
            if (!Match(TokenType.TermSymbol))
            {
                throw Error("Expected a term.", Current.Position);
            }

            var token = Previous;

            if (firstOrderLanguage.IsVariable(token.Text))
            {
                return new VariableTermNode(token.Text);
            }

            if (firstOrderLanguage.IsConstant(token.Text))
            {
                return new ConstantTermNode(token.Text);
            }

            throw Error($"Term symbol '{token.Text}' is not in the first-order language.", token.Position);
        }

        private void CheckPredicateArity(Token predicateToken, int arity)
        {
            if (_predicateArities.TryGetValue(predicateToken.Text, out var expectedArity))
            {
                if (expectedArity != arity)
                {
                    throw Error(
                        $"Predicate '{predicateToken.Text}' was previously used with arity {expectedArity}, but is used here with arity {arity}.",
                        predicateToken.Position);
                }

                return;
            }

            _predicateArities[predicateToken.Text] = arity;
        }

        private void Expect(TokenType type)
        {
            if (Match(type))
            {
                return;
            }

            if (type == TokenType.RightParen && Check(TokenType.EndOfInput))
            {
                throw Error("Expected ')' before end of input.", Current.Position);
            }

            throw Error($"Expected '{GetDisplayText(type)}'.", Current.Position);
        }

        private void EnsureFormulaStartsAfter(Token operatorToken)
        {
            if (!StartsFormula(Current.Type))
            {
                throw Error($"Expected a formula after '{operatorToken.Text}'.", Current.Position);
            }
        }

        private bool Match(TokenType type)
        {
            if (!Check(type))
            {
                return false;
            }

            Advance();
            return true;
        }

        private bool Check(TokenType type)
        {
            return Current.Type == type;
        }

        private Token Advance()
        {
            if (!Check(TokenType.EndOfInput))
            {
                _current++;
            }

            return Previous;
        }

        private Token Current => tokens[_current];

        private Token Previous => tokens[_current - 1];

        private bool IsFirstOrderLike => logicSystem is LogicSystem.FirstOrderWithIdentity or LogicSystem.Modal;

        private bool IsModal => logicSystem == LogicSystem.Modal;

        private bool StartsFormula(TokenType type)
        {
            if (type is TokenType.Not or TokenType.LeftParen or TokenType.SentenceLetter)
            {
                return true;
            }

            if (IsModal && type is TokenType.Box or TokenType.Diamond)
            {
                return true;
            }

            return IsFirstOrderLike && type is (TokenType.ForAll or TokenType.Exists or TokenType.TermSymbol);
        }

        private static string GetDisplayText(TokenType type)
        {
            return type switch
            {
                TokenType.LeftParen => "(",
                TokenType.RightParen => ")",
                TokenType.Comma => ",",
                TokenType.Equal => "=",
                TokenType.NotEqual => "≠",
                TokenType.Box => "□",
                TokenType.Diamond => "◇",
                _ => type.ToString()
            };
        }

        private static IReadOnlyCollection<string> CollectFreeVariables(FormulaNode root)
        {
            var variables = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            VisitFormula(root, new HashSet<string>(StringComparer.Ordinal), variables, seen);
            return variables;
        }

        private static void VisitFormula(
            FormulaNode node,
            IReadOnlySet<string> boundVariables,
            ICollection<string> variables,
            ISet<string> seen)
        {
            switch (node)
            {
                case SentenceLetterNode:
                    return;
                case NegationNode negation:
                    VisitFormula(negation.Operand, boundVariables, variables, seen);
                    return;
                case ModalNode modal:
                    VisitFormula(modal.Operand, boundVariables, variables, seen);
                    return;
                case BinaryFormulaNode binary:
                    VisitFormula(binary.Left, boundVariables, variables, seen);
                    VisitFormula(binary.Right, boundVariables, variables, seen);
                    return;
                case PredicateFormulaNode predicate:
                    foreach (var argument in predicate.Arguments)
                    {
                        VisitTerm(argument, boundVariables, variables, seen);
                    }

                    return;
                case IdentityFormulaNode identity:
                    VisitTerm(identity.Left, boundVariables, variables, seen);
                    VisitTerm(identity.Right, boundVariables, variables, seen);
                    return;
                case QuantifiedFormulaNode quantified:
                    var nextBoundVariables = new HashSet<string>(boundVariables, StringComparer.Ordinal)
                    {
                        quantified.Variable
                    };
                    VisitFormula(quantified.Operand, nextBoundVariables, variables, seen);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(node), node, "Unknown formula node.");
            }
        }

        private static void VisitTerm(
            TermNode node,
            IReadOnlySet<string> boundVariables,
            ICollection<string> variables,
            ISet<string> seen)
        {
            if (node is not VariableTermNode variable || boundVariables.Contains(variable.Name))
            {
                return;
            }

            if (seen.Add(variable.Name))
            {
                variables.Add(variable.Name);
            }
        }

        private static ParseException Error(string message, int position)
        {
            return new ParseException(message, position);
        }

        private ParseResult Failure(string message, int position)
        {
            return ParseResult.Failure(message, position, normalizedFormula);
        }
    }

    private sealed record TokenizationResult(
        IReadOnlyList<Token> Tokens,
        string? ErrorMessage,
        int? ErrorPosition)
    {
        public static TokenizationResult Success(IReadOnlyList<Token> tokens)
        {
            return new TokenizationResult(tokens, null, null);
        }

        public static TokenizationResult Failure(string message, int errorPosition)
        {
            return new TokenizationResult(Array.Empty<Token>(), message, errorPosition);
        }
    }

    private sealed class ParseException(string message, int position) : Exception(message)
    {
        public int Position { get; } = position;
    }
}
