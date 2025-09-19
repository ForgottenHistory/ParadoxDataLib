using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ParadoxDataLib.Core.Common
{
    /// <summary>
    /// String interning pool for memory optimization with reference counting
    /// Optimized for Paradox game data where many strings are repeated (cultures, religions, etc.)
    /// </summary>
    public class StringPool
    {
        private readonly ConcurrentDictionary<string, InternedString> _pool;
        private readonly object _lockObject = new object();
        private int _nextId = 1;

        public StringPool()
        {
            _pool = new ConcurrentDictionary<string, InternedString>();
        }

        /// <summary>
        /// Interns a string and returns the shared instance
        /// </summary>
        public string Intern(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            return _pool.AddOrUpdate(value,
                key => new InternedString(key, _nextId++, 1),
                (key, existing) =>
                {
                    existing.IncrementRefCount();
                    return existing;
                }).Value;
        }

        /// <summary>
        /// Releases a reference to an interned string
        /// </summary>
        public void Release(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            if (_pool.TryGetValue(value, out var internedString))
            {
                if (internedString.DecrementRefCount() <= 0)
                {
                    // Remove from pool when no more references
                    _pool.TryRemove(value, out _);
                }
            }
        }

        /// <summary>
        /// Gets the ID for an interned string (useful for fast lookups)
        /// </summary>
        public int? GetId(string value)
        {
            return _pool.TryGetValue(value, out var internedString) ? (int?)internedString.Id : null;
        }

        /// <summary>
        /// Gets statistics about the string pool
        /// </summary>
        public StringPoolStatistics GetStatistics()
        {
            var totalStrings = 0;
            var totalReferences = 0;
            var totalMemoryEstimate = 0L;

            foreach (var kvp in _pool)
            {
                totalStrings++;
                totalReferences += kvp.Value.RefCount;
                totalMemoryEstimate += kvp.Key.Length * 2; // rough estimate for UTF-16
            }

            return new StringPoolStatistics
            {
                UniqueStrings = totalStrings,
                TotalReferences = totalReferences,
                EstimatedMemoryBytes = totalMemoryEstimate,
                MemorySavedEstimate = (totalReferences - totalStrings) * 50 // rough estimate
            };
        }

        /// <summary>
        /// Clears all interned strings (use with caution)
        /// </summary>
        public void Clear()
        {
            _pool.Clear();
        }

        /// <summary>
        /// Gets all interned strings (for debugging/serialization)
        /// </summary>
        public IReadOnlyDictionary<string, int> GetAll()
        {
            var result = new Dictionary<string, int>();
            foreach (var kvp in _pool)
            {
                result[kvp.Key] = kvp.Value.Id;
            }
            return result;
        }
    }

    internal class InternedString
    {
        public string Value { get; }
        public int Id { get; }
        public int RefCount { get; private set; }
        private readonly object _refCountLock = new object();

        public InternedString(string value, int id, int initialRefCount = 1)
        {
            Value = value;
            Id = id;
            RefCount = initialRefCount;
        }

        public void IncrementRefCount()
        {
            lock (_refCountLock)
            {
                RefCount++;
            }
        }

        public int DecrementRefCount()
        {
            lock (_refCountLock)
            {
                return --RefCount;
            }
        }
    }

    public class StringPoolStatistics
    {
        public int UniqueStrings { get; set; }
        public int TotalReferences { get; set; }
        public long EstimatedMemoryBytes { get; set; }
        public long MemorySavedEstimate { get; set; }
    }

    /// <summary>
    /// Global string pool for common Paradox game strings
    /// </summary>
    public static class GlobalStringPool
    {
        private static readonly StringPool _instance = new StringPool();

        public static string Intern(string value) => _instance.Intern(value);
        public static void Release(string value) => _instance.Release(value);
        public static int? GetId(string value) => _instance.GetId(value);
        public static StringPoolStatistics GetStatistics() => _instance.GetStatistics();
        public static void Clear() => _instance.Clear();
        public static IReadOnlyDictionary<string, int> GetAll() => _instance.GetAll();

        // Pre-defined common strings for Paradox games
        public static class CommonStrings
        {
            public static readonly string Owner = Intern("owner");
            public static readonly string Controller = Intern("controller");
            public static readonly string Culture = Intern("culture");
            public static readonly string Religion = Intern("religion");
            public static readonly string Capital = Intern("capital");
            public static readonly string Government = Intern("government");
            public static readonly string BaseTax = Intern("base_tax");
            public static readonly string BaseProduction = Intern("base_production");
            public static readonly string BaseManpower = Intern("base_manpower");
            public static readonly string TradeGoods = Intern("trade_goods");
            public static readonly string IsCity = Intern("is_city");
            public static readonly string Hre = Intern("hre");
            public static readonly string AddCore = Intern("add_core");
            public static readonly string RemoveCore = Intern("remove_core");
            public static readonly string DiscoveredBy = Intern("discovered_by");
            public static readonly string Yes = Intern("yes");
            public static readonly string No = Intern("no");

            // Common values
            public static readonly string Catholic = Intern("catholic");
            public static readonly string Orthodox = Intern("orthodox");
            public static readonly string Protestant = Intern("protestant");
            public static readonly string Sunni = Intern("sunni");
            public static readonly string Shiite = Intern("shiite");
            public static readonly string Monarchy = Intern("monarchy");
            public static readonly string Republic = Intern("republic");
            public static readonly string Western = Intern("western");
            public static readonly string Eastern = Intern("eastern");
            public static readonly string Muslim = Intern("muslim");
            public static readonly string Ottoman = Intern("ottoman");
        }
    }
}