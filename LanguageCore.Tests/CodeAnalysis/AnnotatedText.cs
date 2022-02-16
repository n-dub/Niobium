using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LanguageCore.CodeAnalysis.Text;

namespace LanguageCore.Tests.CodeAnalysis
{
    internal sealed class AnnotatedText
    {
        public string Text { get; }
        public IReadOnlyList<TextSpan> Spans { get; }

        public AnnotatedText(string text, IReadOnlyList<TextSpan> spans)
        {
            Text = text;
            Spans = spans;
        }

        public static AnnotatedText Parse(string text)
        {
            text = UnIndent(text);

            var textBuilder = new StringBuilder();
            var spanBuilder = new List<TextSpan>();
            var startStack = new Stack<int>();

            var position = 0;

            foreach (var c in text)
            {
                switch (c)
                {
                    case '[':
                        startStack.Push(position);
                        break;
                    case ']' when startStack.Count == 0:
                        throw new ArgumentException("Too many ']' in text", nameof(text));
                    case ']':
                    {
                        var start = startStack.Pop();
                        var span = TextSpan.FromBounds(start, position);
                        spanBuilder.Add(span);
                        break;
                    }
                    default:
                        position++;
                        textBuilder.Append(c);
                        break;
                }
            }

            if (startStack.Count != 0)
            {
                throw new ArgumentException("Missing ']' in text", nameof(text));
            }

            return new AnnotatedText(textBuilder.ToString(), spanBuilder.ToArray());
        }

        public static string[] UnIndentLines(string text)
        {
            var lines = new List<string>();

            using (var reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }

            var minIndentation = int.MaxValue;
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                if (line.Trim().Length == 0)
                {
                    lines[i] = string.Empty;
                    continue;
                }

                var indentation = line.Length - line.TrimStart().Length;
                minIndentation = Math.Min(minIndentation, indentation);
            }

            for (var i = 0; i < lines.Count; i++)
            {
                if (lines[i].Length == 0)
                {
                    continue;
                }

                lines[i] = lines[i].Substring(minIndentation);
            }

            while (lines.Count > 0 && lines[0].Length == 0)
            {
                lines.RemoveAt(0);
            }

            while (lines.Count > 0 && lines[lines.Count - 1].Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            return lines.ToArray();
        }

        private static string UnIndent(string text)
        {
            var lines = UnIndentLines(text);
            return string.Join(Environment.NewLine, lines);
        }
    }
}
