using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ParadoxDataLib.Core.Parsers.Bitmap.DataStructures;

namespace ParadoxDataLib.Core.Parsers.Bitmap.Interpreters
{
    /// <summary>
    /// Interprets RGB pixel values to province IDs using definition.csv mapping data.
    /// Optimized for high-performance processing of large province maps (5632x2048 pixels).
    /// </summary>
    public class RgbToProvinceInterpreter : IPixelInterpreter<int>
    {
        private readonly Dictionary<int, int> _rgbToProvinceMap;
        private readonly PixelInterpretationStats _stats;
        private readonly HashSet<int> _uniqueValues;
        private readonly int _defaultProvinceId;

        /// <summary>
        /// Gets a human-readable description of this interpreter
        /// </summary>
        public string Description => "Converts RGB pixel values to Paradox province IDs using definition.csv mapping";

        /// <summary>
        /// Gets the expected input format for pixels
        /// </summary>
        public string ExpectedFormat => "24-bit RGB";

        /// <summary>
        /// Default province ID to return for unmapped RGB values (typically 0 for "no province")
        /// </summary>
        public int DefaultProvinceId => _defaultProvinceId;

        /// <summary>
        /// Number of RGB mappings loaded
        /// </summary>
        public int MappingCount => _rgbToProvinceMap.Count;

        /// <summary>
        /// Creates a new RGB to province interpreter
        /// </summary>
        /// <param name="rgbToProvinceMap">Dictionary mapping RGB values (0xRRGGBB) to province IDs</param>
        /// <param name="defaultProvinceId">Default province ID for unmapped colors (default: 0)</param>
        public RgbToProvinceInterpreter(Dictionary<int, int> rgbToProvinceMap, int defaultProvinceId = 0)
        {
            _rgbToProvinceMap = rgbToProvinceMap ?? throw new ArgumentNullException(nameof(rgbToProvinceMap));
            _defaultProvinceId = defaultProvinceId;
            _stats = new PixelInterpretationStats();
            _uniqueValues = new HashSet<int>();

            if (_rgbToProvinceMap.Count == 0)
                throw new ArgumentException("RGB to province mapping cannot be empty", nameof(rgbToProvinceMap));
        }

        /// <summary>
        /// Creates a new RGB to province interpreter from province definition data
        /// </summary>
        /// <param name="provinceDefinitions">Province definitions from CSV parser</param>
        /// <param name="defaultProvinceId">Default province ID for unmapped colors</param>
        public static RgbToProvinceInterpreter FromProvinceDefinitions(
            IEnumerable<Csv.DataStructures.ProvinceDefinition> provinceDefinitions,
            int defaultProvinceId = 0)
        {
            var rgbMap = new Dictionary<int, int>();

            foreach (var definition in provinceDefinitions)
            {
                var rgbValue = definition.RgbValue;
                if (rgbMap.ContainsKey(rgbValue))
                {
                    throw new InvalidOperationException(
                        $"Duplicate RGB value {definition.RgbString} found for provinces {rgbMap[rgbValue]} and {definition.ProvinceId}");
                }
                rgbMap[rgbValue] = definition.ProvinceId;
            }

            return new RgbToProvinceInterpreter(rgbMap, defaultProvinceId);
        }

        /// <summary>
        /// Interprets a single pixel into a province ID
        /// </summary>
        /// <param name="pixel">The pixel to interpret</param>
        /// <returns>The province ID for the pixel's RGB value</returns>
        public int InterpretPixel(Pixel pixel)
        {
            var startTime = Stopwatch.GetTimestamp();

            try
            {
                var rgbValue = pixel.ToRgb();
                var provinceId = _rgbToProvinceMap.GetValueOrDefault(rgbValue, _defaultProvinceId);

                _uniqueValues.Add(provinceId);
                _stats.TotalPixelsInterpreted++;

                return provinceId;
            }
            catch (Exception)
            {
                _stats.FailedInterpretations++;
                throw;
            }
            finally
            {
                var elapsed = Stopwatch.GetTimestamp() - startTime;
                _stats.TotalTime = _stats.TotalTime.Add(TimeSpan.FromTicks(elapsed));
            }
        }

        /// <summary>
        /// Interprets multiple pixels into province IDs for batch processing
        /// </summary>
        /// <param name="pixels">Array of pixels to interpret</param>
        /// <returns>Array of province IDs in the same order</returns>
        public int[] InterpretPixels(Pixel[] pixels)
        {
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));

            var results = new int[pixels.Length];
            InterpretPixels(pixels.AsSpan(), results.AsSpan());
            return results;
        }

        /// <summary>
        /// Interprets multiple pixels using spans for high-performance processing
        /// </summary>
        /// <param name="pixels">Span of pixels to interpret</param>
        /// <param name="results">Span to store the interpreted province IDs</param>
        public void InterpretPixels(ReadOnlySpan<Pixel> pixels, Span<int> results)
        {
            if (pixels.Length != results.Length)
                throw new ArgumentException("Pixels and results spans must have the same length");

            var startTime = Stopwatch.GetTimestamp();

            try
            {
                for (var i = 0; i < pixels.Length; i++)
                {
                    var rgbValue = pixels[i].ToRgb();
                    var provinceId = _rgbToProvinceMap.GetValueOrDefault(rgbValue, _defaultProvinceId);
                    results[i] = provinceId;

                    _uniqueValues.Add(provinceId);
                }

                _stats.TotalPixelsInterpreted += pixels.Length;
                _stats.UniqueValues = _uniqueValues.Count;
            }
            catch (Exception)
            {
                _stats.FailedInterpretations += pixels.Length;
                throw;
            }
            finally
            {
                var elapsed = Stopwatch.GetTimestamp() - startTime;
                _stats.TotalTime = _stats.TotalTime.Add(TimeSpan.FromTicks(elapsed));
            }
        }

        /// <summary>
        /// Attempts to interpret a pixel without throwing exceptions
        /// </summary>
        /// <param name="pixel">The pixel to interpret</param>
        /// <param name="result">The province ID if successful</param>
        /// <param name="errorMessage">Error message if interpretation failed</param>
        /// <returns>True if interpretation succeeded</returns>
        public bool TryInterpretPixel(Pixel pixel, out int result, out string errorMessage)
        {
            try
            {
                result = InterpretPixel(pixel);
                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                result = _defaultProvinceId;
                errorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Validates that a pixel can be successfully interpreted
        /// </summary>
        /// <param name="pixel">The pixel to validate</param>
        /// <returns>True if the pixel can be interpreted (always true for this interpreter)</returns>
        public bool CanInterpretPixel(Pixel pixel)
        {
            // This interpreter can handle any pixel, it just returns default for unknown RGB values
            return true;
        }

        /// <summary>
        /// Gets statistics about interpretation operations
        /// </summary>
        public PixelInterpretationStats GetStatistics()
        {
            var stats = new PixelInterpretationStats
            {
                TotalPixelsInterpreted = _stats.TotalPixelsInterpreted,
                FailedInterpretations = _stats.FailedInterpretations,
                UniqueValues = _uniqueValues.Count,
                TotalTime = _stats.TotalTime
            };
            return stats;
        }

        /// <summary>
        /// Resets interpretation statistics
        /// </summary>
        public void ResetStatistics()
        {
            _stats.Reset();
            _uniqueValues.Clear();
        }

        /// <summary>
        /// Checks if the specified RGB value has a province mapping
        /// </summary>
        /// <param name="rgbValue">RGB value to check (0xRRGGBB format)</param>
        /// <returns>True if a province mapping exists</returns>
        public bool HasProvinceMapping(int rgbValue)
        {
            return _rgbToProvinceMap.ContainsKey(rgbValue);
        }

        /// <summary>
        /// Checks if the specified pixel has a province mapping
        /// </summary>
        /// <param name="pixel">Pixel to check</param>
        /// <returns>True if a province mapping exists</returns>
        public bool HasProvinceMapping(Pixel pixel)
        {
            return HasProvinceMapping(pixel.ToRgb());
        }

        /// <summary>
        /// Gets the province ID for the specified RGB value
        /// </summary>
        /// <param name="rgbValue">RGB value (0xRRGGBB format)</param>
        /// <returns>Province ID, or default if not found</returns>
        public int GetProvinceId(int rgbValue)
        {
            return _rgbToProvinceMap.GetValueOrDefault(rgbValue, _defaultProvinceId);
        }

        /// <summary>
        /// Gets all mapped RGB values
        /// </summary>
        /// <returns>Collection of RGB values that have province mappings</returns>
        public IEnumerable<int> GetMappedRgbValues()
        {
            return _rgbToProvinceMap.Keys;
        }

        /// <summary>
        /// Gets all mapped province IDs
        /// </summary>
        /// <returns>Collection of province IDs in the mapping</returns>
        public IEnumerable<int> GetMappedProvinceIds()
        {
            return _rgbToProvinceMap.Values;
        }

        /// <summary>
        /// Gets the RGB value for the specified province ID (reverse lookup)
        /// </summary>
        /// <param name="provinceId">Province ID to find</param>
        /// <returns>RGB value for the province, or -1 if not found</returns>
        public int GetRgbValue(int provinceId)
        {
            var kvp = _rgbToProvinceMap.FirstOrDefault(x => x.Value == provinceId);
            return kvp.Key; // Returns 0 (black) if not found, which is reasonable default
        }

        /// <summary>
        /// Gets mapping statistics
        /// </summary>
        /// <returns>Summary of the RGB to province mapping</returns>
        public string GetMappingSummary()
        {
            var totalMappings = _rgbToProvinceMap.Count;
            var minProvinceId = _rgbToProvinceMap.Count > 0 ? _rgbToProvinceMap.Values.Min() : 0;
            var maxProvinceId = _rgbToProvinceMap.Count > 0 ? _rgbToProvinceMap.Values.Max() : 0;

            return $"RGB→Province Mapping: {totalMappings:N0} mappings, " +
                   $"Province IDs {minProvinceId}-{maxProvinceId}, " +
                   $"Default: {_defaultProvinceId}";
        }

        public override string ToString()
        {
            return $"RgbToProvinceInterpreter: {GetMappingSummary()}";
        }
    }
}