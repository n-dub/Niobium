using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Repl
{
    public abstract class Repl
    {
        private readonly MetaCommand[] metaCommands;

        private readonly List<string> submissionHistory = new List<string>();
        private int submissionHistoryIndex;
        private int globalLineCount;

        private bool done;
        private bool exitRequested;

        protected Repl()
        {
            var methods = GetType()
                .GetMethods(BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.Static |
                            BindingFlags.Instance |
                            BindingFlags.FlattenHierarchy)
                .Where(x => x.GetCustomAttribute<MetaCommandAttribute>() != null)
                .ToArray();

            metaCommands = methods
                .Select(x => x.GetCustomAttribute<MetaCommandAttribute>())
                .Zip(methods, (a, m) => new MetaCommand(a.Names, a.Description, m))
                .OrderByDescending(x => x.Names.Contains("help"))
                .ThenBy(x => x.Names[0])
                .ToArray();
        }

        public void Start()
        {
            while (true)
            {
                var text = EditSubmission();

                if (!text.Contains(Environment.NewLine) && text.StartsWith(":"))
                {
                    EvaluateMetaCommand(text);
                    if (exitRequested)
                    {
                        return;
                    }
                }
                else
                {
                    EvaluateSubmission(text);
                }

                submissionHistory.Add(text);
                submissionHistoryIndex = 0;

                globalLineCount += text.Count(x => x == '\n') + 1;
            }
        }

        // ReSharper disable once UnusedMember.Global
        [MetaCommand(new[] {"help", "?"}, "Show this message with the list of commands")]
        protected void EvaluateHelp()
        {
            Console.WriteLine(
                @"In Niobium REPL you can use:
Enter to evaluate an expression, Ctrl+Enter to break the line.
Arrows (↑ and ↓) to navigate within a multi-line submission.
PageUp and PageDown to navigate through submission history.
Home and End to move cursor to the start and to the end of the current line respectively.
Esc to clear the current line.

Meta-commands available:");

            string ConcatNames(IEnumerable<string> names)
            {
                return string.Join(", ", names.Select(x => ":" + x));
            }

            var maxNameLength = metaCommands
                .Select(x => ConcatNames(x.Names))
                .Max(x => x.Length);

            foreach (var metaCommand in metaCommands)
            {
                var parameters = metaCommand.Method.GetParameters();
                var name = ConcatNames(metaCommand.Names);
                if (parameters.Any())
                {
                    var spaces = new string(' ', maxNameLength + 4);
                    var stringParams = string.Join(" ", parameters.Select(x => $"<{x.Name!}>"));
                    Console.WriteLine($"  {name} {stringParams}\n{spaces}{metaCommand.Description}");
                }
                else
                {
                    var paddedName = name.PadRight(maxNameLength);
                    Console.WriteLine($"  {paddedName}  {metaCommand.Description}");
                }
            }
        }

        // ReSharper disable once UnusedMember.Global
        [MetaCommand(new[] {"quit", "q"}, "Exit the REPL")]
        protected void EvaluateQuit()
        {
            exitRequested = true;
        }

        private string EditSubmission()
        {
            done = false;

            var document = new ObservableCollection<string> {""};
            var view = new SubmissionView(RenderLine, document, globalLineCount);

            while (!done)
            {
                var key = Console.ReadKey(true);
                HandleKey(key, document, view);
            }

            view.CurrentLine = document.Count - 1;
            view.CurrentCharacter = document[view.CurrentLine].Length;
            Console.WriteLine();

            return string.Join(Environment.NewLine, document);
        }

        private void HandleKey(ConsoleKeyInfo key, ObservableCollection<string> document, SubmissionView view)
        {
            if (key.Modifiers == default)
            {
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        HandleEscape(document, view);
                        break;
                    case ConsoleKey.Enter:
                        HandleEnter(document, view);
                        break;
                    case ConsoleKey.LeftArrow:
                        HandleLeftArrow(document, view);
                        break;
                    case ConsoleKey.RightArrow:
                        HandleRightArrow(document, view);
                        break;
                    case ConsoleKey.UpArrow:
                        HandleUpArrow(document, view);
                        break;
                    case ConsoleKey.DownArrow:
                        HandleDownArrow(document, view);
                        break;
                    case ConsoleKey.Backspace:
                        HandleBackspace(document, view);
                        break;
                    case ConsoleKey.Delete:
                        HandleDelete(document, view);
                        break;
                    case ConsoleKey.Home:
                        HandleHome(document, view);
                        break;
                    case ConsoleKey.End:
                        HandleEnd(document, view);
                        break;
                    case ConsoleKey.Tab:
                        HandleTab(document, view);
                        break;
                    case ConsoleKey.PageUp:
                        HandlePageUp(document, view);
                        break;
                    case ConsoleKey.PageDown:
                        HandlePageDown(document, view);
                        break;
                }
            }
            else if (key.Modifiers == ConsoleModifiers.Control)
            {
                switch (key.Key)
                {
                    case ConsoleKey.LeftArrow:
                        HandleControlLeftArrow(document, view);
                        break;
                    case ConsoleKey.RightArrow:
                        HandleControlRightArrow(document, view);
                        break;
                    case ConsoleKey.Enter:
                        HandleControlEnter(document, view);
                        break;
                }
            }

            if (key.Key != ConsoleKey.Backspace && key.KeyChar >= ' ')
            {
                HandleTyping(document, view, key.KeyChar.ToString());
            }
        }

        private void HandleEscape(ObservableCollection<string> document, SubmissionView view)
        {
            document.Clear();
            document.Add(string.Empty);
            view.CurrentLine = 0;
            view.CurrentCharacter = 0;
        }

        private void HandleEnter(ObservableCollection<string> document, SubmissionView view)
        {
            var submissionText = string.Join(Environment.NewLine, document);
            if (submissionText.StartsWith(":") || IsCompleteSubmission(submissionText))
            {
                done = true;
                return;
            }

            InsertLine(document, view);
        }

        private void HandleControlEnter(ObservableCollection<string> document, SubmissionView view)
        {
            InsertLine(document, view);
        }

        private static void InsertLine(ObservableCollection<string> document, SubmissionView view)
        {
            var remainder = document[view.CurrentLine].Substring(view.CurrentCharacter);
            document[view.CurrentLine] = document[view.CurrentLine].Substring(0, view.CurrentCharacter);

            var lineIndex = view.CurrentLine + 1;
            document.Insert(lineIndex, remainder);
            view.CurrentCharacter = 0;
            view.CurrentLine = lineIndex;
        }

        private void HandleLeftArrow(ObservableCollection<string> document, SubmissionView view)
        {
            if (view.CurrentCharacter > 0)
            {
                view.CurrentCharacter--;
            }
        }

        private void HandleRightArrow(ObservableCollection<string> document, SubmissionView view)
        {
            var line = document[view.CurrentLine];
            if (view.CurrentCharacter <= line.Length - 1)
            {
                view.CurrentCharacter++;
            }
        }

        private void HandleControlLeftArrow(ObservableCollection<string> document, SubmissionView view)
        {
            var currentLine = document[view.CurrentLine];
            var startIndex = Math.Max(0, view.CurrentCharacter - 1);
            var index = currentLine.LastIndexOf(' ', startIndex);
            view.CurrentCharacter = index != -1 ? index : 0;
        }

        private void HandleControlRightArrow(ObservableCollection<string> document, SubmissionView view)
        {
            var currentLine = document[view.CurrentLine];
            var startIndex = Math.Min(currentLine.Length - 1, view.CurrentCharacter + 1);
            var index = currentLine.IndexOf(' ', startIndex);
            view.CurrentCharacter = index != -1 ? index : currentLine.Length - 1;
        }

        private void HandleUpArrow(ObservableCollection<string> document, SubmissionView view)
        {
            if (view.CurrentLine > 0)
            {
                view.CurrentLine--;
            }
        }

        private void HandleDownArrow(ObservableCollection<string> document, SubmissionView view)
        {
            if (view.CurrentLine < document.Count - 1)
            {
                view.CurrentLine++;
            }
        }

        private void HandleBackspace(ObservableCollection<string> document, SubmissionView view)
        {
            var start = view.CurrentCharacter;
            if (start == 0)
            {
                if (view.CurrentLine == 0)
                {
                    return;
                }

                var currentLine = document[view.CurrentLine];
                var previousLine = document[view.CurrentLine - 1];
                document.RemoveAt(view.CurrentLine);
                view.CurrentLine--;
                document[view.CurrentLine] = previousLine + currentLine;
                view.CurrentCharacter = previousLine.Length;
            }
            else
            {
                var lineIndex = view.CurrentLine;
                var line = document[lineIndex];
                var before = line.Substring(0, start - 1);
                var after = line.Substring(start);
                document[lineIndex] = before + after;
                view.CurrentCharacter--;
            }
        }

        private void HandleDelete(ObservableCollection<string> document, SubmissionView view)
        {
            var lineIndex = view.CurrentLine;
            var line = document[lineIndex];
            var start = view.CurrentCharacter;
            if (start >= line.Length)
            {
                if (view.CurrentLine == document.Count - 1)
                {
                    return;
                }

                var nextLine = document[view.CurrentLine + 1];
                document[view.CurrentLine] += nextLine;
                document.RemoveAt(view.CurrentLine + 1);
                return;
            }

            var before = line.Substring(0, start);
            var after = line.Substring(start + 1);
            document[lineIndex] = before + after;
        }

        private void HandleHome(ObservableCollection<string> document, SubmissionView view)
        {
            view.CurrentCharacter = 0;
        }

        private void HandleEnd(ObservableCollection<string> document, SubmissionView view)
        {
            view.CurrentCharacter = document[view.CurrentLine].Length;
        }

        private void HandleTab(ObservableCollection<string> document, SubmissionView view)
        {
            const int tabWidth = 4;
            var start = view.CurrentCharacter;
            var remainingSpaces = tabWidth - start % tabWidth;
            var line = document[view.CurrentLine];
            document[view.CurrentLine] = line.Insert(start, new string(' ', remainingSpaces));
            view.CurrentCharacter += remainingSpaces;
        }

        private void HandlePageUp(ObservableCollection<string> document, SubmissionView view)
        {
            submissionHistoryIndex--;
            if (submissionHistoryIndex < 0)
            {
                submissionHistoryIndex = submissionHistory.Count - 1;
            }

            UpdateDocumentFromHistory(document, view);
        }

        private void HandlePageDown(ObservableCollection<string> document, SubmissionView view)
        {
            submissionHistoryIndex++;
            if (submissionHistoryIndex > submissionHistory.Count - 1)
            {
                submissionHistoryIndex = 0;
            }

            UpdateDocumentFromHistory(document, view);
        }

        private void UpdateDocumentFromHistory(ObservableCollection<string> document, SubmissionView view)
        {
            if (submissionHistory.Count == 0)
            {
                return;
            }

            document.Clear();

            var historyItem = submissionHistory[submissionHistoryIndex];
            var lines = historyItem.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
            foreach (var line in lines)
            {
                document.Add(line);
            }

            view.CurrentLine = document.Count - 1;
            view.CurrentCharacter = document[view.CurrentLine].Length;
        }

        private void HandleTyping(ObservableCollection<string> document, SubmissionView view, string text)
        {
            var lineIndex = view.CurrentLine;
            var start = view.CurrentCharacter;
            document[lineIndex] = document[lineIndex].Insert(start, text);
            view.CurrentCharacter += text.Length;
        }

        protected virtual object? RenderLine(IReadOnlyList<string> lines, int lineIndex, object? state)
        {
            Console.Write(lines[lineIndex]);
            return state;
        }

        private void EvaluateMetaCommand(string input)
        {
            var args = new List<string>();
            var inQuotes = false;
            var position = 1;
            var sb = new StringBuilder();
            while (position < input.Length)
            {
                var c = input[position];
                var l = position + 1 >= input.Length ? '\0' : input[position + 1];

                if (char.IsWhiteSpace(c))
                {
                    if (!inQuotes)
                    {
                        CommitPendingArgument();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else if (c == '\"')
                {
                    if (!inQuotes)
                    {
                        inQuotes = true;
                    }
                    else if (l == '\"')
                    {
                        sb.Append(c);
                        position++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }

                position++;
            }

            CommitPendingArgument();

            void CommitPendingArgument()
            {
                var arg = sb.ToString();
                if (!string.IsNullOrWhiteSpace(arg))
                {
                    args.Add(arg);
                }

                sb.Clear();
            }

            var commandName = args.FirstOrDefault();
            if (args.Count > 0)
            {
                args.RemoveAt(0);
            }

            var command = metaCommands.SingleOrDefault(mc => mc.Names.Contains(commandName));
            if (command == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Invalid command {input}.");
                Console.ResetColor();
                return;
            }

            var parameters = command.Method.GetParameters();

            if (args.Count != parameters.Length)
            {
                var parameterNames = string.Join(" ", parameters.Select(p => $"<{p.Name}>"));
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("error: invalid number of arguments");
                Console.WriteLine($"usage: :{command.Names[0]} {parameterNames}");
                Console.ResetColor();
                return;
            }

            command.Method.Invoke(command.Method.IsStatic ? null : this, args.Cast<object>().ToArray());
        }

        protected abstract bool IsCompleteSubmission(string text);

        protected abstract void EvaluateSubmission(string text);

        private delegate object? LineRenderHandler(IReadOnlyList<string> lines, int lineIndex, object? state);

        [AttributeUsage(AttributeTargets.Method)]
        protected sealed class MetaCommandAttribute : Attribute
        {
            public string[] Names { get; }
            public string Description { get; }

            public MetaCommandAttribute(string[] names, string description)
            {
                Names = names;
                Description = description;
            }
        }

        private sealed class MetaCommand
        {
            public string[] Names { get; }
            public string Description { get; }
            public MethodInfo Method { get; }

            public MetaCommand(string[] names, string description, MethodInfo method)
            {
                Names = names;
                Description = description;
                Method = method;
            }
        }

        private sealed class SubmissionView
        {
            public int CurrentLine
            {
                get => currentLine;
                set
                {
                    if (currentLine != value)
                    {
                        currentLine = value;
                        currentCharacter = Math.Min(submissionDocument[currentLine].Length, currentCharacter);

                        UpdateCursorPosition();
                    }
                }
            }

            public int CurrentCharacter
            {
                get => currentCharacter;
                set
                {
                    if (currentCharacter != value)
                    {
                        currentCharacter = value;
                        UpdateCursorPosition();
                    }
                }
            }

            private readonly LineRenderHandler lineRenderer;
            private readonly ObservableCollection<string> submissionDocument;
            private int cursorTop;
            private int renderedLineCount;
            private int currentLine;
            private int currentCharacter;

            private readonly int lineCounterOffset;

            public SubmissionView(LineRenderHandler lineRenderer, ObservableCollection<string> submissionDocument,
                int lineCounterOffset)
            {
                this.lineRenderer = lineRenderer;
                this.submissionDocument = submissionDocument;
                this.lineCounterOffset = lineCounterOffset;
                this.submissionDocument.CollectionChanged += SubmissionDocumentChanged;
                cursorTop = Console.CursorTop;
                Render();
            }

            private void SubmissionDocumentChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                Render();
            }

            private void Render()
            {
                Console.CursorVisible = false;

                var lineCount = 0;
                object? state = null;

                foreach (var line in submissionDocument)
                {
                    if (cursorTop + lineCount >= Console.WindowHeight)
                    {
                        Console.SetCursorPosition(0, Console.WindowHeight - 1);
                        Console.WriteLine();
                        if (cursorTop > 0)
                        {
                            cursorTop--;
                        }
                    }

                    Console.SetCursorPosition(0, cursorTop + lineCount);
                    Console.ForegroundColor = ConsoleColor.DarkGray;

                    Console.Write($"{lineCounterOffset + lineCount + 1,4}{(lineCount == 0 ? '>' : '.')} ");

                    Console.ResetColor();
                    state = lineRenderer(submissionDocument, lineCount, state);
                    Console.Write(new string(' ', Console.WindowWidth - line.Length - 2));
                    lineCount++;
                }

                var numberOfBlankLines = renderedLineCount - lineCount;
                if (numberOfBlankLines > 0)
                {
                    var blankLine = new string(' ', Console.WindowWidth);
                    for (var i = 0; i < numberOfBlankLines; i++)
                    {
                        Console.SetCursorPosition(0, cursorTop + lineCount + i);
                        Console.WriteLine(blankLine);
                    }
                }

                renderedLineCount = lineCount;

                Console.CursorVisible = true;
                UpdateCursorPosition();
            }

            private void UpdateCursorPosition()
            {
                Console.CursorTop = cursorTop + currentLine;
                Console.CursorLeft = 6 + currentCharacter;
            }
        }
    }
}
