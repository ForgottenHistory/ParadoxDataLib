using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using ParadoxDataLib.Core.DataManagers;
using ParadoxDataLib.Serialization;

namespace ParadoxDataLib.Tests
{
    public class CacheTests : IDisposable
    {
        private readonly string _testCacheDir;
        private readonly DataCache _dataCache;

        public CacheTests()
        {
            _testCacheDir = Path.Combine(Path.GetTempPath(), "ParadoxDataLib_Test_Cache_" + Guid.NewGuid().ToString("N")[..8]);
            _dataCache = new DataCache(_testCacheDir);
        }

        [Fact]
        public void CacheManager_Should_CreateCacheDirectory()
        {
            using var cacheManager = new CacheManager(_testCacheDir);
            Assert.True(Directory.Exists(_testCacheDir));
        }

        [Fact]
        public void CacheKey_Should_BeConsistent_ForSameFiles()
        {
            // Create test files
            var testDir = Path.Combine(Path.GetTempPath(), "cache_key_test_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(testDir);

            var file1 = Path.Combine(testDir, "test1.txt");
            var file2 = Path.Combine(testDir, "test2.txt");

            File.WriteAllText(file1, "Test content 1");
            File.WriteAllText(file2, "Test content 2");

            try
            {
                var key1 = CacheManager.CreateCacheKey(file1, file2);
                var key2 = CacheManager.CreateCacheKey(file1, file2);

                Assert.Equal(key1, key2);
                Assert.NotEmpty(key1);
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        [Fact]
        public void CacheKey_Should_BeDifferent_ForDifferentFiles()
        {
            // Create test files
            var testDir = Path.Combine(Path.GetTempPath(), "cache_key_test2_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(testDir);

            var file1 = Path.Combine(testDir, "test1.txt");
            var file2 = Path.Combine(testDir, "test2.txt");
            var file3 = Path.Combine(testDir, "test3.txt");

            File.WriteAllText(file1, "Test content 1");
            File.WriteAllText(file2, "Test content 2");
            File.WriteAllText(file3, "Test content 3");

            try
            {
                var key1 = CacheManager.CreateCacheKey(file1, file2);
                var key2 = CacheManager.CreateCacheKey(file1, file3);

                Assert.NotEqual(key1, key2);
            }
            finally
            {
                Directory.Delete(testDir, true);
            }
        }

        [Fact]
        public void CacheManager_Should_HandleMissingFiles()
        {
            var nonExistentFile = Path.Combine(Path.GetTempPath(), "does_not_exist_" + Guid.NewGuid().ToString("N") + ".txt");
            var cacheKey = CacheManager.CreateCacheKey(nonExistentFile);

            // Should not throw and should return a valid (though empty-based) cache key
            Assert.NotNull(cacheKey);
        }

        [Fact]
        public void CacheStatistics_Should_ReturnValidData()
        {
            using var cacheManager = new CacheManager(_testCacheDir);
            var stats = cacheManager.GetStatistics();

            Assert.NotNull(stats);
            Assert.Equal(_testCacheDir, stats.CacheDirectory);
            Assert.True(stats.TotalEntries >= 0);
            Assert.True(stats.ValidEntries >= 0);
            Assert.True(stats.TotalSizeBytes >= 0);
            Assert.NotNull(stats.TotalSizeFormatted);
        }

        [Fact]
        public void ClearCache_Should_RemoveAllEntries()
        {
            using var cacheManager = new CacheManager(_testCacheDir);

            // Clear cache (should work even when empty)
            cacheManager.ClearCache();

            var stats = cacheManager.GetStatistics();
            Assert.Equal(0, stats.TotalEntries);
            Assert.Equal(0, stats.ValidEntries);
            Assert.Equal(0, stats.TotalSizeBytes);
        }

        [Fact]
        public async Task DataCache_Should_HandleNonExistentDirectories()
        {
            var nonExistentDir = Path.Combine(Path.GetTempPath(), "does_not_exist_" + Guid.NewGuid().ToString("N"));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _dataCache.LoadGameDataAsync(nonExistentDir, nonExistentDir));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _dataCache.LoadProvincesAsync(nonExistentDir));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                _dataCache.LoadCountriesAsync(nonExistentDir));
        }

        [Fact]
        public async Task DataCache_Should_HandleEmptyDirectories()
        {
            // Create empty test directories
            var emptyProvincesDir = Path.Combine(Path.GetTempPath(), "empty_provinces_" + Guid.NewGuid().ToString("N")[..8]);
            var emptyCountriesDir = Path.Combine(Path.GetTempPath(), "empty_countries_" + Guid.NewGuid().ToString("N")[..8]);

            Directory.CreateDirectory(emptyProvincesDir);
            Directory.CreateDirectory(emptyCountriesDir);

            try
            {
                var (provinces, countries) = await _dataCache.LoadGameDataAsync(emptyProvincesDir, emptyCountriesDir);

                Assert.NotNull(provinces);
                Assert.NotNull(countries);
                Assert.Empty(provinces);
                Assert.Empty(countries);
            }
            finally
            {
                Directory.Delete(emptyProvincesDir);
                Directory.Delete(emptyCountriesDir);
            }
        }

        [Fact]
        public void DataCache_CacheOperations_Should_Work()
        {
            var stats = _dataCache.GetCacheStatistics();
            Assert.NotNull(stats);

            // These should not throw
            _dataCache.ClearCache();
            _dataCache.CleanupOldCache(TimeSpan.FromDays(1));
        }

        public void Dispose()
        {
            _dataCache?.Dispose();

            if (Directory.Exists(_testCacheDir))
            {
                try
                {
                    Directory.Delete(_testCacheDir, true);
                }
                catch
                {
                    // Ignore cleanup errors in tests
                }
            }
        }
    }
}