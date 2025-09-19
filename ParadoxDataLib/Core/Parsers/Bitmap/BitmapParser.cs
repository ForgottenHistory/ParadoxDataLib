using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ParadoxDataLib.Core.Parsers.Bitmap.DataStructures;
using ParadoxDataLib.Core.Parsers.Bitmap.Interpreters;

namespace ParadoxDataLib.Core.Parsers.Bitmap
{
    /// <summary>
    /// Generic bitmap parser that uses pluggable pixel interpreters to convert bitmap data to strongly-typed objects.
    /// Provides high-performance parsing with error handling and statistics collection.
    /// </summary>
    /// <typeparam name="T">The type of objects to create from pixel interpretation</typeparam>
    public class BitmapParser<T>
    {
        private readonly IPixelInterpreter<T> _interpreter;
        private readonly IBitmapReader _bitmapReader;
        private readonly bool _enableProgressReporting;

        /// <summary>
        /// Event raised when parsing progress changes (for large operations)
        /// </summary>
        public event EventHandler<BitmapParsingProgressEventArgs> ProgressChanged;

        /// <summary>
        /// Creates a new bitmap parser with the specified interpreter and reader
        /// </summary>
        /// <param name="interpreter">Pixel interpreter to convert pixels to objects</param>
        /// <param name="bitmapReader">Bitmap reader for file I/O operations</param>
        /// <param name="enableProgressReporting">Whether to report progress during parsing</param>
        public BitmapParser(IPixelInterpreter<T> interpreter, IBitmapReader bitmapReader = null, bool enableProgressReporting = true)
        {
            _interpreter = interpreter ?? throw new ArgumentNullException(nameof(interpreter));
            _bitmapReader = bitmapReader ?? new BmpReader();
            _enableProgressReporting = enableProgressReporting;

            if (_enableProgressReporting && _bitmapReader is BmpReader bmpReader)
            {
                bmpReader.ProgressChanged += OnBitmapReaderProgress;
            }
        }

        /// <summary>
        /// Parses a bitmap file and returns the interpreted data
        /// </summary>
        /// <param name="filePath">Path to the bitmap file</param>
        /// <returns>Bitmap data container with interpreted values</returns>
        public BitmapData<T> ParseFile(string filePath)
        {
            var stats = new BitmapParsingStats();
            return ParseFile(filePath, out stats);
        }

        /// <summary>
        /// Parses a bitmap file and returns the interpreted data with statistics
        /// </summary>
        /// <param name="filePath">Path to the bitmap file</param>
        /// <param name="stats">Output parameter containing parsing statistics</param>
        /// <returns>Bitmap data container with interpreted values</returns>
        public BitmapData<T> ParseFile(string filePath, out BitmapParsingStats stats)
        {
            var startTime = DateTime.UtcNow;
            stats = new BitmapParsingStats { FilePath = filePath };

            try
            {
                _bitmapReader.Open(filePath);
                var header = _bitmapReader.Header;

                stats.Width = header.Width;
                stats.Height = header.AbsoluteHeight;
                stats.TotalPixels = header.TotalPixels;
                stats.BitsPerPixel = header.BitsPerPixel;

                var bitmapData = new BitmapData<T>(header.Width, header.AbsoluteHeight, (int)header.TotalPixels);

                // Parse all pixels and interpret them
                var processedPixels = 0L;
                foreach (var pixel in _bitmapReader.StreamPixels())
                {
                    if (_interpreter.TryInterpretPixel(pixel, out var value, out var errorMessage))
                    {
                        bitmapData.SetData(pixel.X, pixel.Y, value);
                        stats.SuccessfulPixels++;
                    }
                    else
                    {
                        stats.FailedPixels++;
                        stats.Errors.Add($"Pixel ({pixel.X},{pixel.Y}): {errorMessage}");
                    }

                    processedPixels++;
                    if (processedPixels % 100000 == 0) // Report progress every 100k pixels
                    {
                        OnProgressChanged(processedPixels, stats.TotalPixels, $"Interpreting pixels... {processedPixels:N0}/{stats.TotalPixels:N0}");
                    }
                }

                stats.InterpretationStats = _interpreter.GetStatistics();
                return bitmapData;
            }
            catch (Exception ex)
            {
                stats.Errors.Add($"Parsing failed: {ex.Message}");
                throw new BitmapParsingException($"Failed to parse bitmap file: {filePath}", ex, filePath);
            }
            finally
            {
                _bitmapReader?.Close();
                stats.ElapsedTime = DateTime.UtcNow - startTime;
            }
        }

        /// <summary>
        /// Asynchronously parses a bitmap file and returns the interpreted data
        /// </summary>
        /// <param name="filePath">Path to the bitmap file</param>
        /// <returns>Bitmap data container with interpreted values</returns>
        public async Task<BitmapData<T>> ParseFileAsync(string filePath)
        {
            var result = await ParseFileAsync(filePath, new BitmapParsingStats());
            return result.Item1;
        }

        /// <summary>
        /// Asynchronously parses a bitmap file and returns the interpreted data with statistics
        /// </summary>
        /// <param name="filePath">Path to the bitmap file</param>
        /// <param name="stats">Statistics object to populate</param>
        /// <returns>Tuple containing bitmap data and statistics</returns>
        public async Task<(BitmapData<T>, BitmapParsingStats)> ParseFileAsync(string filePath, BitmapParsingStats stats)
        {
            var startTime = DateTime.UtcNow;
            stats.FilePath = filePath;

            try
            {
                await _bitmapReader.OpenAsync(filePath);
                var header = _bitmapReader.Header;

                stats.Width = header.Width;
                stats.Height = header.AbsoluteHeight;
                stats.TotalPixels = header.TotalPixels;
                stats.BitsPerPixel = header.BitsPerPixel;

                var bitmapData = new BitmapData<T>(header.Width, header.AbsoluteHeight, (int)header.TotalPixels);

                // Parse all pixels and interpret them asynchronously
                var processedPixels = 0L;
                await foreach (var pixel in _bitmapReader.StreamPixelsAsync())
                {
                    if (_interpreter.TryInterpretPixel(pixel, out var value, out var errorMessage))
                    {
                        bitmapData.SetData(pixel.X, pixel.Y, value);
                        stats.SuccessfulPixels++;
                    }
                    else
                    {
                        stats.FailedPixels++;
                        stats.Errors.Add($"Pixel ({pixel.X},{pixel.Y}): {errorMessage}");
                    }

                    processedPixels++;
                    if (processedPixels % 100000 == 0)
                    {
                        OnProgressChanged(processedPixels, stats.TotalPixels, $"Interpreting pixels... {processedPixels:N0}/{stats.TotalPixels:N0}");
                        await Task.Yield(); // Allow other tasks to run
                    }
                }

                stats.InterpretationStats = _interpreter.GetStatistics();
                return (bitmapData, stats);
            }
            catch (Exception ex)
            {
                stats.Errors.Add($"Parsing failed: {ex.Message}");
                throw new BitmapParsingException($"Failed to parse bitmap file: {filePath}", ex, filePath);
            }
            finally
            {
                _bitmapReader?.Close();
                stats.ElapsedTime = DateTime.UtcNow - startTime;
            }
        }

        /// <summary>
        /// Parses a specific region of a bitmap file
        /// </summary>
        /// <param name="filePath">Path to the bitmap file</param>
        /// <param name="x">Starting X coordinate</param>
        /// <param name="y">Starting Y coordinate</param>
        /// <param name="width">Width of the region</param>
        /// <param name="height">Height of the region</param>
        /// <returns>Bitmap data for the specified region</returns>
        public BitmapData<T> ParseRegion(string filePath, int x, int y, int width, int height)
        {
            try
            {
                _bitmapReader.Open(filePath);

                if (!_bitmapReader.IsValidRegion(x, y, width, height))
                    throw new ArgumentOutOfRangeException($"Region ({x},{y},{width}x{height}) is outside bitmap bounds");

                var bitmapData = new BitmapData<T>(width, height);

                var relativeX = 0;
                var relativeY = 0;

                foreach (var pixel in _bitmapReader.StreamRegion(x, y, width, height))
                {
                    if (_interpreter.TryInterpretPixel(pixel, out var value, out _))
                    {
                        bitmapData.SetData(relativeX, relativeY, value);
                    }

                    relativeX++;
                    if (relativeX >= width)
                    {
                        relativeX = 0;
                        relativeY++;
                    }
                }

                return bitmapData;
            }
            finally
            {
                _bitmapReader?.Close();
            }
        }

        /// <summary>
        /// Validates a bitmap file without fully parsing it
        /// </summary>
        /// <param name="filePath">Path to the bitmap file</param>
        /// <returns>Validation statistics</returns>
        public BitmapValidationResult ValidateFile(string filePath)
        {
            var result = new BitmapValidationResult { FilePath = filePath };
            var startTime = DateTime.UtcNow;

            try
            {
                _bitmapReader.Open(filePath);
                var header = _bitmapReader.Header;

                result.IsValidBitmap = header.IsValid();
                result.Width = header.Width;
                result.Height = header.AbsoluteHeight;
                result.BitsPerPixel = header.BitsPerPixel;
                result.FileSize = header.FileSize;

                if (!result.IsValidBitmap)
                {
                    result.ValidationErrors.Add($"Invalid bitmap header: {header.GetSummary()}");
                }

                // Sample some pixels to test interpretation
                var sampleCount = Math.Min(1000, (int)header.TotalPixels);
                var samplePixels = _bitmapReader.StreamPixels().Take(sampleCount).ToArray();

                foreach (var pixel in samplePixels)
                {
                    if (!_interpreter.CanInterpretPixel(pixel))
                    {
                        result.ValidationErrors.Add($"Pixel at ({pixel.X},{pixel.Y}) cannot be interpreted by {_interpreter.GetType().Name}");
                    }
                }

                result.SamplePixelsChecked = sampleCount;
            }
            catch (Exception ex)
            {
                result.ValidationErrors.Add($"Validation error: {ex.Message}");
            }
            finally
            {
                _bitmapReader?.Close();
                result.ElapsedTime = DateTime.UtcNow - startTime;
            }

            return result;
        }

        /// <summary>
        /// Gets information about the bitmap without parsing pixel data
        /// </summary>
        /// <param name="filePath">Path to the bitmap file</param>
        /// <returns>Bitmap information</returns>
        public BitmapInfo GetBitmapInfo(string filePath)
        {
            try
            {
                _bitmapReader.Open(filePath);
                var header = _bitmapReader.Header;

                return new BitmapInfo
                {
                    FilePath = filePath,
                    Width = header.Width,
                    Height = header.AbsoluteHeight,
                    BitsPerPixel = header.BitsPerPixel,
                    FileSize = header.FileSize,
                    Compression = header.Compression.ToString(),
                    HasColorPalette = header.HasColorPalette,
                    IsBottomUp = header.IsBottomUp,
                    Summary = header.GetSummary()
                };
            }
            finally
            {
                _bitmapReader?.Close();
            }
        }

        /// <summary>
        /// Raises the ProgressChanged event
        /// </summary>
        private void OnProgressChanged(long processed, long total, string message)
        {
            if (_enableProgressReporting)
            {
                ProgressChanged?.Invoke(this, new BitmapParsingProgressEventArgs(processed, total, message));
            }
        }

        /// <summary>
        /// Handles progress events from the bitmap reader
        /// </summary>
        private void OnBitmapReaderProgress(object sender, BitmapProgressEventArgs e)
        {
            OnProgressChanged(e.PixelsProcessed, e.TotalPixels, "Reading bitmap...");
        }
    }

    /// <summary>
    /// Statistics about bitmap parsing operations
    /// </summary>
    public class BitmapParsingStats
    {
        /// <summary>
        /// Path to the bitmap file
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Width of the bitmap in pixels
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Height of the bitmap in pixels
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Bits per pixel in the bitmap
        /// </summary>
        public int BitsPerPixel { get; set; }

        /// <summary>
        /// Total number of pixels in the bitmap
        /// </summary>
        public long TotalPixels { get; set; }

        /// <summary>
        /// Number of pixels successfully interpreted
        /// </summary>
        public long SuccessfulPixels { get; set; }

        /// <summary>
        /// Number of pixels that failed interpretation
        /// </summary>
        public long FailedPixels { get; set; }

        /// <summary>
        /// Time taken to complete the parsing operation
        /// </summary>
        public TimeSpan ElapsedTime { get; set; }

        /// <summary>
        /// List of errors encountered during parsing
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Statistics from the pixel interpreter
        /// </summary>
        public PixelInterpretationStats InterpretationStats { get; set; }

        /// <summary>
        /// Success rate (0.0 to 1.0)
        /// </summary>
        public double SuccessRate => TotalPixels > 0 ? (double)SuccessfulPixels / TotalPixels : 0.0;

        /// <summary>
        /// Pixels processed per second
        /// </summary>
        public double PixelsPerSecond => ElapsedTime.TotalSeconds > 0 ? TotalPixels / ElapsedTime.TotalSeconds : 0.0;

        /// <summary>
        /// Whether the parsing completed successfully
        /// </summary>
        public bool IsSuccessful => FailedPixels == 0 && Errors.Count == 0;

        public override string ToString()
        {
            return $"Parsed {Width}x{Height} bitmap, {SuccessfulPixels:N0}/{TotalPixels:N0} pixels successful, " +
                   $"{ElapsedTime.TotalMilliseconds:F1}ms, {PixelsPerSecond:N0} pixels/sec";
        }
    }

    /// <summary>
    /// Progress event arguments for bitmap parsing operations
    /// </summary>
    public class BitmapParsingProgressEventArgs : EventArgs
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
        /// Progress message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Progress percentage (0.0 to 1.0)
        /// </summary>
        public double ProgressPercentage => TotalPixels > 0 ? (double)PixelsProcessed / TotalPixels : 0.0;

        public BitmapParsingProgressEventArgs(long pixelsProcessed, long totalPixels, string message)
        {
            PixelsProcessed = pixelsProcessed;
            TotalPixels = totalPixels;
            Message = message;
        }
    }

    /// <summary>
    /// Validation result for bitmap files
    /// </summary>
    public class BitmapValidationResult
    {
        /// <summary>
        /// Path to the bitmap file
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Whether the bitmap is valid
        /// </summary>
        public bool IsValidBitmap { get; set; }

        /// <summary>
        /// Width of the bitmap
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Height of the bitmap
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Bits per pixel
        /// </summary>
        public int BitsPerPixel { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public uint FileSize { get; set; }

        /// <summary>
        /// Number of sample pixels checked
        /// </summary>
        public int SamplePixelsChecked { get; set; }

        /// <summary>
        /// Time taken for validation
        /// </summary>
        public TimeSpan ElapsedTime { get; set; }

        /// <summary>
        /// List of validation errors
        /// </summary>
        public List<string> ValidationErrors { get; set; } = new List<string>();

        /// <summary>
        /// Whether validation passed
        /// </summary>
        public bool IsValid => IsValidBitmap && ValidationErrors.Count == 0;
    }

    /// <summary>
    /// Information about a bitmap file
    /// </summary>
    public class BitmapInfo
    {
        /// <summary>
        /// Path to the bitmap file
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Width in pixels
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Height in pixels
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Bits per pixel
        /// </summary>
        public int BitsPerPixel { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public uint FileSize { get; set; }

        /// <summary>
        /// Compression type
        /// </summary>
        public string Compression { get; set; }

        /// <summary>
        /// Whether the bitmap has a color palette
        /// </summary>
        public bool HasColorPalette { get; set; }

        /// <summary>
        /// Whether the bitmap is stored bottom-up
        /// </summary>
        public bool IsBottomUp { get; set; }

        /// <summary>
        /// Summary description
        /// </summary>
        public string Summary { get; set; }
    }

    /// <summary>
    /// Exception thrown when bitmap parsing fails
    /// </summary>
    public class BitmapParsingException : Exception
    {
        /// <summary>
        /// Path to the bitmap file that caused the error
        /// </summary>
        public string FilePath { get; }

        public BitmapParsingException(string message, string filePath = null) : base(message)
        {
            FilePath = filePath;
        }

        public BitmapParsingException(string message, Exception innerException, string filePath = null)
            : base(message, innerException)
        {
            FilePath = filePath;
        }
    }
}