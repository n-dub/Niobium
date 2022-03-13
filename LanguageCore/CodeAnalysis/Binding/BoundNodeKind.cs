namespace LanguageCore.CodeAnalysis.Binding
{
    public enum BoundNodeKind
    {
        // Statement
        BlockStatement,
        NopStatement,
        VariableDeclarationStatement,
        IfStatement,
        WhileStatement,
        RepeatWhileStatement,
        ForStatement,
        LabelStatement,
        GotoStatement,
        ConditionalGotoStatement,
        ReturnStatement,
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
