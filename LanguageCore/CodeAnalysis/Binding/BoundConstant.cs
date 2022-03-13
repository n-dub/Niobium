namespace LanguageCore.CodeAnalysis.Binding
{
    public sealed class BoundConstant
    {
        public object Value { get; }

        public BoundConstant(object value)
        {
            Value = value;
        }
    }
}
