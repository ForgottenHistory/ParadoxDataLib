using System;
using System.IO;
using ParadoxDataLib.Core.Common;
using ParadoxDataLib.Core.Extractors;
using ParadoxDataLib.Core.Parsers;

namespace ParadoxDataLib.Core
{
    /// <summary>
    /// Unified file reader that combines generic parsing with specific data extraction.
    /// This class provides the main entry point for parsing any Paradox game file into strongly-typed objects.
    /// </summary>
    public class ParadoxFileReader
    {
        private readonly GenericParadoxParser _parser;

        /// <summary>
        /// Initializes a new instance of the ParadoxFileReader
        /// </summary>
        public ParadoxFileReader()
        {
            _parser = new GenericParadoxParser();
        }

        /// <summary>
        /// Reads and parses a Paradox file using the specified extractor
        /// </summary>
        /// <typeparam name="T">The type of data to extract</typeparam>
        /// <param name="filePath">Path to the Paradox file</param>
        /// <param name="extractor">The extractor to use for converting the parsed data</param>
        /// <returns>The extracted data object</returns>
        /// <exception cref="FileNotFoundException">Thrown when the file doesn't exist</exception>
        /// <exception cref="InvalidOperationException">Thrown when the extractor cannot process the file</exception>
        public T ReadFile<T>(string filePath, IDataExtractor<T> extractor)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            var rootNode = _parser.ParseFile(filePath);

            if (!extractor.CanExtract(rootNode))
                throw new InvalidOperationException($"The provided extractor cannot process the file: {filePath}");

            return extractor.Extract(rootNode);
        }

        /// <summary>
        /// Reads and parses Paradox content from a string using the specified extractor
        /// </summary>
        /// <typeparam name="T">The type of data to extract</typeparam>
        /// <param name="content">The Paradox file content as a string</param>
        /// <param name="extractor">The extractor to use for converting the parsed data</param>
        /// <returns>The extracted data object</returns>
        /// <exception cref="InvalidOperationException">Thrown when the extractor cannot process the content</exception>
        public T ReadContent<T>(string content, IDataExtractor<T> extractor)
        {
            var rootNode = ParseContentToNode(content);

            if (!extractor.CanExtract(rootNode))
                throw new InvalidOperationException("The provided extractor cannot process the given content");

            return extractor.Extract(rootNode);
        }

        /// <summary>
        /// Parses a file into the generic ParadoxNode tree structure without extraction
        /// </summary>
        /// <param name="filePath">Path to the Paradox file</param>
        /// <returns>The parsed ParadoxNode tree</returns>
        public ParadoxNode ParseFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            return _parser.ParseFile(filePath);
        }

        /// <summary>
        /// Parses content into the generic ParadoxNode tree structure without extraction
        /// </summary>
        /// <param name="content">The Paradox file content as a string</param>
        /// <returns>The parsed ParadoxNode tree</returns>
        public ParadoxNode ParseContent(string content)
        {
            return ParseContentToNode(content);
        }

        /// <summary>
        /// Helper method to parse content string into ParadoxNode
        /// </summary>
        /// <param name="content">The content to parse</param>
        /// <returns>The parsed ParadoxNode tree</returns>
        private ParadoxNode ParseContentToNode(string content)
        {
            // Create a temporary file to use existing ParseFile method
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, content);
                return _parser.ParseFile(tempFile);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Validates whether a file can be processed by a specific extractor
        /// </summary>
        /// <typeparam name="T">The type of data to extract</typeparam>
        /// <param name="filePath">Path to the Paradox file</param>
        /// <param name="extractor">The extractor to validate against</param>
        /// <returns>True if the extractor can process the file</returns>
        public bool CanExtract<T>(string filePath, IDataExtractor<T> extractor)
        {
            try
            {
                var rootNode = ParseFile(filePath);
                return extractor.CanExtract(rootNode);
            }
            catch
            {
                return false;
            }
        }
    }
}