using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class Conversion
    {
        public static readonly Conversion None = new Conversion(false, false, false);
        public static readonly Conversion Identity = new Conversion(true, true, true);
        public static readonly Conversion Implicit = new Conversion(true, false, true);
        public static readonly Conversion Explicit = new Conversion(true, false, false);

        public bool Exists { get; }
        public bool IsIdentity { get; }
        public bool IsImplicit { get; }
        public bool IsExplicit => Exists && !IsImplicit;

        private Conversion(bool exists, bool isIdentity, bool isImplicit)
        {
            Exists = exists;
            IsIdentity = isIdentity;
            IsImplicit = isImplicit;
        }

        public static Conversion Classify(TypeSymbol from, TypeSymbol to)
        {
            if (from == to)
            {
                return Identity;
            }

            if (from == TypeSymbol.Bool || from == TypeSymbol.Int32)
            {
                if (to == TypeSymbol.String)
                {
                    return Explicit;
                }
            }

            if (from == TypeSymbol.String)
            {
                if (to == TypeSymbol.Bool || to == TypeSymbol.Int32)
                {
                    return Explicit;
                }
            }

            return None;
        }
    }
}
