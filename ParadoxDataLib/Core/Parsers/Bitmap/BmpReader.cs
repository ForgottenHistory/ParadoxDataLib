using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ParadoxDataLib.Core.Parsers.Bitmap.DataStructures;

namespace ParadoxDataLib.Core.Parsers.Bitmap
{
    /// <summary>
    /// High-performance Windows BMP file reader with memory-mapped file support.
    /// Optimized for large bitmap files like Paradox game maps (5632x2048 pixels).
    /// </summary>
    public class BmpReader : IBitmapReader
    {
        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _accessor;
        private string _filePath;
        private BitmapHeader _header;
        private Pixel[] _colorPalette;
        private bool _disposed;

        /// <summary>
        /// Header information from the bitmap file
        /// </summary>
        public BitmapHeader Header => _header;

        /// <summary>
        /// Whether the bitmap file has been opened for reading
        /// </summary>
        public bool IsOpen => _mmf != null && !_disposed;

        /// <summary>
        /// Color palette for indexed color bitmaps (null for direct color bitmaps)
        /// </summary>
        public Pixel[] ColorPalette => _colorPalette;

        /// <summary>
        /// Event raised when pixel reading progress changes (for large operations)
        /// </summary>
        public event EventHandler<BitmapProgressEventArgs> ProgressChanged;

        /// <summary>
        /// Opens a bitmap file for reading
        /// </summary>
        /// <param name="filePath">Path to the bitmap file</param>
        public void Open(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Bitmap file not found: {filePath}");

            Close(); // Close any existing file

            try
            {
                _filePath = filePath;

                // Open memory-mapped file for efficient access
                _mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, "bmp", 0, MemoryMappedFileAccess.Read);
                _accessor = _mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

                // Read and validate header
                _header = ReadHeader();
                if (!_header.IsValid())
                    throw new InvalidDataException($"Invalid BMP header in file: {filePath}");

                // Read color palette if present
                if (_header.HasColorPalette)
                {
                    _colorPalette = ReadColorPalette();
                }
            }
            catch (Exception ex)
            {
                Close();
                throw new BitmapException($"Failed to open bitmap file: {filePath}", ex, filePath);
            }
        }

        /// <summary>
        /// Asynchronously opens a bitmap file for reading
        /// </summary>
        /// <param name="filePath">Path to the bitmap file</param>
        public Task OpenAsync(string filePath)
        {
            return Task.Run(() => Open(filePath));
        }

        /// <summary>
        /// Gets a single pixel at the specified coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Pixel at the specified coordinates</returns>
        public Pixel GetPixel(int x, int y)
        {
            EnsureOpen();

            if (!IsValidCoordinate(x, y))
                throw new ArgumentOutOfRangeException($"Coordinates ({x}, {y}) are outside bitmap bounds ({_header.Width}x{_header.AbsoluteHeight})");

            return ReadPixelAt(x, y);
        }

        /// <summary>
        /// Gets a single pixel at the specified point
        /// </summary>
        /// <param name="point">Point coordinates</param>
        /// <returns>Pixel at the specified point</returns>
        public Pixel GetPixel(Point point)
        {
            return GetPixel(point.X, point.Y);
        }

        /// <summary>
        /// Gets pixels from a rectangular region of the bitmap
        /// </summary>
        /// <param name="x">Starting X coordinate</param>
        /// <param name="y">Starting Y coordinate</param>
        /// <param name="width">Width of the region</param>
        /// <param name="height">Height of the region</param>
        /// <returns>Array of pixels from the region</returns>
        public Pixel[] GetPixels(int x, int y, int width, int height)
        {
            EnsureOpen();

            if (!IsValidRegion(x, y, width, height))
                throw new ArgumentOutOfRangeException($"Region ({x}, {y}, {width}x{height}) is outside bitmap bounds");

            var pixels = new Pixel[width * height];
            var index = 0;

            for (var py = y; py < y + height; py++)
            {
                for (var px = x; px < x + width; px++)
                {
                    pixels[index++] = ReadPixelAt(px, py);
                }
            }

            return pixels;
        }

        /// <summary>
        /// Gets all pixels from the bitmap as a single array
        /// </summary>
        /// <returns>Array of all pixels in the bitmap</returns>
        public Pixel[] GetAllPixels()
        {
            EnsureOpen();
            return GetPixels(0, 0, _header.Width, _header.AbsoluteHeight);
        }

        /// <summary>
        /// Streams pixels from the bitmap one at a time for memory-efficient processing
        /// </summary>
        /// <returns>Enumerable of pixels in row-major order</returns>
        public IEnumerable<Pixel> StreamPixels()
        {
            EnsureOpen();

            var totalPixels = _header.TotalPixels;
            var processedPixels = 0L;

            for (var y = 0; y < _header.AbsoluteHeight; y++)
            {
                for (var x = 0; x < _header.Width; x++)
                {
                    yield return ReadPixelAt(x, y);

                    processedPixels++;
                    if (processedPixels % 10000 == 0) // Report progress every 10k pixels
                    {
                        OnProgressChanged(processedPixels, totalPixels);
                    }
                }
            }

            OnProgressChanged(totalPixels, totalPixels); // Final progress update
        }

        /// <summary>
        /// Asynchronously streams pixels from the bitmap
        /// </summary>
        /// <returns>Async enumerable of pixels in row-major order</returns>
        public async IAsyncEnumerable<Pixel> StreamPixelsAsync()
        {
            EnsureOpen();

            var totalPixels = _header.TotalPixels;
            var processedPixels = 0L;

            for (var y = 0; y < _header.AbsoluteHeight; y++)
            {
                for (var x = 0; x < _header.Width; x++)
                {
                    yield return ReadPixelAt(x, y);

                    processedPixels++;
                    if (processedPixels % 10000 == 0)
                    {
                        OnProgressChanged(processedPixels, totalPixels);
                        await Task.Yield(); // Allow other tasks to run
                    }
                }
            }

            OnProgressChanged(totalPixels, totalPixels);
        }

        /// <summary>
        /// Streams pixels from a specific row
        /// </summary>
        /// <param name="row">Row index (0-based)</param>
        /// <returns>Enumerable of pixels from the specified row</returns>
        public IEnumerable<Pixel> StreamRow(int row)
        {
            EnsureOpen();

            if (row < 0 || row >= _header.AbsoluteHeight)
                throw new ArgumentOutOfRangeException(nameof(row), $"Row {row} is outside bitmap bounds (0-{_header.AbsoluteHeight - 1})");

            for (var x = 0; x < _header.Width; x++)
            {
                yield return ReadPixelAt(x, row);
            }
        }

        /// <summary>
        /// Streams pixels from a rectangular region
        /// </summary>
        /// <param name="x">Starting X coordinate</param>
        /// <param name="y">Starting Y coordinate</param>
        /// <param name="width">Width of the region</param>
        /// <param name="height">Height of the region</param>
        /// <returns>Enumerable of pixels from the region</returns>
        public IEnumerable<Pixel> StreamRegion(int x, int y, int width, int height)
        {
            EnsureOpen();

            if (!IsValidRegion(x, y, width, height))
                throw new ArgumentOutOfRangeException($"Region ({x}, {y}, {width}x{height}) is outside bitmap bounds");

            for (var py = y; py < y + height; py++)
            {
                for (var px = x; px < x + width; px++)
                {
                    yield return ReadPixelAt(px, py);
                }
            }
        }

        /// <summary>
        /// Gets raw pixel data as bytes for advanced processing
        /// </summary>
        /// <param name="x">Starting X coordinate</param>
        /// <param name="y">Starting Y coordinate</param>
        /// <param name="width">Width of the region</param>
        /// <param name="height">Height of the region</param>
        /// <returns>Raw pixel data bytes</returns>
        public ReadOnlySpan<byte> GetRawPixelData(int x, int y, int width, int height)
        {
            EnsureOpen();

            if (!IsValidRegion(x, y, width, height))
                throw new ArgumentOutOfRangeException($"Region ({x}, {y}, {width}x{height}) is outside bitmap bounds");

            // Calculate the start offset for the region
            var startY = _header.IsBottomUp ? _header.AbsoluteHeight - y - height : y;
            var bytesPerPixel = _header.BytesPerPixel;
            var stride = _header.Stride;

            var startOffset = _header.PixelDataOffset + (startY * stride) + (x * bytesPerPixel);
            var regionSize = height * stride;

            // Create a span over the memory-mapped region
            unsafe
            {
                var ptr = (byte*)_accessor.SafeMemoryMappedViewHandle.DangerousGetHandle().ToPointer();
                return new ReadOnlySpan<byte>(ptr + startOffset, regionSize);
            }
        }

        /// <summary>
        /// Validates that the specified coordinates are within bitmap bounds
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>True if coordinates are valid</returns>
        public bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < _header.Width && y >= 0 && y < _header.AbsoluteHeight;
        }

        /// <summary>
        /// Validates that the specified point is within bitmap bounds
        /// </summary>
        /// <param name="point">Point to validate</param>
        /// <returns>True if point is valid</returns>
        public bool IsValidCoordinate(Point point)
        {
            return IsValidCoordinate(point.X, point.Y);
        }

        /// <summary>
        /// Validates that the specified region is within bitmap bounds
        /// </summary>
        /// <param name="x">Starting X coordinate</param>
        /// <param name="y">Starting Y coordinate</param>
        /// <param name="width">Width of the region</param>
        /// <param name="height">Height of the region</param>
        /// <returns>True if region is valid</returns>
        public bool IsValidRegion(int x, int y, int width, int height)
        {
            return x >= 0 && y >= 0 && width > 0 && height > 0 &&
                   x + width <= _header.Width && y + height <= _header.AbsoluteHeight;
        }

        /// <summary>
        /// Closes the bitmap file and releases resources
        /// </summary>
        public void Close()
        {
            _accessor?.Dispose();
            _accessor = null;

            _mmf?.Dispose();
            _mmf = null;

            _colorPalette = null;
            _filePath = null;
        }

        /// <summary>
        /// Disposes of the bitmap reader resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Close();
                _disposed = true;
            }
        }

        /// <summary>
        /// Reads the bitmap header from the file
        /// </summary>
        private BitmapHeader ReadHeader()
        {
            // Read BITMAPFILEHEADER (14 bytes)
            var signature = $"{(char)_accessor.ReadByte(0)}{(char)_accessor.ReadByte(1)}";
            var fileSize = _accessor.ReadUInt32(2);
            var pixelDataOffset = _accessor.ReadUInt32(10);

            // Read BITMAPINFOHEADER
            var headerSize = _accessor.ReadUInt32(14);
            var width = _accessor.ReadInt32(18);
            var height = _accessor.ReadInt32(22);
            var planes = _accessor.ReadUInt16(26);
            var bitsPerPixel = _accessor.ReadUInt16(28);
            var compression = (BitmapCompression)_accessor.ReadUInt32(30);
            var imageSize = _accessor.ReadUInt32(34);
            var xPixelsPerMeter = _accessor.ReadInt32(38);
            var yPixelsPerMeter = _accessor.ReadInt32(42);
            var colorsUsed = _accessor.ReadUInt32(46);
            var importantColors = _accessor.ReadUInt32(50);

            return new BitmapHeader(signature, fileSize, pixelDataOffset, headerSize,
                                   width, height, planes, bitsPerPixel, compression, imageSize,
                                   xPixelsPerMeter, yPixelsPerMeter, colorsUsed, importantColors);
        }

        /// <summary>
        /// Reads the color palette for indexed color bitmaps
        /// </summary>
        private Pixel[] ReadColorPalette()
        {
            var paletteSize = (int)_header.PaletteColors;
            var palette = new Pixel[paletteSize];
            var paletteOffset = 54; // Start after BITMAPINFOHEADER

            for (var i = 0; i < paletteSize; i++)
            {
                var offset = paletteOffset + (i * 4);
                var b = _accessor.ReadByte(offset);
                var g = _accessor.ReadByte(offset + 1);
                var r = _accessor.ReadByte(offset + 2);
                var a = _accessor.ReadByte(offset + 3); // Usually 0 in BMP

                palette[i] = new Pixel(r, g, b, a == 0 ? (byte)255 : a);
            }

            return palette;
        }

        /// <summary>
        /// Reads a single pixel at the specified coordinates
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Pixel ReadPixelAt(int x, int y)
        {
            // Convert coordinates for bottom-up bitmaps
            var actualY = _header.IsBottomUp ? _header.AbsoluteHeight - 1 - y : y;

            var bytesPerPixel = _header.BytesPerPixel;
            var stride = _header.Stride;
            var offset = _header.PixelDataOffset + (actualY * stride) + (x * bytesPerPixel);

            switch (_header.BitsPerPixel)
            {
                case 8: // Indexed color
                    var paletteIndex = _accessor.ReadByte(offset);
                    return paletteIndex < _colorPalette.Length
                        ? _colorPalette[paletteIndex].WithPosition(x, y)
                        : new Pixel(0, 0, 0, 255, x, y);

                case 24: // RGB
                    var b24 = _accessor.ReadByte(offset);
                    var g24 = _accessor.ReadByte(offset + 1);
                    var r24 = _accessor.ReadByte(offset + 2);
                    return new Pixel(r24, g24, b24, 255, x, y);

                case 32: // RGBA
                    var b32 = _accessor.ReadByte(offset);
                    var g32 = _accessor.ReadByte(offset + 1);
                    var r32 = _accessor.ReadByte(offset + 2);
                    var a32 = _accessor.ReadByte(offset + 3);
                    return new Pixel(r32, g32, b32, a32, x, y);

                default:
                    throw new NotSupportedException($"Bit depth {_header.BitsPerPixel} is not supported");
            }
        }

        /// <summary>
        /// Ensures the bitmap reader is open and ready
        /// </summary>
        private void EnsureOpen()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BmpReader));

            if (!IsOpen)
                throw new InvalidOperationException("Bitmap reader is not open. Call Open() first.");
        }

        /// <summary>
        /// Raises the ProgressChanged event
        /// </summary>
        private void OnProgressChanged(long pixelsProcessed, long totalPixels)
        {
            ProgressChanged?.Invoke(this, new BitmapProgressEventArgs(pixelsProcessed, totalPixels));
        }
    }
}