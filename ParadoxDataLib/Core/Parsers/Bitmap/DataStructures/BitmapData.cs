using System;
using System.Collections.Generic;
using System.Linq;

namespace ParadoxDataLib.Core.Parsers.Bitmap.DataStructures
{
    /// <summary>
    /// Generic container for parsed bitmap data with spatial indexing capabilities.
    /// Provides efficient storage and lookup for bitmap-derived data.
    /// </summary>
    /// <typeparam name="T">Type of data stored for each pixel</typeparam>
    public class BitmapData<T>
    {
        private readonly Dictionary<Point, T> _data;
        private readonly int _width;
        private readonly int _height;

        /// <summary>
        /// Width of the bitmap in pixels
        /// </summary>
        public int Width => _width;

        /// <summary>
        /// Height of the bitmap in pixels
        /// </summary>
        public int Height => _height;

        /// <summary>
        /// Total number of pixels in the bitmap
        /// </summary>
        public int TotalPixels => _width * _height;

        /// <summary>
        /// Number of pixels that have data stored
        /// </summary>
        public int DataPixelCount => _data.Count;

        /// <summary>
        /// Gets all stored data points
        /// </summary>
        public IEnumerable<KeyValuePair<Point, T>> Data => _data;

        /// <summary>
        /// Gets all unique data values
        /// </summary>
        public IEnumerable<T> Values => _data.Values.Distinct();

        /// <summary>
        /// Gets all points that have data
        /// </summary>
        public IEnumerable<Point> Points => _data.Keys;

        /// <summary>
        /// Creates a new bitmap data container
        /// </summary>
        /// <param name="width">Width of the bitmap</param>
        /// <param name="height">Height of the bitmap</param>
        /// <param name="capacity">Initial capacity for the data dictionary</param>
        public BitmapData(int width, int height, int capacity = 0)
        {
            if (width <= 0) throw new ArgumentException("Width must be positive", nameof(width));
            if (height <= 0) throw new ArgumentException("Height must be positive", nameof(height));

            _width = width;
            _height = height;
            _data = capacity > 0 ? new Dictionary<Point, T>(capacity) : new Dictionary<Point, T>();
        }

        /// <summary>
        /// Gets or sets data at the specified coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Data value at the coordinates</returns>
        public T this[int x, int y]
        {
            get => GetData(x, y);
            set => SetData(x, y, value);
        }

        /// <summary>
        /// Gets or sets data at the specified point
        /// </summary>
        /// <param name="point">Point coordinates</param>
        /// <returns>Data value at the point</returns>
        public T this[Point point]
        {
            get => GetData(point);
            set => SetData(point, value);
        }

        /// <summary>
        /// Gets data at the specified coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Data value, or default(T) if no data exists</returns>
        public T GetData(int x, int y)
        {
            return GetData(new Point(x, y));
        }

        /// <summary>
        /// Gets data at the specified point
        /// </summary>
        /// <param name="point">Point coordinates</param>
        /// <returns>Data value, or default(T) if no data exists</returns>
        public T GetData(Point point)
        {
            return _data.TryGetValue(point, out var value) ? value : default(T);
        }

        /// <summary>
        /// Tries to get data at the specified coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="value">Output value if found</param>
        /// <returns>True if data exists at the coordinates</returns>
        public bool TryGetData(int x, int y, out T value)
        {
            return TryGetData(new Point(x, y), out value);
        }

        /// <summary>
        /// Tries to get data at the specified point
        /// </summary>
        /// <param name="point">Point coordinates</param>
        /// <param name="value">Output value if found</param>
        /// <returns>True if data exists at the point</returns>
        public bool TryGetData(Point point, out T value)
        {
            return _data.TryGetValue(point, out value);
        }

        /// <summary>
        /// Sets data at the specified coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="value">Data value to store</param>
        public void SetData(int x, int y, T value)
        {
            SetData(new Point(x, y), value);
        }

        /// <summary>
        /// Sets data at the specified point
        /// </summary>
        /// <param name="point">Point coordinates</param>
        /// <param name="value">Data value to store</param>
        public void SetData(Point point, T value)
        {
            if (!IsValidCoordinate(point))
                throw new ArgumentOutOfRangeException(nameof(point), $"Point {point} is outside bitmap bounds ({Width}x{Height})");

            _data[point] = value;
        }

        /// <summary>
        /// Checks if the specified coordinates are within bitmap bounds
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>True if coordinates are valid</returns>
        public bool IsValidCoordinate(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        /// <summary>
        /// Checks if the specified point is within bitmap bounds
        /// </summary>
        /// <param name="point">Point to check</param>
        /// <returns>True if point is valid</returns>
        public bool IsValidCoordinate(Point point)
        {
            return IsValidCoordinate(point.X, point.Y);
        }

        /// <summary>
        /// Checks if data exists at the specified coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>True if data exists</returns>
        public bool HasData(int x, int y)
        {
            return HasData(new Point(x, y));
        }

        /// <summary>
        /// Checks if data exists at the specified point
        /// </summary>
        /// <param name="point">Point to check</param>
        /// <returns>True if data exists</returns>
        public bool HasData(Point point)
        {
            return _data.ContainsKey(point);
        }

        /// <summary>
        /// Removes data at the specified coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>True if data was removed</returns>
        public bool RemoveData(int x, int y)
        {
            return RemoveData(new Point(x, y));
        }

        /// <summary>
        /// Removes data at the specified point
        /// </summary>
        /// <param name="point">Point coordinates</param>
        /// <returns>True if data was removed</returns>
        public bool RemoveData(Point point)
        {
            return _data.Remove(point);
        }

        /// <summary>
        /// Gets all data within the specified rectangular region
        /// </summary>
        /// <param name="x">Starting X coordinate</param>
        /// <param name="y">Starting Y coordinate</param>
        /// <param name="width">Width of the region</param>
        /// <param name="height">Height of the region</param>
        /// <returns>Enumerable of data within the region</returns>
        public IEnumerable<KeyValuePair<Point, T>> GetRegion(int x, int y, int width, int height)
        {
            var endX = Math.Min(x + width, _width);
            var endY = Math.Min(y + height, _height);

            for (var py = Math.Max(0, y); py < endY; py++)
            {
                for (var px = Math.Max(0, x); px < endX; px++)
                {
                    var point = new Point(px, py);
                    if (_data.TryGetValue(point, out var value))
                    {
                        yield return new KeyValuePair<Point, T>(point, value);
                    }
                }
            }
        }

        /// <summary>
        /// Gets data values grouped by their occurrence count
        /// </summary>
        /// <returns>Dictionary mapping values to their occurrence counts</returns>
        public Dictionary<T, int> GetValueCounts()
        {
            var counts = new Dictionary<T, int>();
            foreach (var value in _data.Values)
            {
                counts[value] = counts.GetValueOrDefault(value, 0) + 1;
            }
            return counts;
        }

        /// <summary>
        /// Finds all points that contain the specified value
        /// </summary>
        /// <param name="value">Value to search for</param>
        /// <returns>List of points containing the value</returns>
        public List<Point> FindPointsWithValue(T value)
        {
            return _data.Where(kvp => EqualityComparer<T>.Default.Equals(kvp.Value, value))
                       .Select(kvp => kvp.Key)
                       .ToList();
        }

        /// <summary>
        /// Clears all data from the bitmap
        /// </summary>
        public void Clear()
        {
            _data.Clear();
        }

        /// <summary>
        /// Creates a copy of this bitmap data
        /// </summary>
        /// <returns>New bitmap data instance with copied data</returns>
        public BitmapData<T> Clone()
        {
            var clone = new BitmapData<T>(_width, _height, _data.Count);
            foreach (var kvp in _data)
            {
                clone._data[kvp.Key] = kvp.Value;
            }
            return clone;
        }

        public override string ToString()
        {
            return $"BitmapData<{typeof(T).Name}>: {Width}x{Height}, {DataPixelCount}/{TotalPixels} pixels with data";
        }
    }
}