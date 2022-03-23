using LanguageCore.CodeAnalysis.Symbols;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class BoundVariableDeclarationStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.VariableDeclarationStatement;
        public VariableSymbol Variable { get; }
        public BoundExpression Initializer { get; }

        public BoundVariableDeclarationStatement(VariableSymbol variable, BoundExpression initializer)
        {
            Variable = variable;
            Initializer = initializer;
        }
    }

    internal static partial class BoundNodeFactory
    {
        public static BoundVariableExpression Variable(BoundVariableDeclarationStatement variable)
        {
            return new BoundVariableExpression(variable.Variable);
        }

        public static BoundVariableDeclarationStatement VariableDeclaration(VariableSymbol symbol,
            BoundExpression initializer)
        {
            return new BoundVariableDeclarationStatement(symbol, initializer);
        }

        public static BoundVariableDeclarationStatement VariableDeclaration(string name, BoundExpression initializer)
            => VariableDeclarationInternal(name, initializer, isReadOnly: false);

        public static BoundVariableDeclarationStatement ConstantDeclaration(string name, BoundExpression initializer)
            => VariableDeclarationInternal(name, initializer, isReadOnly: true);

        private static BoundVariableDeclarationStatement VariableDeclarationInternal(string name,
            BoundExpression initializer, bool isReadOnly)
        {
            var local = new LocalVariableSymbol(name, isReadOnly, initializer.Type, initializer.ConstantValue);
            return new BoundVariableDeclarationStatement(local, initializer);
        }
    }
}
