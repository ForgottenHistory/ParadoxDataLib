using System;

namespace ParadoxDataLib.Core.Parsers.Bitmap.DataStructures
{
    /// <summary>
    /// Represents a single pixel with RGBA color components and coordinate position.
    /// Optimized for high-performance bitmap processing with minimal memory footprint.
    /// </summary>
    public readonly struct Pixel : IEquatable<Pixel>
    {
        /// <summary>
        /// Red color component (0-255)
        /// </summary>
        public byte R { get; }

        /// <summary>
        /// Green color component (0-255)
        /// </summary>
        public byte G { get; }

        /// <summary>
        /// Blue color component (0-255)
        /// </summary>
        public byte B { get; }

        /// <summary>
        /// Alpha transparency component (0-255)
        /// </summary>
        public byte A { get; }

        /// <summary>
        /// X coordinate in the bitmap
        /// </summary>
        public int X { get; }

        /// <summary>
        /// Y coordinate in the bitmap
        /// </summary>
        public int Y { get; }

        /// <summary>
        /// Creates a new pixel with RGBA components and position
        /// </summary>
        /// <param name="r">Red component (0-255)</param>
        /// <param name="g">Green component (0-255)</param>
        /// <param name="b">Blue component (0-255)</param>
        /// <param name="a">Alpha component (0-255, default: 255)</param>
        /// <param name="x">X coordinate (default: 0)</param>
        /// <param name="y">Y coordinate (default: 0)</param>
        public Pixel(byte r, byte g, byte b, byte a = 255, int x = 0, int y = 0)
        {
            R = r;
            G = g;
            B = b;
            A = a;
            X = x;
            Y = y;
        }

        /// <summary>
        /// Creates a pixel from a packed RGB value
        /// </summary>
        /// <param name="rgb">Packed RGB value (0xRRGGBB)</param>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public Pixel(int rgb, int x = 0, int y = 0)
        {
            R = (byte)((rgb >> 16) & 0xFF);
            G = (byte)((rgb >> 8) & 0xFF);
            B = (byte)(rgb & 0xFF);
            A = 255;
            X = x;
            Y = y;
        }

        /// <summary>
        /// Creates a grayscale pixel from a single intensity value
        /// </summary>
        /// <param name="intensity">Grayscale intensity (0-255)</param>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public static Pixel FromGrayscale(byte intensity, int x = 0, int y = 0)
        {
            return new Pixel(intensity, intensity, intensity, 255, x, y);
        }

        /// <summary>
        /// Gets the RGB color as a packed integer value (0xRRGGBB)
        /// </summary>
        public int ToRgb() => (R << 16) | (G << 8) | B;

        /// <summary>
        /// Gets the ARGB color as a packed integer value (0xAARRGGBB)
        /// </summary>
        public int ToArgb() => (A << 24) | (R << 16) | (G << 8) | B;

        /// <summary>
        /// Gets the grayscale intensity using standard luminance formula
        /// </summary>
        public byte ToGrayscale() => (byte)(0.299 * R + 0.587 * G + 0.114 * B);

        /// <summary>
        /// Gets a string representation of the RGB color
        /// </summary>
        public string RgbString => $"({R},{G},{B})";

        /// <summary>
        /// Gets a string representation of the RGBA color
        /// </summary>
        public string RgbaString => $"({R},{G},{B},{A})";

        /// <summary>
        /// Gets the position as a coordinate pair
        /// </summary>
        public (int X, int Y) Position => (X, Y);

        /// <summary>
        /// Checks if this pixel is transparent (alpha < 128)
        /// </summary>
        public bool IsTransparent => A < 128;

        /// <summary>
        /// Checks if this pixel is fully opaque (alpha = 255)
        /// </summary>
        public bool IsOpaque => A == 255;

        /// <summary>
        /// Checks if this pixel is pure black (R=G=B=0)
        /// </summary>
        public bool IsBlack => R == 0 && G == 0 && B == 0;

        /// <summary>
        /// Checks if this pixel is pure white (R=G=B=255)
        /// </summary>
        public bool IsWhite => R == 255 && G == 255 && B == 255;

        /// <summary>
        /// Creates a new pixel with the same color but different position
        /// </summary>
        /// <param name="x">New X coordinate</param>
        /// <param name="y">New Y coordinate</param>
        /// <returns>New pixel with updated position</returns>
        public Pixel WithPosition(int x, int y) => new Pixel(R, G, B, A, x, y);

        /// <summary>
        /// Creates a new pixel with the same position but different color
        /// </summary>
        /// <param name="r">New red component</param>
        /// <param name="g">New green component</param>
        /// <param name="b">New blue component</param>
        /// <param name="a">New alpha component</param>
        /// <returns>New pixel with updated color</returns>
        public Pixel WithColor(byte r, byte g, byte b, byte a = 255) => new Pixel(r, g, b, a, X, Y);

        public bool Equals(Pixel other)
        {
            return R == other.R && G == other.G && B == other.B && A == other.A && X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is Pixel other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(R, G, B, A, X, Y);
        }

        public static bool operator ==(Pixel left, Pixel right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Pixel left, Pixel right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"Pixel({X},{Y}) {RgbaString}";
        }
    }
}