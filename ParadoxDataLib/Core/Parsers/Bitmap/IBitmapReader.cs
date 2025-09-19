using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ParadoxDataLib.Core.Parsers.Bitmap.DataStructures;

namespace ParadoxDataLib.Core.Parsers.Bitmap
{
    /// <summary>
    /// Defines the contract for reading bitmap files with support for different formats,
    /// memory-mapped access, and streaming operations.
    /// </summary>
    public interface IBitmapReader : IDisposable
    {
        /// <summary>
        /// Header information from the bitmap file
        /// </summary>
        BitmapHeader Header { get; }

        /// <summary>
        /// Whether the bitmap file has been opened for reading
        /// </summary>
        bool IsOpen { get; }

        /// <summary>
        /// Color palette for indexed color bitmaps (null for direct color bitmaps)
        /// </summary>
        Pixel[] ColorPalette { get; }

        /// <summary>
        /// Opens a bitmap file for reading
        /// </summary>
        /// <param name="filePath">Path to the bitmap file</param>
        /// <exception cref="FileNotFoundException">When the file does not exist</exception>
        /// <exception cref="InvalidDataException">When the file is not a valid bitmap</exception>
        /// <exception cref="NotSupportedException">When the bitmap format is not supported</exception>
        void Open(string filePath);

        /// <summary>
        /// Asynchronously opens a bitmap file for reading
        /// </summary>
        /// <param name="filePath">Path to the bitmap file</param>
        /// <exception cref="FileNotFoundException">When the file does not exist</exception>
        /// <exception cref="InvalidDataException">When the file is not a valid bitmap</exception>
        /// <exception cref="NotSupportedException">When the bitmap format is not supported</exception>
        Task OpenAsync(string filePath);

        /// <summary>
        /// Gets a single pixel at the specified coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Pixel at the specified coordinates</returns>
        /// <exception cref="ArgumentOutOfRangeException">When coordinates are outside bitmap bounds</exception>
        Pixel GetPixel(int x, int y);

        /// <summary>
        /// Gets a single pixel at the specified point
        /// </summary>
        /// <param name="point">Point coordinates</param>
        /// <returns>Pixel at the specified point</returns>
        /// <exception cref="ArgumentOutOfRangeException">When point is outside bitmap bounds</exception>
        Pixel GetPixel(Point point);

        /// <summary>
        /// Gets pixels from a rectangular region of the bitmap
        /// </summary>
        /// <param name="x">Starting X coordinate</param>
        /// <param name="y">Starting Y coordinate</param>
        /// <param name="width">Width of the region</param>
        /// <param name="height">Height of the region</param>
        /// <returns>Array of pixels from the region</returns>
        Pixel[] GetPixels(int x, int y, int width, int height);

        /// <summary>
        /// Gets all pixels from the bitmap as a single array
        /// </summary>
        /// <returns>Array of all pixels in the bitmap</returns>
        Pixel[] GetAllPixels();

        /// <summary>
        /// Streams pixels from the bitmap one at a time for memory-efficient processing
        /// </summary>
        /// <returns>Enumerable of pixels in row-major order</returns>
        IEnumerable<Pixel> StreamPixels();

        /// <summary>
        /// Asynchronously streams pixels from the bitmap
        /// </summary>
        /// <returns>Async enumerable of pixels in row-major order</returns>
        IAsyncEnumerable<Pixel> StreamPixelsAsync();

        /// <summary>
        /// Streams pixels from a specific row
        /// </summary>
        /// <param name="row">Row index (0-based)</param>
        /// <returns>Enumerable of pixels from the specified row</returns>
        IEnumerable<Pixel> StreamRow(int row);

        /// <summary>
        /// Streams pixels from a rectangular region
        /// </summary>
        /// <param name="x">Starting X coordinate</param>
        /// <param name="y">Starting Y coordinate</param>
        /// <param name="width">Width of the region</param>
        /// <param name="height">Height of the region</param>
        /// <returns>Enumerable of pixels from the region</returns>
        IEnumerable<Pixel> StreamRegion(int x, int y, int width, int height);

        /// <summary>
        /// Gets raw pixel data as bytes for advanced processing
        /// </summary>
        /// <param name="x">Starting X coordinate</param>
        /// <param name="y">Starting Y coordinate</param>
        /// <param name="width">Width of the region</param>
        /// <param name="height">Height of the region</param>
        /// <returns>Raw pixel data bytes</returns>
        ReadOnlySpan<byte> GetRawPixelData(int x, int y, int width, int height);

        /// <summary>
        /// Validates that the specified coordinates are within bitmap bounds
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>True if coordinates are valid</returns>
        bool IsValidCoordinate(int x, int y);

        /// <summary>
        /// Validates that the specified point is within bitmap bounds
        /// </summary>
        /// <param name="point">Point to validate</param>
        /// <returns>True if point is valid</returns>
        bool IsValidCoordinate(Point point);

        /// <summary>
        /// Validates that the specified region is within bitmap bounds
        /// </summary>
        /// <param name="x">Starting X coordinate</param>
        /// <param name="y">Starting Y coordinate</param>
        /// <param name="width">Width of the region</param>
        /// <param name="height">Height of the region</param>
        /// <returns>True if region is valid</returns>
        bool IsValidRegion(int x, int y, int width, int height);

        /// <summary>
        /// Closes the bitmap file and releases resources
        /// </summary>
        void Close();

        /// <summary>
        /// Event raised when pixel reading progress changes (for large operations)
        /// </summary>
        event EventHandler<BitmapProgressEventArgs> ProgressChanged;
    }

    /// <summary>
    /// Event arguments for bitmap reading progress updates
    /// </summary>
    public class BitmapProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Number of pixels processed
        /// </summary>
        public long PixelsProcessed { get; }

        /// <summary>
        /// Total number of pixels to process
        /// </summary>
        public long TotalPixels { get; }

        /// <summary>
        /// Progress percentage (0.0 to 1.0)
        /// </summary>
        public double ProgressPercentage => TotalPixels > 0 ? (double)PixelsProcessed / TotalPixels : 0.0;

        /// <summary>
        /// Creates new progress event arguments
        /// </summary>
        /// <param name="pixelsProcessed">Number of pixels processed</param>
        /// <param name="totalPixels">Total number of pixels</param>
        public BitmapProgressEventArgs(long pixelsProcessed, long totalPixels)
        {
            PixelsProcessed = pixelsProcessed;
            TotalPixels = totalPixels;
        }
    }

    /// <summary>
    /// Exception thrown when bitmap operations fail
    /// </summary>
    public class BitmapException : Exception
    {
        /// <summary>
        /// Path to the bitmap file that caused the error
        /// </summary>
        public string FilePath { get; }

        public BitmapException(string message, string filePath = null) : base(message)
        {
            FilePath = filePath;
        }

        public BitmapException(string message, Exception innerException, string filePath = null)
            : base(message, innerException)
        {
            FilePath = filePath;
        }
    }
}