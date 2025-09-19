using System;
using System.Globalization;
using ParadoxDataLib.Core.Parsers.Csv.DataStructures;

namespace ParadoxDataLib.Core.Parsers.Csv.Mappers
{
    /// <summary>
    /// Maps CSV rows from map/adjacencies.csv to Adjacency objects.
    /// Handles the format: From;To;Type;Through;start_x;start_y;stop_x;stop_y;Comment
    /// </summary>
    public class AdjacencyMapper : ICsvRowMapper<Adjacency>
    {
        private static readonly string[] ExpectedHeaderValues =
        {
            "From", "To", "Type", "Through", "start_x", "start_y", "stop_x", "stop_y", "Comment"
        };

        private static readonly string[] ValidAdjacencyTypes =
        {
            "sea", "land", "river", "impassable", "canal"
        };

        /// <summary>
        /// Gets the expected number of fields for adjacency rows
        /// </summary>
        public int ExpectedFieldCount => 9;

        /// <summary>
        /// Gets the expected header fields for adjacencies CSV
        /// </summary>
        public string[] ExpectedHeaders => ExpectedHeaderValues;

        /// <summary>
        /// Maps a CSV row to an Adjacency object
        /// </summary>
        /// <param name="fields">Array of field values: [From, To, Type, Through, start_x, start_y, stop_x, stop_y, Comment]</param>
        /// <param name="lineNumber">Line number for error reporting</param>
        /// <returns>Adjacency object</returns>
        public Adjacency MapRow(string[] fields, int lineNumber = 0)
        {
            var validation = ValidateRow(fields, lineNumber);
            if (!validation.IsValid)
                throw new FormatException($"Line {lineNumber}: {validation.ErrorMessage}");

            try
            {
                var from = int.Parse(fields[0], CultureInfo.InvariantCulture);
                var to = int.Parse(fields[1], CultureInfo.InvariantCulture);
                var type = fields[2];
                var through = ParseIntOrDefault(fields[3], -1);
                var startX = ParseIntOrDefault(fields[4], -1);
                var startY = ParseIntOrDefault(fields[5], -1);
                var stopX = ParseIntOrDefault(fields[6], -1);
                var stopY = ParseIntOrDefault(fields[7], -1);
                var comment = fields[8] ?? string.Empty;

                return new Adjacency(from, to, type, through, startX, startY, stopX, stopY, comment);
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException)
            {
                throw new FormatException($"Line {lineNumber}: Failed to parse adjacency - {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates that a CSV row has the correct structure for adjacencies
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

            // Validate From province ID
            if (string.IsNullOrWhiteSpace(fields[0]))
                return ValidationResult.Failure("From province ID cannot be empty");

            if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var fromId))
                return ValidationResult.Failure($"Invalid From province ID: '{fields[0]}'");

            if (fromId <= 0)
                return ValidationResult.Failure($"From province ID must be positive: {fromId}");

            // Validate To province ID
            if (string.IsNullOrWhiteSpace(fields[1]))
                return ValidationResult.Failure("To province ID cannot be empty");

            if (!int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var toId))
                return ValidationResult.Failure($"Invalid To province ID: '{fields[1]}'");

            if (toId <= 0)
                return ValidationResult.Failure($"To province ID must be positive: {toId}");

            // Validate adjacency type
            if (string.IsNullOrWhiteSpace(fields[2]))
                return ValidationResult.Failure("Adjacency type cannot be empty");

            var typeValue = fields[2].Trim().ToLowerInvariant();
            var isValidType = false;
            foreach (var validType in ValidAdjacencyTypes)
            {
                if (typeValue.Equals(validType, StringComparison.OrdinalIgnoreCase))
                {
                    isValidType = true;
                    break;
                }
            }

            if (!isValidType)
                return ValidationResult.Failure($"Invalid adjacency type: '{fields[2]}'. Valid types: {string.Join(", ", ValidAdjacencyTypes)}");

            // Validate Through field (can be -1 or positive integer)
            if (!string.IsNullOrWhiteSpace(fields[3]) && fields[3] != "-1")
            {
                if (!int.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var throughId))
                    return ValidationResult.Failure($"Invalid Through province ID: '{fields[3]}'");

                if (throughId <= 0)
                    return ValidationResult.Failure($"Through province ID must be positive or -1: {throughId}");
            }

            // Validate coordinate fields (can be -1 or any integer)
            for (int i = 4; i <= 7; i++)
            {
                if (!string.IsNullOrWhiteSpace(fields[i]) && fields[i] != "-1")
                {
                    if (!int.TryParse(fields[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                        return ValidationResult.Failure($"Invalid coordinate value in field {i + 1}: '{fields[i]}'");
                }
            }

            // Comment field (field 8) can be anything
            if (fields[8] == null)
                return ValidationResult.Failure("Comment field cannot be null");

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
            if (!normalizedHeaders[0].Contains("from"))
                return ValidationResult.Failure($"First column should contain 'from', got '{headers[0]}'");

            if (!normalizedHeaders[1].Contains("to"))
                return ValidationResult.Failure($"Second column should contain 'to', got '{headers[1]}'");

            if (!normalizedHeaders[2].Contains("type"))
                return ValidationResult.Failure($"Third column should contain 'type', got '{headers[2]}'");

            if (!normalizedHeaders[3].Contains("through"))
                return ValidationResult.Failure($"Fourth column should contain 'through', got '{headers[3]}'");

            return ValidationResult.Success;
        }

        /// <summary>
        /// Attempts to map a row without throwing exceptions
        /// </summary>
        /// <param name="fields">Array of field values</param>
        /// <param name="lineNumber">Line number</param>
        /// <param name="result">The mapped Adjacency if successful</param>
        /// <param name="errorMessage">Error message if mapping failed</param>
        /// <returns>True if mapping succeeded</returns>
        public bool TryMapRow(string[] fields, int lineNumber, out Adjacency result, out string errorMessage)
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

        /// <summary>
        /// Parses an integer field that can be empty, -1, or a valid integer
        /// </summary>
        /// <param name="value">String value to parse</param>
        /// <param name="defaultValue">Default value if parsing fails</param>
        /// <returns>Parsed integer or default value</returns>
        private static int ParseIntOrDefault(string value, int defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value) || value == "-1")
                return -1;

            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
                ? result
                : defaultValue;
        }
    }
}