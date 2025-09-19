using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ParadoxDataLib.Core.Parsers.Csv
{
    /// <summary>
    /// Defines the contract for reading CSV files with support for different encodings,
    /// separators, and streaming operations.
    /// </summary>
    public interface ICsvReader : IDisposable
    {
        /// <summary>
        /// The character used to separate CSV fields
        /// </summary>
        char Separator { get; }

        /// <summary>
        /// The encoding used to read the CSV file
        /// </summary>
        Encoding Encoding { get; }

        /// <summary>
        /// Whether the CSV file has been opened for reading
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Opens a CSV file for reading
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <exception cref="FileNotFoundException">When the file does not exist</exception>
        /// <exception cref="IOException">When the file cannot be opened</exception>
        void Open(string filePath);

        /// <summary>
        /// Asynchronously opens a CSV file for reading
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <exception cref="FileNotFoundException">When the file does not exist</exception>
        /// <exception cref="IOException">When the file cannot be opened</exception>
        Task OpenAsync(string filePath);

        /// <summary>
        /// Reads a single line from the CSV file and splits it into fields
        /// </summary>
        /// <returns>Array of field values, or null if end of file</returns>
        string[] ReadLine();

        /// <summary>
        /// Asynchronously reads a single line from the CSV file and splits it into fields
        /// </summary>
        /// <returns>Array of field values, or null if end of file</returns>
        Task<string[]> ReadLineAsync();

        /// <summary>
        /// Reads all lines from the CSV file
        /// </summary>
        /// <returns>Collection of field arrays</returns>
        IEnumerable<string[]> ReadAllLines();

        /// <summary>
        /// Asynchronously reads all lines from the CSV file
        /// </summary>
        /// <returns>Collection of field arrays</returns>
        IAsyncEnumerable<string[]> ReadAllLinesAsync();

        /// <summary>
        /// Reads the header row from the CSV file
        /// </summary>
        /// <returns>Array of column names</returns>
        string[] ReadHeader();

        /// <summary>
        /// Asynchronously reads the header row from the CSV file
        /// </summary>
        /// <returns>Array of column names</returns>
        Task<string[]> ReadHeaderAsync();

        /// <summary>
        /// Parses a CSV line into fields, handling quotes and escaping
        /// </summary>
        /// <param name="line">The CSV line to parse</param>
        /// <returns>Array of field values</returns>
        string[] ParseLine(string line);

        /// <summary>
        /// Closes the CSV file and releases resources
        /// </summary>
        void Close();
    }
}