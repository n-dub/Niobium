using System.Collections.Generic;

namespace LanguageCore.CodeAnalysis.Text
{
    public sealed class SourceText
    {
        public IReadOnlyList<TextLine> Lines { get; }

        public char this[int index] => text[index];

        public int Length => text.Length;
        private readonly string text;

        private SourceText(string text)
        {
            this.text = text;
            Lines = ParseLines(this, text);
        }

        public int GetLineIndex(int position)
        {
            var lower = 0;
            var upper = Lines.Count - 1;

            while (lower <= upper)
            {
                var index = lower + (upper - lower) / 2;
                var start = Lines[index].Start;

                if (position == start)
                {
                    return index;
                }

                if (start > position)
                {
                    upper = index - 1;
                }
                else
                {
                    lower = index + 1;
                }
            }

            return lower - 1;
        }

        public static SourceText From(string text)
        {
            return new SourceText(text);
        }

        public override string ToString()
        {
            return text;
        }

        public string ToString(int start, int length)
        {
            return text.Substring(start, length);
        }

        public string ToString(TextSpan span)
        {
            return ToString(span.Start, span.Length);
        }

        private static IReadOnlyList<TextLine> ParseLines(SourceText sourceText, string text)
        {
            var result = new List<TextLine>();

            var position = 0;
            var lineStart = 0;

            while (position < text.Length)
            {
                var lineBreakWidth = GetLineBreakWidth(text, position);

                if (lineBreakWidth == 0)
                {
                    position++;
                }
                else
                {
                    AddLine(result, sourceText, position, lineStart, lineBreakWidth);

                    position += lineBreakWidth;
                    lineStart = position;
                }
            }

            if (position > lineStart)
            {
                AddLine(result, sourceText, position, lineStart, 0);
            }

            return result;
        }

        private static void AddLine(ICollection<TextLine> result, SourceText sourceText, int position, int lineStart,
            int lineBreakWidth)
        {
            var lineLength = position - lineStart;
            var lineLengthIncludingLineBreak = lineLength + lineBreakWidth;
            var line = new TextLine(sourceText, lineStart, lineLength, lineLengthIncludingLineBreak);
            result.Add(line);
        }

        private static int GetLineBreakWidth(string text, int position)
        {
            var c = text[position];
            var l = position + 1 >= text.Length ? '\0' : text[position + 1];

            if (c == '\r' && l == '\n')
            {
                return 2;
            }

            if (c == '\r' || c == '\n')
            {
                return 1;
            }

            return 0;
        }
    }
}
