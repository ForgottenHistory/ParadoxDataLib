using System;
using System.Diagnostics;
using ParadoxDataLib.Core.Parsers.Bitmap.DataStructures;

namespace ParadoxDataLib.Core.Parsers.Bitmap.Interpreters
{
    /// <summary>
    /// Interprets grayscale pixel values to elevation heights for heightmap processing.
    /// Converts 8-bit grayscale values (0-255) to configurable height ranges.
    /// </summary>
    public class GrayscaleToHeightInterpreter : IPixelInterpreter<float>
    {
        private readonly float _minHeight;
        private readonly float _maxHeight;
        private readonly float _heightRange;
        private readonly PixelInterpretationStats _stats;

        /// <summary>
        /// Gets a human-readable description of this interpreter
        /// </summary>
        public string Description => $"Converts 8-bit grayscale to elevation heights ({_minHeight:F0}m to {_maxHeight:F0}m)";

        /// <summary>
        /// Gets the expected input format for pixels
        /// </summary>
        public string ExpectedFormat => "8-bit Grayscale";

        /// <summary>
        /// Minimum height value in meters
        /// </summary>
        public float MinHeight => _minHeight;

        /// <summary>
        /// Maximum height value in meters
        /// </summary>
        public float MaxHeight => _maxHeight;

        /// <summary>
        /// Creates a new grayscale to height interpreter
        /// </summary>
        /// <param name="minHeight">Minimum height value (corresponds to grayscale 0)</param>
        /// <param name="maxHeight">Maximum height value (corresponds to grayscale 255)</param>
        public GrayscaleToHeightInterpreter(float minHeight = 0.0f, float maxHeight = 8848.0f)
        {
            if (maxHeight <= minHeight)
                throw new ArgumentException("Maximum height must be greater than minimum height");

            _minHeight = minHeight;
            _maxHeight = maxHeight;
            _heightRange = _maxHeight - _minHeight;
            _stats = new PixelInterpretationStats();
        }

        /// <summary>
        /// Creates an interpreter for sea level to Mount Everest (standard Earth heights)
        /// </summary>
        public static GrayscaleToHeightInterpreter CreateEarthHeights()
        {
            return new GrayscaleToHeightInterpreter(0.0f, 8848.0f); // Sea level to Mount Everest
        }

        /// <summary>
        /// Creates an interpreter for a custom height range
        /// </summary>
        /// <param name="minHeight">Minimum height</param>
        /// <param name="maxHeight">Maximum height</param>
        public static GrayscaleToHeightInterpreter CreateCustomRange(float minHeight, float maxHeight)
        {
            return new GrayscaleToHeightInterpreter(minHeight, maxHeight);
        }

        /// <summary>
        /// Interprets a single pixel into a height value
        /// </summary>
        /// <param name="pixel">The pixel to interpret</param>
        /// <returns>The height value corresponding to the pixel's grayscale intensity</returns>
        public float InterpretPixel(Pixel pixel)
        {
            var startTime = Stopwatch.GetTimestamp();

            try
            {
                var grayscale = pixel.ToGrayscale();
                var normalizedValue = grayscale / 255.0f; // Convert to 0.0-1.0 range
                var height = _minHeight + (normalizedValue * _heightRange);

                _stats.TotalPixelsInterpreted++;
                return height;
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
        /// Interprets multiple pixels into height values for batch processing
        /// </summary>
        /// <param name="pixels">Array of pixels to interpret</param>
        /// <returns>Array of height values in the same order</returns>
        public float[] InterpretPixels(Pixel[] pixels)
        {
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));

            var results = new float[pixels.Length];
            InterpretPixels(pixels.AsSpan(), results.AsSpan());
            return results;
        }

        /// <summary>
        /// Interprets multiple pixels using spans for high-performance processing
        /// </summary>
        /// <param name="pixels">Span of pixels to interpret</param>
        /// <param name="results">Span to store the interpreted height values</param>
        public void InterpretPixels(ReadOnlySpan<Pixel> pixels, Span<float> results)
        {
            if (pixels.Length != results.Length)
                throw new ArgumentException("Pixels and results spans must have the same length");

            var startTime = Stopwatch.GetTimestamp();

            try
            {
                for (var i = 0; i < pixels.Length; i++)
                {
                    var grayscale = pixels[i].ToGrayscale();
                    var normalizedValue = grayscale / 255.0f;
                    results[i] = _minHeight + (normalizedValue * _heightRange);
                }

                _stats.TotalPixelsInterpreted += pixels.Length;
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
        /// <param name="result">The height value if successful</param>
        /// <param name="errorMessage">Error message if interpretation failed</param>
        /// <returns>True if interpretation succeeded</returns>
        public bool TryInterpretPixel(Pixel pixel, out float result, out string errorMessage)
        {
            try
            {
                result = InterpretPixel(pixel);
                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                result = _minHeight;
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
            return true; // Can interpret any pixel as grayscale
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
                UniqueValues = 256, // Maximum possible unique grayscale values
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
        }

        /// <summary>
        /// Converts a height value back to a grayscale value
        /// </summary>
        /// <param name="height">Height value to convert</param>
        /// <returns>Grayscale value (0-255)</returns>
        public byte HeightToGrayscale(float height)
        {
            var clampedHeight = Math.Clamp(height, _minHeight, _maxHeight);
            var normalizedHeight = (clampedHeight - _minHeight) / _heightRange;
            return (byte)Math.Round(normalizedHeight * 255.0f);
        }

        /// <summary>
        /// Gets the height value for a specific grayscale intensity
        /// </summary>
        /// <param name="grayscale">Grayscale value (0-255)</param>
        /// <returns>Corresponding height value</returns>
        public float GrayscaleToHeight(byte grayscale)
        {
            var normalizedValue = grayscale / 255.0f;
            return _minHeight + (normalizedValue * _heightRange);
        }

        public override string ToString()
        {
            return $"GrayscaleToHeightInterpreter: {_minHeight:F0}m to {_maxHeight:F0}m";
        }
    }
}