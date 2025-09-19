using System;
using System.Linq;
using ParadoxDataLib.Core.Common;
using ParadoxDataLib.Core.DataModels;
using ParadoxDataLib.Core.Extractors;
using Xunit;

namespace ParadoxDataLib.Tests
{
    /// <summary>
    /// Unit tests for data extractor classes
    /// </summary>
    public class DataExtractorTests
    {
        #region ProvinceExtractor Tests

        [Fact]
        public void ProvinceExtractor_BasicProvince_ExtractsCorrectly()
        {
            var extractor = new ProvinceExtractor(183, "Paris");
            var node = CreateProvinceNode();

            var result = extractor.Extract(node);

            Assert.Equal(183, result.ProvinceId);
            Assert.Equal("Paris", result.Name);
            Assert.Equal("FRA", result.Owner);
            Assert.Equal("FRA", result.Controller);
            Assert.Equal("french", result.Culture);
            Assert.Equal("catholic", result.Religion);
            Assert.Equal("grain", result.TradeGood);
            Assert.Equal(3.0f, result.BaseTax);
            Assert.Equal(2.0f, result.BaseProduction);
            Assert.Equal(1.0f, result.BaseManpower);
            Assert.True(result.IsHre);
            Assert.True(result.IsCity);
        }

        [Fact]
        public void ProvinceExtractor_WithBuildings_ExtractsBuildings()
        {
            var extractor = new ProvinceExtractor(183, "Paris");
            var node = CreateProvinceNodeWithBuildings();

            var result = extractor.Extract(node);

            Assert.Contains("fort_15th", result.Buildings);
            Assert.Contains("marketplace", result.Buildings);
            Assert.Contains("temple", result.Buildings);
            Assert.Equal(3, result.Buildings.Count);
        }

        [Fact]
        public void ProvinceExtractor_WithCores_ExtractsCores()
        {
            var extractor = new ProvinceExtractor(183, "Paris");
            var node = CreateProvinceNodeWithCores();

            var result = extractor.Extract(node);

            Assert.Contains("FRA", result.Cores);
            Assert.Contains("ENG", result.Cores);
            Assert.Equal(2, result.Cores.Count);
        }

        [Fact]
        public void ProvinceExtractor_WithModifiers_ExtractsModifiers()
        {
            var extractor = new ProvinceExtractor(183, "Paris");
            var node = CreateProvinceNodeWithModifiers();

            var result = extractor.Extract(node);

            Assert.NotEmpty(result.Modifiers);
            var modifier = result.Modifiers.First();
            Assert.Equal("fertile_lands", modifier.Name);
            Assert.Contains("local_tax_modifier", modifier.Effects.Keys);
        }

        [Fact]
        public void ProvinceExtractor_CanExtract_ValidProvince_ReturnsTrue()
        {
            var extractor = new ProvinceExtractor(183, "Paris");
            var node = CreateProvinceNode();

            var canExtract = extractor.CanExtract(node);

            Assert.True(canExtract);
        }

        [Fact]
        public void ProvinceExtractor_CanExtract_InvalidNode_ReturnsFalse()
        {
            var extractor = new ProvinceExtractor(183, "Paris");
            var node = ParadoxNode.CreateScalar("simple", "value");

            var canExtract = extractor.CanExtract(node);

            Assert.False(canExtract);
        }

        [Fact]
        public void ProvinceExtractor_CanExtract_EmptyObject_ReturnsFalse()
        {
            var extractor = new ProvinceExtractor(183, "Paris");
            var node = ParadoxNode.CreateObject("empty");

            var canExtract = extractor.CanExtract(node);

            Assert.False(canExtract);
        }

        #endregion

        #region CountryExtractor Tests

        [Fact]
        public void CountryExtractor_BasicCountry_ExtractsCorrectly()
        {
            var extractor = new CountryExtractor("FRA", "France");
            var node = CreateCountryNode();

            var result = extractor.Extract(node);

            Assert.Equal("FRA", result.Tag);
            Assert.Equal("France", result.Name);
            Assert.Equal("monarchy", result.Government);
            Assert.Equal("french", result.PrimaryCulture);
            Assert.Equal("catholic", result.Religion);
            Assert.Equal("western", result.TechnologyGroup);
            Assert.Equal(183, result.Capital);
        }

        [Fact]
        public void CountryExtractor_WithIdeas_ExtractsIdeas()
        {
            var extractor = new CountryExtractor("FRA", "France");
            var node = CreateCountryNodeWithIdeas();

            var result = extractor.Extract(node);

            Assert.Contains("administrative_ideas", result.Ideas.Keys);
            Assert.Equal(7, result.Ideas["administrative_ideas"]);
            Assert.Contains("diplomatic_ideas", result.Ideas.Keys);
            Assert.Equal(3, result.Ideas["diplomatic_ideas"]);
            Assert.Contains("custom_idea", result.Ideas.Keys);
            Assert.Equal(1, result.Ideas["custom_idea"]);
        }

        [Fact]
        public void CountryExtractor_WithRuler_ExtractsRuler()
        {
            var extractor = new CountryExtractor("FRA", "France");
            var node = CreateCountryNodeWithRuler();

            var result = extractor.Extract(node);

            // Assert.NotNull(result.Monarch); // Ruler is a struct, cannot be null
            Assert.Equal("Louis XI", result.Monarch.Name);
            Assert.Equal("de Valois", result.Monarch.Dynasty);
            Assert.Equal(5, result.Monarch.ADM);
            Assert.Equal(4, result.Monarch.DIP);
            Assert.Equal(6, result.Monarch.MIL);
            Assert.Equal("french", result.Monarch.Culture);
            Assert.Equal("catholic", result.Monarch.Religion);
        }

        [Fact]
        public void CountryExtractor_WithDiplomaticRelations_ExtractsRelations()
        {
            var extractor = new CountryExtractor("FRA", "France");
            var node = CreateCountryNodeWithDiplomacy();

            var result = extractor.Extract(node);

            Assert.Contains("ENG", result.HistoricalFriends);
            Assert.Contains("HRE", result.HistoricalRivals);
            Assert.Contains("TUR", result.HistoricalEnemies);
        }

        [Fact]
        public void CountryExtractor_WithPolicies_ExtractsPolicies()
        {
            var extractor = new CountryExtractor("FRA", "France");
            var node = CreateCountryNodeWithPolicies();

            var result = extractor.Extract(node);

            Assert.Contains("land_acquisition_act", result.Policies);
            Assert.Contains("cultural_unity_act", result.Policies);
        }

        [Fact]
        public void CountryExtractor_CanExtract_ValidCountry_ReturnsTrue()
        {
            var extractor = new CountryExtractor("FRA", "France");
            var node = CreateCountryNode();

            var canExtract = extractor.CanExtract(node);

            Assert.True(canExtract);
        }

        [Fact]
        public void CountryExtractor_CanExtract_InvalidNode_ReturnsFalse()
        {
            var extractor = new CountryExtractor("FRA", "France");
            var node = ParadoxNode.CreateScalar("simple", "value");

            var canExtract = extractor.CanExtract(node);

            Assert.False(canExtract);
        }

        #endregion

        #region BaseDataExtractor Tests

        [Fact]
        public void BaseDataExtractor_ExtractModifiers_ExtractsCorrectly()
        {
            var extractor = new ProvinceExtractor(1, "Test");
            var node = CreateNodeWithModifiers();

            var result = extractor.Extract(node);

            Assert.NotEmpty(result.Modifiers);
            var modifier = result.Modifiers.First();
            Assert.Equal("test_modifier", modifier.Name);
            Assert.Contains("local_tax_modifier", modifier.Effects.Keys);
            Assert.Equal(0.1f, modifier.Effects["local_tax_modifier"]);
        }

        [Fact]
        public void BaseDataExtractor_ExtractHistoricalEntries_ExtractsCorrectly()
        {
            var extractor = new ProvinceExtractor(1, "Test");
            var node = CreateNodeWithHistoricalEntries();

            var result = extractor.Extract(node);

            Assert.NotEmpty(result.HistoricalEntries);
            var entry = result.HistoricalEntries.First();
            Assert.Equal(new DateTime(1444, 11, 11), entry.Date);
            Assert.NotEmpty(entry.Changes);
        }

        #endregion

        #region Helper Methods

        private ParadoxNode CreateProvinceNode()
        {
            var node = ParadoxNode.CreateObject("province");
            node.AddChild(ParadoxNode.CreateScalar("owner", "FRA"));
            node.AddChild(ParadoxNode.CreateScalar("controller", "FRA"));
            node.AddChild(ParadoxNode.CreateScalar("culture", "french"));
            node.AddChild(ParadoxNode.CreateScalar("religion", "catholic"));
            node.AddChild(ParadoxNode.CreateScalar("trade_goods", "grain"));
            node.AddChild(ParadoxNode.CreateScalar("base_tax", 3.0f));
            node.AddChild(ParadoxNode.CreateScalar("base_production", 2.0f));
            node.AddChild(ParadoxNode.CreateScalar("base_manpower", 1.0f));
            node.AddChild(ParadoxNode.CreateScalar("hre", true));
            node.AddChild(ParadoxNode.CreateScalar("is_city", true));
            return node;
        }

        private ParadoxNode CreateProvinceNodeWithBuildings()
        {
            var node = CreateProvinceNode();
            node.AddChild(ParadoxNode.CreateScalar("fort_15th", true));
            node.AddChild(ParadoxNode.CreateScalar("marketplace", true));
            node.AddChild(ParadoxNode.CreateScalar("temple", true));
            node.AddChild(ParadoxNode.CreateScalar("dock", false)); // Should not be included
            return node;
        }

        private ParadoxNode CreateProvinceNodeWithCores()
        {
            var node = CreateProvinceNode();
            node.AddChildAccumulating(ParadoxNode.CreateScalar("add_core", "FRA"));
            node.AddChildAccumulating(ParadoxNode.CreateScalar("add_core", "ENG"));
            return node;
        }

        private ParadoxNode CreateProvinceNodeWithModifiers()
        {
            var node = CreateProvinceNode();
            var modifier = ParadoxNode.CreateObject("add_permanent_province_modifier");
            modifier.AddChild(ParadoxNode.CreateScalar("name", "fertile_lands"));
            modifier.AddChild(ParadoxNode.CreateScalar("local_tax_modifier", 0.1f));
            modifier.AddChild(ParadoxNode.CreateScalar("duration", -1));
            node.AddChild(modifier);
            return node;
        }

        private ParadoxNode CreateCountryNode()
        {
            var node = ParadoxNode.CreateObject("country");
            node.AddChild(ParadoxNode.CreateScalar("government", "monarchy"));
            node.AddChild(ParadoxNode.CreateScalar("primary_culture", "french"));
            node.AddChild(ParadoxNode.CreateScalar("religion", "catholic"));
            node.AddChild(ParadoxNode.CreateScalar("technology_group", "western"));
            node.AddChild(ParadoxNode.CreateScalar("capital", 183));
            return node;
        }

        private ParadoxNode CreateCountryNodeWithIdeas()
        {
            var node = CreateCountryNode();
            node.AddChild(ParadoxNode.CreateScalar("administrative_ideas", 7));
            node.AddChild(ParadoxNode.CreateScalar("diplomatic_ideas", 3));
            node.AddChild(ParadoxNode.CreateScalar("add_idea", "custom_idea"));
            return node;
        }

        private ParadoxNode CreateCountryNodeWithRuler()
        {
            var node = CreateCountryNode();
            var monarch = ParadoxNode.CreateObject("monarch");
            monarch.AddChild(ParadoxNode.CreateScalar("name", "Louis XI"));
            monarch.AddChild(ParadoxNode.CreateScalar("dynasty", "de Valois"));
            monarch.AddChild(ParadoxNode.CreateScalar("adm", 5));
            monarch.AddChild(ParadoxNode.CreateScalar("dip", 4));
            monarch.AddChild(ParadoxNode.CreateScalar("mil", 6));
            monarch.AddChild(ParadoxNode.CreateScalar("culture", "french"));
            monarch.AddChild(ParadoxNode.CreateScalar("religion", "catholic"));
            node.AddChild(monarch);
            return node;
        }

        private ParadoxNode CreateCountryNodeWithDiplomacy()
        {
            var node = CreateCountryNode();
            node.AddChild(ParadoxNode.CreateScalar("historical_friend", "ENG"));
            node.AddChild(ParadoxNode.CreateScalar("historical_rival", "HRE"));
            node.AddChild(ParadoxNode.CreateScalar("historical_enemy", "TUR"));
            return node;
        }

        private ParadoxNode CreateCountryNodeWithPolicies()
        {
            var node = CreateCountryNode();
            node.AddChildAccumulating(ParadoxNode.CreateScalar("add_active_policy", "land_acquisition_act"));
            node.AddChildAccumulating(ParadoxNode.CreateScalar("add_active_policy", "cultural_unity_act"));
            return node;
        }

        private ParadoxNode CreateNodeWithModifiers()
        {
            var node = ParadoxNode.CreateObject("test");
            var modifier = ParadoxNode.CreateObject("add_permanent_province_modifier");
            modifier.AddChild(ParadoxNode.CreateScalar("name", "test_modifier"));
            modifier.AddChild(ParadoxNode.CreateScalar("local_tax_modifier", 0.1f));
            modifier.AddChild(ParadoxNode.CreateScalar("duration", -1));
            node.AddChild(modifier);
            return node;
        }

        private ParadoxNode CreateNodeWithHistoricalEntries()
        {
            var node = ParadoxNode.CreateObject("test");
            var dateNode = ParadoxNode.CreateDate("1444.11.11", new DateTime(1444, 11, 11));
            dateNode.AddChild(ParadoxNode.CreateScalar("owner", "FRA"));
            dateNode.AddChild(ParadoxNode.CreateScalar("add_core", "FRA"));
            node.AddChild(dateNode);
            return node;
        }

        #endregion
    }
}