namespace LanguageCore.CodeAnalysis.Binding
{
    public enum BoundNodeKind
    {
        // Statement
        BlockStatement,
        VariableDeclarationStatement,
        IfStatement,
        WhileStatement,
        ForStatement,
        ExpressionStatement,

        // Expressions
        LiteralExpression,
        VariableExpression,
        AssignmentExpression,
        UnaryExpression,
        BinaryExpression
    }
}
