﻿namespace LanguageCore.CodeAnalysis.Syntax
{
    public enum SyntaxKind
    {
        // Tokens
        EndOfFileToken,
        BadTokenTrivia,
        WhitespaceTrivia,
        SingleLineCommentTrivia,
        MultiLineCommentTrivia,
        NumberToken,
        StringToken,
        PlusToken,
        MinusToken,
        StarToken,
        SlashToken,
        BangToken,
        EqualsToken,
        TildeToken,
        HatToken,
        ArrowToken,
        AmpersandToken,
        AmpersandAmpersandToken,
        PipeToken,
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
        ParenthesizedExpression,
        AssignmentExpression,
        CallExpression
    }
}
