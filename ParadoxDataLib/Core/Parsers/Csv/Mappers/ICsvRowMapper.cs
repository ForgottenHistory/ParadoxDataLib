using System;

namespace ParadoxDataLib.Core.Parsers.Csv.Mappers
{
    /// <summary>
    /// Represents the result of validating a CSV row
    /// </summary>
    public readonly struct ValidationResult
    {
        /// <summary>
        /// Whether the validation passed
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Error message if validation failed
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        public static ValidationResult Success => new ValidationResult(true, null);

        /// <summary>
        /// Creates a failed validation result with an error message
        /// </summary>
        /// <param name="errorMessage">Description of the validation error</param>
        public static ValidationResult Failure(string errorMessage) => new ValidationResult(false, errorMessage);

        private ValidationResult(bool isValid, string errorMessage)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Defines the contract for mapping CSV rows to strongly-typed objects.
    /// Implementations handle the conversion from string arrays to specific data types.
    /// </summary>
    /// <typeparam name="T">The type of object to create from CSV rows</typeparam>
    public interface ICsvRowMapper<T>
    {
        /// <summary>
        /// Maps a CSV row (array of field values) to a strongly-typed object
        /// </summary>
        /// <param name="fields">Array of field values from the CSV row</param>
        /// <param name="lineNumber">Line number in the CSV file (for error reporting)</param>
        /// <returns>The mapped object</returns>
        /// <exception cref="ArgumentException">When the field array is invalid</exception>
        /// <exception cref="FormatException">When field values cannot be converted to the target type</exception>
        T MapRow(string[] fields, int lineNumber = 0);

        /// <summary>
        /// Validates that a CSV row has the correct structure and data types
        /// </summary>
        /// <param name="fields">Array of field values from the CSV row</param>
        /// <param name="lineNumber">Line number in the CSV file (for error reporting)</param>
        /// <returns>Validation result indicating success or failure with error message</returns>
        ValidationResult ValidateRow(string[] fields, int lineNumber = 0);

        /// <summary>
        /// Gets the expected number of fields for this mapper
        /// </summary>
        int ExpectedFieldCount { get; }

        /// <summary>
        /// Gets the expected header fields for this mapper
        /// </summary>
        string[] ExpectedHeaders { get; }

        /// <summary>
        /// Validates that the header row matches the expected format
        /// </summary>
        /// <param name="headers">Header field values</param>
        /// <returns>Validation result indicating if headers are compatible</returns>
        ValidationResult ValidateHeaders(string[] headers);

        /// <summary>
        /// Attempts to map a row without throwing exceptions
        /// </summary>
        /// <param name="fields">Array of field values from the CSV row</param>
        /// <param name="lineNumber">Line number in the CSV file</param>
        /// <param name="result">The mapped object if successful</param>
        /// <param name="errorMessage">Error message if mapping failed</param>
        /// <returns>True if mapping succeeded, false otherwise</returns>
        bool TryMapRow(string[] fields, int lineNumber, out T result, out string errorMessage);
    }
}