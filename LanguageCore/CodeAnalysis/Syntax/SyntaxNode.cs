using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LanguageCore.CodeAnalysis.Text;

namespace LanguageCore.CodeAnalysis.Syntax
{
    public abstract class SyntaxNode
    {
        public SyntaxTree SyntaxTree { get; }
        public abstract SyntaxKind Kind { get; }

        public TextLocation Location => new TextLocation(SyntaxTree.SourceText, Span);

        public virtual TextSpan Span
        {
            get
            {
                var first = GetChildren().First().Span;
                var last = GetChildren().Last().Span;
                return TextSpan.FromBounds(first.Start, last.End);
            }
        }

        public virtual TextSpan FullSpan
        {
            get
            {
                var first = GetChildren().First().FullSpan;
                var last = GetChildren().Last().FullSpan;
                return TextSpan.FromBounds(first.Start, last.End);
            }
        }

        protected SyntaxNode(SyntaxTree syntaxTree)
        {
            SyntaxTree = syntaxTree;
        }

        public IEnumerable<SyntaxNode> GetChildren()
        {
            var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (typeof(SyntaxNode).IsAssignableFrom(property.PropertyType))
                {
                    var child = (SyntaxNode) property.GetValue(this);
                    if (child != null)
                    {
                        yield return child;
                    }
                }
                else if (typeof(SeparatedSyntaxList).IsAssignableFrom(property.PropertyType))
                {
                    var separatedSyntaxList = (SeparatedSyntaxList) property.GetValue(this);
                    foreach (var child in separatedSyntaxList.GetWithSeparators())
                    {
                        yield return child;
                    }
                }
                else if (typeof(IEnumerable<SyntaxNode>).IsAssignableFrom(property.PropertyType))
                {
                    var children = (IEnumerable<SyntaxNode>) property.GetValue(this);
                    foreach (var child in children.Where(x => x != null))
                    {
                        yield return child;
                    }
                }
            }
        }

        public SyntaxToken GetLastToken()
        {
            return GetLastToken(this);
        }

        public void WriteTo(TextWriter writer)
        {
            PrettyPrint(writer, this);
        }

        public override string ToString()
        {
            using (var writer = new StringWriter())
            {
                WriteTo(writer);
                return writer.ToString();
            }
        }

        private static SyntaxToken GetLastToken(SyntaxNode node)
        {
            while (true)
            {
                if (node is SyntaxToken token)
                {
                    return token;
                }

                node = node.GetChildren().Last();
            }
        }

        private static void PrettyPrint(TextWriter writer, SyntaxNode node, string indent = "", bool isLast = true)
        {
            if (node is null)
            {
                return;
            }

            var writerIsConsole = writer == Console.Out;
            var token = node as SyntaxToken;

            if (token != null)
            {
                foreach (var trivia in token.LeadingTrivia)
                {
                    if (writerIsConsole)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                    }

                    writer.Write(indent);
                    writer.Write("├──");

                    if (writerIsConsole)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                    }

                    writer.WriteLine($"L: {trivia.Kind}");
                }
            }

            var hasTrailingTrivia = token != null && token.TrailingTrivia.Any();
            var tokenMarker = !hasTrailingTrivia && isLast ? "└──" : "├──";

            if (writerIsConsole)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
            }

            writer.Write(indent);
            writer.Write(tokenMarker);

            if (writerIsConsole)
            {
                Console.ForegroundColor = node is SyntaxToken ? ConsoleColor.Blue : ConsoleColor.Cyan;
            }

            writer.Write(node.Kind);

            if (token?.Value != null)
            {
                writer.Write(" ");
                writer.Write(token.Value);
            }

            if (writerIsConsole)
            {
                Console.ResetColor();
            }

            writer.WriteLine();

            if (token != null)
            {
                foreach (var trivia in token.TrailingTrivia)
                {
                    var isLastTrailingTrivia = trivia == token.TrailingTrivia.Last();
                    var triviaMarker = isLast && isLastTrailingTrivia ? "└──" : "├──";

                    if (writerIsConsole)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                    }

                    writer.Write(indent);
                    writer.Write(triviaMarker);

                    if (writerIsConsole)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                    }

                    writer.WriteLine($"T: {trivia.Kind}");
                }
            }

            indent += isLast ? "   " : "│  ";
            var lastChild = node.GetChildren().LastOrDefault();

            foreach (var child in node.GetChildren())
            {
                PrettyPrint(writer, child, indent, child == lastChild);
            }
        }
    }
}
