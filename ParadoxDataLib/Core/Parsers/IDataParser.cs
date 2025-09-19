using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ParadoxDataLib.Core.Parsers
{
    /// <summary>
    /// Defines the contract for parsing Paradox game data files into strongly-typed objects.
    /// Supports both synchronous and asynchronous operations for single and batch parsing.
    /// </summary>
    /// <typeparam name="T">The type of object to parse into (e.g., ProvinceData, CountryData)</typeparam>
    public interface IDataParser<T>
    {
        /// <summary>
        /// Parses Paradox script content into a strongly-typed object.
        /// </summary>
        /// <param name="content">The raw Paradox script content to parse</param>
        /// <returns>The parsed object of type T</returns>
        /// <exception cref="ParsingException">Thrown when the content contains syntax errors or invalid data</exception>
        T Parse(string content);

        /// <summary>
        /// Parses a Paradox script file into a strongly-typed object.
        /// Automatically detects file encoding (UTF-8 or Windows-1252).
        /// </summary>
        /// <param name="filePath">The absolute path to the file to parse</param>
        /// <returns>The parsed object of type T</returns>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist</exception>
        /// <exception cref="ParsingException">Thrown when the file contains syntax errors or invalid data</exception>
        T ParseFile(string filePath);

        /// <summary>
        /// Asynchronously parses Paradox script content into a strongly-typed object.
        /// Recommended for large files or when parsing on the main thread.
        /// </summary>
        /// <param name="content">The raw Paradox script content to parse</param>
        /// <returns>A task that represents the asynchronous parse operation</returns>
        /// <exception cref="ParsingException">Thrown when the content contains syntax errors or invalid data</exception>
        Task<T> ParseAsync(string content);

        /// <summary>
        /// Asynchronously parses a Paradox script file into a strongly-typed object.
        /// Uses async I/O for better performance with large files.
        /// </summary>
        /// <param name="filePath">The absolute path to the file to parse</param>
        /// <returns>A task that represents the asynchronous parse operation</returns>
        /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist</exception>
        /// <exception cref="ParsingException">Thrown when the file contains syntax errors or invalid data</exception>
        Task<T> ParseFileAsync(string filePath);

        /// <summary>
        /// Parses multiple Paradox script contents in parallel for improved performance.
        /// Each content string is parsed independently and results are returned in order.
        /// </summary>
        /// <param name="contents">Collection of raw Paradox script contents to parse</param>
        /// <returns>List of parsed objects in the same order as input</returns>
        /// <exception cref="AggregateException">Contains all parsing exceptions from failed items</exception>
        List<T> ParseMultiple(IEnumerable<string> contents);

        /// <summary>
        /// Asynchronously parses multiple Paradox script contents in parallel.
        /// Optimized for batch processing with controlled parallelism.
        /// </summary>
        /// <param name="contents">Collection of raw Paradox script contents to parse</param>
        /// <returns>A task containing the list of parsed objects in input order</returns>
        /// <exception cref="AggregateException">Contains all parsing exceptions from failed items</exception>
        Task<List<T>> ParseMultipleAsync(IEnumerable<string> contents);

        /// <summary>
        /// Attempts to parse content without throwing exceptions on failure.
        /// Recommended for validation scenarios or when parsing user-provided content.
        /// </summary>
        /// <param name="content">The raw Paradox script content to parse</param>
        /// <param name="result">The parsed object if successful, default(T) if failed</param>
        /// <param name="error">Detailed error message if parsing failed, null if successful</param>
        /// <returns>True if parsing succeeded, false otherwise</returns>
        bool TryParse(string content, out T result, out string error);
    }
}