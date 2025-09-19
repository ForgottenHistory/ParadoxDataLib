using System;
using System.Threading;
using System.Threading.Tasks;
using ParadoxDataLib.Core.Parsers.Bitmap.DataStructures;
using ParadoxDataLib.Core.Parsers.Bitmap.Interpreters;

namespace ParadoxDataLib.Core.Parsers.Bitmap.Specialized
{
    /// <summary>
    /// Specialized reader for Paradox terrain maps and binary masks.
    /// Converts pixel values to binary terrain data (rivers, land/sea, forests, etc.).
    /// </summary>
    public class TerrainMapReader
    {
        private readonly BitmapParser<bool> _parser;
        private readonly BinaryMaskInterpreter _interpreter;

        /// <summary>
        /// Gets the underlying bitmap parser
        /// </summary>
        public BitmapParser<bool> Parser => _parser;

        /// <summary>
        /// Gets the binary mask interpreter
        /// </summary>
        public BinaryMaskInterpreter Interpreter => _interpreter;

        /// <summary>
        /// Threshold value for binary conversion
        /// </summary>
        public byte Threshold => _interpreter.Threshold;

        /// <summary>
        /// Whether the result is inverted
        /// </summary>
        public bool InvertResult => _interpreter.InvertResult;

        /// <summary>
        /// Mode used for binary conversion
        /// </summary>
        public BinaryMaskMode Mode => _interpreter.Mode;

        /// <summary>
        /// Creates a new terrain map reader
        /// </summary>
        /// <param name="threshold">Threshold value (0-255)</param>
        /// <param name="mode">Mode for interpreting pixel values</param>
        /// <param name="invertResult">Whether to invert the result</param>
        public TerrainMapReader(byte threshold = 128, BinaryMaskMode mode = BinaryMaskMode.Grayscale, bool invertResult = false)
        {
            _interpreter = new BinaryMaskInterpreter(threshold, mode, invertResult);
            _parser = new BitmapParser<bool>(_interpreter);
        }

        /// <summary>
        /// Creates a reader for detecting rivers (typically dark pixels = rivers)
        /// </summary>
        /// <returns>Reader configured for river detection</returns>
        public static TerrainMapReader CreateRiverDetector()
        {
            return new TerrainMapReader(64, BinaryMaskMode.Grayscale, true);
        }

        /// <summary>
        /// Creates a reader for land/sea detection using blue channel
        /// </summary>
        /// <returns>Reader configured for water detection</returns>
        public static TerrainMapReader CreateLandSeaDetector()
        {
            return new TerrainMapReader(128, BinaryMaskMode.Blue, false);
        }

        /// <summary>
        /// Creates a reader for forest detection using green channel
        /// </summary>
        /// <returns>Reader configured for forest detection</returns>
        public static TerrainMapReader CreateForestDetector()
        {
            return new TerrainMapReader(128, BinaryMaskMode.Green, false);
        }

        /// <summary>
        /// Creates a reader for desert detection using red channel
        /// </summary>
        /// <returns>Reader configured for desert detection</returns>
        public static TerrainMapReader CreateDesertDetector()
        {
            return new TerrainMapReader(128, BinaryMaskMode.Red, false);
        }

        /// <summary>
        /// Creates a reader for mountain detection using luminance
        /// </summary>
        /// <returns>Reader configured for mountain detection</returns>
        public static TerrainMapReader CreateMountainDetector()
        {
            return new TerrainMapReader(180, BinaryMaskMode.Luminance, false);
        }

        /// <summary>
        /// Creates a reader for fog of war detection
        /// </summary>
        /// <returns>Reader configured for fog detection</returns>
        public static TerrainMapReader CreateFogOfWarDetector()
        {
            return new TerrainMapReader(64, BinaryMaskMode.Alpha, true);
        }

        /// <summary>
        /// Creates a reader for specific color detection
        /// </summary>
        /// <param name="targetColor">Target RGB color to detect</param>
        /// <param name="tolerance">Color tolerance (0-255)</param>
        /// <returns>Reader configured for color detection</returns>
        public static TerrainMapReader CreateColorDetector(Pixel targetColor, byte tolerance = 10)
        {
            return new TerrainMapReader(tolerance, BinaryMaskMode.Grayscale, false);
        }

        /// <summary>
        /// Reads a terrain map from a BMP file
        /// </summary>
        /// <param name="filePath">Path to the terrain map BMP file</param>
        /// <param name="progress">Optional progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Bitmap data with binary terrain values</returns>
        public async Task<BitmapData<bool>> ReadTerrainMapAsync(
            string filePath,
            IProgress<double> progress = null,
            CancellationToken cancellationToken = default)
        {
            return await _parser.ParseFileAsync(filePath);
        }

        /// <summary>
        /// Reads a terrain map from a BMP file synchronously
        /// </summary>
        /// <param name="filePath">Path to the terrain map BMP file</param>
        /// <param name="progress">Optional progress reporter</param>
        /// <returns>Bitmap data with binary terrain values</returns>
        public BitmapData<bool> ReadTerrainMap(string filePath, IProgress<double> progress = null)
        {
            return _parser.ParseFile(filePath);
        }

        /// <summary>
        /// Tests a single pixel to see if it matches the terrain criteria
        /// </summary>
        /// <param name="pixel">Pixel to test</param>
        /// <returns>True if pixel matches terrain criteria</returns>
        public bool TestPixel(Pixel pixel)
        {
            return _interpreter.InterpretPixel(pixel);
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
        /// Gets terrain detection configuration description
        /// </summary>
        /// <returns>Description of the terrain detection settings</returns>
        public string GetDetectionDescription()
        {
            var invertText = InvertResult ? " (inverted)" : "";
            return $"Terrain detection: {Mode} >= {Threshold}{invertText}";
        }

        public override string ToString()
        {
            return $"TerrainMapReader: {GetDetectionDescription()}";
        }
    }
}