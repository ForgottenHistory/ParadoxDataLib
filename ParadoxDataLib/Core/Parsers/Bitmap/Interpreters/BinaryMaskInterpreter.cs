using System;
using System.Diagnostics;
using ParadoxDataLib.Core.Parsers.Bitmap.DataStructures;

namespace ParadoxDataLib.Core.Parsers.Bitmap.Interpreters
{
    /// <summary>
    /// Interprets pixel values as binary masks using configurable thresholds.
    /// Useful for rivers, trade routes, fog of war, and other binary data layers.
    /// </summary>
    public class BinaryMaskInterpreter : IPixelInterpreter<bool>
    {
        private readonly byte _threshold;
        private readonly bool _invertResult;
        private readonly BinaryMaskMode _mode;
        private readonly PixelInterpretationStats _stats;

        /// <summary>
        /// Gets a human-readable description of this interpreter
        /// </summary>
        public string Description => $"Binary mask using {_mode} threshold {_threshold} (inverted: {_invertResult})";

        /// <summary>
        /// Gets the expected input format for pixels
        /// </summary>
        public string ExpectedFormat => "8-bit Grayscale or RGB";

        /// <summary>
        /// Threshold value for binary conversion
        /// </summary>
        public byte Threshold => _threshold;

        /// <summary>
        /// Whether the result is inverted (true becomes false and vice versa)
        /// </summary>
        public bool InvertResult => _invertResult;

        /// <summary>
        /// Mode used for binary conversion
        /// </summary>
        public BinaryMaskMode Mode => _mode;

        /// <summary>
        /// Creates a new binary mask interpreter
        /// </summary>
        /// <param name="threshold">Threshold value (0-255)</param>
        /// <param name="mode">Mode for interpreting pixel values</param>
        /// <param name="invertResult">Whether to invert the result</param>
        public BinaryMaskInterpreter(byte threshold = 128, BinaryMaskMode mode = BinaryMaskMode.Grayscale, bool invertResult = false)
        {
            _threshold = threshold;
            _mode = mode;
            _invertResult = invertResult;
            _stats = new PixelInterpretationStats();
        }

        /// <summary>
        /// Creates an interpreter for river detection (typically black pixels = rivers)
        /// </summary>
        public static BinaryMaskInterpreter CreateRiverDetector()
        {
            return new BinaryMaskInterpreter(64, BinaryMaskMode.Grayscale, true); // Dark pixels = rivers
        }

        /// <summary>
        /// Creates an interpreter for land/sea detection
        /// </summary>
        public static BinaryMaskInterpreter CreateLandSeaDetector()
        {
            return new BinaryMaskInterpreter(128, BinaryMaskMode.Blue, false); // Blue channel for water
        }

        /// <summary>
        /// Creates an interpreter for specific color detection
        /// </summary>
        /// <param name="targetColor">Target RGB color</param>
        /// <param name="tolerance">Color tolerance (0-255)</param>
        public static BinaryMaskInterpreter CreateColorDetector(Pixel targetColor, byte tolerance = 10)
        {
            // This is a simplified version - a full implementation would need color distance calculation
            return new BinaryMaskInterpreter(tolerance, BinaryMaskMode.Grayscale, false);
        }

        /// <summary>
        /// Interprets a single pixel into a binary value
        /// </summary>
        /// <param name="pixel">The pixel to interpret</param>
        /// <returns>True or false based on the threshold and mode</returns>
        public bool InterpretPixel(Pixel pixel)
        {
            var startTime = Stopwatch.GetTimestamp();

            try
            {
                var value = _mode switch
                {
                    BinaryMaskMode.Grayscale => pixel.ToGrayscale(),
                    BinaryMaskMode.Red => pixel.R,
                    BinaryMaskMode.Green => pixel.G,
                    BinaryMaskMode.Blue => pixel.B,
                    BinaryMaskMode.Alpha => pixel.A,
                    BinaryMaskMode.Luminance => (byte)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B),
                    _ => throw new ArgumentOutOfRangeException($"Unsupported binary mask mode: {_mode}")
                };

                var result = value >= _threshold;
                if (_invertResult)
                    result = !result;

                _stats.TotalPixelsInterpreted++;
                return result;
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
        /// Interprets multiple pixels into binary values for batch processing
        /// </summary>
        /// <param name="pixels">Array of pixels to interpret</param>
        /// <returns>Array of binary values in the same order</returns>
        public bool[] InterpretPixels(Pixel[] pixels)
        {
            if (pixels == null) throw new ArgumentNullException(nameof(pixels));

            var results = new bool[pixels.Length];
            InterpretPixels(pixels.AsSpan(), results.AsSpan());
            return results;
        }

        /// <summary>
        /// Interprets multiple pixels using spans for high-performance processing
        /// </summary>
        /// <param name="pixels">Span of pixels to interpret</param>
        /// <param name="results">Span to store the interpreted binary values</param>
        public void InterpretPixels(ReadOnlySpan<Pixel> pixels, Span<bool> results)
        {
            if (pixels.Length != results.Length)
                throw new ArgumentException("Pixels and results spans must have the same length");

            var startTime = Stopwatch.GetTimestamp();

            try
            {
                for (var i = 0; i < pixels.Length; i++)
                {
                    var pixel = pixels[i];
                    var value = _mode switch
                    {
                        BinaryMaskMode.Grayscale => pixel.ToGrayscale(),
                        BinaryMaskMode.Red => pixel.R,
                        BinaryMaskMode.Green => pixel.G,
                        BinaryMaskMode.Blue => pixel.B,
                        BinaryMaskMode.Alpha => pixel.A,
                        BinaryMaskMode.Luminance => (byte)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B),
                        _ => throw new ArgumentOutOfRangeException($"Unsupported binary mask mode: {_mode}")
                    };

                    var result = value >= _threshold;
                    if (_invertResult)
                        result = !result;

                    results[i] = result;
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
        /// <param name="result">The binary value if successful</param>
        /// <param name="errorMessage">Error message if interpretation failed</param>
        /// <returns>True if interpretation succeeded</returns>
        public bool TryInterpretPixel(Pixel pixel, out bool result, out string errorMessage)
        {
            try
            {
                result = InterpretPixel(pixel);
                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                result = false;
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
            return true; // Can interpret any pixel as binary
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
                UniqueValues = 2, // Only two possible values: true and false
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

        public override string ToString()
        {
            return $"BinaryMaskInterpreter: {_mode} >= {_threshold}" + (_invertResult ? " (inverted)" : "");
        }
    }

    /// <summary>
    /// Mode for binary mask interpretation
    /// </summary>
    public enum BinaryMaskMode
    {
        /// <summary>
        /// Use grayscale intensity
        /// </summary>
        Grayscale,

        /// <summary>
        /// Use red channel value
        /// </summary>
        Red,

        /// <summary>
        /// Use green channel value
        /// </summary>
        Green,

        /// <summary>
        /// Use blue channel value
        /// </summary>
        Blue,

        /// <summary>
        /// Use alpha channel value
        /// </summary>
        Alpha,

        /// <summary>
        /// Use luminance calculation (weighted RGB)
        /// </summary>
        Luminance
    }
}