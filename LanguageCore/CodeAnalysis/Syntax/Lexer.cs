using System;
using System.Linq;

namespace LanguageCore.CodeAnalysis.Syntax
{
    internal sealed class Lexer
    {
        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();

        private readonly string sourceText;
        private int position;
        private int start;
        private SyntaxKind kind;
        private object value;

        private static readonly (SyntaxKind kind, string text)[] operatorKindTexts;

        private char Current => Peek(0);

        static Lexer()
        {
            var operatorKinds = Enum.GetValues(typeof(SyntaxKind)).Cast<SyntaxKind>().ToArray();
            var operatorTexts = operatorKinds.Select(SyntaxFacts.GetText).ToArray();
            operatorKindTexts = operatorKinds
                .Zip(operatorTexts, (k, t) => (k, t))
                .Where(x => x.t != null)
                .OrderByDescending(x => x.t.Length)
                .ToArray();
        }

        public Lexer(string sourceText)
        {
            this.sourceText = sourceText;
        }

        public SyntaxToken Lex()
        {
            start = position;
            kind = SyntaxKind.BadToken;
            value = null;

            if (Current == '\0')
            {
                kind = SyntaxKind.EndOfFileToken;
            }
            else if (char.IsLetter(Current) || Current == '_')
            {
                ReadIdentifierOrKeyword();
            }
            else if (char.IsDigit(Current))
            {
                ReadNumberToken();
            }
            else if (char.IsWhiteSpace(Current))
            {
                ReadWhiteSpace();
            }
            else
            {
                var operatorKind = operatorKindTexts.FirstOrDefault(x => TryMatchString(x.text));

                if (operatorKind.Equals(default))
                {
                    Diagnostics.ReportBadCharacter(position, Current);
                    position++;
                }
                else
                {
                    kind = operatorKind.kind;
                    position += operatorKind.text.Length;
                }
            }

            var length = position - start;
            var text = SyntaxFacts.GetText(kind) ?? sourceText.Substring(start, length);

            return new SyntaxToken(kind, start, text, value);
        }

        private bool TryMatchString(string match)
        {
            for (var i = 0; i < match.Length; i++)
            {
                if (Peek(i) != match[i])
                {
                    return false;
                }
            }

            return true;
        }

        private char Peek(int offset)
        {
            var index = position + offset;

            return index >= sourceText.Length ? '\0' : sourceText[index];
        }

        private void ReadWhiteSpace()
        {
            while (char.IsWhiteSpace(Current))
                position++;

            kind = SyntaxKind.WhitespaceToken;
        }

        private void ReadNumberToken()
        {
            while (char.IsDigit(Current))
                position++;

            var length = position - start;
            var text = sourceText.Substring(start, length);
            if (!int.TryParse(text, out var result))
                Diagnostics.ReportInvalidNumber(new TextSpan(start, length), sourceText, typeof(int));

            value = result;
            kind = SyntaxKind.NumberToken;
        }

        private void ReadIdentifierOrKeyword()
        {
            while (char.IsLetter(Current))
                position++;

            var length = position - start;
            var text = sourceText.Substring(start, length);
            kind = SyntaxFacts.GetKeywordKind(text);
        }
    }
}
