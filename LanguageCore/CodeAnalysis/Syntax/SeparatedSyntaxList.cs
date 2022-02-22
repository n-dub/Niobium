using System.Collections;
using System.Collections.Generic;

namespace LanguageCore.CodeAnalysis.Syntax
{
    public abstract class SeparatedSyntaxList
    {
        public abstract IReadOnlyList<SyntaxNode> GetWithSeparators();
    }

    public sealed class SeparatedSyntaxList<T> : SeparatedSyntaxList, IEnumerable<T>
        where T : SyntaxNode
    {
        public int Count => (nodesAndSeparators.Count + 1) / 2;

        public T this[int index] => (T) nodesAndSeparators[index * 2];
        private readonly IReadOnlyList<SyntaxNode> nodesAndSeparators;

        public SeparatedSyntaxList(IReadOnlyList<SyntaxNode> nodesAndSeparators)
        {
            this.nodesAndSeparators = nodesAndSeparators;
        }

        public SyntaxToken GetSeparator(int index)
        {
            if (index == Count - 1)
            {
                return null;
            }

            return (SyntaxToken) nodesAndSeparators[index * 2 + 1];
        }

        public override IReadOnlyList<SyntaxNode> GetWithSeparators()
        {
            return nodesAndSeparators;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
