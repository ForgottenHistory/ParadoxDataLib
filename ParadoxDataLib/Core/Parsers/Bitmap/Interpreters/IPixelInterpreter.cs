using System;
using ParadoxDataLib.Core.Parsers.Bitmap.DataStructures;

namespace ParadoxDataLib.Core.Parsers.Bitmap.Interpreters
{
    /// <summary>
    /// Defines the contract for interpreting pixel data into strongly-typed values.
    /// Implementations handle conversion from pixels to specific data types (e.g., province IDs, heights, terrain types).
    /// </summary>
    /// <typeparam name="T">The type of data to interpret from pixels</typeparam>
    public interface IPixelInterpreter<T>
    {
        /// <summary>
        /// Interprets a single pixel into a data value
        /// </summary>
        /// <param name="pixel">The pixel to interpret</param>
        /// <returns>The interpreted data value</returns>
        T InterpretPixel(Pixel pixel);

        /// <summary>
        /// Interprets multiple pixels into data values for batch processing
        /// </summary>
        /// <param name="pixels">Array of pixels to interpret</param>
        /// <returns>Array of interpreted data values in the same order</returns>
        T[] InterpretPixels(Pixel[] pixels);

        /// <summary>
        /// Interprets multiple pixels using a span for high-performance processing
        /// </summary>
        /// <param name="pixels">Span of pixels to interpret</param>
        /// <param name="results">Span to store the interpreted results</param>
        void InterpretPixels(ReadOnlySpan<Pixel> pixels, Span<T> results);

        /// <summary>
        /// Attempts to interpret a pixel without throwing exceptions
        /// </summary>
        /// <param name="pixel">The pixel to interpret</param>
        /// <param name="result">The interpreted data value if successful</param>
        /// <param name="errorMessage">Error message if interpretation failed</param>
        /// <returns>True if interpretation succeeded</returns>
        bool TryInterpretPixel(Pixel pixel, out T result, out string errorMessage);

        /// <summary>
        /// Validates that a pixel can be successfully interpreted
        /// </summary>
        /// <param name="pixel">The pixel to validate</param>
        /// <returns>True if the pixel can be interpreted</returns>
        bool CanInterpretPixel(Pixel pixel);

        /// <summary>
        /// Gets a human-readable description of this interpreter
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the expected input format for pixels (e.g., "RGB", "Grayscale", "Indexed")
        /// </summary>
        string ExpectedFormat { get; }

        /// <summary>
        /// Gets statistics about interpretation operations (optional)
        /// </summary>
        PixelInterpretationStats GetStatistics();

        /// <summary>
        /// Resets interpretation statistics
        /// </summary>
        void ResetStatistics();
    }

    /// <summary>
    /// Statistics about pixel interpretation operations
    /// </summary>
    public class PixelInterpretationStats
    {
        /// <summary>
        /// Total number of pixels interpreted
        /// </summary>
        public long TotalPixelsInterpreted { get; set; }

        /// <summary>
        /// Number of pixels that failed interpretation
        /// </summary>
        public long FailedInterpretations { get; set; }

        /// <summary>
        /// Number of unique values encountered
        /// </summary>
        public long UniqueValues { get; set; }

        /// <summary>
        /// Success rate (0.0 to 1.0)
        /// </summary>
        public double SuccessRate => TotalPixelsInterpreted > 0
            ? (double)(TotalPixelsInterpreted - FailedInterpretations) / TotalPixelsInterpreted
            : 0.0;

        /// <summary>
        /// Time spent on interpretation operations
        /// </summary>
        public TimeSpan TotalTime { get; set; }

        /// <summary>
        /// Average pixels interpreted per second
        /// </summary>
        public double PixelsPerSecond => TotalTime.TotalSeconds > 0
            ? TotalPixelsInterpreted / TotalTime.TotalSeconds
            : 0.0;

        /// <summary>
        /// Creates a new statistics instance
        /// </summary>
        public PixelInterpretationStats()
        {
            Reset();
        }

        /// <summary>
        /// Resets all statistics to zero
        /// </summary>
        public void Reset()
        {
            TotalPixelsInterpreted = 0;
            FailedInterpretations = 0;
            UniqueValues = 0;
            TotalTime = TimeSpan.Zero;
        }

        /// <summary>
        /// Adds statistics from another instance
        /// </summary>
        /// <param name="other">Other statistics to add</param>
        public void Add(PixelInterpretationStats other)
        {
            TotalPixelsInterpreted += other.TotalPixelsInterpreted;
            FailedInterpretations += other.FailedInterpretations;
            UniqueValues += other.UniqueValues;
            TotalTime = TotalTime.Add(other.TotalTime);
        }

        public override string ToString()
        {
            return $"Interpreted {TotalPixelsInterpreted:N0} pixels, " +
                   $"{SuccessRate:P1} success rate, " +
                   $"{PixelsPerSecond:N0} pixels/sec, " +
                   $"{UniqueValues:N0} unique values";
        }
    }

    /// <summary>
    /// Exception thrown when pixel interpretation fails
    /// </summary>
    public class PixelInterpretationException : Exception
    {
        /// <summary>
        /// The pixel that caused the interpretation failure
        /// </summary>
        public Pixel Pixel { get; }

        /// <summary>
        /// The interpreter type that failed
        /// </summary>
        public Type InterpreterType { get; }

        public PixelInterpretationException(string message, Pixel pixel, Type interpreterType = null)
            : base(message)
        {
            Pixel = pixel;
            InterpreterType = interpreterType;
        }

        public PixelInterpretationException(string message, Exception innerException, Pixel pixel, Type interpreterType = null)
            : base(message, innerException)
        {
            Pixel = pixel;
            InterpreterType = interpreterType;
        }

        public override string ToString()
        {
            var interpreterName = InterpreterType?.Name ?? "Unknown";
            return $"{Message} (Pixel: {Pixel}, Interpreter: {interpreterName})";
        }
    }
}