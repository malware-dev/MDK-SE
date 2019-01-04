using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DocGen.Markdown
{
    class MarkdownWriter
    {
        static readonly string[] NewLines = {"\r\n", "\n", "\r"};
        static string SoftNewlines(string text) => Regex.Replace(text, @" *(?:\r\n|\n|\r)", $"  {Environment.NewLine}");
        static string HtmlNewlines(string text) => Regex.Replace(text, @" *(?:\r\n|\n|\r)", "<br />");

        static string TrimNewlines(string text)
        {
            var lines = text.Split(NewLines, StringSplitOptions.None).ToList();
            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[lines.Count - 1]))
                lines.RemoveAt(lines.Count - 1);
            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[0]))
                lines.RemoveAt(0);
            return string.Join(Environment.NewLine, lines);
        }

        readonly TextWriter _writer;
        readonly Stack<ITransaction> _transaction = new Stack<ITransaction>();

        public MarkdownWriter(TextWriter writer)
        {
            _writer = writer;
            _transaction.Push(new RootTransaction(this));
        }

        void EndTransaction(ITransaction transaction)
        {
            if (_transaction.Count == 1)
                throw new InvalidOperationException("No transaction to end");
            if (_transaction.Peek() != transaction)
                throw new InvalidOperationException("Bad transaction disposal order");
            _transaction.Pop();
        }

        public Task WriteAsync(string text) => _transaction.Peek().WriteAsync(text);
        public Task WriteLineAsync(string text) => _transaction.Peek().WriteLineAsync(text);
        public Task WriteLineAsync() => WriteLineAsync("");

        public Task BeginParagraphAsync()
        {
            _transaction.Push(new ParagraphTransaction(this));
            return Task.CompletedTask;
        }

        public Task EndParagraphAsync() => ((ParagraphTransaction)_transaction.Peek()).CommitAsync();

        public Task BeginCodeBlockAsync()
        {
            _transaction.Push(new CodeTransaction(this));
            return Task.CompletedTask;
        }

        public Task EndCodeBlockAsync() => ((CodeTransaction)_transaction.Peek()).CommitAsync();

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

        public Task EndTableAsync() => ((TableTransaction)_transaction.Peek()).CommitAsync();

        public Task BeginTableCellAsync()
        {
            var table = _transaction.Peek() as TableTransaction;
            if (table == null)
                throw new InvalidOperationException("Table Cells must be within Tables");
            _transaction.Push(new TableCellTransaction(this, table));
            return Task.CompletedTask;
        }

        public Task EndTableCellAsync() => ((TableCellTransaction)_transaction.Peek()).CommitAsync();

        public Task FlushAsync() => _writer.FlushAsync();

        public interface ITransaction
        {
            Task WriteAsync(string text);
            Task WriteLineAsync(string text);
            Task CommitAsync();
        }

        class RootTransaction : ITransaction
        {
            MarkdownWriter _writer;

            public RootTransaction(MarkdownWriter writer)
            {
                _writer = writer;
            }

            public Task WriteAsync(string text) => _writer._writer.WriteAsync(text);
            public Task WriteLineAsync(string text) => _writer._writer.WriteLineAsync(text);
            public Task CommitAsync() => Task.CompletedTask;
        }

        class ParagraphTransaction : ITransaction
        {
            MarkdownWriter _writer;
            StringWriter _buffer = new StringWriter();

            public ParagraphTransaction(MarkdownWriter writer)
            {
                _writer = writer;
            }

            public Task WriteAsync(string text) => _buffer.WriteAsync(text);

            public Task WriteLineAsync(string text) => _buffer.WriteLineAsync(text);

            public async Task CommitAsync()
            {
                _writer.EndTransaction(this);
                var content = SoftNewlines(TrimNewlines(_buffer.ToString()));
                await _writer.WriteLineAsync(content);
                await _writer.WriteLineAsync();
            }
        }

        class CodeTransaction : ITransaction
        {
            MarkdownWriter _writer;
            StringWriter _buffer = new StringWriter();

            public CodeTransaction(MarkdownWriter writer)
            {
                _writer = writer;
            }

            public Task WriteAsync(string text) => _buffer.WriteAsync(text);

            public Task WriteLineAsync(string text) => _buffer.WriteLineAsync(text);

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

        class TableTransaction : ITransaction
        {
            readonly string[] _headers;
            MarkdownWriter _writer;
            List<string> _cells = new List<string>();

            public TableTransaction(MarkdownWriter writer, string[] headers)
            {
                _writer = writer;
                _headers = headers;
            }

            public Task WriteAsync(string text) => throw new InvalidOperationException("Begin a cell first");

            public Task WriteLineAsync(string text) => throw new InvalidOperationException("Begin a cell first");

            public async Task CommitAsync()
            {
                _writer.EndTransaction(this);
                await _writer.WriteAsync("|");
                await _writer.WriteAsync(string.Join("|", _headers.Select(h => HtmlNewlines(TrimNewlines(h)))));
                await _writer.WriteLineAsync("|");
                await _writer.WriteAsync("|");
                await _writer.WriteAsync(string.Join("|", Enumerable.Range(0, _headers.Length).Select(n => "---")));
                await _writer.WriteLineAsync("|");

                var rows = (int)Math.Ceiling(_cells.Count / 2.0);
                for (var row = 0; row < rows; row++)
                {
                    var range = _cells.Skip(row * 2).Take(_headers.Length);
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

        class TableCellTransaction : ITransaction
        {
            readonly MarkdownWriter _writer;
            readonly TableTransaction _table;
            StringWriter _buffer = new StringWriter();

            public TableCellTransaction(MarkdownWriter writer, TableTransaction table)
            {
                _writer = writer;
                _table = table;
            }

            public Task WriteAsync(string text) => _buffer.WriteAsync(text);

            public Task WriteLineAsync(string text) => _buffer.WriteLineAsync(text);

            public Task CommitAsync()
            {
                _writer.EndTransaction(this);
                _table.AddCell(HtmlNewlines(HtmlNewlines(TrimNewlines(_buffer.ToString()))));
                return Task.CompletedTask;
            }
        }
    }
}