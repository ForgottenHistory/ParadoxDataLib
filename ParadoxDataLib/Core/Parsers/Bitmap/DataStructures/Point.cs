using System;

namespace ParadoxDataLib.Core.Parsers.Bitmap.DataStructures
{
    /// <summary>
    /// Represents a 2D coordinate point with integer values.
    /// Optimized for bitmap coordinate operations and spatial calculations.
    /// </summary>
    public readonly struct Point : IEquatable<Point>, IComparable<Point>
    {
        /// <summary>
        /// X coordinate
        /// </summary>
        public int X { get; }

        /// <summary>
        /// Y coordinate
        /// </summary>
        public int Y { get; }

        /// <summary>
        /// Creates a new point with the specified coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Origin point (0, 0)
        /// </summary>
        public static readonly Point Zero = new Point(0, 0);

        /// <summary>
        /// Gets the distance from the origin (0, 0)
        /// </summary>
        public double DistanceFromOrigin => Math.Sqrt(X * X + Y * Y);

        /// <summary>
        /// Gets the Manhattan distance from the origin (|X| + |Y|)
        /// </summary>
        public int ManhattanDistanceFromOrigin => Math.Abs(X) + Math.Abs(Y);

        /// <summary>
        /// Calculates the Euclidean distance to another point
        /// </summary>
        /// <param name="other">The other point</param>
        /// <returns>Distance between the points</returns>
        public double DistanceTo(Point other)
        {
            var dx = X - other.X;
            var dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Calculates the Manhattan distance to another point
        /// </summary>
        /// <param name="other">The other point</param>
        /// <returns>Manhattan distance between the points</returns>
        public int ManhattanDistanceTo(Point other)
        {
            return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
        }

        /// <summary>
        /// Checks if this point is within the specified bounds (inclusive)
        /// </summary>
        /// <param name="minX">Minimum X coordinate</param>
        /// <param name="minY">Minimum Y coordinate</param>
        /// <param name="maxX">Maximum X coordinate</param>
        /// <param name="maxY">Maximum Y coordinate</param>
        /// <returns>True if the point is within bounds</returns>
        public bool IsWithinBounds(int minX, int minY, int maxX, int maxY)
        {
            return X >= minX && X <= maxX && Y >= minY && Y <= maxY;
        }

        /// <summary>
        /// Checks if this point is within the specified rectangle bounds
        /// </summary>
        /// <param name="width">Width of the rectangle</param>
        /// <param name="height">Height of the rectangle</param>
        /// <returns>True if the point is within the rectangle starting from (0,0)</returns>
        public bool IsWithinBounds(int width, int height)
        {
            return X >= 0 && X < width && Y >= 0 && Y < height;
        }

        /// <summary>
        /// Creates a new point with an offset applied
        /// </summary>
        /// <param name="dx">X offset</param>
        /// <param name="dy">Y offset</param>
        /// <returns>New point with offset applied</returns>
        public Point Offset(int dx, int dy) => new Point(X + dx, Y + dy);

        /// <summary>
        /// Creates a new point with another point's offset applied
        /// </summary>
        /// <param name="offset">Point containing offset values</param>
        /// <returns>New point with offset applied</returns>
        public Point Offset(Point offset) => new Point(X + offset.X, Y + offset.Y);

        /// <summary>
        /// Gets the 4-connected neighbors (North, South, East, West)
        /// </summary>
        /// <returns>Array of neighboring points</returns>
        public Point[] GetNeighbors4()
        {
            return new[]
            {
                new Point(X, Y - 1), // North
                new Point(X, Y + 1), // South
                new Point(X + 1, Y), // East
                new Point(X - 1, Y)  // West
            };
        }

        /// <summary>
        /// Gets the 8-connected neighbors (including diagonals)
        /// </summary>
        /// <returns>Array of neighboring points</returns>
        public Point[] GetNeighbors8()
        {
            return new[]
            {
                new Point(X - 1, Y - 1), // Northwest
                new Point(X, Y - 1),     // North
                new Point(X + 1, Y - 1), // Northeast
                new Point(X + 1, Y),     // East
                new Point(X + 1, Y + 1), // Southeast
                new Point(X, Y + 1),     // South
                new Point(X - 1, Y + 1), // Southwest
                new Point(X - 1, Y)      // West
            };
        }

        /// <summary>
        /// Addition operator for point offset
        /// </summary>
        public static Point operator +(Point left, Point right)
        {
            return new Point(left.X + right.X, left.Y + right.Y);
        }

        /// <summary>
        /// Subtraction operator for point offset
        /// </summary>
        public static Point operator -(Point left, Point right)
        {
            return new Point(left.X - right.X, left.Y - right.Y);
        }

        /// <summary>
        /// Scalar multiplication operator
        /// </summary>
        public static Point operator *(Point point, int scalar)
        {
            return new Point(point.X * scalar, point.Y * scalar);
        }

        /// <summary>
        /// Scalar division operator
        /// </summary>
        public static Point operator /(Point point, int divisor)
        {
            return new Point(point.X / divisor, point.Y / divisor);
        }

        public bool Equals(Point other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is Point other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(Point left, Point right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Point left, Point right)
        {
            return !left.Equals(right);
        }

        public int CompareTo(Point other)
        {
            var xComparison = X.CompareTo(other.X);
            return xComparison != 0 ? xComparison : Y.CompareTo(other.Y);
        }

        /// <summary>
        /// Converts to a tuple
        /// </summary>
        public (int X, int Y) ToTuple() => (X, Y);

        /// <summary>
        /// Converts from a tuple
        /// </summary>
        public static Point FromTuple((int X, int Y) tuple) => new Point(tuple.X, tuple.Y);

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}