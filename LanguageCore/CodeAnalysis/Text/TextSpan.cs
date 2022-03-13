namespace LanguageCore.CodeAnalysis.Text
{
    public class TextSpan
    {
        public int Start { get; }
        public int Length { get; }
        public int End => Start + Length;

        public TextSpan(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public static TextSpan FromBounds(int start, int end)
        {
            var length = end - start;
            return new TextSpan(start, length);
        }

        public bool OverlapsWith(TextSpan span)
        {
            return Start < span.End && End > span.Start;
        }

        public override string ToString()
        {
            return $"{Start}..{End}";
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((TextSpan) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Start * 397) ^ Length;
            }
        }

        private bool Equals(TextSpan other)
        {
            return Start == other.Start && Length == other.Length;
        }
    }
}
