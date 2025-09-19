using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ParadoxDataLib.Core.Parsers.Csv.DataStructures;
using ParadoxDataLib.Core.Parsers.Csv.Mappers;

namespace ParadoxDataLib.Core.Parsers.Csv.Specialized
{
    /// <summary>
    /// Specialized reader for map/adjacencies.csv files that contain province adjacency definitions.
    /// Provides convenient methods for reading and validating adjacency data.
    /// </summary>
    public class AdjacenciesReader
    {
        private readonly CsvParser<Adjacency> _parser;

        /// <summary>
        /// Creates a new adjacencies reader
        /// </summary>
        /// <param name="continueOnError">Whether to continue parsing when individual rows fail</param>
        public AdjacenciesReader(bool continueOnError = true)
        {
            var csvReader = new StreamingCsvReader(';', encoding: null, '"', true);
            var mapper = new AdjacencyMapper();
            _parser = new CsvParser<Adjacency>(mapper, csvReader, false, continueOnError);
        }

        /// <summary>
        /// Reads adjacency definitions from a CSV file
        /// </summary>
        /// <param name="filePath">Path to the adjacencies.csv file</param>
        /// <returns>List of adjacency definitions</returns>
        public List<Adjacency> ReadFile(string filePath)
        {
            return _parser.ParseFile(filePath);
        }

        /// <summary>
        /// Reads adjacency definitions from a CSV file with statistics
        /// </summary>
        /// <param name="filePath">Path to the adjacencies.csv file</param>
        /// <param name="stats">Output parameter containing parsing statistics</param>
        /// <returns>List of adjacency definitions</returns>
        public List<Adjacency> ReadFile(string filePath, out CsvParsingStats stats)
        {
            return _parser.ParseFile(filePath, out stats);
        }

        /// <summary>
        /// Asynchronously reads adjacency definitions from a CSV file
        /// </summary>
        /// <param name="filePath">Path to the adjacencies.csv file</param>
        /// <returns>List of adjacency definitions</returns>
        public Task<List<Adjacency>> ReadFileAsync(string filePath)
        {
            return _parser.ParseFileAsync(filePath);
        }

        /// <summary>
        /// Reads adjacencies and groups them by source province for fast lookup
        /// </summary>
        /// <param name="filePath">Path to the adjacencies.csv file</param>
        /// <returns>Dictionary mapping province IDs to their adjacencies</returns>
        public Dictionary<int, List<Adjacency>> ReadAsLookup(string filePath)
        {
            var adjacencies = _parser.ParseFile(filePath);
            return GroupAdjacenciesByProvince(adjacencies);
        }

        /// <summary>
        /// Reads adjacencies as a lookup dictionary with statistics
        /// </summary>
        /// <param name="filePath">Path to the adjacencies.csv file</param>
        /// <param name="stats">Output parameter containing parsing statistics</param>
        /// <returns>Dictionary mapping province IDs to their adjacencies</returns>
        public Dictionary<int, List<Adjacency>> ReadAsLookup(string filePath, out CsvParsingStats stats)
        {
            var adjacencies = _parser.ParseFile(filePath, out stats);
            return GroupAdjacenciesByProvince(adjacencies);
        }

        /// <summary>
        /// Asynchronously reads adjacencies as a lookup dictionary
        /// </summary>
        /// <param name="filePath">Path to the adjacencies.csv file</param>
        /// <returns>Dictionary mapping province IDs to their adjacencies</returns>
        public async Task<Dictionary<int, List<Adjacency>>> ReadAsLookupAsync(string filePath)
        {
            var adjacencies = await _parser.ParseFileAsync(filePath);
            return GroupAdjacenciesByProvince(adjacencies);
        }

        /// <summary>
        /// Reads adjacencies and creates a bidirectional lookup (including reverse connections)
        /// </summary>
        /// <param name="filePath">Path to the adjacencies.csv file</param>
        /// <returns>Dictionary with both directions of each adjacency</returns>
        public Dictionary<int, List<Adjacency>> ReadAsBidirectionalLookup(string filePath)
        {
            var adjacencies = _parser.ParseFile(filePath);
            return CreateBidirectionalLookup(adjacencies);
        }

        /// <summary>
        /// Filters adjacencies by type (sea, land, river, etc.)
        /// </summary>
        /// <param name="filePath">Path to the adjacencies.csv file</param>
        /// <param name="adjacencyType">Type of adjacencies to include</param>
        /// <returns>List of adjacencies of the specified type</returns>
        public List<Adjacency> ReadByType(string filePath, string adjacencyType)
        {
            var adjacencies = _parser.ParseFile(filePath);
            return adjacencies.Where(a => a.Type.Equals(adjacencyType, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Validates adjacency definitions and cross-references with province definitions
        /// </summary>
        /// <param name="filePath">Path to the adjacencies.csv file</param>
        /// <param name="validProvinceIds">Set of valid province IDs for cross-reference validation</param>
        /// <returns>Validation result with any errors found</returns>
        public AdjacencyValidationResult ValidateFile(string filePath, HashSet<int> validProvinceIds = null)
        {
            var stats = _parser.ValidateFile(filePath);
            var adjacencies = new List<Adjacency>();

            try
            {
                adjacencies = _parser.ParseFile(filePath);
            }
            catch
            {
                // If parsing fails completely, we can't do additional validation
            }

            return new AdjacencyValidationResult(stats, adjacencies, validProvinceIds);
        }

        /// <summary>
        /// Reads CSV content from a string
        /// </summary>
        /// <param name="csvContent">CSV content as string</param>
        /// <returns>List of adjacency definitions</returns>
        public List<Adjacency> ReadContent(string csvContent)
        {
            return _parser.ParseContent(csvContent);
        }

        /// <summary>
        /// Groups adjacencies by source province ID
        /// </summary>
        private static Dictionary<int, List<Adjacency>> GroupAdjacenciesByProvince(List<Adjacency> adjacencies)
        {
            var lookup = new Dictionary<int, List<Adjacency>>();

            foreach (var adjacency in adjacencies)
            {
                if (!lookup.ContainsKey(adjacency.From))
                    lookup[adjacency.From] = new List<Adjacency>();

                lookup[adjacency.From].Add(adjacency);
            }

            return lookup;
        }

        /// <summary>
        /// Creates a bidirectional lookup including reverse adjacencies
        /// </summary>
        private static Dictionary<int, List<Adjacency>> CreateBidirectionalLookup(List<Adjacency> adjacencies)
        {
            var lookup = new Dictionary<int, List<Adjacency>>();

            foreach (var adjacency in adjacencies)
            {
                // Add original adjacency
                if (!lookup.ContainsKey(adjacency.From))
                    lookup[adjacency.From] = new List<Adjacency>();
                lookup[adjacency.From].Add(adjacency);

                // Add reverse adjacency for bidirectional types
                if (adjacency.IsBidirectional)
                {
                    if (!lookup.ContainsKey(adjacency.To))
                        lookup[adjacency.To] = new List<Adjacency>();
                    lookup[adjacency.To].Add(adjacency.Reverse());
                }
            }

            return lookup;
        }
    }

    /// <summary>
    /// Extended validation result for adjacencies
    /// </summary>
    public class AdjacencyValidationResult
    {
        /// <summary>
        /// Basic CSV parsing statistics
        /// </summary>
        public CsvParsingStats ParsingStats { get; }

        /// <summary>
        /// List of adjacencies that were successfully parsed
        /// </summary>
        public List<Adjacency> ValidAdjacencies { get; }

        /// <summary>
        /// List of adjacencies that reference non-existent provinces
        /// </summary>
        public List<string> InvalidProvinceReferences { get; }

        /// <summary>
        /// List of duplicate adjacency connections found
        /// </summary>
        public List<string> DuplicateConnections { get; }

        /// <summary>
        /// Statistics about adjacency types
        /// </summary>
        public Dictionary<string, int> TypeStatistics { get; }

        /// <summary>
        /// Whether the validation passed all checks
        /// </summary>
        public bool IsValid => ParsingStats.IsSuccessful && InvalidProvinceReferences.Count == 0;

        public AdjacencyValidationResult(CsvParsingStats parsingStats, List<Adjacency> adjacencies, HashSet<int> validProvinceIds)
        {
            ParsingStats = parsingStats;
            ValidAdjacencies = adjacencies;
            InvalidProvinceReferences = new List<string>();
            DuplicateConnections = new List<string>();
            TypeStatistics = new Dictionary<string, int>();

            PerformAdditionalValidation(validProvinceIds);
        }

        private void PerformAdditionalValidation(HashSet<int> validProvinceIds)
        {
            if (ValidAdjacencies == null || ValidAdjacencies.Count == 0)
                return;

            // Calculate type statistics
            foreach (var adjacency in ValidAdjacencies)
            {
                var type = adjacency.Type.ToLowerInvariant();
                TypeStatistics[type] = TypeStatistics.GetValueOrDefault(type, 0) + 1;
            }

            // Check for invalid province references if valid province set provided
            if (validProvinceIds != null)
            {
                foreach (var adjacency in ValidAdjacencies)
                {
                    if (!validProvinceIds.Contains(adjacency.From))
                    {
                        InvalidProvinceReferences.Add($"From province {adjacency.From} does not exist");
                    }

                    if (!validProvinceIds.Contains(adjacency.To))
                    {
                        InvalidProvinceReferences.Add($"To province {adjacency.To} does not exist");
                    }

                    if (adjacency.Through > 0 && !validProvinceIds.Contains(adjacency.Through))
                    {
                        InvalidProvinceReferences.Add($"Through province {adjacency.Through} does not exist");
                    }
                }
            }

            // Check for duplicate connections
            var connectionSet = new HashSet<string>();
            foreach (var adjacency in ValidAdjacencies)
            {
                var connection = $"{adjacency.From}->{adjacency.To}";
                if (connectionSet.Contains(connection))
                {
                    DuplicateConnections.Add(connection);
                }
                else
                {
                    connectionSet.Add(connection);
                }
            }
        }
    }
}