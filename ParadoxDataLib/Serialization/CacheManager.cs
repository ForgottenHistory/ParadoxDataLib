using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ParadoxDataLib.Core.DataModels;

namespace ParadoxDataLib.Serialization
{
    /// <summary>
    /// High-performance cache manager with automatic binary serialization
    /// </summary>
    public class CacheManager : IDisposable
    {
        private readonly string _cacheDirectory;
        private readonly Dictionary<string, CacheEntry> _cacheIndex;
        private readonly object _lockObject;
        private bool _disposed;

        public CacheManager(string cacheDirectory = "cache")
        {
            _cacheDirectory = cacheDirectory ?? throw new ArgumentNullException(nameof(cacheDirectory));
            _cacheIndex = new Dictionary<string, CacheEntry>();
            _lockObject = new object();

            EnsureCacheDirectoryExists();
            LoadCacheIndex();
        }

        /// <summary>
        /// Attempts to load cached data. Returns true if cache hit, false if cache miss.
        /// </summary>
        public bool TryLoadFromCache<T>(string cacheKey, out T data, Func<string>? hashProvider = null) where T : class
        {
            data = default!;
            CacheEntry? entry = null;
            string cacheFilePath = "";

            lock (_lockObject)
            {
                if (!_cacheIndex.TryGetValue(cacheKey, out entry))
                    return false;

                // Check if cache is still valid (file exists and hash matches if provided)
                cacheFilePath = Path.Combine(_cacheDirectory, entry.FileName);
                if (!File.Exists(cacheFilePath))
                {
                    _cacheIndex.Remove(cacheKey);
                    return false;
                }

                // Validate hash if provided
                if (hashProvider != null)
                {
                    var currentHash = hashProvider();
                    if (entry.SourceHash != currentHash)
                    {
                        // Cache is stale, remove it
                        _cacheIndex.Remove(cacheKey);
                        File.Delete(cacheFilePath);
                        return false;
                    }
                }

                // Check cache age
                if (DateTime.UtcNow - entry.CreatedUtc > TimeSpan.FromDays(7))
                {
                    // Cache is too old, remove it
                    _cacheIndex.Remove(cacheKey);
                    File.Delete(cacheFilePath);
                    return false;
                }
            }

            try
            {
                // Load from cache
                using (var fileStream = File.OpenRead(cacheFilePath))
                using (var reader = new BinaryDataReader(fileStream))
                {
                    if (typeof(T) == typeof(Tuple<List<ProvinceData>, List<CountryData>>))
                    {
                        var gameData = reader.ReadGameData();
                        data = (T)(object)Tuple.Create(gameData.Provinces, gameData.Countries);
                        return true;
                    }
                    else if (typeof(T) == typeof(List<ProvinceData>))
                    {
                        var gameData = reader.ReadGameData();
                        data = (T)(object)gameData.Provinces;
                        return true;
                    }
                    else if (typeof(T) == typeof(List<CountryData>))
                    {
                        var gameData = reader.ReadGameData();
                        data = (T)(object)gameData.Countries;
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // Cache file is corrupted, remove it
                lock (_lockObject)
                {
                    _cacheIndex.Remove(cacheKey);
                }
                return false;
            }

            return false;
        }

        /// <summary>
        /// Saves data to cache with automatic binary serialization
        /// </summary>
        public async Task SaveToCacheAsync<T>(string cacheKey, T data, Func<string>? hashProvider = null) where T : class
        {
            var fileName = GenerateCacheFileName(cacheKey);
            var filePath = Path.Combine(_cacheDirectory, fileName);

            // Write to temporary file first to avoid corruption
            var tempFilePath = filePath + ".tmp";

            try
            {
                using (var fileStream = File.Create(tempFilePath))
                using (var writer = new BinaryDataWriter(fileStream, useCompression: true))
                {
                    if (data is Tuple<List<ProvinceData>, List<CountryData>> gameData)
                    {
                        await Task.Run(() => writer.WriteGameData(gameData.Item1, gameData.Item2));
                    }
                    else if (data is List<ProvinceData> provinces)
                    {
                        await Task.Run(() => writer.WriteGameData(provinces, new List<CountryData>()));
                    }
                    else if (data is List<CountryData> countries)
                    {
                        await Task.Run(() => writer.WriteGameData(new List<ProvinceData>(), countries));
                    }
                    else
                    {
                        throw new NotSupportedException($"Cache type {typeof(T)} is not supported");
                    }
                }

                // Atomically replace old cache file
                if (File.Exists(filePath))
                    File.Delete(filePath);
                File.Move(tempFilePath, filePath);

                // Update cache index
                lock (_lockObject)
                {
                    _cacheIndex[cacheKey] = new CacheEntry
                    {
                        FileName = fileName,
                        CreatedUtc = DateTime.UtcNow,
                        SourceHash = hashProvider?.Invoke(),
                        SizeBytes = new FileInfo(filePath).Length
                    };
                }

                SaveCacheIndex();
            }
            catch
            {
                // Clean up temporary file if it exists
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
                throw;
            }
        }

        /// <summary>
        /// Gets or creates cached data with automatic serialization
        /// </summary>
        public async Task<T> GetOrCreateAsync<T>(string cacheKey, Func<Task<T>> factory, Func<string>? hashProvider = null) where T : class
        {
            // Try to load from cache first
            if (TryLoadFromCache<T>(cacheKey, out var cachedData, hashProvider))
            {
                return cachedData;
            }

            // Cache miss - create new data
            var newData = await factory();

            // Save to cache for next time
            await SaveToCacheAsync(cacheKey, newData, hashProvider);

            return newData;
        }

        /// <summary>
        /// Clears all cache data
        /// </summary>
        public void ClearCache()
        {
            lock (_lockObject)
            {
                foreach (var entry in _cacheIndex.Values)
                {
                    var filePath = Path.Combine(_cacheDirectory, entry.FileName);
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }

                _cacheIndex.Clear();
                SaveCacheIndex();
            }
        }

        /// <summary>
        /// Gets cache statistics
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            lock (_lockObject)
            {
                long totalSize = 0;
                int validEntries = 0;

                foreach (var entry in _cacheIndex.Values)
                {
                    var filePath = Path.Combine(_cacheDirectory, entry.FileName);
                    if (File.Exists(filePath))
                    {
                        totalSize += entry.SizeBytes;
                        validEntries++;
                    }
                }

                return new CacheStatistics
                {
                    TotalEntries = _cacheIndex.Count,
                    ValidEntries = validEntries,
                    TotalSizeBytes = totalSize,
                    CacheDirectory = _cacheDirectory
                };
            }
        }

        /// <summary>
        /// Removes expired cache entries
        /// </summary>
        public void CleanupExpiredEntries(TimeSpan maxAge)
        {
            lock (_lockObject)
            {
                var expiredKeys = new List<string>();
                var cutoffTime = DateTime.UtcNow - maxAge;

                foreach (var kvp in _cacheIndex)
                {
                    if (kvp.Value.CreatedUtc < cutoffTime)
                    {
                        expiredKeys.Add(kvp.Key);

                        var filePath = Path.Combine(_cacheDirectory, kvp.Value.FileName);
                        if (File.Exists(filePath))
                            File.Delete(filePath);
                    }
                }

                foreach (var key in expiredKeys)
                {
                    _cacheIndex.Remove(key);
                }

                if (expiredKeys.Count > 0)
                    SaveCacheIndex();
            }
        }

        /// <summary>
        /// Creates a cache key for a collection of files
        /// </summary>
        public static string CreateCacheKey(params string[] filePaths)
        {
            var sb = new StringBuilder();
            foreach (var path in filePaths)
            {
                if (File.Exists(path))
                {
                    sb.Append(path);
                    sb.Append("|");
                    sb.Append(File.GetLastWriteTimeUtc(path).Ticks);
                    sb.Append("|");
                    sb.Append(new FileInfo(path).Length);
                    sb.Append(";");
                }
            }

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                return Convert.ToBase64String(hash).Replace('/', '_').Replace('+', '-').TrimEnd('=');
            }
        }

        private void EnsureCacheDirectoryExists()
        {
            if (!Directory.Exists(_cacheDirectory))
                Directory.CreateDirectory(_cacheDirectory);
        }

        private void LoadCacheIndex()
        {
            var indexPath = Path.Combine(_cacheDirectory, "cache.index");
            if (!File.Exists(indexPath))
                return;

            try
            {
                using (var reader = new BinaryReader(File.OpenRead(indexPath)))
                {
                    var count = reader.ReadInt32();
                    for (int i = 0; i < count; i++)
                    {
                        var key = reader.ReadString();
                        var entry = new CacheEntry
                        {
                            FileName = reader.ReadString(),
                            CreatedUtc = DateTime.FromBinary(reader.ReadInt64()),
                            SourceHash = reader.ReadString(),
                            SizeBytes = reader.ReadInt64()
                        };

                        // Only add if the file still exists
                        if (File.Exists(Path.Combine(_cacheDirectory, entry.FileName)))
                            _cacheIndex[key] = entry;
                    }
                }
            }
            catch
            {
                // If index is corrupted, start fresh
                _cacheIndex.Clear();
            }
        }

        private void SaveCacheIndex()
        {
            var indexPath = Path.Combine(_cacheDirectory, "cache.index");
            var tempPath = indexPath + ".tmp";

            try
            {
                using (var writer = new BinaryWriter(File.Create(tempPath)))
                {
                    writer.Write(_cacheIndex.Count);
                    foreach (var kvp in _cacheIndex)
                    {
                        writer.Write(kvp.Key);
                        writer.Write(kvp.Value.FileName);
                        writer.Write(kvp.Value.CreatedUtc.ToBinary());
                        writer.Write(kvp.Value.SourceHash ?? "");
                        writer.Write(kvp.Value.SizeBytes);
                    }
                }

                if (File.Exists(indexPath))
                    File.Delete(indexPath);
                File.Move(tempPath, indexPath);
            }
            catch
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
                throw;
            }
        }

        private string GenerateCacheFileName(string cacheKey)
        {
            // Create a safe filename from cache key
            return $"{cacheKey.Substring(0, Math.Min(32, cacheKey.Length))}.cache";
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                SaveCacheIndex();
                _disposed = true;
            }
        }

        private class CacheEntry
        {
            public string FileName { get; set; } = "";
            public DateTime CreatedUtc { get; set; }
            public string? SourceHash { get; set; }
            public long SizeBytes { get; set; }
        }
    }

    /// <summary>
    /// Cache statistics information
    /// </summary>
    public class CacheStatistics
    {
        public int TotalEntries { get; set; }
        public int ValidEntries { get; set; }
        public long TotalSizeBytes { get; set; }
        public string CacheDirectory { get; set; } = "";

        public string TotalSizeFormatted =>
            TotalSizeBytes < 1024 ? $"{TotalSizeBytes} B" :
            TotalSizeBytes < 1024 * 1024 ? $"{TotalSizeBytes / 1024.0:F1} KB" :
            TotalSizeBytes < 1024 * 1024 * 1024 ? $"{TotalSizeBytes / (1024.0 * 1024):F1} MB" :
            $"{TotalSizeBytes / (1024.0 * 1024 * 1024):F1} GB";
    }
}