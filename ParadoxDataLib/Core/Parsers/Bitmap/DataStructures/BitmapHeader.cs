using System;

namespace ParadoxDataLib.Core.Parsers.Bitmap.DataStructures
{
    /// <summary>
    /// Represents the header information from a bitmap file.
    /// Contains metadata about dimensions, color depth, compression, and format details.
    /// </summary>
    public readonly struct BitmapHeader
    {
        /// <summary>
        /// Bitmap file signature (should be "BM" for Windows BMP)
        /// </summary>
        public string Signature { get; }

        /// <summary>
        /// Total size of the bitmap file in bytes
        /// </summary>
        public uint FileSize { get; }

        /// <summary>
        /// Offset to the start of pixel data in bytes
        /// </summary>
        public uint PixelDataOffset { get; }

        /// <summary>
        /// Size of the bitmap info header
        /// </summary>
        public uint HeaderSize { get; }

        /// <summary>
        /// Width of the bitmap in pixels
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Height of the bitmap in pixels (positive = bottom-up, negative = top-down)
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Number of color planes (should be 1)
        /// </summary>
        public ushort Planes { get; }

        /// <summary>
        /// Number of bits per pixel (1, 4, 8, 16, 24, 32)
        /// </summary>
        public ushort BitsPerPixel { get; }

        /// <summary>
        /// Compression method used
        /// </summary>
        public BitmapCompression Compression { get; }

        /// <summary>
        /// Size of the raw image data (may be 0 for uncompressed images)
        /// </summary>
        public uint ImageSize { get; }

        /// <summary>
        /// Horizontal resolution in pixels per meter
        /// </summary>
        public int XPixelsPerMeter { get; }

        /// <summary>
        /// Vertical resolution in pixels per meter
        /// </summary>
        public int YPixelsPerMeter { get; }

        /// <summary>
        /// Number of colors in the color palette (0 means use maximum for bit depth)
        /// </summary>
        public uint ColorsUsed { get; }

        /// <summary>
        /// Number of important colors (0 means all colors are important)
        /// </summary>
        public uint ImportantColors { get; }

        /// <summary>
        /// Creates a new bitmap header with the specified properties
        /// </summary>
        public BitmapHeader(string signature, uint fileSize, uint pixelDataOffset, uint headerSize,
                           int width, int height, ushort planes, ushort bitsPerPixel,
                           BitmapCompression compression, uint imageSize,
                           int xPixelsPerMeter, int yPixelsPerMeter,
                           uint colorsUsed, uint importantColors)
        {
            Signature = signature;
            FileSize = fileSize;
            PixelDataOffset = pixelDataOffset;
            HeaderSize = headerSize;
            Width = width;
            Height = height;
            Planes = planes;
            BitsPerPixel = bitsPerPixel;
            Compression = compression;
            ImageSize = imageSize;
            XPixelsPerMeter = xPixelsPerMeter;
            YPixelsPerMeter = yPixelsPerMeter;
            ColorsUsed = colorsUsed;
            ImportantColors = importantColors;
        }

        /// <summary>
        /// Gets the absolute height (always positive)
        /// </summary>
        public int AbsoluteHeight => Math.Abs(Height);

        /// <summary>
        /// Gets whether the bitmap is stored bottom-up (standard) or top-down
        /// </summary>
        public bool IsBottomUp => Height > 0;

        /// <summary>
        /// Gets whether the bitmap is stored top-down
        /// </summary>
        public bool IsTopDown => Height < 0;

        /// <summary>
        /// Gets the number of bytes per row (including padding)
        /// </summary>
        public int Stride => ((Width * BitsPerPixel + 31) / 32) * 4;

        /// <summary>
        /// Gets the total number of pixels in the bitmap
        /// </summary>
        public long TotalPixels => (long)Width * AbsoluteHeight;

        /// <summary>
        /// Gets the number of bytes per pixel
        /// </summary>
        public int BytesPerPixel => BitsPerPixel / 8;

        /// <summary>
        /// Gets whether this bitmap has a color palette
        /// </summary>
        public bool HasColorPalette => BitsPerPixel <= 8;

        /// <summary>
        /// Gets the maximum number of colors for this bit depth
        /// </summary>
        public uint MaxColors => HasColorPalette ? (uint)(1 << BitsPerPixel) : 0;

        /// <summary>
        /// Gets the actual number of colors in the palette
        /// </summary>
        public uint PaletteColors => HasColorPalette ? (ColorsUsed == 0 ? MaxColors : ColorsUsed) : 0;

        /// <summary>
        /// Gets the size of the color palette in bytes
        /// </summary>
        public uint PaletteSize => PaletteColors * 4; // Each palette entry is 4 bytes (RGBA)

        /// <summary>
        /// Validates that the header contains reasonable values
        /// </summary>
        public bool IsValid()
        {
            return Signature == "BM" &&
                   Width > 0 && AbsoluteHeight > 0 &&
                   Planes == 1 &&
                   (BitsPerPixel == 1 || BitsPerPixel == 4 || BitsPerPixel == 8 ||
                    BitsPerPixel == 16 || BitsPerPixel == 24 || BitsPerPixel == 32) &&
                   PixelDataOffset >= HeaderSize + 14; // 14 = size of BITMAPFILEHEADER
        }

        /// <summary>
        /// Gets a summary string of the bitmap properties
        /// </summary>
        public string GetSummary()
        {
            var compressionName = Compression switch
            {
                BitmapCompression.None => "Uncompressed",
                BitmapCompression.RLE8 => "RLE 8-bit",
                BitmapCompression.RLE4 => "RLE 4-bit",
                BitmapCompression.Bitfields => "Bitfields",
                _ => Compression.ToString()
            };

            return $"{Width}x{AbsoluteHeight}, {BitsPerPixel}-bit, {compressionName}";
        }

        public override string ToString()
        {
            return $"BMP Header: {GetSummary()}, Size: {FileSize:N0} bytes";
        }
    }

    /// <summary>
    /// Bitmap compression methods
    /// </summary>
    public enum BitmapCompression : uint
    {
        /// <summary>
        /// No compression (RGB)
        /// </summary>
        None = 0,

        /// <summary>
        /// RLE 8-bit compression
        /// </summary>
        RLE8 = 1,

        /// <summary>
        /// RLE 4-bit compression
        /// </summary>
        RLE4 = 2,

        /// <summary>
        /// Bitfields compression (RGB with bit masks)
        /// </summary>
        Bitfields = 3,

        /// <summary>
        /// JPEG compression (not commonly supported)
        /// </summary>
        JPEG = 4,

        /// <summary>
        /// PNG compression (not commonly supported)
        /// </summary>
        PNG = 5
    }
}