using System;
using System.Globalization;
using ParadoxDataLib.Core.Parsers.Csv.DataStructures;

namespace ParadoxDataLib.Core.Parsers.Csv.Mappers
{
    /// <summary>
    /// Maps CSV rows from map/definition.csv to ProvinceDefinition objects.
    /// Handles the format: province;red;green;blue;name;x
    /// </summary>
    public class ProvinceDefinitionMapper : ICsvRowMapper<ProvinceDefinition>
    {
        private static readonly string[] ExpectedHeaderValues =
        {
            "province", "red", "green", "blue", "x", "x"
        };

        /// <summary>
        /// Gets the expected number of fields for province definition rows
        /// </summary>
        public int ExpectedFieldCount => 6;

        /// <summary>
        /// Gets the expected header fields for province definition CSV
        /// </summary>
        public string[] ExpectedHeaders => ExpectedHeaderValues;

        /// <summary>
        /// Maps a CSV row to a ProvinceDefinition object
        /// </summary>
        /// <param name="fields">Array of field values: [province, red, green, blue, name, unused]</param>
        /// <param name="lineNumber">Line number for error reporting</param>
        /// <returns>ProvinceDefinition object</returns>
        public ProvinceDefinition MapRow(string[] fields, int lineNumber = 0)
        {
            var validation = ValidateRow(fields, lineNumber);
            if (!validation.IsValid)
                throw new FormatException($"Line {lineNumber}: {validation.ErrorMessage}");

            try
            {
                var provinceId = int.Parse(fields[0], CultureInfo.InvariantCulture);
                var red = byte.Parse(fields[1], CultureInfo.InvariantCulture);
                var green = byte.Parse(fields[2], CultureInfo.InvariantCulture);
                var blue = byte.Parse(fields[3], CultureInfo.InvariantCulture);
                var name = fields[4];
                var unused = fields[5];

                return new ProvinceDefinition(provinceId, red, green, blue, name, unused);
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException)
            {
                throw new FormatException($"Line {lineNumber}: Failed to parse province definition - {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates that a CSV row has the correct structure for province definitions
        /// </summary>
        /// <param name="fields">Array of field values</param>
        /// <param name="lineNumber">Line number for error reporting</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateRow(string[] fields, int lineNumber = 0)
        {
            if (fields == null)
                return ValidationResult.Failure("Row is null");

            if (fields.Length != ExpectedFieldCount)
                return ValidationResult.Failure($"Expected {ExpectedFieldCount} fields, got {fields.Length}");

            // Validate province ID
            if (string.IsNullOrWhiteSpace(fields[0]))
                return ValidationResult.Failure("Province ID cannot be empty");

            if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var provinceId))
                return ValidationResult.Failure($"Invalid province ID: '{fields[0]}'");

            if (provinceId <= 0)
                return ValidationResult.Failure($"Province ID must be positive: {provinceId}");

            // Validate RGB values
            for (int i = 1; i <= 3; i++)
            {
                if (string.IsNullOrWhiteSpace(fields[i]))
                    return ValidationResult.Failure($"RGB component {i} cannot be empty");

                if (!byte.TryParse(fields[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var colorValue))
                    return ValidationResult.Failure($"Invalid RGB component {i}: '{fields[i]}'");
            }

            // Validate province name (field 4) - can be empty but not null
            if (fields[4] == null)
                return ValidationResult.Failure("Province name cannot be null");

            // Field 5 (unused) can be anything
            if (fields[5] == null)
                return ValidationResult.Failure("Unused field cannot be null");

            return ValidationResult.Success;
        }

        /// <summary>
        /// Validates that the header row matches the expected format
        /// </summary>
        /// <param name="headers">Header field values</param>
        /// <returns>Validation result</returns>
        public ValidationResult ValidateHeaders(string[] headers)
        {
            if (headers == null)
                return ValidationResult.Failure("Headers are null");

            if (headers.Length != ExpectedFieldCount)
                return ValidationResult.Failure($"Expected {ExpectedFieldCount} header fields, got {headers.Length}");

            // Check for required headers (allowing some flexibility in naming)
            var normalizedHeaders = new string[headers.Length];
            for (int i = 0; i < headers.Length; i++)
            {
                normalizedHeaders[i] = headers[i]?.Trim().ToLowerInvariant() ?? string.Empty;
            }

            // Validate critical headers
            if (!normalizedHeaders[0].Contains("province"))
                return ValidationResult.Failure($"First column should contain 'province', got '{headers[0]}'");

            if (!normalizedHeaders[1].Contains("red"))
                return ValidationResult.Failure($"Second column should contain 'red', got '{headers[1]}'");

            if (!normalizedHeaders[2].Contains("green"))
                return ValidationResult.Failure($"Third column should contain 'green', got '{headers[2]}'");

            if (!normalizedHeaders[3].Contains("blue"))
                return ValidationResult.Failure($"Fourth column should contain 'blue', got '{headers[3]}'");

            return ValidationResult.Success;
        }

        /// <summary>
        /// Attempts to map a row without throwing exceptions
        /// </summary>
        /// <param name="fields">Array of field values</param>
        /// <param name="lineNumber">Line number</param>
        /// <param name="result">The mapped ProvinceDefinition if successful</param>
        /// <param name="errorMessage">Error message if mapping failed</param>
        /// <returns>True if mapping succeeded</returns>
        public bool TryMapRow(string[] fields, int lineNumber, out ProvinceDefinition result, out string errorMessage)
        {
            result = default;
            errorMessage = null;

            try
            {
                var validation = ValidateRow(fields, lineNumber);
                if (!validation.IsValid)
                {
                    errorMessage = validation.ErrorMessage;
                    return false;
                }

                result = MapRow(fields, lineNumber);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}