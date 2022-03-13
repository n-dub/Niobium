using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LanguageCore.CodeAnalysis.Binding;
using LanguageCore.CodeAnalysis.Symbols;
using LanguageCore.CodeAnalysis.Syntax;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace LanguageCore.CodeAnalysis.Emit
{
    internal sealed class Emitter
    {
        private readonly DiagnosticBag diagnostics = new DiagnosticBag();

        private readonly Dictionary<TypeSymbol, TypeReference> knownTypes;

        private readonly TypeReference randomReference;
        private readonly MethodReference randomCtorReference;
        private readonly MethodReference randomNextReference;

        private readonly MethodReference[] stringConcatReferences;
        private readonly MethodReference stringConcatArrayReference;

        private readonly MethodReference objectEqualsReference;
        private readonly MethodReference consoleWriteLineReference;
        private readonly MethodReference consoleReadLineReference;
        private readonly MethodReference convertToBooleanReference;
        private readonly MethodReference convertToInt32Reference;
        private readonly MethodReference convertToStringReference;

        private readonly AssemblyDefinition assemblyDefinition;
        private readonly Dictionary<FunctionSymbol, MethodDefinition> methods;
        private readonly Dictionary<VariableSymbol, VariableDefinition> locals;

        private readonly Dictionary<BoundLabel, int> labels;
        private readonly List<(int InstructionIndex, BoundLabel Target)> fixUps;

        private TypeDefinition typeDefinition;
        private FieldDefinition randomFieldDefinition;

        private Emitter(string moduleName, IReadOnlyList<string> references)
        {
            locals = new Dictionary<VariableSymbol, VariableDefinition>();
            fixUps = new List<(int InstructionIndex, BoundLabel Target)>();
            labels = new Dictionary<BoundLabel, int>();
            methods = new Dictionary<FunctionSymbol, MethodDefinition>();
            var assemblies = new List<AssemblyDefinition>();

            foreach (var reference in references)
            {
                try
                {
                    var assembly = AssemblyDefinition.ReadAssembly(reference);
                    assemblies.Add(assembly);
                }
                catch (BadImageFormatException)
                {
                    diagnostics.ReportInvalidReference(reference);
                }
            }

            var builtInTypes = TypeSymbol.AllTypes
                .Select(TypeSymbol.ToSystemType)
                .Zip(TypeSymbol.AllTypes, (x, y) => (y, x.FullName));
            var assemblyName = new AssemblyNameDefinition(moduleName, new Version(1, 0));
            assemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyName, moduleName, ModuleKind.Console);
            knownTypes = new Dictionary<TypeSymbol, TypeReference>();

            foreach (var (typeSymbol, metadataName) in builtInTypes)
            {
                var typeReference = ResolveType(typeSymbol.Name, metadataName);
                knownTypes.Add(typeSymbol, typeReference);
            }

            TypeReference ResolveType(string niobiumName, string metadataName)
            {
                var foundTypes = assemblies.SelectMany(a => a.Modules)
                    .SelectMany(m => m.Types)
                    .Where(t => t.FullName == metadataName)
                    .ToArray();
                if (foundTypes.Length == 1)
                {
                    var typeReference = assemblyDefinition.MainModule.ImportReference(foundTypes[0]);
                    return typeReference;
                }

                if (foundTypes.Length == 0)
                {
                    diagnostics.ReportRequiredTypeNotFound(niobiumName, metadataName);
                }
                else
                {
                    diagnostics.ReportRequiredTypeAmbiguous(niobiumName, metadataName, foundTypes);
                }

                return null;
            }

            MethodReference ResolveMethod(string typeName, string methodName, string[] parameterTypeNames)
            {
                var foundTypes = assemblies.SelectMany(a => a.Modules)
                    .SelectMany(m => m.Types)
                    .Where(t => t.FullName == typeName)
                    .ToArray();
                if (foundTypes.Length == 1)
                {
                    var foundType = foundTypes[0];
                    var methodDefinitions = foundType.Methods.Where(m => m.Name == methodName);

                    foreach (var method in methodDefinitions)
                    {
                        if (method.Parameters.Count != parameterTypeNames.Length)
                        {
                            continue;
                        }

                        var allParametersMatch = true;

                        for (var i = 0; i < parameterTypeNames.Length; i++)
                        {
                            if (method.Parameters[i].ParameterType.FullName != parameterTypeNames[i])
                            {
                                allParametersMatch = false;
                                break;
                            }
                        }

                        if (!allParametersMatch)
                        {
                            continue;
                        }

                        return assemblyDefinition.MainModule.ImportReference(method);
                    }

                    diagnostics.ReportRequiredMethodNotFound(typeName, methodName, parameterTypeNames);
                    return null;
                }

                if (foundTypes.Length == 0)
                {
                    diagnostics.ReportRequiredTypeNotFound(null, typeName);
                }
                else
                {
                    diagnostics.ReportRequiredTypeAmbiguous(null, typeName, foundTypes);
                }

                return null;
            }

            stringConcatReferences = new MethodReference[5];
            for (var i = 2; i < stringConcatReferences.Length; ++i)
            {
                var parameters = Enumerable.Repeat("System.String", i).ToArray();
                stringConcatReferences[i] = ResolveMethod("System.String", "Concat", parameters);
            }

            stringConcatArrayReference = ResolveMethod("System.String", "Concat", new[] {"System.String[]"});

            randomReference = ResolveType(null, "System.Random");
            randomCtorReference = ResolveMethod("System.Random", ".ctor", Array.Empty<string>());
            randomNextReference = ResolveMethod("System.Random", "Next", new[] {"System.Int32", "System.Int32"});

            objectEqualsReference = ResolveMethod("System.Object", "Equals", new[] {"System.Object", "System.Object"});

            consoleReadLineReference = ResolveMethod("System.Console", "ReadLine", Array.Empty<string>());
            consoleWriteLineReference = ResolveMethod("System.Console", "WriteLine", new[] {"System.Object"});

            convertToBooleanReference = ResolveMethod("System.Convert", "ToBoolean", new[] {"System.Object"});
            convertToInt32Reference = ResolveMethod("System.Convert", "ToInt32", new[] {"System.Object"});
            convertToStringReference = ResolveMethod("System.Convert", "ToString", new[] {"System.Object"});
        }

        public static IReadOnlyList<Diagnostic> Emit(BoundProgram program, string moduleName,
            IReadOnlyList<string> references, string outputPath)
        {
            if (program.Diagnostics.Any())
            {
                return program.Diagnostics;
            }

            var emitter = new Emitter(moduleName, references);
            return emitter.Emit(program, outputPath);
        }

        public IReadOnlyList<Diagnostic> Emit(BoundProgram program, string outputPath)
        {
            if (diagnostics.Any())
            {
                return diagnostics.ToArray();
            }

            var objectType = knownTypes[TypeSymbol.Any];
            typeDefinition = new TypeDefinition("", "Program", TypeAttributes.Abstract | TypeAttributes.Sealed,
                objectType);
            assemblyDefinition.MainModule.Types.Add(typeDefinition);

            foreach (var functionWithBody in program.Functions)
            {
                EmitFunctionDeclaration(functionWithBody.Key);
            }

            foreach (var functionWithBody in program.Functions)
            {
                EmitFunctionBody(functionWithBody.Key, functionWithBody.Value);
            }

            if (program.MainFunction != null)
            {
                assemblyDefinition.EntryPoint = methods[program.MainFunction];
            }

            assemblyDefinition.Write(outputPath);

            return diagnostics.ToArray();
        }

        private void EmitFunctionDeclaration(FunctionSymbol function)
        {
            var functionType = knownTypes[function.Type];
            var attributes = MethodAttributes.Static | MethodAttributes.Private;
            var method = new MethodDefinition(function.Name, attributes, functionType);

            foreach (var parameter in function.Parameters)
            {
                var parameterType = knownTypes[parameter.Type];
                var parameterAttributes = ParameterAttributes.None;
                var parameterDefinition = new ParameterDefinition(parameter.Name, parameterAttributes, parameterType);
                method.Parameters.Add(parameterDefinition);
            }

            typeDefinition.Methods.Add(method);
            methods.Add(function, method);
        }

        private void EmitFunctionBody(FunctionSymbol function, BoundBlockStatement body)
        {
            var method = methods[function];
            var ilProcessor = method.Body.GetILProcessor();

            locals.Clear();
            labels.Clear();
            fixUps.Clear();

            foreach (var statement in body.Statements)
            {
                EmitStatement(ilProcessor, statement);
            }

            foreach (var (instructionIndex, targetLabel) in fixUps)
            {
                var targetInstructionIndex = labels[targetLabel];
                var targetInstruction = ilProcessor.Body.Instructions[targetInstructionIndex];
                var instructionToFixup = ilProcessor.Body.Instructions[instructionIndex];
                instructionToFixup.Operand = targetInstruction;
            }

            method.Body.OptimizeMacros();
        }

        private void EmitStatement(ILProcessor ilProcessor, BoundStatement node)
        {
            switch (node.Kind)
            {
                case BoundNodeKind.NopStatement:
                    EmitNopStatement(ilProcessor, (BoundNopStatement) node);
                    break;
                case BoundNodeKind.VariableDeclarationStatement:
                    EmitVariableDeclaration(ilProcessor, (BoundVariableDeclarationStatement) node);
                    break;
                case BoundNodeKind.LabelStatement:
                    EmitLabelStatement(ilProcessor, (BoundLabelStatement) node);
                    break;
                case BoundNodeKind.GotoStatement:
                    EmitGotoStatement(ilProcessor, (BoundGotoStatement) node);
                    break;
                case BoundNodeKind.ConditionalGotoStatement:
                    EmitConditionalGotoStatement(ilProcessor, (BoundConditionalGotoStatement) node);
                    break;
                case BoundNodeKind.ReturnStatement:
                    EmitReturnStatement(ilProcessor, (BoundReturnStatement) node);
                    break;
                case BoundNodeKind.ExpressionStatement:
                    EmitExpressionStatement(ilProcessor, (BoundExpressionStatement) node);
                    break;
                default:
                    throw new Exception($"Unexpected node kind {node.Kind}");
            }
        }

        private void EmitNopStatement(ILProcessor ilProcessor, BoundNopStatement node)
        {
            ilProcessor.Emit(OpCodes.Nop);
        }

        private void EmitVariableDeclaration(ILProcessor ilProcessor, BoundVariableDeclarationStatement node)
        {
            var typeReference = knownTypes[node.Variable.Type];
            var variableDefinition = new VariableDefinition(typeReference);
            locals.Add(node.Variable, variableDefinition);
            ilProcessor.Body.Variables.Add(variableDefinition);

            EmitExpression(ilProcessor, node.Initializer);
            ilProcessor.Emit(OpCodes.Stloc, variableDefinition);
        }

        private void EmitLabelStatement(ILProcessor ilProcessor, BoundLabelStatement node)
        {
            labels.Add(node.Label, ilProcessor.Body.Instructions.Count);
        }

        private void EmitGotoStatement(ILProcessor ilProcessor, BoundGotoStatement node)
        {
            fixUps.Add((ilProcessor.Body.Instructions.Count, node.Label));
            ilProcessor.Emit(OpCodes.Br, Instruction.Create(OpCodes.Nop));
        }

        private void EmitConditionalGotoStatement(ILProcessor ilProcessor, BoundConditionalGotoStatement node)
        {
            EmitExpression(ilProcessor, node.Condition);

            var opCode = node.JumpIfTrue ? OpCodes.Brtrue : OpCodes.Brfalse;
            fixUps.Add((ilProcessor.Body.Instructions.Count, node.Label));
            ilProcessor.Emit(opCode, Instruction.Create(OpCodes.Nop));
        }

        private void EmitReturnStatement(ILProcessor ilProcessor, BoundReturnStatement node)
        {
            if (node.Expression != null)
            {
                EmitExpression(ilProcessor, node.Expression);
            }

            ilProcessor.Emit(OpCodes.Ret);
        }

        private void EmitExpressionStatement(ILProcessor ilProcessor, BoundExpressionStatement node)
        {
            EmitExpression(ilProcessor, node.Expression);

            if (node.Expression.Type != TypeSymbol.Void)
            {
                ilProcessor.Emit(OpCodes.Pop);
            }
        }

        private void EmitExpression(ILProcessor ilProcessor, BoundExpression node)
        {
            if (node.ConstantValue != null)
            {
                EmitConstantExpression(ilProcessor, node);
                return;
            }

            switch (node.Kind)
            {
                case BoundNodeKind.VariableExpression:
                    EmitVariableExpression(ilProcessor, (BoundVariableExpression) node);
                    break;
                case BoundNodeKind.AssignmentExpression:
                    EmitAssignmentExpression(ilProcessor, (BoundAssignmentExpression) node);
                    break;
                case BoundNodeKind.UnaryExpression:
                    EmitUnaryExpression(ilProcessor, (BoundUnaryExpression) node);
                    break;
                case BoundNodeKind.BinaryExpression:
                    EmitBinaryExpression(ilProcessor, (BoundBinaryExpression) node);
                    break;
                case BoundNodeKind.CallExpression:
                    EmitCallExpression(ilProcessor, (BoundCallExpression) node);
                    break;
                case BoundNodeKind.ConversionExpression:
                    EmitConversionExpression(ilProcessor, (BoundConversionExpression) node);
                    break;
                default:
                    throw new Exception($"Unexpected node kind {node.Kind}");
            }
        }

        private void EmitConstantExpression(ILProcessor ilProcessor, BoundExpression node)
        {
            if (node.Type == TypeSymbol.Bool)
            {
                var value = (bool) node.ConstantValue.Value;
                var instruction = value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0;
                ilProcessor.Emit(instruction);
            }
            else if (node.Type == TypeSymbol.Int32)
            {
                var value = (int) node.ConstantValue.Value;
                ilProcessor.Emit(OpCodes.Ldc_I4, value);
            }
            else if (node.Type == TypeSymbol.String)
            {
                var value = (string) node.ConstantValue.Value;
                ilProcessor.Emit(OpCodes.Ldstr, value);
            }
            else
            {
                throw new Exception($"Unexpected constant expression type: {node.Type}");
            }
        }

        private void EmitVariableExpression(ILProcessor ilProcessor, BoundVariableExpression node)
        {
            if (node.Variable is ParameterSymbol parameter)
            {
                ilProcessor.Emit(OpCodes.Ldarg, parameter.Ordinal);
            }
            else
            {
                var variableDefinition = locals[node.Variable];
                ilProcessor.Emit(OpCodes.Ldloc, variableDefinition);
            }
        }

        private void EmitAssignmentExpression(ILProcessor ilProcessor, BoundAssignmentExpression node)
        {
            var variableDefinition = locals[node.Variable];
            EmitExpression(ilProcessor, node.Expression);
            ilProcessor.Emit(OpCodes.Dup);
            ilProcessor.Emit(OpCodes.Stloc, variableDefinition);
        }

        private void EmitUnaryExpression(ILProcessor ilProcessor, BoundUnaryExpression node)
        {
            EmitExpression(ilProcessor, node.Operand);

            switch (node.Op.Kind)
            {
                case BoundUnaryOperatorKind.Identity:
                    // Done
                    break;
                case BoundUnaryOperatorKind.LogicalNegation:
                    ilProcessor.Emit(OpCodes.Ldc_I4_0);
                    ilProcessor.Emit(OpCodes.Ceq);
                    break;
                case BoundUnaryOperatorKind.Negation:
                    ilProcessor.Emit(OpCodes.Neg);
                    break;
                case BoundUnaryOperatorKind.OnesComplement:
                    ilProcessor.Emit(OpCodes.Not);
                    break;
                default:
                    throw new Exception(
                        $"Unexpected unary operator {SyntaxFacts.GetText(node.Op.SyntaxKind)}({node.Operand.Type})");
            }
        }

        private void EmitBinaryExpression(ILProcessor ilProcessor, BoundBinaryExpression node)
        {
            if (node.Op.Kind == BoundBinaryOperatorKind.Addition)
            {
                if (node.Left.Type == TypeSymbol.String && node.Right.Type == TypeSymbol.String)
                {
                    EmitStringConcatExpression(ilProcessor, node);
                    return;
                }
            }

            EmitExpression(ilProcessor, node.Left);
            EmitExpression(ilProcessor, node.Right);

            if (node.Op.Kind == BoundBinaryOperatorKind.Equals)
            {
                if (node.Left.Type == TypeSymbol.Any && node.Right.Type == TypeSymbol.Any ||
                    node.Left.Type == TypeSymbol.String && node.Right.Type == TypeSymbol.String)
                {
                    ilProcessor.Emit(OpCodes.Call, objectEqualsReference);
                    return;
                }
            }

            if (node.Op.Kind == BoundBinaryOperatorKind.NotEquals)
            {
                if (node.Left.Type == TypeSymbol.Any && node.Right.Type == TypeSymbol.Any ||
                    node.Left.Type == TypeSymbol.String && node.Right.Type == TypeSymbol.String)
                {
                    ilProcessor.Emit(OpCodes.Call, objectEqualsReference);
                    ilProcessor.Emit(OpCodes.Ldc_I4_0);
                    ilProcessor.Emit(OpCodes.Ceq);
                    return;
                }
            }

            switch (node.Op.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    ilProcessor.Emit(OpCodes.Add);
                    break;
                case BoundBinaryOperatorKind.Subtraction:
                    ilProcessor.Emit(OpCodes.Sub);
                    break;
                case BoundBinaryOperatorKind.Multiplication:
                    ilProcessor.Emit(OpCodes.Mul);
                    break;
                case BoundBinaryOperatorKind.Division:
                    ilProcessor.Emit(OpCodes.Div);
                    break;
                // TODO: Implement short-circuit evaluation
                case BoundBinaryOperatorKind.LogicalAnd:
                case BoundBinaryOperatorKind.BitwiseAnd:
                    ilProcessor.Emit(OpCodes.And);
                    break;
                // TODO: Implement short-circuit evaluation
                case BoundBinaryOperatorKind.LogicalOr:
                case BoundBinaryOperatorKind.BitwiseOr:
                    ilProcessor.Emit(OpCodes.Or);
                    break;
                case BoundBinaryOperatorKind.BitwiseXor:
                    ilProcessor.Emit(OpCodes.Xor);
                    break;
                case BoundBinaryOperatorKind.Equals:
                    ilProcessor.Emit(OpCodes.Ceq);
                    break;
                case BoundBinaryOperatorKind.NotEquals:
                    ilProcessor.Emit(OpCodes.Ceq);
                    ilProcessor.Emit(OpCodes.Ldc_I4_0);
                    ilProcessor.Emit(OpCodes.Ceq);
                    break;
                case BoundBinaryOperatorKind.Less:
                    ilProcessor.Emit(OpCodes.Clt);
                    break;
                case BoundBinaryOperatorKind.LessOrEquals:
                    ilProcessor.Emit(OpCodes.Cgt);
                    ilProcessor.Emit(OpCodes.Ldc_I4_0);
                    ilProcessor.Emit(OpCodes.Ceq);
                    break;
                case BoundBinaryOperatorKind.Greater:
                    ilProcessor.Emit(OpCodes.Cgt);
                    break;
                case BoundBinaryOperatorKind.GreaterOrEquals:
                    ilProcessor.Emit(OpCodes.Clt);
                    ilProcessor.Emit(OpCodes.Ldc_I4_0);
                    ilProcessor.Emit(OpCodes.Ceq);
                    break;
                default:
                    throw new Exception(
                        $"Unexpected binary operator {SyntaxFacts.GetText(node.Op.SyntaxKind)}({node.Left.Type}, {node.Right.Type})");
            }
        }

        private void EmitStringConcatExpression(ILProcessor ilProcessor, BoundBinaryExpression node)
        {
            var nodes = FoldConstants(Flatten(node)).ToList();

            if (nodes.Count == 0)
            {
                ilProcessor.Emit(OpCodes.Ldstr, string.Empty);
            }
            else if (nodes.Count < stringConcatReferences.Length)
            {
                foreach (var operand in nodes)
                {
                    EmitExpression(ilProcessor, operand);
                }

                if (stringConcatReferences[nodes.Count] != null)
                {
                    ilProcessor.Emit(OpCodes.Call, stringConcatReferences[nodes.Count]);
                }
            }
            else
            {
                ilProcessor.Emit(OpCodes.Ldc_I4, nodes.Count);
                ilProcessor.Emit(OpCodes.Newarr, knownTypes[TypeSymbol.String]);

                for (var i = 0; i < nodes.Count; i++)
                {
                    ilProcessor.Emit(OpCodes.Dup);
                    ilProcessor.Emit(OpCodes.Ldc_I4, i);
                    EmitExpression(ilProcessor, nodes[i]);
                    ilProcessor.Emit(OpCodes.Stelem_Ref);
                }

                ilProcessor.Emit(OpCodes.Call, stringConcatArrayReference);
            }

            static IEnumerable<BoundExpression> Flatten(BoundExpression node)
            {
                if (node is BoundBinaryExpression binaryExpression &&
                    binaryExpression.Op.Kind == BoundBinaryOperatorKind.Addition &&
                    binaryExpression.Left.Type == TypeSymbol.String &&
                    binaryExpression.Right.Type == TypeSymbol.String)
                {
                    foreach (var result in Flatten(binaryExpression.Left))
                    {
                        yield return result;
                    }

                    foreach (var result in Flatten(binaryExpression.Right))
                    {
                        yield return result;
                    }
                }
                else
                {
                    if (node.Type != TypeSymbol.String)
                    {
                        throw new Exception($"Unexpected node type in string concatenation: {node.Type}");
                    }

                    yield return node;
                }
            }

            static IEnumerable<BoundExpression> FoldConstants(IEnumerable<BoundExpression> nodes)
            {
                StringBuilder sb = null;

                foreach (var node in nodes)
                {
                    if (node.ConstantValue != null)
                    {
                        var stringValue = (string) node.ConstantValue.Value;

                        if (string.IsNullOrEmpty(stringValue))
                        {
                            continue;
                        }

                        sb ??= new StringBuilder();
                        sb.Append(stringValue);
                    }
                    else
                    {
                        if (sb?.Length > 0)
                        {
                            yield return new BoundLiteralExpression(sb.ToString());
                            sb.Clear();
                        }

                        yield return node;
                    }
                }

                if (sb?.Length > 0)
                {
                    yield return new BoundLiteralExpression(sb.ToString());
                }
            }
        }

        private void EmitCallExpression(ILProcessor ilProcessor, BoundCallExpression node)
        {
            if (node.Function == BuiltinFunctions.Random)
            {
                if (randomFieldDefinition == null)
                {
                    EmitRandomField();
                }

                ilProcessor.Emit(OpCodes.Ldsfld, randomFieldDefinition);

                foreach (var argument in node.Arguments)
                {
                    EmitExpression(ilProcessor, argument);
                }

                ilProcessor.Emit(OpCodes.Callvirt, randomNextReference);
                return;
            }

            foreach (var argument in node.Arguments)
            {
                EmitExpression(ilProcessor, argument);
            }

            if (node.Function == BuiltinFunctions.ReadLine)
            {
                ilProcessor.Emit(OpCodes.Call, consoleReadLineReference);
            }
            else if (node.Function == BuiltinFunctions.Print)
            {
                ilProcessor.Emit(OpCodes.Call, consoleWriteLineReference);
            }
            else
            {
                ilProcessor.Emit(OpCodes.Call, methods[node.Function]);
            }
        }

        private void EmitRandomField()
        {
            randomFieldDefinition = new FieldDefinition(
                "__rnd",
                FieldAttributes.Static | FieldAttributes.Private,
                randomReference
            );
            typeDefinition.Fields.Add(randomFieldDefinition);

            var staticConstructor = new MethodDefinition(
                ".cctor",
                MethodAttributes.Static |
                MethodAttributes.Private |
                MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName,
                knownTypes[TypeSymbol.Void]
            );
            typeDefinition.Methods.Insert(0, staticConstructor);

            var ilProcessor = staticConstructor.Body.GetILProcessor();
            ilProcessor.Emit(OpCodes.Newobj, randomCtorReference);
            ilProcessor.Emit(OpCodes.Stsfld, randomFieldDefinition);
            ilProcessor.Emit(OpCodes.Ret);
        }

        private void EmitConversionExpression(ILProcessor ilProcessor, BoundConversionExpression node)
        {
            EmitExpression(ilProcessor, node.Expression);
            if (node.Expression.Type == TypeSymbol.Bool || node.Expression.Type == TypeSymbol.Int32)
            {
                ilProcessor.Emit(OpCodes.Box, knownTypes[node.Expression.Type]);
            }

            if (node.Type == TypeSymbol.Any)
            {
                // Done
            }
            else if (node.Type == TypeSymbol.Bool)
            {
                ilProcessor.Emit(OpCodes.Call, convertToBooleanReference);
            }
            else if (node.Type == TypeSymbol.Int32)
            {
                ilProcessor.Emit(OpCodes.Call, convertToInt32Reference);
            }
            else if (node.Type == TypeSymbol.String)
            {
                ilProcessor.Emit(OpCodes.Call, convertToStringReference);
            }
            else
            {
                throw new Exception($"Unexpected conversion from {node.Expression.Type} to {node.Type}");
            }
        }
    }
}
