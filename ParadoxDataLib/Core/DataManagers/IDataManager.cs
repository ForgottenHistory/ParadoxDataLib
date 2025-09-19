using System.Collections.Generic;
using System.Threading.Tasks;
using ParadoxDataLib.Core.DataModels;

namespace ParadoxDataLib.Core.DataManagers
{
    /// <summary>
    /// Generic interface for managing collections of game data with thread-safe operations.
    /// Provides CRUD operations and bulk access patterns optimized for Paradox game data.
    /// </summary>
    /// <typeparam name="TKey">The type used to uniquely identify data items (e.g., int for provinces, string for countries)</typeparam>
    /// <typeparam name="TData">The type of data being managed (e.g., ProvinceData, CountryData)</typeparam>
    public interface IDataManager<TKey, TData>
    {
        /// <summary>
        /// Adds or updates an item in the collection.
        /// </summary>
        /// <param name="key">The unique identifier for the item</param>
        /// <param name="data">The data to store</param>
        /// <exception cref="ArgumentNullException">Thrown when key or data is null</exception>
        void Add(TKey key, TData data);

        /// <summary>
        /// Attempts to retrieve an item without throwing exceptions.
        /// </summary>
        /// <param name="key">The key to search for</param>
        /// <param name="data">The retrieved data if found, default value if not found</param>
        /// <returns>True if the item was found, false otherwise</returns>
        bool TryGet(TKey key, out TData data);

        /// <summary>
        /// Retrieves an item by its key.
        /// </summary>
        /// <param name="key">The key to search for</param>
        /// <returns>The data associated with the key</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the key is not found</exception>
        TData Get(TKey key);

        /// <summary>
        /// Removes an item from the collection.
        /// </summary>
        /// <param name="key">The key of the item to remove</param>
        /// <returns>True if the item was removed, false if it wasn't found</returns>
        bool Remove(TKey key);

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        void Clear();

        /// <summary>
        /// Checks if the collection contains an item with the specified key.
        /// </summary>
        /// <param name="key">The key to search for</param>
        /// <returns>True if the key exists, false otherwise</returns>
        bool Contains(TKey key);

        /// <summary>Gets the total number of items in the collection</summary>
        int Count { get; }

        /// <summary>Gets a read-only collection of all keys in the collection</summary>
        IReadOnlyCollection<TKey> Keys { get; }

        /// <summary>Gets a read-only collection of all values in the collection</summary>
        IReadOnlyCollection<TData> Values { get; }

        /// <summary>
        /// Gets a read-only snapshot of all data in the collection.
        /// Useful for iteration and bulk operations.
        /// </summary>
        /// <returns>A read-only dictionary containing all current data</returns>
        IReadOnlyDictionary<TKey, TData> GetAll();
    }

    /// <summary>
    /// Specialized manager for province data with game-specific query capabilities.
    /// Extends the basic data manager with province-specific filtering and persistence operations.
    /// </summary>
    public interface IProvinceManager : IDataManager<int, ProvinceData>
    {
        /// <summary>
        /// Gets all provinces owned by the specified country.
        /// </summary>
        /// <param name="countryTag">The country tag (e.g., "FRA", "ENG")</param>
        /// <returns>Collection of provinces owned by the country</returns>
        IEnumerable<ProvinceData> GetProvincesByOwner(string countryTag);

        /// <summary>
        /// Gets all provinces with the specified primary culture.
        /// </summary>
        /// <param name="culture">The culture name to filter by</param>
        /// <returns>Collection of provinces with the specified culture</returns>
        IEnumerable<ProvinceData> GetProvincesByCulture(string culture);

        /// <summary>
        /// Gets all provinces with the specified dominant religion.
        /// </summary>
        /// <param name="religion">The religion name to filter by</param>
        /// <returns>Collection of provinces with the specified religion</returns>
        IEnumerable<ProvinceData> GetProvincesByReligion(string religion);

        /// <summary>
        /// Gets all provinces that produce the specified trade good.
        /// </summary>
        /// <param name="tradeGood">The trade good name to filter by</param>
        /// <returns>Collection of provinces producing the trade good</returns>
        IEnumerable<ProvinceData> GetProvincesByTradeGood(string tradeGood);

        /// <summary>
        /// Gets all provinces where the specified country has cores.
        /// </summary>
        /// <param name="countryTag">The country tag to search for in province cores</param>
        /// <returns>Collection of provinces with cores belonging to the country</returns>
        IEnumerable<ProvinceData> GetProvincesWithCores(string countryTag);

        /// <summary>
        /// Asynchronously loads province data from all files in the specified directory.
        /// Supports parallel loading for improved performance with large datasets.
        /// </summary>
        /// <param name="directoryPath">Path to directory containing province files</param>
        /// <returns>Task representing the asynchronous load operation</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the directory doesn't exist</exception>
        Task LoadFromDirectoryAsync(string directoryPath);

        /// <summary>
        /// Asynchronously saves all province data to files in the specified directory.
        /// Creates the directory if it doesn't exist.
        /// </summary>
        /// <param name="directoryPath">Path to directory where files will be saved</param>
        /// <returns>Task representing the asynchronous save operation</returns>
        Task SaveToDirectoryAsync(string directoryPath);
    }

    /// <summary>
    /// Specialized manager for country data with diplomatic and political query capabilities.
    /// Extends the basic data manager with country-specific filtering and persistence operations.
    /// </summary>
    public interface ICountryManager : IDataManager<string, CountryData>
    {
        /// <summary>
        /// Gets all countries with the specified government type.
        /// </summary>
        /// <param name="government">The government type to filter by (e.g., "monarchy", "republic")</param>
        /// <returns>Collection of countries with the specified government type</returns>
        IEnumerable<CountryData> GetCountriesByGovernment(string government);

        /// <summary>
        /// Gets all countries with the specified primary culture.
        /// </summary>
        /// <param name="culture">The culture name to filter by</param>
        /// <returns>Collection of countries with the specified primary culture</returns>
        IEnumerable<CountryData> GetCountriesByCulture(string culture);

        /// <summary>
        /// Gets all countries with the specified state religion.
        /// </summary>
        /// <param name="religion">The religion name to filter by</param>
        /// <returns>Collection of countries with the specified religion</returns>
        IEnumerable<CountryData> GetCountriesByReligion(string religion);

        /// <summary>
        /// Gets all countries in the specified technology group.
        /// Technology groups affect research costs and unit types.
        /// </summary>
        /// <param name="techGroup">The technology group to filter by (e.g., "western", "eastern")</param>
        /// <returns>Collection of countries in the specified technology group</returns>
        IEnumerable<CountryData> GetCountriesByTechnologyGroup(string techGroup);

        /// <summary>
        /// Asynchronously loads country data from all files in the specified directory.
        /// Supports parallel loading for improved performance.
        /// </summary>
        /// <param name="directoryPath">Path to directory containing country files</param>
        /// <returns>Task representing the asynchronous load operation</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the directory doesn't exist</exception>
        Task LoadFromDirectoryAsync(string directoryPath);

        /// <summary>
        /// Asynchronously saves all country data to files in the specified directory.
        /// Creates the directory if it doesn't exist.
        /// </summary>
        /// <param name="directoryPath">Path to directory where files will be saved</param>
        /// <returns>Task representing the asynchronous save operation</returns>
        Task SaveToDirectoryAsync(string directoryPath);
    }
}