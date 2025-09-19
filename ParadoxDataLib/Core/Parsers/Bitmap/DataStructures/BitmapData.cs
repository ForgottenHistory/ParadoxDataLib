using System;
using System.Collections.Generic;
using System.Linq;

namespace ParadoxDataLib.Core.Parsers.Bitmap.DataStructures
{
    /// <summary>
    /// Generic container for parsed bitmap data with spatial indexing capabilities.
    /// Provides efficient storage and lookup for bitmap-derived data with automatic
    /// optimization between dense array and sparse dictionary storage.
    /// </summary>
    /// <typeparam name="T">Type of data stored for each pixel</typeparam>
    public class BitmapData<T>
    {
        private readonly Dictionary<Point, T> _sparseData;
        private T[,] _denseArray;
        private readonly int _width;
        private readonly int _height;
        private bool _useDenseStorage;
        private int _pixelCount;
        private const double DENSITY_THRESHOLD = 0.3; // Switch to dense storage at 30% density

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
        public int DataPixelCount => _pixelCount;

        /// <summary>
        /// Gets all stored data points
        /// </summary>
        public IEnumerable<KeyValuePair<Point, T>> Data => GetAllData();

        /// <summary>
        /// Gets all unique data values
        /// </summary>
        public IEnumerable<T> Values => GetAllData().Select(kvp => kvp.Value).Distinct();

        /// <summary>
        /// Gets all points that have data
        /// </summary>
        public IEnumerable<Point> Points => GetAllData().Select(kvp => kvp.Key);

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
            _sparseData = capacity > 0 ? new Dictionary<Point, T>(capacity) : new Dictionary<Point, T>();
            _useDenseStorage = false;
            _pixelCount = 0;
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
            if (_useDenseStorage)
            {
                if (point.X >= 0 && point.X < _width && point.Y >= 0 && point.Y < _height)
                    return _denseArray[point.X, point.Y];
                return default(T);
            }
            else
            {
                return _sparseData.TryGetValue(point, out var value) ? value : default(T);
            }
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
            if (_useDenseStorage)
            {
                if (point.X >= 0 && point.X < _width && point.Y >= 0 && point.Y < _height)
                {
                    value = _denseArray[point.X, point.Y];
                    return !EqualityComparer<T>.Default.Equals(value, default(T));
                }
                value = default(T);
                return false;
            }
            else
            {
                return _sparseData.TryGetValue(point, out value);
            }
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

            if (_useDenseStorage)
            {
                var currentValue = _denseArray[point.X, point.Y];
                var isCurrentDefault = EqualityComparer<T>.Default.Equals(currentValue, default(T));
                var isNewDefault = EqualityComparer<T>.Default.Equals(value, default(T));

                _denseArray[point.X, point.Y] = value;

                // Update pixel count
                if (isCurrentDefault && !isNewDefault)
                    _pixelCount++;
                else if (!isCurrentDefault && isNewDefault)
                    _pixelCount--;
            }
            else
            {
                var existed = _sparseData.ContainsKey(point);
                _sparseData[point] = value;

                // Update pixel count
                if (!existed)
                    _pixelCount++;

                // Check if we should switch to dense storage
                var density = (double)_pixelCount / TotalPixels;
                if (density >= DENSITY_THRESHOLD)
                {
                    SwitchToDenseStorage();
                }
            }
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
            return TryGetData(point, out _);
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
            if (!IsValidCoordinate(point))
                return false;

            if (_useDenseStorage)
            {
                var currentValue = _denseArray[point.X, point.Y];
                var hasData = !EqualityComparer<T>.Default.Equals(currentValue, default(T));
                if (hasData)
                {
                    _denseArray[point.X, point.Y] = default(T);
                    _pixelCount--;
                    return true;
                }
                return false;
            }
            else
            {
                var removed = _sparseData.Remove(point);
                if (removed)
                    _pixelCount--;
                return removed;
            }
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
                    if (TryGetData(point, out var value))
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
            foreach (var kvp in GetAllData())
            {
                counts[kvp.Value] = counts.GetValueOrDefault(kvp.Value, 0) + 1;
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
            return GetAllData().Where(kvp => EqualityComparer<T>.Default.Equals(kvp.Value, value))
                              .Select(kvp => kvp.Key)
                              .ToList();
        }

        /// <summary>
        /// Clears all data from the bitmap
        /// </summary>
        public void Clear()
        {
            if (_useDenseStorage)
            {
                _denseArray = new T[_width, _height];
            }
            else
            {
                _sparseData.Clear();
            }
            _pixelCount = 0;
        }

        /// <summary>
        /// Creates a copy of this bitmap data
        /// </summary>
        /// <returns>New bitmap data instance with copied data</returns>
        public BitmapData<T> Clone()
        {
            var clone = new BitmapData<T>(_width, _height, _pixelCount);
            foreach (var kvp in GetAllData())
            {
                clone.SetData(kvp.Key, kvp.Value);
            }
            return clone;
        }

        /// <summary>
        /// Switches from sparse dictionary storage to dense array storage
        /// </summary>
        private void SwitchToDenseStorage()
        {
            if (_useDenseStorage) return;

            _denseArray = new T[_width, _height];

            // Copy data from sparse to dense storage
            foreach (var kvp in _sparseData)
            {
                _denseArray[kvp.Key.X, kvp.Key.Y] = kvp.Value;
            }

            _sparseData.Clear();
            _useDenseStorage = true;
        }

        /// <summary>
        /// Gets all data points from either storage type
        /// </summary>
        private IEnumerable<KeyValuePair<Point, T>> GetAllData()
        {
            if (_useDenseStorage)
            {
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        var value = _denseArray[x, y];
                        if (!EqualityComparer<T>.Default.Equals(value, default(T)))
                        {
                            yield return new KeyValuePair<Point, T>(new Point(x, y), value);
                        }
                    }
                }
            }
            else
            {
                foreach (var kvp in _sparseData)
                {
                    yield return kvp;
                }
            }
        }

        public override string ToString()
        {
            var storageType = _useDenseStorage ? "Dense" : "Sparse";
            return $"BitmapData<{typeof(T).Name}>: {Width}x{Height}, {DataPixelCount}/{TotalPixels} pixels ({storageType})";
        }
    }
}