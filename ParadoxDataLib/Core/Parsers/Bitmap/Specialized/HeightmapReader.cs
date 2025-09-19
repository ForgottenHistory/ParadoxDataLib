using System;
using System.Threading;
using System.Threading.Tasks;
using ParadoxDataLib.Core.Parsers.Bitmap.DataStructures;
using ParadoxDataLib.Core.Parsers.Bitmap.Interpreters;

namespace ParadoxDataLib.Core.Parsers.Bitmap.Specialized
{
    /// <summary>
    /// Specialized reader for Paradox heightmaps (heightmap.bmp files).
    /// Converts 8-bit grayscale pixel values to elevation heights in meters.
    /// </summary>
    public class HeightmapReader
    {
        private readonly BitmapParser<float> _parser;
        private readonly GrayscaleToHeightInterpreter _interpreter;

        /// <summary>
        /// Gets the underlying bitmap parser
        /// </summary>
        public BitmapParser<float> Parser => _parser;

        /// <summary>
        /// Gets the grayscale to height interpreter
        /// </summary>
        public GrayscaleToHeightInterpreter Interpreter => _interpreter;

        /// <summary>
        /// Minimum height value in meters
        /// </summary>
        public float MinHeight => _interpreter.MinHeight;

        /// <summary>
        /// Maximum height value in meters
        /// </summary>
        public float MaxHeight => _interpreter.MaxHeight;

        /// <summary>
        /// Creates a new heightmap reader
        /// </summary>
        /// <param name="minHeight">Minimum height value (corresponds to grayscale 0)</param>
        /// <param name="maxHeight">Maximum height value (corresponds to grayscale 255)</param>
        public HeightmapReader(float minHeight = 0.0f, float maxHeight = 8848.0f)
        {
            _interpreter = new GrayscaleToHeightInterpreter(minHeight, maxHeight);
            _parser = new BitmapParser<float>(_interpreter);
        }

        /// <summary>
        /// Creates a heightmap reader for standard Earth elevations (sea level to Mount Everest)
        /// </summary>
        /// <returns>Reader configured for 0m to 8848m range</returns>
        public static HeightmapReader CreateEarthHeights()
        {
            return new HeightmapReader(0.0f, 8848.0f);
        }

        /// <summary>
        /// Creates a heightmap reader for custom height range
        /// </summary>
        /// <param name="minHeight">Minimum height</param>
        /// <param name="maxHeight">Maximum height</param>
        /// <returns>Reader configured for the specified range</returns>
        public static HeightmapReader CreateCustomRange(float minHeight, float maxHeight)
        {
            return new HeightmapReader(minHeight, maxHeight);
        }

        /// <summary>
        /// Creates a heightmap reader for below-sea-level to mountain ranges
        /// </summary>
        /// <param name="seaFloorDepth">Depth below sea level (positive value)</param>
        /// <param name="mountainHeight">Maximum mountain height</param>
        /// <returns>Reader configured for the specified range</returns>
        public static HeightmapReader CreateSeaFloorToMountains(float seaFloorDepth = 500.0f, float mountainHeight = 5000.0f)
        {
            return new HeightmapReader(-seaFloorDepth, mountainHeight);
        }

        /// <summary>
        /// Reads a heightmap from a BMP file
        /// </summary>
        /// <param name="filePath">Path to the heightmap.bmp file</param>
        /// <param name="progress">Optional progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Bitmap data with elevation heights</returns>
        public async Task<BitmapData<float>> ReadHeightmapAsync(
            string filePath,
            IProgress<double> progress = null,
            CancellationToken cancellationToken = default)
        {
            return await _parser.ParseFileAsync(filePath);
        }

        /// <summary>
        /// Reads a heightmap from a BMP file synchronously
        /// </summary>
        /// <param name="filePath">Path to the heightmap.bmp file</param>
        /// <param name="progress">Optional progress reporter</param>
        /// <returns>Bitmap data with elevation heights</returns>
        public BitmapData<float> ReadHeightmap(string filePath, IProgress<double> progress = null)
        {
            return _parser.ParseFile(filePath);
        }

        /// <summary>
        /// Converts a height value to its corresponding grayscale value
        /// </summary>
        /// <param name="height">Height value in meters</param>
        /// <returns>Grayscale value (0-255)</returns>
        public byte HeightToGrayscale(float height)
        {
            return _interpreter.HeightToGrayscale(height);
        }

        /// <summary>
        /// Converts a grayscale value to its corresponding height
        /// </summary>
        /// <param name="grayscale">Grayscale value (0-255)</param>
        /// <returns>Height value in meters</returns>
        public float GrayscaleToHeight(byte grayscale)
        {
            return _interpreter.GrayscaleToHeight(grayscale);
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
        /// Resets all statistics
        /// </summary>
        public void ResetStatistics()
        {
            // TODO: Implement ResetStatistics in BitmapParser
            _interpreter.ResetStatistics();
        }

        /// <summary>
        /// Gets height range information
        /// </summary>
        /// <returns>Description of the height range</returns>
        public string GetHeightRangeDescription()
        {
            return $"Height range: {MinHeight:F1}m to {MaxHeight:F1}m";
        }

        public override string ToString()
        {
            return $"HeightmapReader: {GetHeightRangeDescription()}";
        }
    }
}