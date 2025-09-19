using System;
using System.Collections.Generic;
using ParadoxDataLib.Core.Common;

namespace ParadoxDataLib.Core.DataModels
{
    /// <summary>
    /// Represents a province in a Paradox grand strategy game, containing all static and dynamic data.
    /// This is the core data structure for province information including ownership, development, and historical changes.
    /// Implements value semantics (struct) for performance in large collections.
    /// </summary>
    /// <remarks>
    /// Provinces are the fundamental territorial units in Paradox games. Each province has:
    /// - Static attributes: terrain, climate, trade node
    /// - Dynamic attributes: owner, controller, development levels
    /// - Historical data: changes over time, cores, discoveries
    /// - Modifiers: temporary and permanent effects
    ///
    /// Performance considerations:
    /// - Use struct for cache efficiency with large datasets (13k+ provinces)
    /// - Collections are initialized on construction to avoid null checks
    /// - Historical data is sorted by date for efficient lookups
    /// </remarks>
    public struct ProvinceData : IGameEntity, IModifiable, IHistorical
    {
        /// <summary>Gets or sets the unique identifier for this province (1-based, matches game files)</summary>
        public int ProvinceId { get; set; }

        /// <summary>Gets or sets the localized name of the province</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the country tag that owns this province (e.g., "FRA", "ENG")</summary>
        public string Owner { get; set; }

        /// <summary>Gets or sets the country tag that controls this province (may differ from owner during occupation)</summary>
        public string Controller { get; set; }

        /// <summary>Gets or sets the primary culture of this province</summary>
        public string Culture { get; set; }

        /// <summary>Gets or sets the dominant religion of this province</summary>
        public string Religion { get; set; }

        /// <summary>Gets or sets the capital status ("yes" if this province is a national capital)</summary>
        public string Capital { get; set; }

        /// <summary>Gets or sets the trade good produced in this province</summary>
        public string TradeGood { get; set; }

        /// <summary>Gets or sets whether this province is part of the Holy Roman Empire</summary>
        public bool IsHre { get; set; }

        /// <summary>Gets or sets whether this province contains a city (affects certain game mechanics)</summary>
        public bool IsCity { get; set; }

        /// <summary>Gets or sets the base tax development level (affects tax income)</summary>
        public float BaseTax { get; set; }

        /// <summary>Gets or sets the base production development level (affects trade and production income)</summary>
        public float BaseProduction { get; set; }

        /// <summary>Gets or sets the base manpower development level (affects military recruitment)</summary>
        public float BaseManpower { get; set; }

        /// <summary>Gets or sets the extra development cost modifier for this province</summary>
        public float ExtraCost { get; set; }

        /// <summary>Gets or sets the list of country tags that have cores on this province</summary>
        public List<string> Cores { get; set; }

        /// <summary>Gets or sets the list of country tags that have discovered this province</summary>
        public List<string> DiscoveredBy { get; set; }

        /// <summary>Gets or sets the list of buildings constructed in this province</summary>
        public List<string> Buildings { get; set; }

        /// <summary>Gets or sets the active modifiers affecting this province</summary>
        public List<Modifier> Modifiers { get; set; }

        /// <summary>Gets or sets the chronological history of changes to this province</summary>
        public List<HistoricalEntry> HistoricalEntries { get; set; }

        /// <summary>Gets or sets the terrain type (affects movement and combat)</summary>
        public string Terrain { get; set; }

        /// <summary>Gets or sets the climate classification of this province</summary>
        public string Climate { get; set; }

        /// <summary>Gets or sets the trade node this province belongs to</summary>
        public string TradeNode { get; set; }

        /// <summary>Gets or sets the center of trade level (0 = none, 1-3 = increasing importance)</summary>
        public int CenterOfTrade { get; set; }

        /// <summary>Gets the string representation of the province ID for use as a dictionary key</summary>
        public string Id => ProvinceId.ToString();

        /// <summary>
        /// Initializes a new province with the specified ID and name.
        /// All collections are initialized empty, and numerical values are set to zero.
        /// </summary>
        /// <param name="id">The unique province identifier (must be positive)</param>
        /// <param name="name">The province name (cannot be null or empty)</param>
        public ProvinceData(int id, string name)
        {
            ProvinceId = id;
            Name = name;
            Owner = null;
            Controller = null;
            Culture = null;
            Religion = null;
            Capital = null;
            TradeGood = null;
            IsHre = false;
            IsCity = false;

            BaseTax = 0;
            BaseProduction = 0;
            BaseManpower = 0;
            ExtraCost = 0;

            Cores = new List<string>();
            DiscoveredBy = new List<string>();
            Buildings = new List<string>();
            Modifiers = new List<Modifier>();
            HistoricalEntries = new List<HistoricalEntry>();

            Terrain = null;
            Climate = null;
            TradeNode = null;
            CenterOfTrade = 0;
        }

        /// <summary>
        /// Validates that this province has the minimum required data to be considered valid.
        /// </summary>
        /// <returns>True if the province has a positive ID and non-empty name</returns>
        public bool IsValid()
        {
            return ProvinceId > 0 && !string.IsNullOrEmpty(Name);
        }

        /// <summary>
        /// Validates the province data and throws an exception if invalid.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the province is not valid</exception>
        public void Validate()
        {
            if (!IsValid())
            {
                throw new InvalidOperationException($"Province {ProvinceId} is not valid");
            }
        }

        /// <summary>
        /// Applies a modifier to this province. Modifiers can affect various attributes like tax income, manpower, etc.
        /// </summary>
        /// <param name="modifier">The modifier to apply</param>
        public void ApplyModifier(Modifier modifier)
        {
            Modifiers.Add(modifier);
        }

        /// <summary>
        /// Removes all modifiers with the specified ID from this province.
        /// </summary>
        /// <param name="modifierId">The ID of the modifier to remove</param>
        public void RemoveModifier(string modifierId)
        {
            Modifiers.RemoveAll(m => m.Id == modifierId);
        }

        public void ClearModifiers()
        {
            Modifiers.Clear();
        }

        /// <summary>
        /// Adds a historical entry to this province. Entries should be added in chronological order for optimal performance.
        /// </summary>
        /// <param name="entry">The historical entry to add</param>
        public void AddHistoricalEntry(HistoricalEntry entry)
        {
            HistoricalEntries.Add(entry);
        }

        /// <summary>
        /// Gets the most recent historical entry that occurred on or before the specified date.
        /// </summary>
        /// <param name="date">The date to query</param>
        /// <returns>The historical entry active at the specified date, or null if none found</returns>
        public HistoricalEntry GetEntryAtDate(DateTime date)
        {
            HistoricalEntry lastEntry = null;
            foreach (var entry in HistoricalEntries)
            {
                if (entry.Date <= date)
                {
                    lastEntry = entry;
                }
                else
                {
                    break;
                }
            }
            return lastEntry;
        }

        /// <summary>
        /// Applies all historical changes that occurred up to and including the specified date.
        /// This method modifies the province's current state based on its history.
        /// </summary>
        /// <param name="date">The end date for applying historical changes</param>
        public void ApplyHistoryUpToDate(DateTime date)
        {
            foreach (var entry in HistoricalEntries)
            {
                if (entry.Date > date) break;

                foreach (var change in entry.Changes)
                {
                    ApplyHistoricalChange(change.Key, change.Value);
                }
            }
        }

        private void ApplyHistoricalChange(string key, object value)
        {
            switch (key.ToLower())
            {
                case "owner":
                    Owner = value.ToString();
                    break;
                case "controller":
                    Controller = value.ToString();
                    break;
                case "culture":
                    Culture = value.ToString();
                    break;
                case "religion":
                    Religion = value.ToString();
                    break;
                case "add_core":
                    if (!Cores.Contains(value.ToString()))
                        Cores.Add(value.ToString());
                    break;
                case "remove_core":
                    Cores.Remove(value.ToString());
                    break;
                case "base_tax":
                    BaseTax = Convert.ToSingle(value);
                    break;
                case "base_production":
                    BaseProduction = Convert.ToSingle(value);
                    break;
                case "base_manpower":
                    BaseManpower = Convert.ToSingle(value);
                    break;
            }
        }
    }
}