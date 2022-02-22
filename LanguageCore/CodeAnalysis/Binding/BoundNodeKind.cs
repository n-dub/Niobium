namespace LanguageCore.CodeAnalysis.Binding
{
    public enum BoundNodeKind
    {
        // Statement
        BlockStatement,
        VariableDeclarationStatement,
        IfStatement,
        WhileStatement,
        RepeatWhileStatement,
        ForStatement,
        LabelStatement,
        GotoStatement,
        ConditionalGotoStatement,
        ExpressionStatement,

        // Expressions
        ErrorExpression,
        LiteralExpression,
        VariableExpression,
        AssignmentExpression,
        UnaryExpression,
        BinaryExpression,
        CallExpression,
        ConversionExpression
    }
}
