using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ParadoxDataLib.Core.Parsers.Csv.DataStructures;
using ParadoxDataLib.Core.Parsers.Csv.Mappers;

namespace ParadoxDataLib.Core.Parsers.Csv.Specialized
{
    /// <summary>
    /// Specialized reader for map/definition.csv files that contain province definitions.
    /// Provides convenient methods for reading and validating province definition data.
    /// </summary>
    public class ProvinceDefinitionReader
    {
        private readonly CsvParser<ProvinceDefinition> _parser;

        /// <summary>
        /// Creates a new province definition reader
        /// </summary>
        /// <param name="continueOnError">Whether to continue parsing when individual rows fail</param>
        public ProvinceDefinitionReader(bool continueOnError = true)
        {
            var csvReader = new StreamingCsvReader(';', encoding: null, '"', true);
            var mapper = new ProvinceDefinitionMapper();
            _parser = new CsvParser<ProvinceDefinition>(mapper, csvReader, false, continueOnError);
        }

        /// <summary>
        /// Reads province definitions from a CSV file
        /// </summary>
        /// <param name="filePath">Path to the definition.csv file</param>
        /// <returns>List of province definitions</returns>
        public List<ProvinceDefinition> ReadFile(string filePath)
        {
            return _parser.ParseFile(filePath);
        }

        /// <summary>
        /// Reads province definitions from a CSV file with statistics
        /// </summary>
        /// <param name="filePath">Path to the definition.csv file</param>
        /// <param name="stats">Output parameter containing parsing statistics</param>
        /// <returns>List of province definitions</returns>
        public List<ProvinceDefinition> ReadFile(string filePath, out CsvParsingStats stats)
        {
            return _parser.ParseFile(filePath, out stats);
        }

        /// <summary>
        /// Asynchronously reads province definitions from a CSV file
        /// </summary>
        /// <param name="filePath">Path to the definition.csv file</param>
        /// <returns>List of province definitions</returns>
        public Task<List<ProvinceDefinition>> ReadFileAsync(string filePath)
        {
            return _parser.ParseFileAsync(filePath);
        }

        /// <summary>
        /// Reads province definitions and returns them as a dictionary for fast lookup
        /// </summary>
        /// <param name="filePath">Path to the definition.csv file</param>
        /// <returns>Dictionary mapping province IDs to province definitions</returns>
        public Dictionary<int, ProvinceDefinition> ReadAsDictionary(string filePath)
        {
            var definitions = _parser.ParseFile(filePath);
            return definitions.ToDictionary(d => d.ProvinceId, d => d);
        }

        /// <summary>
        /// Reads province definitions and returns them as a dictionary with statistics
        /// </summary>
        /// <param name="filePath">Path to the definition.csv file</param>
        /// <param name="stats">Output parameter containing parsing statistics</param>
        /// <returns>Dictionary mapping province IDs to province definitions</returns>
        public Dictionary<int, ProvinceDefinition> ReadAsDictionary(string filePath, out CsvParsingStats stats)
        {
            var definitions = _parser.ParseFile(filePath, out stats);
            return definitions.ToDictionary(d => d.ProvinceId, d => d);
        }

        /// <summary>
        /// Asynchronously reads province definitions as a dictionary
        /// </summary>
        /// <param name="filePath">Path to the definition.csv file</param>
        /// <returns>Dictionary mapping province IDs to province definitions</returns>
        public async Task<Dictionary<int, ProvinceDefinition>> ReadAsDictionaryAsync(string filePath)
        {
            var definitions = await _parser.ParseFileAsync(filePath);
            return definitions.ToDictionary(d => d.ProvinceId, d => d);
        }

        /// <summary>
        /// Creates an RGB-to-Province ID lookup dictionary
        /// </summary>
        /// <param name="filePath">Path to the definition.csv file</param>
        /// <returns>Dictionary mapping RGB values to province IDs</returns>
        public Dictionary<int, int> CreateRgbLookup(string filePath)
        {
            var definitions = _parser.ParseFile(filePath);
            var lookup = new Dictionary<int, int>();

            foreach (var definition in definitions)
            {
                var rgbValue = definition.RgbValue;
                if (lookup.ContainsKey(rgbValue))
                {
                    throw new InvalidOperationException(
                        $"Duplicate RGB value {definition.RgbString} found for provinces {lookup[rgbValue]} and {definition.ProvinceId}");
                }
                lookup[rgbValue] = definition.ProvinceId;
            }

            return lookup;
        }

        /// <summary>
        /// Validates province definitions for common issues
        /// </summary>
        /// <param name="filePath">Path to the definition.csv file</param>
        /// <returns>Validation statistics including any errors found</returns>
        public ProvinceDefinitionValidationResult ValidateFile(string filePath)
        {
            var stats = _parser.ValidateFile(filePath);
            var definitions = new List<ProvinceDefinition>();

            try
            {
                definitions = _parser.ParseFile(filePath);
            }
            catch
            {
                // If parsing fails completely, we can't do additional validation
            }

            return new ProvinceDefinitionValidationResult(stats, definitions);
        }

        /// <summary>
        /// Reads CSV content from a string
        /// </summary>
        /// <param name="csvContent">CSV content as string</param>
        /// <returns>List of province definitions</returns>
        public List<ProvinceDefinition> ReadContent(string csvContent)
        {
            return _parser.ParseContent(csvContent);
        }
    }

    /// <summary>
    /// Extended validation result for province definitions
    /// </summary>
    public class ProvinceDefinitionValidationResult
    {
        /// <summary>
        /// Basic CSV parsing statistics
        /// </summary>
        public CsvParsingStats ParsingStats { get; }

        /// <summary>
        /// List of province definitions that were successfully parsed
        /// </summary>
        public List<ProvinceDefinition> ValidDefinitions { get; }

        /// <summary>
        /// List of duplicate RGB values found
        /// </summary>
        public List<string> DuplicateRgbValues { get; }

        /// <summary>
        /// List of duplicate province IDs found
        /// </summary>
        public List<int> DuplicateProvinceIds { get; }

        /// <summary>
        /// List of provinces with invalid RGB values (all black or all white)
        /// </summary>
        public List<int> SuspiciousRgbValues { get; }

        /// <summary>
        /// Whether the validation passed all checks
        /// </summary>
        public bool IsValid => ParsingStats.IsSuccessful &&
                              DuplicateRgbValues.Count == 0 &&
                              DuplicateProvinceIds.Count == 0;

        public ProvinceDefinitionValidationResult(CsvParsingStats parsingStats, List<ProvinceDefinition> definitions)
        {
            ParsingStats = parsingStats;
            ValidDefinitions = definitions;
            DuplicateRgbValues = new List<string>();
            DuplicateProvinceIds = new List<int>();
            SuspiciousRgbValues = new List<int>();

            PerformAdditionalValidation();
        }

        private void PerformAdditionalValidation()
        {
            if (ValidDefinitions == null || ValidDefinitions.Count == 0)
                return;

            // Check for duplicate RGB values
            var rgbGroups = ValidDefinitions.GroupBy(d => d.RgbValue);
            foreach (var group in rgbGroups.Where(g => g.Count() > 1))
            {
                var provinces = string.Join(", ", group.Select(d => d.ProvinceId));
                DuplicateRgbValues.Add($"RGB {group.First().RgbString} used by provinces: {provinces}");
            }

            // Check for duplicate province IDs
            var idGroups = ValidDefinitions.GroupBy(d => d.ProvinceId);
            foreach (var group in idGroups.Where(g => g.Count() > 1))
            {
                DuplicateProvinceIds.Add(group.Key);
            }

            // Check for suspicious RGB values
            foreach (var definition in ValidDefinitions)
            {
                // Check for all black (0,0,0) or all white (255,255,255) which might be errors
                if ((definition.Red == 0 && definition.Green == 0 && definition.Blue == 0) ||
                    (definition.Red == 255 && definition.Green == 255 && definition.Blue == 255))
                {
                    SuspiciousRgbValues.Add(definition.ProvinceId);
                }
            }
        }
    }
}