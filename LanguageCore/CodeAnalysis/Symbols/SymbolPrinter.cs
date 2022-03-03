using System;
using System.IO;
using LanguageCore.CodeAnalysis.IO;
using LanguageCore.CodeAnalysis.Syntax;

namespace LanguageCore.CodeAnalysis.Symbols
{
    internal static class SymbolPrinter
    {
        public static void WriteTo(Symbol symbol, TextWriter writer)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Function:
                    WriteFunctionTo((FunctionSymbol) symbol, writer);
                    break;
                case SymbolKind.GlobalVariable:
                    WriteGlobalVariableTo((GlobalVariableSymbol) symbol, writer);
                    break;
                case SymbolKind.LocalVariable:
                    WriteLocalVariableTo((LocalVariableSymbol) symbol, writer);
                    break;
                case SymbolKind.Parameter:
                    WriteParameterTo((ParameterSymbol) symbol, writer);
                    break;
                case SymbolKind.Type:
                    WriteTypeTo((TypeSymbol) symbol, writer);
                    break;
                default:
                    throw new Exception($"Unexpected symbol: {symbol.Kind}");
            }
        }

        private static void WriteFunctionTo(FunctionSymbol symbol, TextWriter writer)
        {
            writer.WriteKeyword(SyntaxKind.FuncKeyword);
            writer.WriteSpace();
            writer.WriteIdentifier(symbol.Name);
            writer.WritePunctuation(SyntaxKind.OpenParenthesisToken);

            for (var i = 0; i < symbol.Parameters.Count; i++)
            {
                if (i > 0)
                {
                    writer.WritePunctuation(SyntaxKind.CommaToken);
                    writer.WriteSpace();
                }

                symbol.Parameters[i].WriteTo(writer);
            }

            writer.WritePunctuation(SyntaxKind.CloseParenthesisToken);

            if (symbol.Type != TypeSymbol.Void)
            {
                writer.WriteSpace();
                writer.WritePunctuation(SyntaxKind.ArrowToken);
                writer.WriteSpace();
                symbol.Type.WriteTo(writer);
            }
        }

        private static void WriteGlobalVariableTo(VariableSymbol symbol, TextWriter writer)
        {
            writer.WriteKeyword(symbol.IsImmutable ? SyntaxKind.LetKeyword : SyntaxKind.VarKeyword);
            writer.WriteSpace();
            writer.WriteIdentifier(symbol.Name);
            writer.WritePunctuation(SyntaxKind.ColonToken);
            writer.WriteSpace();
            symbol.Type.WriteTo(writer);
        }

        private static void WriteLocalVariableTo(VariableSymbol symbol, TextWriter writer)
        {
            writer.WriteKeyword(symbol.IsImmutable ? SyntaxKind.LetKeyword : SyntaxKind.VarKeyword);
            writer.WriteSpace();
            writer.WriteIdentifier(symbol.Name);
            writer.WritePunctuation(SyntaxKind.ColonToken);
            writer.WriteSpace();
            symbol.Type.WriteTo(writer);
        }

        private static void WriteParameterTo(VariableSymbol symbol, TextWriter writer)
        {
            writer.WriteIdentifier(symbol.Name);
            writer.WritePunctuation(SyntaxKind.ColonToken);
            writer.WriteSpace();
            symbol.Type.WriteTo(writer);
        }

        private static void WriteTypeTo(TypeSymbol symbol, TextWriter writer)
        {
            writer.WriteKeyword(symbol.Name);
        }
    }
}
