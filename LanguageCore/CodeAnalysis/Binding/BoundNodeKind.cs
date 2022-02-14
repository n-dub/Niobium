namespace LanguageCore.CodeAnalysis.Binding
{
    public enum BoundNodeKind
    {
        // Statement
        BlockStatement,
        VariableDeclarationStatement,
        ExpressionStatement,

        // Expressions
        LiteralExpression,
        VariableExpression,
        AssignmentExpression,
        UnaryExpression,
        BinaryExpression
    }
}
