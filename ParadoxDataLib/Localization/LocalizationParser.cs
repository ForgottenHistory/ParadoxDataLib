using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ParadoxDataLib.Localization
{
    public class LocalizationParser
    {
        private static readonly Regex LanguageHeaderRegex = new Regex(@"^l_(\w+):$", RegexOptions.Compiled);
        private static readonly Regex KeyValueRegex = new Regex(@"^\s*([^:]+):\d*\s*""([^""]*)""", RegexOptions.Compiled);
        private static readonly Regex CommentRegex = new Regex(@"^\s*#", RegexOptions.Compiled);

        public List<LocalizationEntry> ParseFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Localization file not found: {filePath}");

            var content = File.ReadAllText(filePath, Encoding.UTF8);
            return ParseContent(content, filePath);
        }

        public async Task<List<LocalizationEntry>> ParseFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Localization file not found: {filePath}");

            var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            return ParseContent(content, filePath);
        }

        public List<LocalizationEntry> ParseContent(string content, string sourceFile = null)
        {
            if (string.IsNullOrWhiteSpace(content))
                return new List<LocalizationEntry>();

            var entries = new Dictionary<string, LocalizationEntry>();
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var currentLanguage = string.Empty;
            var lineNumber = 0;

            foreach (var rawLine in lines)
            {
                lineNumber++;
                var line = rawLine.TrimEnd();

                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(line) || CommentRegex.IsMatch(line))
                    continue;

                // Check for language header
                var languageMatch = LanguageHeaderRegex.Match(line);
                if (languageMatch.Success)
                {
                    currentLanguage = languageMatch.Groups[1].Value;
                    continue;
                }

                // Skip lines if no language is set
                if (string.IsNullOrEmpty(currentLanguage))
                    continue;

                // Parse key-value pair
                var kvMatch = KeyValueRegex.Match(line);
                if (kvMatch.Success)
                {
                    var key = kvMatch.Groups[1].Value.Trim();
                    var value = kvMatch.Groups[2].Value;

                    // Process escape sequences
                    value = ProcessEscapeSequences(value);

                    // Get or create entry
                    if (!entries.TryGetValue(key, out LocalizationEntry entry))
                    {
                        entry = new LocalizationEntry
                        {
                            Key = key,
                            SourceFile = sourceFile,
                            LineNumber = lineNumber
                        };
                        entries[key] = entry;
                    }

                    entry.AddTranslation(currentLanguage, value);
                }
            }

            return entries.Values.ToList();
        }

        private string ProcessEscapeSequences(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Process common escape sequences
            value = value.Replace("\\n", "\n");
            value = value.Replace("\\r", "\r");
            value = value.Replace("\\t", "\t");
            value = value.Replace("\\\"", "\"");
            value = value.Replace("\\\\", "\\");

            // Process Paradox-specific sequences
            value = ProcessParadoxColorCodes(value);
            value = ProcessParadoxVariables(value);

            return value;
        }

        private string ProcessParadoxColorCodes(string value)
        {
            // Process color codes like §R, §G, §B, §Y, etc.
            var colorRegex = new Regex(@"§([RGBYWP!])", RegexOptions.IgnoreCase);
            return colorRegex.Replace(value, match =>
            {
                var colorCode = match.Groups[1].Value.ToUpper();
                return colorCode switch
                {
                    "R" => "<color=red>",
                    "G" => "<color=green>",
                    "B" => "<color=blue>",
                    "Y" => "<color=yellow>",
                    "W" => "<color=white>",
                    "P" => "<color=purple>",
                    "!" => "</color>",
                    _ => match.Value
                };
            });
        }

        private string ProcessParadoxVariables(string value)
        {
            // Process variables like $COUNTRY$, $PROVINCE$, etc.
            var variableRegex = new Regex(@"\$([A-Z_]+)\$", RegexOptions.IgnoreCase);
            return variableRegex.Replace(value, match =>
            {
                var variable = match.Groups[1].Value;
                return $"[{variable}]"; // Convert to a more readable format
            });
        }

        public void WriteFile(string filePath, IEnumerable<LocalizationEntry> entries, string language)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (string.IsNullOrEmpty(language))
                throw new ArgumentException("Language cannot be null or empty", nameof(language));

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            WriteContent(writer, entries, language);
        }

        public async Task WriteFileAsync(string filePath, IEnumerable<LocalizationEntry> entries, string language)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (string.IsNullOrEmpty(language))
                throw new ArgumentException("Language cannot be null or empty", nameof(language));

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            await WriteContentAsync(writer, entries, language);
        }

        private void WriteContent(StreamWriter writer, IEnumerable<LocalizationEntry> entries, string language)
        {
            writer.WriteLine($"l_{language}:");

            foreach (var entry in entries.Where(e => e.HasTranslation(language)).OrderBy(e => e.Key))
            {
                var translation = entry.GetTranslation(language);
                var escapedTranslation = EscapeForOutput(translation);
                writer.WriteLine($" {entry.Key}:0 \"{escapedTranslation}\"");
            }
        }

        private async Task WriteContentAsync(StreamWriter writer, IEnumerable<LocalizationEntry> entries, string language)
        {
            await writer.WriteLineAsync($"l_{language}:");

            foreach (var entry in entries.Where(e => e.HasTranslation(language)).OrderBy(e => e.Key))
            {
                var translation = entry.GetTranslation(language);
                var escapedTranslation = EscapeForOutput(translation);
                await writer.WriteLineAsync($" {entry.Key}:0 \"{escapedTranslation}\"");
            }
        }

        private string EscapeForOutput(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            value = value.Replace("\\", "\\\\");
            value = value.Replace("\"", "\\\"");
            value = value.Replace("\n", "\\n");
            value = value.Replace("\r", "\\r");
            value = value.Replace("\t", "\\t");

            return value;
        }

        public LocalizationParseResult ValidateFile(string filePath)
        {
            var result = new LocalizationParseResult { FilePath = filePath };

            if (!File.Exists(filePath))
            {
                result.Errors.Add($"File not found: {filePath}");
                return result;
            }

            try
            {
                var entries = ParseFile(filePath);
                result.Entries = entries;
                result.IsValid = true;

                // Additional validation
                ValidateEntries(entries, result);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Parse error: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        private void ValidateEntries(List<LocalizationEntry> entries, LocalizationParseResult result)
        {
            var duplicateKeys = entries
                .GroupBy(e => e.Key)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicateKey in duplicateKeys)
            {
                result.Warnings.Add($"Duplicate key found: {duplicateKey}");
            }

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Key))
                {
                    result.Errors.Add($"Empty key found at line {entry.LineNumber}");
                }

                if (entry.Key.Length > 200)
                {
                    result.Warnings.Add($"Very long key (>{entry.Key.Length} chars): {entry.Key}");
                }

                foreach (var translation in entry.Translations.Values)
                {
                    if (translation.Length > 1000)
                    {
                        result.Warnings.Add($"Very long translation (>{translation.Length} chars) for key: {entry.Key}");
                    }
                }
            }
        }
    }

    public class LocalizationParseResult
    {
        public string FilePath { get; set; }
        public bool IsValid { get; set; }
        public List<LocalizationEntry> Entries { get; set; } = new List<LocalizationEntry>();
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();

        public bool HasErrors => Errors.Count > 0;
        public bool HasWarnings => Warnings.Count > 0;

        public override string ToString()
        {
            var lines = new List<string>();
            lines.Add($"Parse Result for: {Path.GetFileName(FilePath)}");
            lines.Add($"Valid: {IsValid}");
            lines.Add($"Entries: {Entries.Count}");

            if (HasErrors)
            {
                lines.Add($"Errors ({Errors.Count}):");
                foreach (var error in Errors)
                    lines.Add($"  - {error}");
            }

            if (HasWarnings)
            {
                lines.Add($"Warnings ({Warnings.Count}):");
                foreach (var warning in Warnings)
                    lines.Add($"  - {warning}");
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}