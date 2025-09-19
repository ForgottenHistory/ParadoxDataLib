using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ParadoxDataLib.Core.Parsers.Bitmap.DataStructures;
using ParadoxDataLib.Core.Parsers.Bitmap.Interpreters;

namespace ParadoxDataLib.Core.Parsers.Bitmap.Specialized
{
    /// <summary>
    /// Specialized reader for Paradox province maps (provinces.bmp files).
    /// Converts RGB pixel values to province IDs using definition.csv mapping data.
    /// </summary>
    public class ProvinceMapReader
    {
        private readonly BitmapParser<int> _parser;
        private readonly RgbToProvinceInterpreter _interpreter;

        /// <summary>
        /// Gets the underlying bitmap parser
        /// </summary>
        public BitmapParser<int> Parser => _parser;

        /// <summary>
        /// Gets the RGB to province interpreter
        /// </summary>
        public RgbToProvinceInterpreter Interpreter => _interpreter;

        /// <summary>
        /// Number of RGB mappings loaded
        /// </summary>
        public int MappingCount => _interpreter.MappingCount;

        /// <summary>
        /// Default province ID for unmapped colors
        /// </summary>
        public int DefaultProvinceId => _interpreter.DefaultProvinceId;

        /// <summary>
        /// Creates a new province map reader
        /// </summary>
        /// <param name="rgbToProvinceMap">Dictionary mapping RGB values to province IDs</param>
        /// <param name="defaultProvinceId">Default province ID for unmapped colors</param>
        public ProvinceMapReader(Dictionary<int, int> rgbToProvinceMap, int defaultProvinceId = 0)
        {
            _interpreter = new RgbToProvinceInterpreter(rgbToProvinceMap, defaultProvinceId);
            _parser = new BitmapParser<int>(_interpreter);
        }

        /// <summary>
        /// Creates a new province map reader from province definitions
        /// </summary>
        /// <param name="provinceDefinitions">Province definitions from CSV parser</param>
        /// <param name="defaultProvinceId">Default province ID for unmapped colors</param>
        public static ProvinceMapReader FromProvinceDefinitions(
            IEnumerable<Csv.DataStructures.ProvinceDefinition> provinceDefinitions,
            int defaultProvinceId = 0)
        {
            var interpreter = RgbToProvinceInterpreter.FromProvinceDefinitions(provinceDefinitions, defaultProvinceId);
            var rgbMap = new Dictionary<int, int>();
            foreach (var definition in provinceDefinitions)
            {
                rgbMap[definition.RgbValue] = definition.ProvinceId;
            }
            return new ProvinceMapReader(rgbMap, defaultProvinceId);
        }

        /// <summary>
        /// Reads a province map from a BMP file
        /// </summary>
        /// <param name="filePath">Path to the provinces.bmp file</param>
        /// <param name="progress">Optional progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Bitmap data with province IDs</returns>
        public async Task<BitmapData<int>> ReadProvinceMapAsync(
            string filePath,
            IProgress<double> progress = null,
            CancellationToken cancellationToken = default)
        {
            return await _parser.ParseFileAsync(filePath);
        }

        /// <summary>
        /// Reads a province map from a BMP file synchronously
        /// </summary>
        /// <param name="filePath">Path to the provinces.bmp file</param>
        /// <param name="progress">Optional progress reporter</param>
        /// <returns>Bitmap data with province IDs</returns>
        public BitmapData<int> ReadProvinceMap(string filePath, IProgress<double> progress = null)
        {
            return _parser.ParseFile(filePath);
        }

        /// <summary>
        /// Checks if the specified RGB value has a province mapping
        /// </summary>
        /// <param name="rgbValue">RGB value to check</param>
        /// <returns>True if mapping exists</returns>
        public bool HasProvinceMapping(int rgbValue)
        {
            return _interpreter.HasProvinceMapping(rgbValue);
        }

        /// <summary>
        /// Checks if the specified pixel has a province mapping
        /// </summary>
        /// <param name="pixel">Pixel to check</param>
        /// <returns>True if mapping exists</returns>
        public bool HasProvinceMapping(Pixel pixel)
        {
            return _interpreter.HasProvinceMapping(pixel);
        }

        /// <summary>
        /// Gets the province ID for the specified RGB value
        /// </summary>
        /// <param name="rgbValue">RGB value</param>
        /// <returns>Province ID or default if not found</returns>
        public int GetProvinceId(int rgbValue)
        {
            return _interpreter.GetProvinceId(rgbValue);
        }

        /// <summary>
        /// Gets the RGB value for the specified province ID (reverse lookup)
        /// </summary>
        /// <param name="provinceId">Province ID</param>
        /// <returns>RGB value or 0 if not found</returns>
        public int GetRgbValue(int provinceId)
        {
            return _interpreter.GetRgbValue(provinceId);
        }

        /// <summary>
        /// Gets all mapped RGB values
        /// </summary>
        /// <returns>Collection of RGB values with province mappings</returns>
        public IEnumerable<int> GetMappedRgbValues()
        {
            return _interpreter.GetMappedRgbValues();
        }

        /// <summary>
        /// Gets all mapped province IDs
        /// </summary>
        /// <returns>Collection of province IDs in the mapping</returns>
        public IEnumerable<int> GetMappedProvinceIds()
        {
            return _interpreter.GetMappedProvinceIds();
        }

        /// <summary>
        /// Gets parsing statistics
        /// </summary>
        /// <returns>Statistics about the last parsing operation</returns>
        public BitmapParsingStats GetStatistics()
        {
            return new BitmapParsingStats(); // TODO: Implement statistics in BitmapParser
        }

        /// <summary>
        /// Gets interpreter statistics
        /// </summary>
        /// <returns>Statistics about pixel interpretation operations</returns>
        public PixelInterpretationStats GetInterpreterStatistics()
        {
            return _interpreter.GetStatistics();
        }

        /// <summary>
        /// Gets mapping summary information
        /// </summary>
        /// <returns>Summary of RGB to province mapping</returns>
        public string GetMappingSummary()
        {
            return _interpreter.GetMappingSummary();
        }

        /// <summary>
        /// Resets all statistics
        /// </summary>
        public void ResetStatistics()
        {
            // TODO: Implement ResetStatistics in BitmapParser
            _interpreter.ResetStatistics();
        }

        public override string ToString()
        {
            return $"ProvinceMapReader: {GetMappingSummary()}";
        }
    }
}