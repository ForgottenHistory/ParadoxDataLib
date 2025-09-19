using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using ParadoxDataLib.Core.Tokenizer;

namespace ParadoxDataLib.Validation
{
    public class SchemaValidator
    {
        private static readonly Dictionary<string, HashSet<string>> ExpectedProvinceKeys = new Dictionary<string, HashSet<string>>
        {
            ["required"] = new HashSet<string> { "owner", "controller", "culture", "religion" },
            ["optional"] = new HashSet<string>
            {
                "base_tax", "base_production", "base_manpower", "trade_good", "discovered_by",
                "center_of_trade", "extra_cost", "terrain", "climate", "monsoon", "winter",
                "impassable", "seat_in_parliament", "is_city", "hre", "add_core", "remove_core"
            },
            ["buildings"] = new HashSet<string>
            {
                "fort_15th", "fort_16th", "fort_17th", "fort_18th", "fort_19th",
                "marketplace", "workshop", "temple", "shipyard", "dock", "drydock",
                "grand_shipyard", "admiralty", "conscription_center", "training_fields",
                "regimental_camp", "barracks", "armory"
            }
        };

        private static readonly Dictionary<string, HashSet<string>> ExpectedCountryKeys = new Dictionary<string, HashSet<string>>
        {
            ["required"] = new HashSet<string> { "government", "primary_culture", "religion" },
            ["optional"] = new HashSet<string>
            {
                "technology_group", "capital", "fixed_capital", "religious_unity", "mercantilism",
                "add_accepted_culture", "remove_accepted_culture", "add_stability", "add_prestige",
                "add_legitimacy", "add_republican_tradition", "add_devotion", "add_horde_unity",
                "add_meritocracy", "add_corruption", "add_inflation", "add_manpower", "add_treasury"
            }
        };

        public ValidationResult ValidateFileStructure(string filePath, FileType fileType)
        {
            var result = new ValidationResult();

            if (!File.Exists(filePath))
            {
                result.AddCriticalError("File", $"File does not exist: {filePath}");
                return result;
            }

            try
            {
                var content = File.ReadAllText(filePath);
                return ValidateContentStructure(content, fileType, Path.GetFileName(filePath));
            }
            catch (Exception ex)
            {
                result.AddCriticalError("File", $"Failed to read file: {ex.Message}");
                return result;
            }
        }

        public ValidationResult ValidateContentStructure(string content, FileType fileType, string context = null)
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(content))
            {
                result.AddCriticalError("Content", "File content is empty", context);
                return result;
            }

            try
            {
                var lexer = new Lexer(content);
                var tokens = lexer.Tokenize();
                return ValidateTokenStructure(tokens, fileType, context);
            }
            catch (Exception ex)
            {
                result.AddCriticalError("Parsing", $"Failed to tokenize content: {ex.Message}", context);
                return result;
            }
        }

        private ValidationResult ValidateTokenStructure(List<Token> tokens, FileType fileType, string context)
        {
            var result = new ValidationResult();
            var braceStack = new Stack<int>();
            var currentKeys = new HashSet<string>();
            var expectedKeys = GetExpectedKeys(fileType);

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                switch (token.Type)
                {
                    case TokenType.LeftBrace:
                        braceStack.Push(token.Line);
                        break;

                    case TokenType.RightBrace:
                        if (braceStack.Count == 0)
                        {
                            result.AddError("Structure", "Unmatched closing brace", context, token.Line);
                        }
                        else
                        {
                            braceStack.Pop();
                        }
                        break;

                    case TokenType.String:
                        if (i + 1 < tokens.Count && tokens[i + 1].Type == TokenType.Equals)
                        {
                            var key = token.Value;
                            currentKeys.Add(key);

                            if (braceStack.Count == 1) // Top-level keys
                            {
                                ValidateKey(key, expectedKeys, result, context, token.Line);
                            }
                        }
                        break;

                    case TokenType.Date:
                        if (!ValidateDateFormat(token.Value))
                        {
                            result.AddError("Date", $"Invalid date format: {token.Value}", context, token.Line);
                        }
                        break;

                    case TokenType.Number:
                        if (!ValidateNumber(token.Value))
                        {
                            result.AddError("Number", $"Invalid number format: {token.Value}", context, token.Line);
                        }
                        break;
                }
            }

            if (braceStack.Count > 0)
            {
                result.AddError("Structure", $"{braceStack.Count} unclosed brace(s)", context);
            }

            ValidateRequiredKeys(currentKeys, expectedKeys, result, context);
            ValidateKeyDuplicates(currentKeys, result, context);

            return result;
        }

        private Dictionary<string, HashSet<string>> GetExpectedKeys(FileType fileType)
        {
            return fileType switch
            {
                FileType.Province => ExpectedProvinceKeys,
                FileType.Country => ExpectedCountryKeys,
                _ => new Dictionary<string, HashSet<string>>()
            };
        }

        private void ValidateKey(string key, Dictionary<string, HashSet<string>> expectedKeys, ValidationResult result, string context, int line)
        {
            if (expectedKeys.ContainsKey("required") && expectedKeys["required"].Contains(key))
            {
                return; // Valid required key
            }

            if (expectedKeys.ContainsKey("optional") && expectedKeys["optional"].Contains(key))
            {
                return; // Valid optional key
            }

            if (expectedKeys.ContainsKey("buildings") && expectedKeys["buildings"].Contains(key))
            {
                return; // Valid building key
            }

            if (IsDateKey(key))
            {
                return; // Historical date key
            }

            // Check for common typos or variations
            var suggestions = FindSimilarKeys(key, expectedKeys);
            if (suggestions.Count > 0)
            {
                result.AddWarning("Schema", $"Unknown key '{key}'. Did you mean: {string.Join(", ", suggestions)}?", context, line);
            }
            else
            {
                result.AddInfo("Schema", $"Unknown key '{key}' (may be mod-specific or newer version)", context, line);
            }
        }

        private List<string> FindSimilarKeys(string key, Dictionary<string, HashSet<string>> expectedKeys)
        {
            var suggestions = new List<string>();
            var allKeys = new HashSet<string>();

            foreach (var keySet in expectedKeys.Values)
            {
                foreach (var expectedKey in keySet)
                {
                    allKeys.Add(expectedKey);
                }
            }

            foreach (var expectedKey in allKeys)
            {
                if (CalculateLevenshteinDistance(key.ToLowerInvariant(), expectedKey.ToLowerInvariant()) <= 2)
                {
                    suggestions.Add(expectedKey);
                }
            }

            return suggestions;
        }

        private int CalculateLevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
            if (string.IsNullOrEmpty(b)) return a.Length;

            var distance = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++) distance[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) distance[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    distance[i, j] = Math.Min(Math.Min(
                        distance[i - 1, j] + 1,
                        distance[i, j - 1] + 1),
                        distance[i - 1, j - 1] + cost);
                }
            }

            return distance[a.Length, b.Length];
        }

        private void ValidateRequiredKeys(HashSet<string> foundKeys, Dictionary<string, HashSet<string>> expectedKeys, ValidationResult result, string context)
        {
            if (!expectedKeys.ContainsKey("required")) return;

            foreach (var requiredKey in expectedKeys["required"])
            {
                if (!foundKeys.Contains(requiredKey))
                {
                    result.AddError("Schema", $"Missing required key: '{requiredKey}'", context);
                }
            }
        }

        private void ValidateKeyDuplicates(HashSet<string> keys, ValidationResult result, string context)
        {
            // This method would need access to the original token list to detect duplicates
            // For now, we'll rely on the parser to handle this
        }

        private bool IsDateKey(string key)
        {
            return Regex.IsMatch(key, @"^\d{1,4}\.\d{1,2}\.\d{1,2}$");
        }

        private bool ValidateDateFormat(string date)
        {
            if (!Regex.IsMatch(date, @"^\d{1,4}\.\d{1,2}\.\d{1,2}$"))
                return false;

            var parts = date.Split('.');
            if (parts.Length != 3) return false;

            if (!int.TryParse(parts[0], out int year) || year < 1 || year > 9999) return false;
            if (!int.TryParse(parts[1], out int month) || month < 1 || month > 12) return false;
            if (!int.TryParse(parts[2], out int day) || day < 1 || day > 31) return false;

            // Basic month/day validation
            if ((month == 4 || month == 6 || month == 9 || month == 11) && day > 30) return false;
            if (month == 2 && day > 29) return false;

            return true;
        }

        private bool ValidateNumber(string number)
        {
            return double.TryParse(number, out _);
        }

        public ValidationResult ValidateEncoding(string filePath)
        {
            var result = new ValidationResult();

            if (!File.Exists(filePath))
            {
                result.AddCriticalError("File", $"File does not exist: {filePath}");
                return result;
            }

            try
            {
                var bytes = File.ReadAllBytes(filePath);

                // Check for BOM
                if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                {
                    result.AddInfo("Encoding", "UTF-8 BOM detected", Path.GetFileName(filePath));
                }

                // Simple check for likely encoding issues
                for (int i = 0; i < Math.Min(bytes.Length, 1000); i++)
                {
                    if (bytes[i] == 0)
                    {
                        result.AddWarning("Encoding", "Null bytes detected, possible binary file", Path.GetFileName(filePath));
                        break;
                    }

                    if (bytes[i] > 127 && bytes[i] < 160)
                    {
                        result.AddWarning("Encoding", "Possible encoding issues detected (bytes in range 128-159)", Path.GetFileName(filePath));
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                result.AddError("Encoding", $"Failed to read file for encoding validation: {ex.Message}");
            }

            return result;
        }
    }

    public enum FileType
    {
        Province,
        Country,
        Localization,
        History,
        Common,
        Unknown
    }
}