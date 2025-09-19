using System;
using System.Linq;
using ParadoxDataLib.Core.Common;
using ParadoxDataLib.Core.DataModels;

namespace ParadoxDataLib.Core.Extractors
{
    /// <summary>
    /// Extractor for converting generic ParadoxNode trees into ProvinceData objects.
    /// Handles all province-specific data including basic attributes, modifiers, buildings, and historical entries.
    /// </summary>
    public class ProvinceExtractor : BaseDataExtractor<ProvinceData>
    {
        private readonly int _provinceId;
        private readonly string _provinceName;

        /// <summary>
        /// Initializes a new instance of the ProvinceExtractor
        /// </summary>
        /// <param name="provinceId">The province ID to assign to the extracted province</param>
        /// <param name="provinceName">The province name to assign to the extracted province</param>
        public ProvinceExtractor(int provinceId, string provinceName)
        {
            _provinceId = provinceId;
            _provinceName = provinceName;
        }

        /// <summary>
        /// Extracts ProvinceData from a generic ParadoxNode tree
        /// </summary>
        /// <param name="node">The root node containing parsed province data</param>
        /// <returns>The extracted ProvinceData object</returns>
        public override ProvinceData Extract(ParadoxNode node)
        {
            var province = new ProvinceData(_provinceId, _provinceName);

            if (node.Type != NodeType.Object)
            {
                AddError("Root node must be an object");
                return province;
            }

            // Extract basic attributes
            province.Owner = node.GetValue<string>("owner");
            province.Controller = node.GetValue<string>("controller");
            province.Culture = node.GetValue<string>("culture");
            province.Religion = node.GetValue<string>("religion");
            province.Capital = node.GetValue<string>("capital");
            province.TradeGood = node.GetValue<string>("trade_goods");

            // Extract boolean attributes
            province.IsHre = node.GetValue<bool>("hre");
            province.IsCity = node.GetValue<bool>("is_city");

            // Extract numerical attributes
            province.BaseTax = node.GetValue<float>("base_tax");
            province.BaseProduction = node.GetValue<float>("base_production");
            province.BaseManpower = node.GetValue<float>("base_manpower");
            province.ExtraCost = node.GetValue<float>("extra_cost");
            province.CenterOfTrade = node.GetValue<int>("center_of_trade");

            // Extract terrain and climate
            province.Terrain = node.GetValue<string>("terrain");
            province.Climate = node.GetValue<string>("climate");
            province.TradeNode = node.GetValue<string>("trade_node");

            // Extract lists
            province.Cores.AddRange(node.GetValues<string>("add_core"));
            province.DiscoveredBy.AddRange(node.GetValues<string>("discovered_by"));

            // Remove cores if specified
            foreach (var removeCore in node.GetValues<string>("remove_core"))
            {
                province.Cores.Remove(removeCore);
            }

            // Extract buildings
            ExtractBuildings(node, province);

            // Extract modifiers
            province.Modifiers.AddRange(ExtractModifiers(node));

            // Extract individual effects as modifiers
            ExtractIndividualEffects(node, province);

            // Extract historical entries
            province.HistoricalEntries.AddRange(ExtractHistoricalEntries(node));

            return province;
        }

        /// <summary>
        /// Validates that the node structure is compatible with province extraction
        /// </summary>
        /// <param name="node">The node to validate</param>
        /// <returns>True if the node can be processed as province data</returns>
        public override bool CanExtract(ParadoxNode node)
        {
            if (node.Type != NodeType.Object)
                return false;

            // Check for typical province attributes
            return node.HasChild("owner") ||
                   node.HasChild("culture") ||
                   node.HasChild("religion") ||
                   node.HasChild("base_tax") ||
                   node.HasChild("base_production") ||
                   node.HasChild("base_manpower");
        }

        /// <summary>
        /// Extracts building information from the node
        /// </summary>
        /// <param name="node">The node containing building data</param>
        /// <param name="province">The province to add buildings to</param>
        private void ExtractBuildings(ParadoxNode node, ProvinceData province)
        {
            // Common building names
            var knownBuildings = new[]
            {
                "fort_15th", "fort_16th", "fort_17th", "fort_18th",
                "marketplace", "workshop", "temple", "courthouse",
                "dock", "drydock", "shipyard", "grand_shipyard",
                "barracks", "training_fields", "regimental_camp", "conscription_center",
                "trade_depot", "stock_exchange", "counting_house", "treasury_office",
                "university", "cathedral", "town_hall"
            };

            foreach (var buildingName in knownBuildings)
            {
                if (node.GetValue<bool>(buildingName))
                {
                    province.Buildings.Add(buildingName);
                }
            }

            // Look for other potential buildings (anything ending with specific patterns)
            // but exclude buildings already found in the known buildings list
            foreach (var child in node.Children)
            {
                var key = child.Key.ToLower();
                if ((key.StartsWith("fort_") || key.Contains("manufactory") || key.EndsWith("_building"))
                    && node.GetValue<bool>(key)
                    && !knownBuildings.Contains(key))
                {
                    province.Buildings.Add(key);
                }
            }
        }

        /// <summary>
        /// Extracts individual effect values as simple modifiers
        /// </summary>
        /// <param name="node">The node containing effect data</param>
        /// <param name="province">The province to add modifiers to</param>
        private void ExtractIndividualEffects(ParadoxNode node, ProvinceData province)
        {
            // List of known province effects
            var knownEffects = new[]
            {
                "local_tax_modifier", "local_production_efficiency", "local_manpower_modifier",
                "local_missionary_strength", "local_autonomy", "local_defensiveness",
                "supply_limit_modifier", "local_trade_power", "province_trade_power_value",
                "local_unrest", "local_development_cost", "local_institution_spread",
                "garrison_growth", "fort_level"
            };

            foreach (var effectName in knownEffects)
            {
                if (node.HasChild(effectName))
                {
                    var effectValue = node.GetValue<float>(effectName);
                    if (effectValue != 0) // Only add non-zero effects
                    {
                        var modifier = new Modifier(effectName, effectName, ModifierType.Permanent);
                        modifier.Effects[effectName] = effectValue;
                        province.Modifiers.Add(modifier);
                    }
                }
            }
        }
    }
}