using System;
using System.Collections.Generic;
using LanguageCore.CodeAnalysis;
using LanguageCore.CodeAnalysis.Syntax;
using NUnit.Framework;

namespace LanguageCore.Tests.CodeAnalysis
{
    public class EvaluationTests
    {
        [TestCase("1", 1)]
        [TestCase("+1", 1)]
        [TestCase("-1", -1)]
        [TestCase("14 + 12", 26)]
        [TestCase("12 - 3", 9)]
        [TestCase("4 * 2", 8)]
        [TestCase("9 / 3", 3)]
        [TestCase("(10)", 10)]
        [TestCase("(((10)))", 10)]
        [TestCase("12 == 3", false)]
        [TestCase("3 == 3", true)]
        [TestCase("12 != 3", true)]
        [TestCase("3 != 3", false)]
        [TestCase("false == false", true)]
        [TestCase("true == false", false)]
        [TestCase("false != false", false)]
        [TestCase("true != false", true)]
        [TestCase("true", true)]
        [TestCase("false", false)]
        [TestCase("!true", false)]
        [TestCase("!false", true)]
        [TestCase("{ var a = 0 (a = 10) * a }", 100)]
        [TestCase("2 + 2 * 2", 6)]
        public void SyntaxFact_GetText_RoundTrips(string text, object expectedValue)
        {
            AssertValue(text, expectedValue);
        }
        
        [Test]
        public void Evaluator_VariableDeclaration_Reports_Redeclaration()
        {
            const string text = @"
                {
                    var x = 10
                    var y = 100
                    {
                        var x = 10
                    }
                    var [x] = 5
                }
            ";

            const string diagnostics = @"
                Variable 'x' is already declared.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_Name_Reports_Undefined()
        {
            const string text = @"[x] * 10";

            const string diagnostics = @"
                Variable 'x' doesn't exist.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_Assigned_Reports_Undefined()
        {
            const string text = @"[x] = 10";

            const string diagnostics = @"
                Variable 'x' doesn't exist.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_Assigned_Reports_CannotAssign()
        {
            const string text = @"
                {
                    let x = 10
                    x [=] 0
                }
            ";

            const string diagnostics = @"
                Variable 'x' is immutable and cannot be assigned to.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_Assigned_Reports_CannotConvert()
        {
            const string text = @"
                {
                    var x = 10
                    x = [true]
                }
            ";

            const string diagnostics = @"
                Cannot convert type 'System.Boolean' to 'System.Int32'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_Unary_Reports_Undefined()
        {
            const string text = @"[+]true";

            const string diagnostics = @"
                Unary operator '+' is not defined for type 'System.Boolean'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_Binary_Reports_Undefined()
        {
            const string text = @"10 [*] false";

            const string diagnostics = @"
                Binary operator '*' is not defined for types 'System.Int32' and 'System.Boolean'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        private static void AssertValue(string text, object expectedValue)
        {
            var syntaxTree = SyntaxTree.Parse(text);
            var compilation = new Compilation(syntaxTree);
            var variables = new Dictionary<VariableSymbol, object>();
            var result = compilation.Evaluate(variables);

            Assert.IsEmpty(result.Diagnostics);
            Assert.AreEqual(expectedValue, result.Value);
        }

        private void AssertDiagnostics(string text, string diagnosticText)
        {
            var annotatedText = AnnotatedText.Parse(text);
            var syntaxTree = SyntaxTree.Parse(annotatedText.Text);
            var compilation = new Compilation(syntaxTree);
            var result = compilation.Evaluate(new Dictionary<VariableSymbol, object>());

            var expectedDiagnostics = AnnotatedText.UnIndentLines(diagnosticText);

            if (annotatedText.Spans.Count != expectedDiagnostics.Length)
            {
                throw new Exception("Must mark as many spans as there are expected diagnostics");
            }

            Assert.AreEqual(expectedDiagnostics.Length, result.Diagnostics.Count);

            for (var i = 0; i < expectedDiagnostics.Length; i++)
            {
                var expectedMessage = expectedDiagnostics[i];
                var actualMessage = result.Diagnostics[i].Message;
                Assert.AreEqual(expectedMessage, actualMessage);

                var expectedSpan = annotatedText.Spans[i];
                var actualSpan = result.Diagnostics[i].Span;
                Assert.AreEqual(expectedSpan, actualSpan);
            }
        }
    }
}
