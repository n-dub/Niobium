using System;
using System.Linq;
using System.Text;
using LanguageCore.CodeAnalysis.Symbols;
using LanguageCore.CodeAnalysis.Text;
using Utilities;

namespace LanguageCore.CodeAnalysis.Syntax
{
    internal sealed class Lexer
    {
        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();

        private readonly SyntaxTree syntaxTree;
        private readonly SourceText sourceText;
        private int position;
        private int start;
        private SyntaxKind kind;
        private object value;

        private static readonly (SyntaxKind kind, string text)[] operatorKindTexts;

        private char Current => Peek(0);
        private char Lookahead => Peek(1);

        public Lexer(SyntaxTree syntaxTree)
        {
            this.syntaxTree = syntaxTree;
            sourceText = syntaxTree.SourceText;
        }

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

        public SyntaxToken Lex()
        {
            start = position;
            kind = SyntaxKind.BadTokenTrivia;
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
            else if (Current == '"')
            {
                ReadString();
            }
            else if (Current == '/' && Lookahead == '/')
            {
                ReadSingleLineComment();
            }
            else if (Current == '/' && Lookahead == '*')
            {
                ReadMultiLineComment();
            }
            else
            {
                var operatorKind = operatorKindTexts.FirstOrDefault(x => TryMatchString(x.text));

                if (operatorKind.Equals(default))
                {
                    var span = new TextSpan(position, 1);
                    var location = new TextLocation(sourceText, span);
                    Diagnostics.ReportBadCharacter(location, Current);
                    position++;
                }
                else
                {
                    kind = operatorKind.kind;
                    position += operatorKind.text.Length;
                }
            }

            var length = position - start;
            var text = SyntaxFacts.GetText(kind) ?? sourceText.ToString(start, length);

            return new SyntaxToken(syntaxTree, kind, start, text, value);
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
            {
                position++;
            }

            kind = SyntaxKind.WhitespaceTrivia;
        }

        private void ReadNumberToken()
        {
            while (char.IsDigit(Current))
            {
                position++;
            }

            var length = position - start;
            var text = sourceText.ToString(start, length);
            if (!int.TryParse(text, out var result))
            {
                var span = new TextSpan(start, length);
                var location = new TextLocation(sourceText, span);
                Diagnostics.ReportInvalidNumber(location, text, TypeSymbol.Int32);
            }

            value = result;
            kind = SyntaxKind.NumberToken;
        }

        private void ReadIdentifierOrKeyword()
        {
            while (CharUtils.IsValidIdentifier(Current))
            {
                position++;
            }

            var length = position - start;
            var text = sourceText.ToString(start, length);
            kind = SyntaxFacts.GetKeywordKind(text);
        }

        private void ReadString()
        {
            position++;

            var sb = new StringBuilder();

            while (true)
            {
                if ("\0\r\n".Contains(Current))
                {
                    var span = new TextSpan(start, 1);
                    var location = new TextLocation(sourceText, span);
                    Diagnostics.ReportUnterminatedString(location);
                    break;
                }

                if (Current == '"')
                {
                    position++;
                    break;
                }

                if (Current == '\\')
                {
                    if (TryReadEscapedCharacter(out var escaped))
                    {
                        sb.Append(escaped);
                    }

                    continue;
                }

                sb.Append(Current);
                position++;
            }

            kind = SyntaxKind.StringToken;
            value = sb.ToString();
        }

        private void ReadSingleLineComment()
        {
            position += 2;

            while (true)
            {
                if ("\0\r\n".Contains(Current))
                {
                    break;
                }

                position++;
            }

            kind = SyntaxKind.SingleLineCommentTrivia;
        }

        private void ReadMultiLineComment()
        {
            position += 2;

            while (true)
            {
                if (Current == '\0')
                {
                    var span = new TextSpan(start, 2);
                    var location = new TextLocation(sourceText, span);
                    Diagnostics.ReportUnterminatedMultiLineComment(location);
                    break;
                }

                if (Current == '*' && Lookahead == '/')
                {
                    position += 2;
                    break;
                }

                position++;
            }

            kind = SyntaxKind.MultiLineCommentTrivia;
        }

        private bool TryReadEscapedCharacter(out char character)
        {
            position++;

            switch (Current)
            {
                case '"':
                    character = '\"';
                    break;
                case 'r':
                    character = '\r';
                    break;
                case 'n':
                    character = '\n';
                    break;
                case 't':
                    character = '\t';
                    break;
                case '\\':
                    character = '\\';
                    break;
                default:
                    var span = new TextSpan(position - 1, 2);
                    var location = new TextLocation(sourceText, span);
                    Diagnostics.ReportInvalidEscapedCharacter(location, Current);
                    character = '\0';
                    return false;
            }

            position++;
            return true;
        }
    }
}
