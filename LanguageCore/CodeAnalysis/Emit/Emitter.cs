using System;
using System.Collections.Generic;
using System.Linq;
using LanguageCore.CodeAnalysis.Binding;
using LanguageCore.CodeAnalysis.Symbols;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace LanguageCore.CodeAnalysis.Emit
{
    internal sealed class Emitter
    {
        private readonly DiagnosticBag diagnostics = new DiagnosticBag();

        private readonly Dictionary<TypeSymbol, TypeReference> knownTypes;

        private readonly MethodReference consoleWriteLineReference;
        private readonly MethodReference consoleReadLineReference;
        private readonly MethodReference stringConcatReference;

        private readonly AssemblyDefinition assemblyDefinition;
        private readonly Dictionary<FunctionSymbol, MethodDefinition> methods;

        private readonly Dictionary<VariableSymbol, VariableDefinition> locals =
            new Dictionary<VariableSymbol, VariableDefinition>();

        private TypeDefinition typeDefinition;

        private Emitter(string moduleName, IReadOnlyList<string> references)
        {
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

            consoleReadLineReference = ResolveMethod("System.Console", "ReadLine", Array.Empty<string>());
            consoleWriteLineReference = ResolveMethod("System.Console", "WriteLine", new[] {"System.String"});
            stringConcatReference = ResolveMethod("System.String", "Concat", new[] {"System.String", "System.String"});
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

            foreach (var statement in body.Statements)
            {
                EmitStatement(ilProcessor, statement);
            }

            method.Body.OptimizeMacros();
        }

        private void EmitStatement(ILProcessor ilProcessor, BoundStatement node)
        {
            switch (node.Kind)
            {
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
            throw new NotImplementedException();
        }

        private void EmitGotoStatement(ILProcessor ilProcessor, BoundGotoStatement node)
        {
            throw new NotImplementedException();
        }

        private void EmitConditionalGotoStatement(ILProcessor ilProcessor, BoundConditionalGotoStatement node)
        {
            throw new NotImplementedException();
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
            switch (node.Kind)
            {
                case BoundNodeKind.LiteralExpression:
                    EmitLiteralExpression(ilProcessor, (BoundLiteralExpression) node);
                    break;
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

        private void EmitLiteralExpression(ILProcessor ilProcessor, BoundLiteralExpression node)
        {
            if (node.Type == TypeSymbol.Bool)
            {
                var value = (bool) node.Value;
                var instruction = value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0;
                ilProcessor.Emit(instruction);
            }
            else if (node.Type == TypeSymbol.Int32)
            {
                var value = (int) node.Value;
                ilProcessor.Emit(OpCodes.Ldc_I4, value);
            }
            else if (node.Type == TypeSymbol.String)
            {
                var value = (string) node.Value;
                ilProcessor.Emit(OpCodes.Ldstr, value);
            }
            else
            {
                throw new Exception($"Unexpected literal type: {node.Type}");
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
            throw new NotImplementedException();
        }

        private void EmitBinaryExpression(ILProcessor ilProcessor, BoundBinaryExpression node)
        {
            if (node.Op.Kind == BoundBinaryOperatorKind.Addition)
            {
                if (node.Left.Type == TypeSymbol.String &&
                    node.Right.Type == TypeSymbol.String)
                {
                    EmitExpression(ilProcessor, node.Left);
                    EmitExpression(ilProcessor, node.Right);
                    ilProcessor.Emit(OpCodes.Call, stringConcatReference);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void EmitCallExpression(ILProcessor ilProcessor, BoundCallExpression node)
        {
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
            else if (node.Function == BuiltinFunctions.Random)
            {
                throw new NotImplementedException();
            }
            else
            {
                var methodDefinition = methods[node.Function];
                ilProcessor.Emit(OpCodes.Call, methodDefinition);
            }
        }

        private void EmitConversionExpression(ILProcessor ilProcessor, BoundConversionExpression node)
        {
            throw new NotImplementedException();
        }
    }
}
