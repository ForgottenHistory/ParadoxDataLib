using System;
using System.Linq;
using ParadoxDataLib.Core.Common;
using ParadoxDataLib.Core.DataModels;

namespace ParadoxDataLib.Core.Extractors
{
    /// <summary>
    /// Extractor for converting generic ParadoxNode trees into CountryData objects.
    /// Handles all country-specific data including government, culture, religion, ideas, and diplomatic relations.
    /// </summary>
    public class CountryExtractor : BaseDataExtractor<CountryData>
    {
        private readonly string _countryTag;
        private readonly string _countryName;

        /// <summary>
        /// Initializes a new instance of the CountryExtractor
        /// </summary>
        /// <param name="countryTag">The three-letter country tag</param>
        /// <param name="countryName">The human-readable country name</param>
        public CountryExtractor(string countryTag, string countryName)
        {
            _countryTag = countryTag;
            _countryName = countryName;
        }

        /// <summary>
        /// Extracts CountryData from a generic ParadoxNode tree
        /// </summary>
        /// <param name="node">The root node containing parsed country data</param>
        /// <returns>The extracted CountryData object</returns>
        public override CountryData Extract(ParadoxNode node)
        {
            var country = new CountryData(_countryTag, _countryName);

            if (node.Type != NodeType.Object)
            {
                AddError("Root node must be an object");
                return country;
            }

            // Extract basic attributes
            country.Government = node.GetValue<string>("government");
            country.PrimaryCulture = node.GetValue<string>("primary_culture");
            country.Religion = node.GetValue<string>("religion");
            country.TechnologyGroup = node.GetValue<string>("technology_group");
            country.Capital = node.GetValue<int>("capital");
            country.FixedCapital = node.GetValue<int?>("fixed_capital");

            // Extract government reforms
            country.GovernmentReforms.AddRange(node.GetValues<string>("add_government_reform"));

            // Extract accepted cultures
            country.AcceptedCultures.AddRange(node.GetValues<string>("add_accepted_culture"));

            // Remove accepted cultures if specified
            foreach (var removeCulture in node.GetValues<string>("remove_accepted_culture"))
            {
                country.AcceptedCultures.Remove(removeCulture);
            }

            // Extract diplomatic relations
            country.HistoricalFriends.AddRange(node.GetValues<string>("historical_friend"));
            country.HistoricalRivals.AddRange(node.GetValues<string>("historical_rival"));
            country.HistoricalEnemies.AddRange(node.GetValues<string>("historical_enemy"));

            // Extract ideas and policies
            ExtractIdeasAndPolicies(node, country);

            // Extract rulers (monarchs, heirs, queens)
            ExtractRulers(node, country);

            // Extract modifiers
            country.Modifiers.AddRange(ExtractModifiers(node));

            // Extract individual country effects as modifiers
            ExtractIndividualCountryEffects(node, country);

            // Extract historical entries
            country.HistoricalEntries.AddRange(ExtractHistoricalEntries(node));

            // Extract flags and other miscellaneous data
            ExtractMiscellaneousData(node, country);

            return country;
        }

        /// <summary>
        /// Validates that the node structure is compatible with country extraction
        /// </summary>
        /// <param name="node">The node to validate</param>
        /// <returns>True if the node can be processed as country data</returns>
        public override bool CanExtract(ParadoxNode node)
        {
            if (node.Type != NodeType.Object)
                return false;

            // Check for typical country attributes
            return node.HasChild("government") ||
                   node.HasChild("primary_culture") ||
                   node.HasChild("religion") ||
                   node.HasChild("technology_group") ||
                   node.HasChild("capital");
        }

        /// <summary>
        /// Extracts ideas and policies from the node
        /// </summary>
        /// <param name="node">The node containing ideas data</param>
        /// <param name="country">The country to add ideas to</param>
        private void ExtractIdeasAndPolicies(ParadoxNode node, CountryData country)
        {
            // Extract individual idea groups with levels
            var ideaGroups = new[]
            {
                "administrative_ideas", "diplomatic_ideas", "military_ideas",
                "humanist_ideas", "influence_ideas", "innovative_ideas",
                "religious_ideas", "espionage_ideas", "maritime_ideas",
                "quality_ideas", "economic_ideas", "expansion_ideas",
                "exploration_ideas", "defensive_ideas", "trade_ideas",
                "aristocratic_ideas", "plutocratic_ideas", "offensive_ideas"
            };

            foreach (var ideaGroup in ideaGroups)
            {
                var level = node.GetValue<int>(ideaGroup);
                if (level > 0)
                {
                    country.Ideas[ideaGroup] = level;
                }
            }

            // Extract add_idea commands
            foreach (var idea in node.GetValues<string>("add_idea"))
            {
                if (!country.Ideas.ContainsKey(idea))
                {
                    country.Ideas[idea] = 1; // Default level for individual ideas
                }
            }

            // Extract policies
            country.Policies.AddRange(node.GetValues<string>("add_active_policy"));
        }

        /// <summary>
        /// Extracts ruler information from the node
        /// </summary>
        /// <param name="node">The node containing ruler data</param>
        /// <param name="country">The country to add rulers to</param>
        private void ExtractRulers(ParadoxNode node, CountryData country)
        {
            // Extract monarch
            var monarchNode = node.GetChild("monarch");
            if (monarchNode != null && monarchNode.Type == NodeType.Object)
            {
                country.Monarch = ExtractRulerFromNode(monarchNode);
            }

            // Extract heir
            var heirNode = node.GetChild("heir");
            if (heirNode != null && heirNode.Type == NodeType.Object)
            {
                country.Heir = ExtractRulerFromNode(heirNode);
            }

            // Extract queen
            var queenNode = node.GetChild("queen");
            if (queenNode != null && queenNode.Type == NodeType.Object)
            {
                country.Queen = ExtractRulerFromNode(queenNode);
            }
        }

        /// <summary>
        /// Extracts a ruler from a ruler node
        /// </summary>
        /// <param name="rulerNode">The node containing ruler data</param>
        /// <returns>The extracted Ruler object</returns>
        private Ruler ExtractRulerFromNode(ParadoxNode rulerNode)
        {
            var ruler = new Ruler();

            ruler.Name = rulerNode.GetValue<string>("name", "");
            ruler.Dynasty = rulerNode.GetValue<string>("dynasty", "");

            // Extract birth and death dates
            var birthDateStr = rulerNode.GetValue<string>("birth_date");
            if (!string.IsNullOrEmpty(birthDateStr) && DateTime.TryParse(birthDateStr, out var birthDate))
            {
                ruler.BirthDate = birthDate;
            }

            var deathDateStr = rulerNode.GetValue<string>("death_date");
            if (!string.IsNullOrEmpty(deathDateStr) && DateTime.TryParse(deathDateStr, out var deathDate))
            {
                ruler.DeathDate = deathDate;
            }

            // Extract monarch points
            ruler.ADM = rulerNode.GetValue<int>("adm");
            ruler.DIP = rulerNode.GetValue<int>("dip");
            ruler.MIL = rulerNode.GetValue<int>("mil");

            // Extract additional attributes
            ruler.Culture = rulerNode.GetValue<string>("culture");
            ruler.Religion = rulerNode.GetValue<string>("religion");
            // Note: Claim property not available in current Ruler struct

            return ruler;
        }

        /// <summary>
        /// Extracts individual country effect values as simple modifiers
        /// </summary>
        /// <param name="node">The node containing effect data</param>
        /// <param name="country">The country to add modifiers to</param>
        private void ExtractIndividualCountryEffects(ParadoxNode node, CountryData country)
        {
            // List of known country effects
            var knownEffects = new[]
            {
                "prestige", "stability", "legitimacy", "republican_tradition",
                "inflation", "corruption", "mercantilism", "army_tradition",
                "navy_tradition", "manpower", "sailors", "treasury",
                "war_exhaustion", "power_projection", "splendor"
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
                        country.Modifiers.Add(modifier);
                    }
                }
            }
        }

        /// <summary>
        /// Extracts miscellaneous data like flags and estate privileges
        /// </summary>
        /// <param name="node">The node containing miscellaneous data</param>
        /// <param name="country">The country to add data to</param>
        private void ExtractMiscellaneousData(ParadoxNode node, CountryData country)
        {
            // Extract flags
            foreach (var child in node.Children)
            {
                if (child.Key.StartsWith("set_country_flag") || child.Key.EndsWith("_flag"))
                {
                    if (child.Value.Type == NodeType.Scalar)
                    {
                        country.Flags[child.Key] = child.Value.Value;
                    }
                }
            }

            // Extract estate privileges
            country.EstatePrivileges.AddRange(node.GetValues<string>("add_estate_privilege"));
        }
    }
}