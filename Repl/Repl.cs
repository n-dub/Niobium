﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace Repl
{
    public abstract class Repl
    {
        private readonly List<string> submissionHistory = new List<string>();
        private int submissionHistoryIndex;
        private int globalLineCount;

        private bool done;

        public void Start()
        {
            while (true)
            {
                var text = EditSubmission();
                if (text == ":quit")
                {
                    return;
                }

                if (!text.Contains(Environment.NewLine) && text.StartsWith(":"))
                {
                    EvaluateMetaCommand(text);
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

            if (key.KeyChar >= ' ')
            {
                HandleTyping(document, view, key.KeyChar.ToString());
            }
        }

        private void HandleEscape(ObservableCollection<string> document, SubmissionView view)
        {
            document[view.CurrentLine] = string.Empty;
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

        protected void ClearHistory()
        {
            submissionHistory.Clear();
        }

        protected virtual void RenderLine(string line)
        {
            Console.Write(line);
        }

        protected virtual void EvaluateMetaCommand(string input)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Invalid command {input}.");
            Console.ResetColor();
        }

        protected abstract bool IsCompleteSubmission(string text);

        protected abstract void EvaluateSubmission(string text);

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

            private readonly Action<string> lineRenderer;
            private readonly ObservableCollection<string> submissionDocument;
            private readonly int cursorTop;
            private int renderedLineCount;
            private int currentLine;
            private int currentCharacter;

            private readonly int lineCounterOffset;

            public SubmissionView(Action<string> lineRenderer, ObservableCollection<string> submissionDocument,
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

                foreach (var line in submissionDocument)
                {
                    Console.SetCursorPosition(0, cursorTop + lineCount);
                    Console.ForegroundColor = ConsoleColor.DarkGray;

                    Console.Write($"{lineCounterOffset + lineCount + 1,4}{(lineCount == 0 ? '>' : '.')} ");

                    Console.ResetColor();
                    lineRenderer(line);
                    Console.WriteLine(new string(' ', Console.WindowWidth - line.Length));
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