using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ParadoxDataLib.Core.Parsers.Csv.Mappers;

namespace ParadoxDataLib.Core.Parsers.Csv
{
    /// <summary>
    /// Exception thrown when CSV parsing fails
    /// </summary>
    public class CsvParsingException : Exception
    {
        /// <summary>
        /// Line number where the error occurred
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// The CSV file path where the error occurred
        /// </summary>
        public string FilePath { get; }

        public CsvParsingException(string message, int lineNumber = 0, string filePath = null)
            : base(message)
        {
            LineNumber = lineNumber;
            FilePath = filePath;
        }

        public CsvParsingException(string message, Exception innerException, int lineNumber = 0, string filePath = null)
            : base(message, innerException)
        {
            LineNumber = lineNumber;
            FilePath = filePath;
        }
    }

    /// <summary>
    /// Represents parsing statistics for a CSV operation
    /// </summary>
    public class CsvParsingStats
    {
        /// <summary>
        /// Total number of rows processed (including header)
        /// </summary>
        public int TotalRows { get; set; }

        /// <summary>
        /// Number of data rows successfully parsed
        /// </summary>
        public int SuccessfulRows { get; set; }

        /// <summary>
        /// Number of rows that failed parsing
        /// </summary>
        public int FailedRows { get; set; }

        /// <summary>
        /// Time taken to complete the parsing operation
        /// </summary>
        public TimeSpan ElapsedTime { get; set; }

        /// <summary>
        /// List of errors encountered during parsing
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Whether the parsing operation completed successfully
        /// </summary>
        public bool IsSuccessful => FailedRows == 0;

        /// <summary>
        /// Parsing rate in rows per second
        /// </summary>
        public double RowsPerSecond => ElapsedTime.TotalSeconds > 0 ? TotalRows / ElapsedTime.TotalSeconds : 0;
    }

    /// <summary>
    /// Generic CSV parser that uses pluggable row mappers to convert CSV data to strongly-typed objects.
    /// Provides high-performance parsing with error handling and statistics collection.
    /// </summary>
    /// <typeparam name="T">The type of objects to create from CSV rows</typeparam>
    public class CsvParser<T>
    {
        private readonly ICsvRowMapper<T> _mapper;
        private readonly ICsvReader _csvReader;
        private readonly bool _skipHeaderValidation;
        private readonly bool _continueOnError;

        /// <summary>
        /// Creates a new CSV parser with the specified mapper and reader
        /// </summary>
        /// <param name="mapper">Row mapper to convert CSV rows to objects</param>
        /// <param name="csvReader">CSV reader for file I/O operations</param>
        /// <param name="skipHeaderValidation">Whether to skip header validation</param>
        /// <param name="continueOnError">Whether to continue parsing when individual rows fail</param>
        public CsvParser(ICsvRowMapper<T> mapper, ICsvReader csvReader = null,
                         bool skipHeaderValidation = false, bool continueOnError = true)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _csvReader = csvReader ?? new StreamingCsvReader();
            _skipHeaderValidation = skipHeaderValidation;
            _continueOnError = continueOnError;
        }

        /// <summary>
        /// Parses a CSV file and returns the mapped objects
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <returns>List of parsed objects</returns>
        public List<T> ParseFile(string filePath)
        {
            var stats = new CsvParsingStats();
            return ParseFile(filePath, out stats);
        }

        /// <summary>
        /// Parses a CSV file and returns the mapped objects with parsing statistics
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <param name="stats">Output parameter containing parsing statistics</param>
        /// <returns>List of parsed objects</returns>
        public List<T> ParseFile(string filePath, out CsvParsingStats stats)
        {
            var startTime = DateTime.UtcNow;
            stats = new CsvParsingStats();
            var results = new List<T>();

            try
            {
                _csvReader.Open(filePath);

                // Read and validate headers
                var headers = _csvReader.ReadHeader();
                if (headers != null)
                {
                    stats.TotalRows++;

                    if (!_skipHeaderValidation)
                    {
                        var headerValidation = _mapper.ValidateHeaders(headers);
                        if (!headerValidation.IsValid)
                        {
                            throw new CsvParsingException(
                                $"Invalid CSV headers: {headerValidation.ErrorMessage}",
                                1, filePath);
                        }
                    }
                }

                // Parse data rows
                var lineNumber = 2; // Start at 2 since headers are line 1
                foreach (var fields in _csvReader.ReadAllLines())
                {
                    stats.TotalRows++;

                    if (_mapper.TryMapRow(fields, lineNumber, out var obj, out var errorMessage))
                    {
                        results.Add(obj);
                        stats.SuccessfulRows++;
                    }
                    else
                    {
                        stats.FailedRows++;
                        var error = $"Line {lineNumber}: {errorMessage}";
                        stats.Errors.Add(error);

                        if (!_continueOnError)
                        {
                            throw new CsvParsingException(error, lineNumber, filePath);
                        }
                    }

                    lineNumber++;
                }
            }
            catch (CsvParsingException)
            {
                throw; // Re-throw CSV parsing exceptions as-is
            }
            catch (Exception ex)
            {
                throw new CsvParsingException($"Failed to parse CSV file: {ex.Message}", ex, 0, filePath);
            }
            finally
            {
                _csvReader?.Close();
                stats.ElapsedTime = DateTime.UtcNow - startTime;
            }

            return results;
        }

        /// <summary>
        /// Asynchronously parses a CSV file and returns the mapped objects
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <returns>List of parsed objects</returns>
        public async Task<List<T>> ParseFileAsync(string filePath)
        {
            var result = await ParseFileAsync(filePath, new CsvParsingStats());
            return result.Item1;
        }

        /// <summary>
        /// Asynchronously parses a CSV file and returns the mapped objects with statistics
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <param name="stats">Statistics object to populate</param>
        /// <returns>Tuple containing parsed objects and statistics</returns>
        public async Task<(List<T>, CsvParsingStats)> ParseFileAsync(string filePath, CsvParsingStats stats)
        {
            var startTime = DateTime.UtcNow;
            var results = new List<T>();

            try
            {
                await _csvReader.OpenAsync(filePath);

                // Read and validate headers
                var headers = await _csvReader.ReadHeaderAsync();
                if (headers != null)
                {
                    stats.TotalRows++;

                    if (!_skipHeaderValidation)
                    {
                        var headerValidation = _mapper.ValidateHeaders(headers);
                        if (!headerValidation.IsValid)
                        {
                            throw new CsvParsingException(
                                $"Invalid CSV headers: {headerValidation.ErrorMessage}",
                                1, filePath);
                        }
                    }
                }

                // Parse data rows
                var lineNumber = 2;
                await foreach (var fields in _csvReader.ReadAllLinesAsync())
                {
                    stats.TotalRows++;

                    if (_mapper.TryMapRow(fields, lineNumber, out var obj, out var errorMessage))
                    {
                        results.Add(obj);
                        stats.SuccessfulRows++;
                    }
                    else
                    {
                        stats.FailedRows++;
                        var error = $"Line {lineNumber}: {errorMessage}";
                        stats.Errors.Add(error);

                        if (!_continueOnError)
                        {
                            throw new CsvParsingException(error, lineNumber, filePath);
                        }
                    }

                    lineNumber++;
                }
            }
            catch (CsvParsingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CsvParsingException($"Failed to parse CSV file: {ex.Message}", ex, 0, filePath);
            }
            finally
            {
                _csvReader?.Close();
                stats.ElapsedTime = DateTime.UtcNow - startTime;
            }

            return (results, stats);
        }

        /// <summary>
        /// Parses CSV content from a string
        /// </summary>
        /// <param name="csvContent">CSV content as string</param>
        /// <returns>List of parsed objects</returns>
        public List<T> ParseContent(string csvContent)
        {
            if (string.IsNullOrEmpty(csvContent))
                return new List<T>();

            var lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var results = new List<T>();
            var lineNumber = 1;

            // Skip header if present
            var startIndex = 0;
            if (lines.Length > 0 && !_skipHeaderValidation)
            {
                var headerFields = _csvReader.ParseLine(lines[0]);
                var headerValidation = _mapper.ValidateHeaders(headerFields);
                if (headerValidation.IsValid)
                {
                    startIndex = 1;
                    lineNumber = 2;
                }
            }

            // Parse data lines
            for (int i = startIndex; i < lines.Length; i++)
            {
                var fields = _csvReader.ParseLine(lines[i]);

                if (_mapper.TryMapRow(fields, lineNumber, out var obj, out var errorMessage))
                {
                    results.Add(obj);
                }
                else if (!_continueOnError)
                {
                    throw new CsvParsingException($"Line {lineNumber}: {errorMessage}", lineNumber);
                }

                lineNumber++;
            }

            return results;
        }

        /// <summary>
        /// Validates a CSV file without parsing all data
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <returns>Validation result with any errors found</returns>
        public CsvParsingStats ValidateFile(string filePath)
        {
            var stats = new CsvParsingStats();
            var startTime = DateTime.UtcNow;

            try
            {
                _csvReader.Open(filePath);

                // Validate headers
                var headers = _csvReader.ReadHeader();
                if (headers != null)
                {
                    stats.TotalRows++;

                    var headerValidation = _mapper.ValidateHeaders(headers);
                    if (!headerValidation.IsValid)
                    {
                        stats.Errors.Add($"Line 1: Invalid headers - {headerValidation.ErrorMessage}");
                        stats.FailedRows++;
                    }
                }

                // Validate each row
                var lineNumber = 2;
                foreach (var fields in _csvReader.ReadAllLines())
                {
                    stats.TotalRows++;

                    var validation = _mapper.ValidateRow(fields, lineNumber);
                    if (validation.IsValid)
                    {
                        stats.SuccessfulRows++;
                    }
                    else
                    {
                        stats.FailedRows++;
                        stats.Errors.Add($"Line {lineNumber}: {validation.ErrorMessage}");
                    }

                    lineNumber++;
                }
            }
            catch (Exception ex)
            {
                stats.Errors.Add($"Validation failed: {ex.Message}");
                stats.FailedRows++;
            }
            finally
            {
                _csvReader?.Close();
                stats.ElapsedTime = DateTime.UtcNow - startTime;
            }

            return stats;
        }
    }
}