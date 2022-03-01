using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LanguageCore.CodeAnalysis.Symbols;
using LanguageCore.CodeAnalysis.Syntax;

namespace LanguageCore.CodeAnalysis.Binding
{
    internal sealed class ControlFlowGraph
    {
        public BasicBlock Start { get; }
        public BasicBlock End { get; }
        public List<BasicBlock> Blocks { get; }
        public List<BasicBlockBranch> Branches { get; }

        private ControlFlowGraph(BasicBlock start, BasicBlock end, List<BasicBlock> blocks,
            List<BasicBlockBranch> branches)
        {
            Start = start;
            End = end;
            Blocks = blocks;
            Branches = branches;
        }

        public void WriteTo(TextWriter writer)
        {
            string Quote(string text)
            {
                return "\"" + text.TrimEnd()
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace(Environment.NewLine, "\\l") + "\"";
            }

            writer.WriteLine("digraph G {");

            var blockIds = new Dictionary<BasicBlock, string>();

            for (var i = 0; i < Blocks.Count; i++)
            {
                var id = $"N{i}";
                blockIds.Add(Blocks[i], id);
            }

            foreach (var block in Blocks)
            {
                var id = blockIds[block];
                var label = Quote(block.ToString());
                writer.WriteLine($"    {id} [label = {label}, shape = box]");
            }

            foreach (var branch in Branches)
            {
                var fromId = blockIds[branch.From];
                var toId = blockIds[branch.To];
                var label = Quote(branch.ToString());
                writer.WriteLine($"    {fromId} -> {toId} [label = {label}]");
            }

            writer.WriteLine("}");
        }

        public static ControlFlowGraph Create(BoundBlockStatement body)
        {
            var basicBlockBuilder = new BasicBlockBuilder();
            var blocks = basicBlockBuilder.Build(body);

            var graphBuilder = new GraphBuilder();
            return graphBuilder.Build(blocks);
        }

        public static bool AllPathsReturn(BoundBlockStatement body)
        {
            var graph = Create(body);

            foreach (var branch in graph.End.Incoming)
            {
                var lastStatement = branch.From.Statements.LastOrDefault();
                if (lastStatement?.Kind != BoundNodeKind.ReturnStatement)
                {
                    return false;
                }
            }

            return true;
        }

        public sealed class BasicBlock
        {
            public bool IsStart { get; }
            public bool IsEnd { get; }
            public List<BoundStatement> Statements { get; } = new List<BoundStatement>();
            public List<BasicBlockBranch> Incoming { get; } = new List<BasicBlockBranch>();
            public List<BasicBlockBranch> Outgoing { get; } = new List<BasicBlockBranch>();

            public BasicBlock()
            {
            }

            public BasicBlock(bool isStart)
            {
                IsStart = isStart;
                IsEnd = !isStart;
            }

            public override string ToString()
            {
                if (IsStart)
                {
                    return "<Start>";
                }

                if (IsEnd)
                {
                    return "<End>";
                }

                using (var writer = new StringWriter())
                using (var indentedWriter = new IndentedTextWriter(writer))
                {
                    foreach (var statement in Statements)
                    {
                        statement.WriteTo(indentedWriter);
                    }

                    return writer.ToString();
                }
            }
        }

        public sealed class BasicBlockBranch
        {
            public BasicBlock From { get; }
            public BasicBlock To { get; }
            public BoundExpression Condition { get; }

            public BasicBlockBranch(BasicBlock from, BasicBlock to, BoundExpression condition)
            {
                From = from;
                To = to;
                Condition = condition;
            }

            public override string ToString()
            {
                return Condition?.ToString() ?? string.Empty;
            }
        }

        public sealed class BasicBlockBuilder
        {
            private readonly List<BoundStatement> statements = new List<BoundStatement>();
            private readonly List<BasicBlock> blocks = new List<BasicBlock>();

            public List<BasicBlock> Build(BoundBlockStatement block)
            {
                foreach (var statement in block.Statements)
                {
                    switch (statement.Kind)
                    {
                        case BoundNodeKind.LabelStatement:
                            StartBlock();
                            statements.Add(statement);
                            break;
                        case BoundNodeKind.GotoStatement:
                        case BoundNodeKind.ConditionalGotoStatement:
                        case BoundNodeKind.ReturnStatement:
                            statements.Add(statement);
                            StartBlock();
                            break;
                        case BoundNodeKind.VariableDeclarationStatement:
                        case BoundNodeKind.ExpressionStatement:
                            statements.Add(statement);
                            break;
                        default:
                            throw new Exception($"Unexpected statement: {statement.Kind}");
                    }
                }

                EndBlock();

                return blocks.ToList();
            }

            private void StartBlock()
            {
                EndBlock();
            }

            private void EndBlock()
            {
                if (statements.Any())
                {
                    var block = new BasicBlock();
                    block.Statements.AddRange(statements);
                    blocks.Add(block);
                    statements.Clear();
                }
            }
        }

        public sealed class GraphBuilder
        {
            private readonly Dictionary<BoundStatement, BasicBlock> blockFromStatement =
                new Dictionary<BoundStatement, BasicBlock>();

            private readonly Dictionary<BoundLabel, BasicBlock> blockFromLabel =
                new Dictionary<BoundLabel, BasicBlock>();

            private readonly List<BasicBlockBranch> branches = new List<BasicBlockBranch>();
            private readonly BasicBlock start = new BasicBlock(true);
            private readonly BasicBlock end = new BasicBlock(false);

            public ControlFlowGraph Build(List<BasicBlock> blocks)
            {
                if (!blocks.Any())
                {
                    Connect(start, end);
                }
                else
                {
                    Connect(start, blocks.First());
                }

                foreach (var block in blocks)
                {
                    foreach (var statement in block.Statements)
                    {
                        blockFromStatement.Add(statement, block);
                        if (statement is BoundLabelStatement labelStatement)
                        {
                            blockFromLabel.Add(labelStatement.Label, block);
                        }
                    }
                }

                for (var i = 0; i < blocks.Count; i++)
                {
                    var current = blocks[i];
                    var next = i == blocks.Count - 1 ? end : blocks[i + 1];

                    foreach (var statement in current.Statements)
                    {
                        var isLastStatementInBlock = statement == current.Statements.Last();
                        switch (statement.Kind)
                        {
                            case BoundNodeKind.GotoStatement:
                                var gs = (BoundGotoStatement) statement;
                                var toBlock = blockFromLabel[gs.Label];
                                Connect(current, toBlock);
                                break;
                            case BoundNodeKind.ConditionalGotoStatement:
                                var cgs = (BoundConditionalGotoStatement) statement;
                                var thenBlock = blockFromLabel[cgs.Label];
                                var negatedCondition = Negate(cgs.Condition);
                                var thenCondition = cgs.JumpIfTrue ? cgs.Condition : negatedCondition;
                                var elseCondition = cgs.JumpIfTrue ? negatedCondition : cgs.Condition;
                                Connect(current, thenBlock, thenCondition);
                                Connect(current, next, elseCondition);
                                break;
                            case BoundNodeKind.ReturnStatement:
                                Connect(current, end);
                                break;
                            case BoundNodeKind.VariableDeclarationStatement:
                            case BoundNodeKind.LabelStatement:
                            case BoundNodeKind.ExpressionStatement:
                                if (isLastStatementInBlock)
                                {
                                    Connect(current, next);
                                }

                                break;
                            default:
                                throw new Exception($"Unexpected statement: {statement.Kind}");
                        }
                    }
                }

                var scanAgain = true;
                while (scanAgain)
                {
                    scanAgain = false;
                    foreach (var block in blocks.Where(block => !block.Incoming.Any()))
                    {
                        RemoveBlock(blocks, block);
                        scanAgain = true;
                        break;
                    }
                }

                blocks.Insert(0, start);
                blocks.Add(end);

                return new ControlFlowGraph(start, end, blocks, branches);
            }

            private void Connect(BasicBlock from, BasicBlock to, BoundExpression condition = null)
            {
                if (condition is BoundLiteralExpression l)
                {
                    var value = (bool) l.Value;
                    if (value)
                    {
                        condition = null;
                    }
                    else
                    {
                        return;
                    }
                }

                var branch = new BasicBlockBranch(from, to, condition);
                from.Outgoing.Add(branch);
                to.Incoming.Add(branch);
                branches.Add(branch);
            }

            private void RemoveBlock(List<BasicBlock> blocks, BasicBlock block)
            {
                foreach (var branch in block.Incoming)
                {
                    branch.From.Outgoing.Remove(branch);
                    branches.Remove(branch);
                }

                foreach (var branch in block.Outgoing)
                {
                    branch.To.Incoming.Remove(branch);
                    branches.Remove(branch);
                }

                blocks.Remove(block);
            }

            private BoundExpression Negate(BoundExpression condition)
            {
                if (condition is BoundLiteralExpression literal)
                {
                    var value = (bool) literal.Value;
                    return new BoundLiteralExpression(!value);
                }

                var op = BoundUnaryOperator.Bind(SyntaxKind.BangToken, TypeSymbol.Bool);
                return new BoundUnaryExpression(op, condition);
            }
        }
    }
}
