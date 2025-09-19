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
        private FileStream _fileStream;
        private byte[] _fileData;
        private bool _useMemoryMappedFile;
        private const int BUFFER_SIZE = 64 * 1024; // 64KB buffer for streaming
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
        public bool IsOpen => (_mmf != null || _fileStream != null) && !_disposed;

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

                // Try memory-mapped file first, fall back to regular file I/O
                try
                {
                    _mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
                    _accessor = _mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
                    _useMemoryMappedFile = true;
                }
                catch (Exception mmfEx) when (mmfEx.Message.Contains("Named maps are not supported") ||
                                               mmfEx is NotSupportedException ||
                                               mmfEx is PlatformNotSupportedException)
                {
                    // Fall back to regular file I/O with minimal initial loading
                    _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    // Only load header data initially (first 1KB should be enough for any BMP header)
                    var headerSize = Math.Min(1024, (int)_fileStream.Length);
                    _fileData = new byte[headerSize];
                    _fileStream.Read(_fileData, 0, headerSize);
                    _fileStream.Position = 0; // Reset position for later streaming reads
                    _useMemoryMappedFile = false;
                }

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

            _fileStream?.Dispose();
            _fileStream = null;

            _fileData = null;
            _colorPalette = null;
            _filePath = null;
            _useMemoryMappedFile = false;
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
            var signature = $"{(char)ReadByte(0)}{(char)ReadByte(1)}";
            var fileSize = ReadUInt32(2);
            var pixelDataOffset = ReadUInt32(10);

            // Read BITMAPINFOHEADER
            var headerSize = ReadUInt32(14);
            var width = ReadInt32(18);
            var height = ReadInt32(22);
            var planes = ReadUInt16(26);
            var bitsPerPixel = ReadUInt16(28);
            var compression = (BitmapCompression)ReadUInt32(30);
            var imageSize = ReadUInt32(34);
            var xPixelsPerMeter = ReadInt32(38);
            var yPixelsPerMeter = ReadInt32(42);
            var colorsUsed = ReadUInt32(46);
            var importantColors = ReadUInt32(50);

            return new BitmapHeader(signature, fileSize, pixelDataOffset, headerSize,
                                   width, height, planes, bitsPerPixel, compression, imageSize,
                                   xPixelsPerMeter, yPixelsPerMeter, colorsUsed, importantColors);
        }

        /// <summary>
        /// Helper methods to read data from either memory-mapped file or file stream
        /// </summary>
        private byte ReadByte(long offset)
        {
            if (_useMemoryMappedFile)
                return _accessor.ReadByte(offset);

            // For file-based access, check if we need to read more data
            if (offset >= _fileData.Length)
                ReadDataAtOffset(offset, 1);

            return _fileData[offset];
        }

        private ushort ReadUInt16(long offset)
        {
            if (_useMemoryMappedFile)
                return _accessor.ReadUInt16(offset);

            // For file-based access, check if we need to read more data
            if (offset + 2 > _fileData.Length)
                ReadDataAtOffset(offset, 2);

            return BitConverter.ToUInt16(_fileData, (int)offset);
        }

        private uint ReadUInt32(long offset)
        {
            if (_useMemoryMappedFile)
                return _accessor.ReadUInt32(offset);

            // For file-based access, check if we need to read more data
            if (offset + 4 > _fileData.Length)
                ReadDataAtOffset(offset, 4);

            return BitConverter.ToUInt32(_fileData, (int)offset);
        }

        private int ReadInt32(long offset)
        {
            if (_useMemoryMappedFile)
                return _accessor.ReadInt32(offset);

            // For file-based access, check if we need to read more data
            if (offset + 4 > _fileData.Length)
                ReadDataAtOffset(offset, 4);

            return BitConverter.ToInt32(_fileData, (int)offset);
        }

        /// <summary>
        /// Reads data from file stream when needed for file-based access
        /// </summary>
        private void ReadDataAtOffset(long offset, int minBytesNeeded)
        {
            if (_useMemoryMappedFile) return;

            // Calculate how much data we need to read
            var endOffset = offset + minBytesNeeded;
            var newSize = Math.Max(endOffset, _fileData.Length * 2); // Grow buffer
            newSize = Math.Min(newSize, _fileStream.Length); // Don't exceed file size

            // Expand the buffer if needed
            if (newSize > _fileData.Length)
            {
                var newBuffer = new byte[newSize];
                Array.Copy(_fileData, newBuffer, _fileData.Length);

                // Read additional data from file
                _fileStream.Position = _fileData.Length;
                var bytesToRead = (int)(newSize - _fileData.Length);
                _fileStream.Read(newBuffer, _fileData.Length, bytesToRead);

                _fileData = newBuffer;
            }
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