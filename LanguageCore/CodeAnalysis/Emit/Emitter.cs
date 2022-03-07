using System;
using System.Collections.Generic;
using System.Linq;
using LanguageCore.CodeAnalysis.Binding;
using LanguageCore.CodeAnalysis.Symbols;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace LanguageCore.CodeAnalysis.Emit
{
    internal static class Emitter
    {
        public static IReadOnlyList<Diagnostic> Emit(BoundProgram program, string moduleName,
            IReadOnlyList<string> references, string outputPath)
        {
            if (program.Diagnostics.Any())
            {
                return program.Diagnostics;
            }

            var assemblies = new List<AssemblyDefinition>();
            var result = new DiagnosticBag();

            foreach (var reference in references)
            {
                try
                {
                    var assembly = AssemblyDefinition.ReadAssembly(reference);
                    assemblies.Add(assembly);
                }
                catch (BadImageFormatException)
                {
                    result.ReportInvalidReference(reference);
                }
            }

            var builtInTypes = TypeSymbol.AllTypes
                .Select(TypeSymbol.ToSystemType)
                .Zip(TypeSymbol.AllTypes, (x, y) => (y, x.FullName));
            var assemblyName = new AssemblyNameDefinition(moduleName, new Version(1, 0));
            var assemblyDefinition = AssemblyDefinition.CreateAssembly(assemblyName, moduleName, ModuleKind.Console);
            var knownTypes = new Dictionary<TypeSymbol, TypeReference>();

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
                    result.ReportRequiredTypeNotFound(niobiumName, metadataName);
                }
                else
                {
                    result.ReportRequiredTypeAmbiguous(niobiumName, metadataName, foundTypes);
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
                    var methods = foundType.Methods.Where(m => m.Name == methodName);

                    foreach (var method in methods)
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

                    result.ReportRequiredMethodNotFound(typeName, methodName, parameterTypeNames);
                    return null;
                }

                if (foundTypes.Length == 0)
                {
                    result.ReportRequiredTypeNotFound(null, typeName);
                }
                else
                {
                    result.ReportRequiredTypeAmbiguous(null, typeName, foundTypes);
                }

                return null;
            }

            var consoleWriteLineReference = ResolveMethod("System.Console", "WriteLine", new[] {"System.String"});

            if (result.Any())
            {
                return result.ToArray();
            }

            var objectType = knownTypes[TypeSymbol.Any];
            var typeDefinition = new TypeDefinition("", "Program", TypeAttributes.Abstract | TypeAttributes.Sealed,
                objectType);
            assemblyDefinition.MainModule.Types.Add(typeDefinition);

            var voidType = knownTypes[TypeSymbol.Void];
            var mainMethod = new MethodDefinition("Main", MethodAttributes.Static | MethodAttributes.Private, voidType);
            typeDefinition.Methods.Add(mainMethod);

            var ilProcessor = mainMethod.Body.GetILProcessor();
            ilProcessor.Emit(OpCodes.Ldstr, "Hello world!");
            ilProcessor.Emit(OpCodes.Call, consoleWriteLineReference);
            ilProcessor.Emit(OpCodes.Ret);

            assemblyDefinition.EntryPoint = mainMethod;

            assemblyDefinition.Write(outputPath);

            return result.ToArray();
        }
    }
}
