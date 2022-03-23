namespace LanguageCore.CodeAnalysis.Syntax
{
    public enum SyntaxKind
    {
        BadToken,

        // Trivia
        SkippedTextTrivia,
        LineBreakTrivia,
        WhitespaceTrivia,
        SingleLineCommentTrivia,
        MultiLineCommentTrivia,

        // Tokens
        EndOfFileToken,
        NumberToken,
        StringToken,
        PlusToken,
        PlusEqualsToken,
        MinusToken,
        MinusEqualsToken,
        StarToken,
        StarEqualsToken,
        SlashToken,
        SlashEqualsToken,
        BangToken,
        EqualsToken,
        TildeToken,
        HatToken,
        HatEqualsToken,
        ArrowToken,
        AmpersandToken,
        AmpersandEqualsToken,
        AmpersandAmpersandToken,
        PipeToken,
        PipeEqualsToken,
        PipePipeToken,
        EqualsEqualsToken,
        BangEqualsToken,
        LessToken,
        LessOrEqualsToken,
        GreaterToken,
        GreaterOrEqualsToken,
        OpenParenthesisToken,
        CloseParenthesisToken,
        OpenBraceToken,
        CloseBraceToken,
        ColonToken,
        CommaToken,
        IdentifierToken,

        // Keywords
        BreakKeyword,
        ContinueKeyword,
        ElseKeyword,
        FalseKeyword,
        ForKeyword,
        FuncKeyword,
        IfKeyword,
        InKeyword,
        LetKeyword,
        RepeatKeyword,
        ReturnKeyword,
        TrueKeyword,
        VarKeyword,
        WhileKeyword,

        // Nodes
        CompilationUnit,
        FunctionDeclaration,
        GlobalStatement,
        Parameter,
        TypeClause,
        ElseClause,

        // Statements
        BlockStatement,
        VariableDeclarationStatement,
        IfStatement,
        WhileStatement,
        RepeatWhileStatement,
        ForStatement,
        BreakStatement,
        ContinueStatement,
        ReturnStatement,
        ExpressionStatement,

        // Expressions
        LiteralExpression,
        NameExpression,
        UnaryExpression,
        BinaryExpression,
        CompoundAssignmentExpression,
        ParenthesizedExpression,
        AssignmentExpression,
        CallExpression
    }
}
