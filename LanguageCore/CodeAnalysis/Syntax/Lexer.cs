namespace LanguageCore.CodeAnalysis.Syntax
{
    internal sealed class Lexer
    {
        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();

        private readonly string sourceText;
        private int position;

        private char Current => Peek(0);

        private char Lookahead => Peek(1);

        public Lexer(string sourceText)
        {
            this.sourceText = sourceText;
        }

        public SyntaxToken Lex()
        {
            if (position >= sourceText.Length)
            {
                return new SyntaxToken(SyntaxKind.EndOfFileToken, position, "\0", null);
            }

            var start = position;

            if (char.IsDigit(Current))
            {
                while (char.IsDigit(Current)) Next();

                var length = position - start;
                var text = sourceText.Substring(start, length);
                if (!int.TryParse(text, out var value))
                {
                    Diagnostics.ReportInvalidNumber(new TextSpan(start, length), sourceText, typeof(int));
                }

                return new SyntaxToken(SyntaxKind.NumberToken, start, text, value);
            }

            if (char.IsWhiteSpace(Current))
            {
                while (char.IsWhiteSpace(Current)) Next();

                var length = position - start;
                var text = sourceText.Substring(start, length);
                return new SyntaxToken(SyntaxKind.WhitespaceToken, start, text, null);
            }

            if (char.IsLetter(Current))
            {
                while (char.IsLetter(Current)) Next();

                var length = position - start;
                var text = sourceText.Substring(start, length);
                var kind = SyntaxFacts.GetKeywordKind(text);
                return new SyntaxToken(kind, start, text, null);
            }

            switch (Current)
            {
                case '+':
                    return new SyntaxToken(SyntaxKind.PlusToken, position++, "+", null);
                case '-':
                    return new SyntaxToken(SyntaxKind.MinusToken, position++, "-", null);
                case '*':
                    return new SyntaxToken(SyntaxKind.StarToken, position++, "*", null);
                case '/':
                    return new SyntaxToken(SyntaxKind.SlashToken, position++, "/", null);
                case '(':
                    return new SyntaxToken(SyntaxKind.OpenParenthesisToken, position++, "(", null);
                case ')':
                    return new SyntaxToken(SyntaxKind.CloseParenthesisToken, position++, ")", null);
                case '&':
                    if (Lookahead == '&')
                    {
                        position += 2;
                        return new SyntaxToken(SyntaxKind.AmpersandAmpersandToken, start, "&&", null);
                    }

                    break;
                case '|':
                    if (Lookahead == '|')
                    {
                        position += 2;
                        return new SyntaxToken(SyntaxKind.PipePipeToken, start, "||", null);
                    }

                    break;
                case '=':
                    if (Lookahead == '=')
                    {
                        position += 2;
                        return new SyntaxToken(SyntaxKind.EqualsEqualsToken, start, "==", null);
                    }

                    break;
                case '!':
                    if (Lookahead == '=')
                    {
                        position += 2;
                        return new SyntaxToken(SyntaxKind.BangEqualsToken, start, "!=", null);
                    }
                    else
                    {
                        position += 1;
                        return new SyntaxToken(SyntaxKind.BangToken, start, "!", null);
                    }
            }

            Diagnostics.ReportBadCharacter(position, Current);
            return new SyntaxToken(SyntaxKind.BadToken, position++, sourceText.Substring(position - 1, 1), null);
        }

        private char Peek(int offset)
        {
            var index = position + offset;

            return index >= sourceText.Length ? '\0' : sourceText[index];
        }

        private void Next()
        {
            position++;
        }
    }
}
