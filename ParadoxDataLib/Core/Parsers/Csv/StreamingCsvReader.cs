using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ParadoxDataLib.Core.Parsers.Csv
{
    /// <summary>
    /// High-performance streaming CSV reader optimized for Paradox game data files.
    /// Uses Span&lt;T&gt; and Memory&lt;T&gt; for efficient parsing with minimal allocations.
    /// </summary>
    public class StreamingCsvReader : ICsvReader
    {
        private StreamReader _streamReader;
        private readonly char _separator;
        private readonly Encoding _encoding;
        private readonly char _quoteChar;
        private readonly bool _trimFields;
        private bool _disposed;

        /// <summary>
        /// Creates a new streaming CSV reader
        /// </summary>
        /// <param name="separator">Field separator character (default: semicolon for Paradox files)</param>
        /// <param name="encoding">Text encoding (default: Windows-1252 for Paradox files)</param>
        /// <param name="quoteChar">Quote character for escaping (default: double quote)</param>
        /// <param name="trimFields">Whether to trim whitespace from fields</param>
        public StreamingCsvReader(char separator = ';', Encoding encoding = null,
                                  char quoteChar = '"', bool trimFields = true)
        {
            _separator = separator;
            _encoding = encoding ?? GetWindows1252Encoding();
            _quoteChar = quoteChar;
            _trimFields = trimFields;
        }

        /// <summary>
        /// The character used to separate CSV fields
        /// </summary>
        public char Separator => _separator;

        /// <summary>
        /// The encoding used to read the CSV file
        /// </summary>
        public Encoding Encoding => _encoding;

        /// <summary>
        /// Whether the CSV file has been opened for reading
        /// </summary>
        public bool IsOpen => _streamReader != null && !_disposed;

        /// <summary>
        /// Opens a CSV file for reading
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        public void Open(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"CSV file not found: {filePath}");

            Close(); // Close any existing stream

            try
            {
                _streamReader = new StreamReader(filePath, _encoding, detectEncodingFromByteOrderMarks: true);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to open CSV file: {filePath}", ex);
            }
        }

        /// <summary>
        /// Asynchronously opens a CSV file for reading
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        public Task OpenAsync(string filePath)
        {
            return Task.Run(() => Open(filePath));
        }

        /// <summary>
        /// Reads a single line from the CSV file and splits it into fields
        /// </summary>
        /// <returns>Array of field values, or null if end of file</returns>
        public string[] ReadLine()
        {
            EnsureOpen();

            var line = _streamReader.ReadLine();
            return line == null ? null : ParseLine(line);
        }

        /// <summary>
        /// Asynchronously reads a single line from the CSV file and splits it into fields
        /// </summary>
        /// <returns>Array of field values, or null if end of file</returns>
        public async Task<string[]> ReadLineAsync()
        {
            EnsureOpen();

            var line = await _streamReader.ReadLineAsync();
            return line == null ? null : ParseLine(line);
        }

        /// <summary>
        /// Reads all lines from the CSV file
        /// </summary>
        /// <returns>Collection of field arrays</returns>
        public IEnumerable<string[]> ReadAllLines()
        {
            EnsureOpen();

            string line;
            while ((line = _streamReader.ReadLine()) != null)
            {
                yield return ParseLine(line);
            }
        }

        /// <summary>
        /// Asynchronously reads all lines from the CSV file
        /// </summary>
        /// <returns>Collection of field arrays</returns>
        public async IAsyncEnumerable<string[]> ReadAllLinesAsync()
        {
            EnsureOpen();

            string line;
            while ((line = await _streamReader.ReadLineAsync()) != null)
            {
                yield return ParseLine(line);
            }
        }

        /// <summary>
        /// Reads the header row from the CSV file
        /// </summary>
        /// <returns>Array of column names</returns>
        public string[] ReadHeader()
        {
            EnsureOpen();
            return ReadLine();
        }

        /// <summary>
        /// Asynchronously reads the header row from the CSV file
        /// </summary>
        /// <returns>Array of column names</returns>
        public Task<string[]> ReadHeaderAsync()
        {
            EnsureOpen();
            return ReadLineAsync();
        }

        /// <summary>
        /// Parses a CSV line into fields using Span&lt;T&gt; for high performance
        /// </summary>
        /// <param name="line">The CSV line to parse</param>
        /// <returns>Array of field values</returns>
        public string[] ParseLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return Array.Empty<string>();

            var span = line.AsSpan();
            var fields = new List<string>();
            var currentField = new StringBuilder();
            var inQuotes = false;
            var i = 0;

            while (i < span.Length)
            {
                var c = span[i];

                if (c == _quoteChar)
                {
                    if (inQuotes && i + 1 < span.Length && span[i + 1] == _quoteChar)
                    {
                        // Escaped quote - add single quote to field
                        currentField.Append(_quoteChar);
                        i += 2;
                    }
                    else
                    {
                        // Start or end quotes
                        inQuotes = !inQuotes;
                        i++;
                    }
                }
                else if (c == _separator && !inQuotes)
                {
                    // Field separator - complete current field
                    var fieldValue = currentField.ToString();
                    if (_trimFields)
                        fieldValue = fieldValue.Trim();
                    fields.Add(fieldValue);
                    currentField.Clear();
                    i++;
                }
                else
                {
                    // Regular character
                    currentField.Append(c);
                    i++;
                }
            }

            // Add the final field
            var finalField = currentField.ToString();
            if (_trimFields)
                finalField = finalField.Trim();
            fields.Add(finalField);

            return fields.ToArray();
        }

        /// <summary>
        /// Closes the CSV file and releases resources
        /// </summary>
        public void Close()
        {
            _streamReader?.Close();
            _streamReader?.Dispose();
            _streamReader = null;
        }

        /// <summary>
        /// Disposes of the CSV reader resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Close();
                _disposed = true;
            }
        }

        /// <summary>
        /// Gets Windows-1252 encoding for Paradox game files
        /// </summary>
        private static Encoding GetWindows1252Encoding()
        {
            try
            {
                // Register the encoding provider for .NET Core/.NET 5+
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                return Encoding.GetEncoding(1252);
            }
            catch
            {
                // Fallback to UTF-8 if Windows-1252 is not available
                return Encoding.UTF8;
            }
        }

        /// <summary>
        /// Ensures the reader is open and ready for reading
        /// </summary>
        private void EnsureOpen()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StreamingCsvReader));

            if (_streamReader == null)
                throw new InvalidOperationException("CSV reader is not open. Call Open() first.");
        }
    }
}