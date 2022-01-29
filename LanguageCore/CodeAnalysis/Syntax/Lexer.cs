using System.Collections.Generic;

namespace LanguageCore.CodeAnalysis.Syntax
{
    internal sealed class Lexer
    {
        public IEnumerable<string> Diagnostics => diagnostics;
        private readonly string sourceText;
        private int position;
        private readonly List<string> diagnostics = new List<string>();

        private char Current => Peek(0);

        private char Lookahead => Peek(1);

        public Lexer(string sourceText)
        {
            this.sourceText = sourceText;
        }

        public SyntaxToken Lex()
        {
            if (position >= sourceText.Length) return new SyntaxToken(SyntaxKind.EndOfFileToken, position, "\0", null);

            if (char.IsDigit(Current))
            {
                var start = position;

                while (char.IsDigit(Current)) Next();

                var length = position - start;
                var text = sourceText.Substring(start, length);
                if (!int.TryParse(text, out var value)) diagnostics.Add($"The number {sourceText} isn't valid Int32.");

                return new SyntaxToken(SyntaxKind.NumberToken, start, text, value);
            }

            if (char.IsWhiteSpace(Current))
            {
                var start = position;

                while (char.IsWhiteSpace(Current)) Next();

                var length = position - start;
                var text = sourceText.Substring(start, length);
                return new SyntaxToken(SyntaxKind.WhitespaceToken, start, text, null);
            }

            if (char.IsLetter(Current))
            {
                var start = position;

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
                        return new SyntaxToken(SyntaxKind.AmpersandAmpersandToken, position += 2, "&&", null);
                    break;
                case '|':
                    if (Lookahead == '|')
                        return new SyntaxToken(SyntaxKind.PipePipeToken, position += 2, "||", null);
                    break;
                case '=':
                    if (Lookahead == '=')
                        return new SyntaxToken(SyntaxKind.EqualsEqualsToken, position += 2, "==", null);
                    break;
                case '!':
                    return Lookahead == '='
                        ? new SyntaxToken(SyntaxKind.BangEqualsToken, position += 2, "!=", null)
                        : new SyntaxToken(SyntaxKind.BangToken, position++, "!", null);
            }

            diagnostics.Add($"ERROR: bad character input: '{Current}'");
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
