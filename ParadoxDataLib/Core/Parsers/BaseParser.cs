using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParadoxDataLib.Core.Performance;
using ParadoxDataLib.Core.Tokenizer;

namespace ParadoxDataLib.Core.Parsers
{
    /// <summary>
    /// Abstract base class for all Paradox data file parsers.
    /// Provides common parsing functionality including tokenization, error handling, and utility methods.
    /// </summary>
    /// <typeparam name="T">The type of data structure this parser produces</typeparam>
    public abstract class BaseParser<T> : IDataParser<T>
    {
        /// <summary>
        /// The list of tokens generated from the input content during parsing
        /// </summary>
        protected List<Token> _tokens;

        /// <summary>
        /// Current position in the token list during parsing
        /// </summary>
        protected int _currentTokenIndex;

        /// <summary>
        /// Collection of error messages encountered during parsing
        /// </summary>
        protected readonly List<string> _errors;

        /// <summary>
        /// Collection of warning messages encountered during parsing
        /// </summary>
        protected readonly List<string> _warnings;

        /// <summary>
        /// Performance metrics collected during parsing operations
        /// </summary>
        protected readonly ParsingMetrics _metrics;

        /// <summary>
        /// Stack of file paths being processed to detect circular includes
        /// </summary>
        protected readonly Stack<string> _includeStack;

        /// <summary>
        /// Maximum depth for include files to prevent infinite recursion
        /// </summary>
        protected const int MaxIncludeDepth = 10;

        /// <summary>
        /// Gets a read-only collection of all parsing errors
        /// </summary>
        public IReadOnlyList<string> Errors => _errors.AsReadOnly();

        /// <summary>
        /// Gets a read-only collection of all parsing warnings
        /// </summary>
        public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

        /// <summary>
        /// Gets whether any errors occurred during parsing
        /// </summary>
        public bool HasErrors => _errors.Any();

        /// <summary>
        /// Gets whether any warnings occurred during parsing
        /// </summary>
        public bool HasWarnings => _warnings.Any();

        /// <summary>
        /// Gets the performance metrics collected during parsing
        /// </summary>
        public ParsingMetrics Metrics => _metrics;

        /// <summary>
        /// Initializes a new instance of the BaseParser class
        /// </summary>
        protected BaseParser()
        {
            _errors = new List<string>();
            _warnings = new List<string>();
            _metrics = new ParsingMetrics();
            _includeStack = new Stack<string>();
        }

        /// <summary>
        /// Parses the provided content string and returns the parsed data structure
        /// </summary>
        /// <param name="content">The raw content string to parse</param>
        /// <returns>The parsed data structure, or default(T) if parsing fails</returns>
        public virtual T Parse(string content)
        {
            // Reset metrics for new parsing operation
            _metrics.Reset();
            _metrics.MemoryBefore = GC.GetTotalMemory(false);

            if (string.IsNullOrEmpty(content))
            {
                _errors.Add("Content is null or empty");
                return default(T);
            }

            try
            {
                // Record input size and line count
                _metrics.InputSizeBytes = Encoding.UTF8.GetByteCount(content);
                _metrics.LinesProcessed = content.Count(c => c == '\n') + 1;

                // Time tokenization
                T result;
                using (new PerformanceTimer(elapsed => _metrics.TokenizationTime = elapsed))
                {
                    var lexer = new Lexer(content);
                    _tokens = lexer.Tokenize();
                    _metrics.TokensProcessed = _tokens.Count;
                }

                _currentTokenIndex = 0;
                _errors.Clear();
                _warnings.Clear();

                // Time parsing
                using (new PerformanceTimer(elapsed => _metrics.ParsingTime = elapsed))
                {
                    result = ParseTokens();
                }

                // Record final metrics
                _metrics.ErrorCount = _errors.Count;
                _metrics.WarningCount = _warnings.Count;
                _metrics.MemoryAfter = GC.GetTotalMemory(false);

                return result;
            }
            catch (Exception ex)
            {
                _errors.Add($"Parse error: {ex.Message}");
                _metrics.ErrorCount = _errors.Count;
                _metrics.MemoryAfter = GC.GetTotalMemory(false);
                return default(T);
            }
        }

        /// <summary>
        /// Parses a file at the specified path and returns the parsed data structure
        /// </summary>
        /// <param name="filePath">The path to the file to parse</param>
        /// <returns>The parsed data structure, or default(T) if parsing fails</returns>
        public virtual T ParseFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _errors.Add($"File not found: {filePath}");
                return default(T);
            }

            try
            {
                string content;
                // Time file I/O operation
                using (new PerformanceTimer(elapsed => _metrics.FileIOTime = elapsed))
                {
                    content = File.ReadAllText(filePath, GetFileEncoding(filePath));
                }

                return Parse(content);
            }
            catch (Exception ex)
            {
                _errors.Add($"File read error: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// Asynchronously parses the provided content string and returns the parsed data structure
        /// </summary>
        /// <param name="content">The raw content string to parse</param>
        /// <returns>A task that represents the asynchronous parse operation. The task result contains the parsed data structure, or default(T) if parsing fails</returns>
        public virtual async Task<T> ParseAsync(string content)
        {
            return await Task.Run(() => Parse(content));
        }

        /// <summary>
        /// Asynchronously parses a file at the specified path and returns the parsed data structure
        /// </summary>
        /// <param name="filePath">The path to the file to parse</param>
        /// <returns>A task that represents the asynchronous parse operation. The task result contains the parsed data structure, or default(T) if parsing fails</returns>
        public virtual async Task<T> ParseFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _errors.Add($"File not found: {filePath}");
                return default(T);
            }

            try
            {
                string content;
                var stopwatch = Stopwatch.StartNew();

                content = await File.ReadAllTextAsync(filePath, GetFileEncoding(filePath));

                stopwatch.Stop();
                _metrics.FileIOTime = stopwatch.Elapsed;

                return await ParseAsync(content);
            }
            catch (Exception ex)
            {
                _errors.Add($"File read error: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// Parses multiple content strings and returns a list of parsed data structures
        /// </summary>
        /// <param name="contents">The collection of content strings to parse</param>
        /// <returns>A list of successfully parsed data structures. Failed parses are excluded from the result</returns>
        public virtual List<T> ParseMultiple(IEnumerable<string> contents)
        {
            var results = new List<T>();
            foreach (var content in contents)
            {
                var result = Parse(content);
                if (result != null && !result.Equals(default(T)))
                {
                    results.Add(result);
                }
            }
            return results;
        }

        /// <summary>
        /// Asynchronously parses multiple content strings in parallel and returns a list of parsed data structures
        /// </summary>
        /// <param name="contents">The collection of content strings to parse</param>
        /// <returns>A task that represents the asynchronous parse operation. The task result contains a list of successfully parsed data structures</returns>
        public virtual async Task<List<T>> ParseMultipleAsync(IEnumerable<string> contents)
        {
            var tasks = contents.Select(ParseAsync);
            var results = await Task.WhenAll(tasks);
            return results.Where(r => r != null && !r.Equals(default(T))).ToList();
        }

        /// <summary>
        /// Attempts to parse the provided content string, returning whether the parse was successful
        /// </summary>
        /// <param name="content">The raw content string to parse</param>
        /// <param name="result">When this method returns, contains the parsed data structure if parsing succeeded, or default(T) if parsing failed</param>
        /// <param name="error">When this method returns, contains the error message if parsing failed, or null if parsing succeeded</param>
        /// <returns>true if parsing succeeded; otherwise, false</returns>
        public virtual bool TryParse(string content, out T result, out string error)
        {
            result = Parse(content);
            error = HasErrors ? string.Join("; ", _errors) : null;
            return !HasErrors && result != null && !result.Equals(default(T));
        }

        /// <summary>
        /// Abstract method that derived classes must implement to parse the tokenized content
        /// </summary>
        /// <returns>The parsed data structure</returns>
        protected abstract T ParseTokens();

        /// <summary>
        /// Gets the token at the current position in the token stream
        /// </summary>
        /// <returns>The current token, or an EndOfFile token if past the end of the stream</returns>
        protected Token CurrentToken()
        {
            if (_currentTokenIndex < _tokens.Count)
                return _tokens[_currentTokenIndex];
            return new Token(TokenType.EndOfFile, "", -1, -1, -1);
        }

        /// <summary>
        /// Looks ahead in the token stream without advancing the current position
        /// </summary>
        /// <param name="offset">The number of tokens to look ahead (default is 1)</param>
        /// <returns>The token at the specified offset, or an EndOfFile token if past the end of the stream</returns>
        protected Token PeekToken(int offset = 1)
        {
            var index = _currentTokenIndex + offset;
            if (index < _tokens.Count)
                return _tokens[index];
            return new Token(TokenType.EndOfFile, "", -1, -1, -1);
        }

        /// <summary>
        /// Gets the current token and advances to the next position in the token stream
        /// </summary>
        /// <returns>The token that was at the current position</returns>
        protected Token ConsumeToken()
        {
            var token = CurrentToken();
            _currentTokenIndex++;
            return token;
        }

        /// <summary>
        /// Verifies that the current token is of the expected type and consumes it
        /// </summary>
        /// <param name="type">The expected token type</param>
        /// <returns>true if the token matches the expected type; otherwise, false and adds an error</returns>
        protected bool ExpectToken(TokenType type)
        {
            var token = CurrentToken();
            if (token.Type != type)
            {
                AddError($"Expected {type}, but got {token.Type} at line {token.Line}, column {token.Column}");
                return false;
            }
            ConsumeToken();
            return true;
        }

        /// <summary>
        /// Verifies that the current token is of the expected type, returns it, and consumes it
        /// </summary>
        /// <param name="type">The expected token type</param>
        /// <param name="token">When this method returns, contains the current token</param>
        /// <returns>true if the token matches the expected type; otherwise, false and adds an error</returns>
        protected bool ExpectToken(TokenType type, out Token token)
        {
            token = CurrentToken();
            if (token.Type != type)
            {
                AddError($"Expected {type}, but got {token.Type} at line {token.Line}, column {token.Column}");
                return false;
            }
            ConsumeToken();
            return true;
        }

        /// <summary>
        /// Checks if the current token is of the specified type without consuming it
        /// </summary>
        /// <param name="type">The token type to check for</param>
        /// <returns>true if the current token matches the specified type; otherwise, false</returns>
        protected bool IsToken(TokenType type)
        {
            return CurrentToken().Type == type;
        }

        /// <summary>
        /// Advances the token position past any consecutive comment tokens
        /// </summary>
        protected void SkipComments()
        {
            while (IsToken(TokenType.Comment))
            {
                ConsumeToken();
            }
        }

        /// <summary>
        /// Advances the token position to the next statement by skipping to the next right brace or end of file
        /// </summary>
        protected void SkipToNextStatement()
        {
            while (!IsEndOfFile() && !IsToken(TokenType.RightBrace))
            {
                ConsumeToken();
            }
        }

        /// <summary>
        /// Checks if the current position is at the end of the token stream
        /// </summary>
        /// <returns>true if at the end of the token stream; otherwise, false</returns>
        protected bool IsEndOfFile()
        {
            return CurrentToken().Type == TokenType.EndOfFile;
        }

        /// <summary>
        /// Adds an error message to the error collection
        /// </summary>
        /// <param name="message">The error message to add</param>
        protected void AddError(string message)
        {
            _errors.Add(message);
        }

        /// <summary>
        /// Adds a warning message to the warning collection
        /// </summary>
        /// <param name="message">The warning message to add</param>
        protected void AddWarning(string message)
        {
            _warnings.Add(message);
        }

        /// <summary>
        /// Detects the appropriate encoding for a Paradox game file
        /// </summary>
        /// <param name="filePath">The path to the file to analyze</param>
        /// <returns>The detected encoding, defaulting to Windows-1252 for Paradox games or UTF-8 on detection failure</returns>
        protected Encoding GetFileEncoding(string filePath)
        {
            // Paradox games often use Windows-1252 encoding
            // We'll try to detect it, but default to UTF-8
            try
            {
                var bytes = File.ReadAllBytes(filePath);

                // Check for UTF-8 BOM
                if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                {
                    return Encoding.UTF8;
                }

                // Check for UTF-16 BOM
                if (bytes.Length >= 2)
                {
                    if ((bytes[0] == 0xFF && bytes[1] == 0xFE) || (bytes[0] == 0xFE && bytes[1] == 0xFF))
                    {
                        return Encoding.Unicode;
                    }
                }

                // Try to detect if it's valid UTF-8
                try
                {
                    var utf8 = Encoding.UTF8;
                    var text = utf8.GetString(bytes);
                    var reencoded = utf8.GetBytes(text);
                    if (bytes.SequenceEqual(reencoded))
                    {
                        return utf8;
                    }
                }
                catch { }

                // Default to Windows-1252 for Paradox games
                return Encoding.GetEncoding(1252);
            }
            catch
            {
                return Encoding.UTF8;
            }
        }

        /// <summary>
        /// Parses a date string in Paradox game format (YYYY.MM.DD)
        /// </summary>
        /// <param name="dateString">The date string to parse</param>
        /// <returns>The parsed DateTime</returns>
        /// <exception cref="FormatException">Thrown when the date string is not in the expected format</exception>
        protected DateTime ParseDate(string dateString)
        {
            var parts = dateString.Split('.');
            if (parts.Length == 3)
            {
                if (int.TryParse(parts[0], out var year) &&
                    int.TryParse(parts[1], out var month) &&
                    int.TryParse(parts[2], out var day))
                {
                    try
                    {
                        return new DateTime(year, month, day);
                    }
                    catch { }
                }
            }
            throw new FormatException($"Invalid date format: {dateString}");
        }

        /// <summary>
        /// Parses a brace-delimited list of string values from the token stream
        /// </summary>
        /// <returns>A list containing all parsed string values</returns>
        protected List<string> ParseStringList()
        {
            var list = new List<string>();

            if (!ExpectToken(TokenType.LeftBrace))
                return list;

            SkipComments();

            while (!IsToken(TokenType.RightBrace) && !IsEndOfFile())
            {
                var token = CurrentToken();

                switch (token.Type)
                {
                    case TokenType.Identifier:
                    case TokenType.String:
                    case TokenType.Number:
                        list.Add(token.Value);
                        ConsumeToken();
                        break;

                    case TokenType.Comment:
                        ConsumeToken(); // Skip comments
                        break;

                    default:
                        AddWarning($"Unexpected token in list: {token.Type} '{token.Value}' at line {token.Line}");
                        ConsumeToken();
                        break;
                }

                SkipComments();
            }

            ExpectToken(TokenType.RightBrace);
            return list;
        }

        /// <summary>
        /// Parses a brace-delimited list of integer values from the token stream
        /// </summary>
        /// <returns>A list containing all successfully parsed integer values</returns>
        protected List<int> ParseIntegerList()
        {
            var list = new List<int>();

            if (!ExpectToken(TokenType.LeftBrace))
                return list;

            SkipComments();

            while (!IsToken(TokenType.RightBrace) && !IsEndOfFile())
            {
                var token = CurrentToken();

                switch (token.Type)
                {
                    case TokenType.Number:
                        if (int.TryParse(token.Value, out var intValue))
                        {
                            list.Add(intValue);
                        }
                        else
                        {
                            AddWarning($"Cannot parse integer: '{token.Value}' at line {token.Line}");
                        }
                        ConsumeToken();
                        break;

                    case TokenType.Comment:
                        ConsumeToken(); // Skip comments
                        break;

                    default:
                        AddWarning($"Expected number in integer list, got {token.Type} '{token.Value}' at line {token.Line}");
                        ConsumeToken();
                        break;
                }

                SkipComments();
            }

            ExpectToken(TokenType.RightBrace);
            return list;
        }

        /// <summary>
        /// Parses a brace-delimited map of string keys to integer values from the token stream
        /// </summary>
        /// <returns>A dictionary containing all successfully parsed key-value pairs</returns>
        protected Dictionary<string, int> ParseStringIntegerMap()
        {
            var map = new Dictionary<string, int>();

            if (!ExpectToken(TokenType.LeftBrace))
                return map;

            SkipComments();

            while (!IsToken(TokenType.RightBrace) && !IsEndOfFile())
            {
                var keyToken = CurrentToken();

                if (keyToken.Type == TokenType.Comment)
                {
                    ConsumeToken();
                    continue;
                }

                if (keyToken.Type != TokenType.String && keyToken.Type != TokenType.Identifier)
                {
                    AddWarning($"Expected string key, got {keyToken.Type} '{keyToken.Value}' at line {keyToken.Line}");
                    SkipToNextStatement();
                    continue;
                }

                ConsumeToken(); // consume key

                if (!ExpectToken(TokenType.Equals))
                {
                    SkipToNextStatement();
                    continue;
                }

                var valueToken = CurrentToken();
                if (valueToken.Type == TokenType.Number && int.TryParse(valueToken.Value, out var intValue))
                {
                    map[keyToken.Value] = intValue;
                    ConsumeToken();
                }
                else
                {
                    AddWarning($"Expected integer value, got {valueToken.Type} '{valueToken.Value}' at line {valueToken.Line}");
                    ConsumeToken();
                }

                SkipComments();
            }

            ExpectToken(TokenType.RightBrace);
            return map;
        }

        /// <summary>
        /// Records a custom timing measurement for a specific operation
        /// </summary>
        /// <param name="operationName">The name of the operation being timed</param>
        /// <param name="elapsed">The time elapsed for the operation</param>
        protected void RecordTiming(string operationName, TimeSpan elapsed)
        {
            _metrics.CustomTimings[operationName] = elapsed;
        }

        /// <summary>
        /// Increments a counter for a specific operation
        /// </summary>
        /// <param name="counterName">The name of the counter</param>
        /// <param name="increment">The amount to increment (default is 1)</param>
        protected void IncrementCounter(string counterName, int increment = 1)
        {
            if (_metrics.Counters.ContainsKey(counterName))
            {
                _metrics.Counters[counterName] += increment;
            }
            else
            {
                _metrics.Counters[counterName] = increment;
            }
        }

        /// <summary>
        /// Creates a performance timer for measuring operation duration
        /// </summary>
        /// <param name="operationName">The name of the operation to time</param>
        /// <returns>A disposable timer that will record the elapsed time when disposed</returns>
        protected PerformanceTimer StartTiming(string operationName)
        {
            return new PerformanceTimer(elapsed => RecordTiming(operationName, elapsed));
        }

        /// <summary>
        /// Processes an @include directive and returns the content of the included file
        /// </summary>
        /// <param name="includePath">The path to the file to include</param>
        /// <param name="currentFilePath">The path of the file containing the include directive</param>
        /// <returns>The content of the included file, or empty string if inclusion fails</returns>
        protected virtual string ProcessInclude(string includePath, string currentFilePath)
        {
            try
            {
                // Resolve the include path relative to the current file
                var resolvedPath = ResolveIncludePath(includePath, currentFilePath);

                // Check for circular includes
                if (_includeStack.Contains(resolvedPath))
                {
                    AddError($"Circular include detected: {resolvedPath}");
                    return string.Empty;
                }

                // Check include depth
                if (_includeStack.Count >= MaxIncludeDepth)
                {
                    AddError($"Maximum include depth ({MaxIncludeDepth}) exceeded");
                    return string.Empty;
                }

                // Check if file exists
                if (!File.Exists(resolvedPath))
                {
                    AddError($"Include file not found: {resolvedPath}");
                    return string.Empty;
                }

                // Track this include
                _includeStack.Push(resolvedPath);
                IncrementCounter("IncludesProcessed");

                try
                {
                    using (StartTiming($"Include_{Path.GetFileName(resolvedPath)}"))
                    {
                        var content = File.ReadAllText(resolvedPath, GetFileEncoding(resolvedPath));

                        // Process nested includes in the included content
                        content = ProcessNestedIncludes(content, resolvedPath);

                        return content;
                    }
                }
                finally
                {
                    _includeStack.Pop();
                }
            }
            catch (Exception ex)
            {
                AddError($"Error processing include '{includePath}': {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Resolves an include path relative to the current file
        /// </summary>
        /// <param name="includePath">The include path from the directive</param>
        /// <param name="currentFilePath">The path of the file containing the include</param>
        /// <returns>The resolved absolute path</returns>
        protected virtual string ResolveIncludePath(string includePath, string currentFilePath)
        {
            // Remove quotes if present
            includePath = includePath.Trim('"', '\'');

            // If it's already an absolute path, use it as-is
            if (Path.IsPathRooted(includePath))
            {
                return includePath;
            }

            // Make it relative to the current file's directory
            var currentDirectory = string.IsNullOrEmpty(currentFilePath)
                ? Directory.GetCurrentDirectory()
                : Path.GetDirectoryName(currentFilePath);

            return Path.GetFullPath(Path.Combine(currentDirectory ?? "", includePath));
        }

        /// <summary>
        /// Processes nested @include directives in content
        /// </summary>
        /// <param name="content">The content to process</param>
        /// <param name="currentFilePath">The path of the file containing this content</param>
        /// <returns>Content with all includes processed</returns>
        protected virtual string ProcessNestedIncludes(string content, string currentFilePath)
        {
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var result = new System.Text.StringBuilder();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Check for @include directive
                if (trimmedLine.StartsWith("@include", StringComparison.OrdinalIgnoreCase))
                {
                    var includeDirective = trimmedLine.Substring(8).Trim();

                    // Handle different @include formats
                    // @include "path/to/file.txt"
                    // @include path/to/file.txt
                    var includePath = includeDirective.Trim('"', '\'');

                    if (!string.IsNullOrEmpty(includePath))
                    {
                        var includedContent = ProcessInclude(includePath, currentFilePath);
                        result.AppendLine($"# Included from: {includePath}");
                        result.AppendLine(includedContent);
                        result.AppendLine($"# End include: {includePath}");
                    }
                    else
                    {
                        AddWarning($"Empty include path in directive: {trimmedLine}");
                        result.AppendLine(line); // Keep original line
                    }
                }
                else
                {
                    result.AppendLine(line);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Parses a file with include processing support
        /// </summary>
        /// <param name="filePath">The path to the file to parse</param>
        /// <returns>The parsed data structure</returns>
        public virtual T ParseFileWithIncludes(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _errors.Add($"File not found: {filePath}");
                return default(T);
            }

            try
            {
                string content;
                // Time file I/O operation
                using (new PerformanceTimer(elapsed => _metrics.FileIOTime = elapsed))
                {
                    content = File.ReadAllText(filePath, GetFileEncoding(filePath));

                    // Process includes before parsing
                    content = ProcessNestedIncludes(content, filePath);
                }

                return Parse(content);
            }
            catch (Exception ex)
            {
                _errors.Add($"File read error: {ex.Message}");
                return default(T);
            }
        }

        /// <summary>
        /// Parses a file asynchronously with include processing support
        /// </summary>
        /// <param name="filePath">The path to the file to parse</param>
        /// <returns>A task representing the asynchronous parse operation</returns>
        public virtual async Task<T> ParseFileWithIncludesAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _errors.Add($"File not found: {filePath}");
                return default(T);
            }

            try
            {
                string content;
                var stopwatch = Stopwatch.StartNew();

                content = await File.ReadAllTextAsync(filePath, GetFileEncoding(filePath));

                // Process includes before parsing
                content = ProcessNestedIncludes(content, filePath);

                stopwatch.Stop();
                _metrics.FileIOTime = stopwatch.Elapsed;

                return await ParseAsync(content);
            }
            catch (Exception ex)
            {
                _errors.Add($"File read error: {ex.Message}");
                return default(T);
            }
        }
    }
}