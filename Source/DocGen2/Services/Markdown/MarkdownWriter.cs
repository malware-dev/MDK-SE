using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mal.DocGen2.Services.Markdown
{
    internal class MarkdownWriter
    {
        private static readonly string[] NewLines = {"\r\n", "\n", "\r"};

        private static string SoftNewlines(string text)
        {
            return Regex.Replace(text, @" *(?:\r\n|\n|\r)", $"  {Environment.NewLine}");
        }

        private static string HtmlNewlines(string text)
        {
            return Regex.Replace(text, @" *(?:\r\n|\n|\r)", "<br />");
        }

        private static string TrimNewlines(string text)
        {
            var lines = text.Split(NewLines, StringSplitOptions.None).ToList();
            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[lines.Count - 1]))
                lines.RemoveAt(lines.Count - 1);
            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
                lines.RemoveAt(0);
            return string.Join(Environment.NewLine, lines);
        }

        private readonly TextWriter _writer;
        private readonly Stack<ITransaction> _transaction = new Stack<ITransaction>();

        public MarkdownWriter(TextWriter writer)
        {
            _writer = writer;
            _transaction.Push(new RootTransaction(this));
        }

        private void EndTransaction(ITransaction transaction)
        {
            if (_transaction.Count == 1)
                throw new InvalidOperationException("No transaction to end");
            if (_transaction.Peek() != transaction)
                throw new InvalidOperationException("Bad transaction disposal order");
            _transaction.Pop();
        }

        public Task WriteAsync(string text)
        {
            return _transaction.Peek().WriteAsync(text);
        }

        public Task WriteLineAsync(string text)
        {
            return _transaction.Peek().WriteLineAsync(text);
        }

        public Task WriteLineAsync()
        {
            return WriteLineAsync("");
        }

        public Task BeginParagraphAsync()
        {
            _transaction.Push(new ParagraphTransaction(this));
            return Task.CompletedTask;
        }

        public Task EndParagraphAsync()
        {
            return ((ParagraphTransaction)_transaction.Peek()).CommitAsync();
        }

        public Task BeginCodeBlockAsync()
        {
            _transaction.Push(new CodeTransaction(this));
            return Task.CompletedTask;
        }

        public Task EndCodeBlockAsync()
        {
            return ((CodeTransaction)_transaction.Peek()).CommitAsync();
        }

        public async Task WriteHeaderAsync(int type, string text)
        {
            await WriteAsync(new string('#', type));
            await WriteAsync(" ");
            await WriteLineAsync(HtmlNewlines(TrimNewlines(text)));
            await WriteLineAsync();
        }

        public async Task WriteUnorderedListItemAsync(string text)
        {
            await WriteAsync("* ");
            await WriteLineAsync(SoftNewlines(TrimNewlines(text)));
        }

        public Task BeginTableAsync(params string[] headers)
        {
            _transaction.Push(new TableTransaction(this, headers));
            return Task.CompletedTask;
        }

        public Task EndTableAsync()
        {
            return ((TableTransaction)_transaction.Peek()).CommitAsync();
        }

        public Task BeginTableCellAsync()
        {
            var table = _transaction.Peek() as TableTransaction;
            if (table == null)
                throw new InvalidOperationException("Table Cells must be within Tables");
            _transaction.Push(new TableCellTransaction(this, table));
            return Task.CompletedTask;
        }

        public Task EndTableCellAsync()
        {
            return ((TableCellTransaction)_transaction.Peek()).CommitAsync();
        }

        public Task FlushAsync()
        {
            return _writer.FlushAsync();
        }

        public async Task WriteImageLinkAsync(string description, string imageUrl)
        {
            await WriteAsync("![");
            await WriteAsync(description);
            await WriteAsync("](");
            await WriteAsync(imageUrl);
            await WriteAsync(")");
        }

        public async Task WriteLinkAsync(string description, string url)
        {
            await WriteAsync("[");
            await WriteAsync(description);
            await WriteAsync("](");
            await WriteAsync(url);
            await WriteAsync(")");
        }

        public Task WriteRulerAsync()
        {
            return WriteLineAsync("- - -");
        }

        public interface ITransaction
        {
            Task WriteAsync(string text);
            Task WriteLineAsync(string text);
            Task CommitAsync();
        }

        private class RootTransaction: ITransaction
        {
            private readonly MarkdownWriter _writer;

            public RootTransaction(MarkdownWriter writer)
            {
                _writer = writer;
            }

            public Task WriteAsync(string text)
            {
                return _writer._writer.WriteAsync(text);
            }

            public Task WriteLineAsync(string text)
            {
                return _writer._writer.WriteLineAsync(text);
            }

            public Task CommitAsync()
            {
                return Task.CompletedTask;
            }
        }

        private class ParagraphTransaction: ITransaction
        {
            private readonly MarkdownWriter _writer;
            private readonly StringWriter _buffer = new StringWriter();

            public ParagraphTransaction(MarkdownWriter writer)
            {
                _writer = writer;
            }

            public Task WriteAsync(string text)
            {
                return _buffer.WriteAsync(text);
            }

            public Task WriteLineAsync(string text)
            {
                return _buffer.WriteLineAsync(text);
            }

            public async Task CommitAsync()
            {
                _writer.EndTransaction(this);
                var content = SoftNewlines(TrimNewlines(_buffer.ToString()));
                await _writer.WriteLineAsync(content);
                await _writer.WriteLineAsync();
            }
        }

        private class CodeTransaction: ITransaction
        {
            private readonly MarkdownWriter _writer;
            private readonly StringWriter _buffer = new StringWriter();

            public CodeTransaction(MarkdownWriter writer)
            {
                _writer = writer;
            }

            public Task WriteAsync(string text)
            {
                return _buffer.WriteAsync(text);
            }

            public Task WriteLineAsync(string text)
            {
                return _buffer.WriteLineAsync(text);
            }

            public async Task CommitAsync()
            {
                _writer.EndTransaction(this);
                await _writer.WriteLineAsync("```csharp");
                var content = SoftNewlines(TrimNewlines(_buffer.ToString()));
                await _writer.WriteLineAsync(content);
                await _writer.WriteLineAsync("```");
                await _writer.WriteLineAsync();
            }
        }

        private class TableTransaction: ITransaction
        {
            private readonly string[] _headers;
            private readonly MarkdownWriter _writer;
            private readonly List<string> _cells = new List<string>();

            public TableTransaction(MarkdownWriter writer, string[] headers)
            {
                _writer = writer;
                _headers = headers;
            }

            public Task WriteAsync(string text)
            {
                throw new InvalidOperationException("Begin a cell first");
            }

            public Task WriteLineAsync(string text)
            {
                throw new InvalidOperationException("Begin a cell first");
            }

            public async Task CommitAsync()
            {
                _writer.EndTransaction(this);
                await _writer.WriteAsync("|");
                await _writer.WriteAsync(string.Join("|", _headers.Select(h => HtmlNewlines(TrimNewlines(h)))));
                await _writer.WriteLineAsync("|");
                await _writer.WriteAsync("|");
                await _writer.WriteAsync(string.Join("|", Enumerable.Range(0, _headers.Length).Select(n => "---")));
                await _writer.WriteLineAsync("|");

                var rows = (int)Math.Ceiling(_cells.Count / (double)_headers.Length);
                for (var row = 0; row < rows; row++)
                {
                    var range = _cells.Skip(row * _headers.Length).Take(_headers.Length);
                    await _writer.WriteAsync("|");
                    await _writer.WriteAsync(string.Join("|", range));
                    await _writer.WriteLineAsync("|");
                }

                await _writer.WriteLineAsync();
            }

            public void AddCell(string content)
            {
                _cells.Add(content);
            }
        }

        private class TableCellTransaction: ITransaction
        {
            private readonly MarkdownWriter _writer;
            private readonly TableTransaction _table;
            private readonly StringWriter _buffer = new StringWriter();

            public TableCellTransaction(MarkdownWriter writer, TableTransaction table)
            {
                _writer = writer;
                _table = table;
            }

            public Task WriteAsync(string text)
            {
                return _buffer.WriteAsync(text);
            }

            public Task WriteLineAsync(string text)
            {
                return _buffer.WriteLineAsync(text);
            }

            public Task CommitAsync()
            {
                _writer.EndTransaction(this);
                _table.AddCell(HtmlNewlines(TrimNewlines(_buffer.ToString())));
                return Task.CompletedTask;
            }
        }
    }
}