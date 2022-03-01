using System;
using System.Collections.Generic;
using LanguageCore.CodeAnalysis;
using LanguageCore.CodeAnalysis.Symbols;
using LanguageCore.CodeAnalysis.Syntax;
using NUnit.Framework;

namespace LanguageCore.Tests.CodeAnalysis
{
    public class EvaluationTests
    {
        [TestCase("1", 1)]
        [TestCase("+1", 1)]
        [TestCase("-1", -1)]
        [TestCase("~1", -2)]
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
        [TestCase("3 < 4", true)]
        [TestCase("5 < 4", false)]
        [TestCase("4 <= 4", true)]
        [TestCase("4 <= 5", true)]
        [TestCase("5 <= 4", false)]
        [TestCase("4 > 3", true)]
        [TestCase("4 > 5", false)]
        [TestCase("4 >= 4", true)]
        [TestCase("5 >= 4", true)]
        [TestCase("4 >= 5", false)]
        [TestCase("1 | 2", 3)]
        [TestCase("1 | 0", 1)]
        [TestCase("1 & 3", 1)]
        [TestCase("1 & 0", 0)]
        [TestCase("1 ^ 0", 1)]
        [TestCase("0 ^ 1", 1)]
        [TestCase("1 ^ 3", 2)]
        [TestCase("false == false", true)]
        [TestCase("true == false", false)]
        [TestCase("false != false", false)]
        [TestCase("true != false", true)]
        [TestCase("true && true", true)]
        [TestCase("false || false", false)]
        [TestCase("false | false", false)]
        [TestCase("false | true", true)]
        [TestCase("true | false", true)]
        [TestCase("true | true", true)]
        [TestCase("false & false", false)]
        [TestCase("false & true", false)]
        [TestCase("true & false", false)]
        [TestCase("true & true", true)]
        [TestCase("false ^ false", false)]
        [TestCase("true ^ false", true)]
        [TestCase("false ^ true", true)]
        [TestCase("true ^ true", false)]
        [TestCase("true", true)]
        [TestCase("false", false)]
        [TestCase("!true", false)]
        [TestCase("!false", true)]
        [TestCase("\"test\"", "test")]
        [TestCase("\"te\\\"st\"", "te\"st")]
        [TestCase("\"test\" == \"test\"", true)]
        [TestCase("\"test\" != \"test\"", false)]
        [TestCase("\"test\" == \"abc\"", false)]
        [TestCase("\"test\" != \"abc\"", true)]
        [TestCase("\"test\" + \"Abc\"", "testAbc")]
        [TestCase("var a = 10", 10)]
        [TestCase("{ var a = 10 (a * a) }", 100)]
        [TestCase("{ var a = 0 (a = 10) * a }", 100)]
        [TestCase("{ var a = 0 if a == 0 { a = 10 } a }", 10)]
        [TestCase("{ var a = 0 if a == 4 { a = 10 } a }", 0)]
        [TestCase("{ var a = 0 if a == 0 { a = 10 } else { a = 5 } a }", 10)]
        [TestCase("{ var a = 0 if a == 4 { a = 10 } else { a = 5 } a }", 5)]
        [TestCase("{ var i = 10 var result = 0 while i > 0 { result = result + i i = i - 1 } result }", 55)]
        [TestCase("{ var result = 0 for i = 1 in 11 { result = result + i } result }", 55)]
        [TestCase("{ var a = 10 for i = 1 in (a = a - 1) { } a }", 9)]
        [TestCase("{ var a = 0 repeat { a = a + 1 } while a < 10 a}", 10)]
        [TestCase("{ var i = 0 while i < 5 { i = i + 1 if i == 5 { continue } } i }", 5)]
        [TestCase("{ var i = 0 repeat { i = i + 1 if i == 5 { continue } } while i < 5 i }", 5)]
        public void Evaluator_Computes_CorrectValues(string text, object expectedValue)
        {
            AssertValue(text, expectedValue);
        }

        [Test]
        public void Evaluator_InvokeFunctionArguments_Missing()
        {
            const string text = @"
                print([)]
            ";

            const string diagnostics = @"
                Function 'print' requires 1 arguments but was given 0.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_InvokeFunctionArguments_Exceeding()
        {
            const string text = @"
                print(""Hello""[, "" "", "" world!""])
            ";

            const string diagnostics = @"
                Function 'print' requires 1 arguments but was given 3.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_BlockStatement_NoInfiniteLoop()
        {
            const string text = @"
                {
                [)][]
            ";

            const string diagnostics = @"
                Unexpected token <CloseParenthesisToken>, expected <IdentifierToken>.
                Unexpected token <EndOfFileToken>, expected <CloseBraceToken>.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_InvokeFunctionArguments_NoInfiniteLoop()
        {
            const string text = @"
                print(""Hi""[[=]][)]
            ";

            const string diagnostics = @"
                Unexpected token <EqualsToken>, expected <CloseParenthesisToken>.
                Unexpected token <EqualsToken>, expected <IdentifierToken>.
                Unexpected token <CloseParenthesisToken>, expected <IdentifierToken>.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_FunctionParameters_NoInfiniteLoop()
        {
            const string text = @"
                func hi(name: String[[[=]]][)] {
                    print(""Hi "" + name + ""!"" )
                }[]
            ";

            const string diagnostics = @"
                Unexpected token <EqualsToken>, expected <CloseParenthesisToken>.
                Unexpected token <EqualsToken>, expected <OpenBraceToken>.
                Unexpected token <EqualsToken>, expected <IdentifierToken>.
                Unexpected token <CloseParenthesisToken>, expected <IdentifierToken>.
                Unexpected token <EndOfFileToken>, expected <CloseBraceToken>.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_ForStatement_Reports_CannotConvert_LowerBound()
        {
            const string text = @"
                {
                    var result = 0
                    for i = [false] in 10 {
                        result = result + i
                    }
                }
            ";

            const string diagnostics = @"
                Cannot convert type 'Bool' to 'Int32'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_ForStatement_Reports_CannotConvert_UpperBound()
        {
            const string text = @"
                {
                    var result = 0
                    for i = 1 in [true] {
                        result = result + i
                    }
                }
            ";

            const string diagnostics = @"
                Cannot convert type 'Bool' to 'Int32'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_IfStatement_Reports_CannotConvert()
        {
            const string text = @"
                {
                    var x = 0
                    if [10] {
                        x = 10
                    }
                }
            ";

            const string diagnostics = @"
                Cannot convert type 'Int32' to 'Bool'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_WhileStatement_Reports_CannotConvert()
        {
            const string text = @"
                {
                    var x = 0
                    while [10] {
                        x = 10
                    }
                }
            ";

            const string diagnostics = @"
                Cannot convert type 'Int32' to 'Bool'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_RepeatWhileStatement_Reports_CannotConvert()
        {
            const string text = @"
                {
                    var x = 0
                    repeat {
                        x = 10
                    }
                    while [10]
                }
            ";

            const string diagnostics = @"
                Cannot convert type 'Int32' to 'Bool'.
            ";

            AssertDiagnostics(text, diagnostics);
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
                'x' is already declared.
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
                Cannot convert type 'Bool' to 'Int32'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_Variables_Can_Shadow_Functions()
        {
            const string text = @"
                {
                    let print = 42
                    [print](""test"")
                }
            ";

            const string diagnostics = @"
                Function 'print' doesn't exist.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_NameExpression_Reports_NoErrorForInsertedToken()
        {
            const string text = @"1 + []";

            const string diagnostics = @"
                Unexpected token <EndOfFileToken>, expected <IdentifierToken>.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_Unary_Reports_Undefined()
        {
            const string text = @"[+]true";

            const string diagnostics = @"
                Unary operator '+' is not defined for type 'Bool'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_Binary_Reports_Undefined()
        {
            const string text = @"10 [*] false";

            const string diagnostics = @"
                Binary operator '*' is not defined for types 'Int32' and 'Bool'.
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

        private static void AssertDiagnostics(string text, string diagnosticText)
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
        
        [Test]
        public void Evaluator_Void_Function_Should_Not_Return_Value()
        {
            const string text = @"
                func test() {
                    return [1]
                }
            ";

            const string diagnostics = @"
                Since the function 'test' does not return a value the 'return' keyword cannot be followed by an expression.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_Function_With_ReturnValue_Should_Not_Return_Void()
        {
            const string text = @"
                func test() -> Int32 {
                    [return]
                }
            ";

            const string diagnostics = @"
                An expression of type 'Int32' is expected.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_Not_All_Code_Paths_Return_Value()
        {
            const string text = @"
                func [test](n: Int32) -> Bool {
                    if (n > 10) {
                       return true
                    }
                }
            ";

            const string diagnostics = @"
                Not all code paths return a value.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_Expression_Must_Have_Value()
        {
            const string text = @"
                func test(n: Int32) {
                    return
                }
                let value = [test(100)]
            ";

            const string diagnostics = @"
                Expression must have a value.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [TestCase("[break]", "break")]
        [TestCase("[continue]", "continue")]
        public void Evaluator_Invalid_Break_Or_Continue(string text, string keyword)
        {
            var diagnostics = $@"
                The keyword '{keyword}' can only be used inside of loops.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_Invalid_Return()
        {
            const string text = @"
                [return]
            ";

            const string diagnostics = @"
                The 'return' keyword can only be used inside of functions.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_Parameter_Already_Declared()
        {
            const string text = @"
                func sum(a: Int32, b: Int32, [a: Int32]) -> Int32 {
                    return a + b + c
                }
            ";

            const string diagnostics = @"
                A parameter with the name 'a' already exists.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_Function_Must_Have_Name()
        {
            const string text = @"
                func [(]a: Int32, b: Int32) -> Int32 {
                    return a + b
                }
            ";

            const string diagnostics = @"
                Unexpected token <OpenParenthesisToken>, expected <IdentifierToken>.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_Wrong_Argument_Type()
        {
            const string text = @"
                func test(n: Int32) -> Bool {
                    return n > 10
                }
                let testValue = ""string""
                test([testValue])
            ";

            const string diagnostics = @"
                Parameter 'n' requires a value of type 'Int32' but was given a value of type 'String'.
            ";

            AssertDiagnostics(text, diagnostics);
        }

        [Test]
        public void Evaluator_Bad_Type()
        {
            const string text = @"
                func test(n: [invalidType]) { }
            ";

            const string diagnostics = @"
                Type 'invalidType' doesn't exist.
            ";

            AssertDiagnostics(text, diagnostics);
        }
    }
}
