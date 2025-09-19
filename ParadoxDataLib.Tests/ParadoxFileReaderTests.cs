using System;
using System.IO;
using System.Linq;
using ParadoxDataLib.Core;
using ParadoxDataLib.Core.Common;
using ParadoxDataLib.Core.DataModels;
using ParadoxDataLib.Core.Extractors;
using Xunit;

namespace ParadoxDataLib.Tests
{
    /// <summary>
    /// Integration tests for the ParadoxFileReader class
    /// </summary>
    public class ParadoxFileReaderTests
    {
        private readonly string _testDirectory;

        public ParadoxFileReaderTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "ParadoxFileReaderTests");
            Directory.CreateDirectory(_testDirectory);
        }

        [Fact]
        public void ReadContent_ProvinceData_ExtractsCorrectly()
        {
            var reader = new ParadoxFileReader();
            var extractor = new ProvinceExtractor(183, "Paris");
            var content = @"
                owner = FRA
                controller = FRA
                culture = french
                religion = catholic
                base_tax = 3
                base_production = 2
                base_manpower = 1
                trade_goods = grain
                hre = yes
                is_city = yes
                fort_15th = yes
                add_core = FRA
                add_permanent_province_modifier = {
                    name = fertile_lands
                    local_tax_modifier = 0.1
                    duration = -1
                }
            ";

            var result = reader.ReadContent(content, extractor);

            Assert.Equal(183, result.ProvinceId);
            Assert.Equal("Paris", result.Name);
            Assert.Equal("FRA", result.Owner);
            Assert.Equal("french", result.Culture);
            Assert.Equal(3.0f, result.BaseTax);
            Assert.True(result.IsHre);
            Assert.Contains("fort_15th", result.Buildings);
            Assert.Contains("FRA", result.Cores);
            Assert.NotEmpty(result.Modifiers);
        }

        [Fact]
        public void ReadContent_CountryData_ExtractsCorrectly()
        {
            var reader = new ParadoxFileReader();
            var extractor = new CountryExtractor("FRA", "France");
            var content = @"
                government = monarchy
                primary_culture = french
                religion = catholic
                technology_group = western
                capital = 183
                add_government_reform = feudalism_reform
                add_accepted_culture = cosmopolitan_french
                historical_friend = ENG
                historical_rival = HRE
                administrative_ideas = 7
                diplomatic_ideas = 3
                add_idea = national_bank
                add_active_policy = land_acquisition_act
                monarch = {
                    name = ""Louis XI""
                    dynasty = ""de Valois""
                    adm = 5
                    dip = 4
                    mil = 6
                    culture = french
                    religion = catholic
                }
                prestige = 25
                stability = 2
            ";

            var result = reader.ReadContent(content, extractor);

            Assert.Equal("FRA", result.Tag);
            Assert.Equal("France", result.Name);
            Assert.Equal("monarchy", result.Government);
            Assert.Equal("french", result.PrimaryCulture);
            Assert.Equal(183, result.Capital);
            Assert.Contains("feudalism_reform", result.GovernmentReforms);
            Assert.Contains("cosmopolitan_french", result.AcceptedCultures);
            Assert.Contains("ENG", result.HistoricalFriends);
            Assert.Contains("HRE", result.HistoricalRivals);
            Assert.Contains("administrative_ideas", result.Ideas.Keys);
            Assert.Equal(7, result.Ideas["administrative_ideas"]);
            Assert.Contains("land_acquisition_act", result.Policies);
            // Assert.NotNull(result.Monarch); // Ruler is a struct, cannot be null
            Assert.Equal("Louis XI", result.Monarch.Name);
            Assert.NotEmpty(result.Modifiers); // Should have prestige and stability modifiers
        }

        [Fact]
        public void ReadFile_ValidFile_ExtractsCorrectly()
        {
            var reader = new ParadoxFileReader();
            var extractor = new ProvinceExtractor(1, "TestProvince");
            var content = @"
                owner = FRA
                culture = french
                base_tax = 3
            ";

            var testFile = Path.Combine(_testDirectory, "test_province.txt");
            File.WriteAllText(testFile, content);

            var result = reader.ReadFile(testFile, extractor);

            Assert.Equal(1, result.ProvinceId);
            Assert.Equal("TestProvince", result.Name);
            Assert.Equal("FRA", result.Owner);
            Assert.Equal("french", result.Culture);
            Assert.Equal(3.0f, result.BaseTax);

            File.Delete(testFile);
        }

        [Fact]
        public void ReadFile_NonExistentFile_ThrowsFileNotFoundException()
        {
            var reader = new ParadoxFileReader();
            var extractor = new ProvinceExtractor(1, "Test");
            var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.txt");

            Assert.Throws<FileNotFoundException>(() => reader.ReadFile(nonExistentFile, extractor));
        }

        [Fact]
        public void ReadContent_IncompatibleExtractor_ThrowsInvalidOperationException()
        {
            var reader = new ParadoxFileReader();
            var provinceExtractor = new ProvinceExtractor(1, "Test");
            var countryContent = @"
                government = monarchy
                primary_culture = french
            ";

            Assert.Throws<InvalidOperationException>(() => reader.ReadContent(countryContent, provinceExtractor));
        }

        [Fact]
        public void ParseContent_GenericStructure_ReturnsParadoxNode()
        {
            var reader = new ParadoxFileReader();
            var content = @"
                title = ""Test File""
                version = 1.32.2
                settings = {
                    difficulty = normal
                    lucky_nations = yes
                }
                1444.11.11 = {
                    event = startup
                }
            ";

            var result = reader.ParseContent(content);

            Assert.Equal("root", result.Key);
            Assert.Equal(NodeType.Object, result.Type);
            Assert.Equal(4, result.Children.Count);
            Assert.Equal("Test File", result.GetValue<string>("title"));
            Assert.Equal("1.32.2", result.GetValue<string>("version"));

            var settings = result.GetChild("settings");
            Assert.NotNull(settings);
            Assert.Equal("normal", settings.GetValue<string>("difficulty"));
            Assert.True(settings.GetValue<bool>("lucky_nations"));

            var dateEntry = result.GetChild("1444.11.11");
            Assert.NotNull(dateEntry);
            Assert.Equal(NodeType.Date, dateEntry.Type);
            Assert.Equal("startup", dateEntry.GetValue<string>("event"));
        }

        [Fact]
        public void ParseFile_ValidFile_ReturnsParadoxNode()
        {
            var reader = new ParadoxFileReader();
            var content = @"
                test_key = test_value
                test_number = 42
            ";

            var testFile = Path.Combine(_testDirectory, "test_generic.txt");
            File.WriteAllText(testFile, content);

            var result = reader.ParseFile(testFile);

            Assert.Equal("root", result.Key);
            Assert.Equal(NodeType.Object, result.Type);
            Assert.Equal("test_value", result.GetValue<string>("test_key"));
            Assert.Equal(42, result.GetValue<int>("test_number"));

            File.Delete(testFile);
        }

        [Fact]
        public void CanExtract_CompatibleContent_ReturnsTrue()
        {
            var reader = new ParadoxFileReader();
            var extractor = new ProvinceExtractor(1, "Test");
            var provinceContent = @"
                owner = FRA
                base_tax = 3
            ";

            var testFile = Path.Combine(_testDirectory, "test_compatible.txt");
            File.WriteAllText(testFile, provinceContent);

            var result = reader.CanExtract(testFile, extractor);

            Assert.True(result);

            File.Delete(testFile);
        }

        [Fact]
        public void CanExtract_IncompatibleContent_ReturnsFalse()
        {
            var reader = new ParadoxFileReader();
            var provinceExtractor = new ProvinceExtractor(1, "Test");
            var countryContent = @"
                government = monarchy
                primary_culture = french
            ";

            var testFile = Path.Combine(_testDirectory, "test_incompatible.txt");
            File.WriteAllText(testFile, countryContent);

            var result = reader.CanExtract(testFile, provinceExtractor);

            Assert.False(result);

            File.Delete(testFile);
        }

        [Fact]
        public void CanExtract_NonExistentFile_ReturnsFalse()
        {
            var reader = new ParadoxFileReader();
            var extractor = new ProvinceExtractor(1, "Test");
            var nonExistentFile = Path.Combine(_testDirectory, "nonexistent.txt");

            var result = reader.CanExtract(nonExistentFile, extractor);

            Assert.False(result);
        }

        [Fact]
        public void ReadContent_ComplexMercenaryCompany_ParsesCorrectly()
        {
            var reader = new ParadoxFileReader();
            var content = @"
                mercenary_company = {
                    name = ""Free Company""
                    sprite = western_mercenary_sprite
                    cost_modifier = 1.0
                    discipline = 0.05
                    infantry = 8
                    cavalry = 4
                    artillery = 0
                    home_province = 183
                    trigger = {
                        owns = 183
                        is_at_war = yes
                    }
                    modifiers = {
                        infantry_power = 0.1
                        cavalry_power = 0.05
                    }
                }
            ";

            var result = reader.ParseContent(content);

            var mercCompany = result.GetChild("mercenary_company");
            Assert.NotNull(mercCompany);
            Assert.Equal("Free Company", mercCompany.GetValue<string>("name"));
            Assert.Equal(1.0f, mercCompany.GetValue<float>("cost_modifier"));
            Assert.Equal(8, mercCompany.GetValue<int>("infantry"));
            Assert.Equal(183, mercCompany.GetValue<int>("home_province"));

            var trigger = mercCompany.GetChild("trigger");
            Assert.NotNull(trigger);
            Assert.Equal(183, trigger.GetValue<int>("owns"));
            Assert.True(trigger.GetValue<bool>("is_at_war"));

            var modifiers = mercCompany.GetChild("modifiers");
            Assert.NotNull(modifiers);
            Assert.Equal(0.1f, modifiers.GetValue<float>("infantry_power"));
        }

        [Fact]
        public void ReadContent_MultipleExtractors_WorksIndependently()
        {
            var reader = new ParadoxFileReader();

            var provinceContent = @"
                owner = FRA
                base_tax = 3
                culture = french
            ";

            var countryContent = @"
                government = monarchy
                primary_culture = french
                capital = 183
            ";

            var provinceExtractor = new ProvinceExtractor(1, "TestProvince");
            var countryExtractor = new CountryExtractor("TST", "TestCountry");

            var provinceResult = reader.ReadContent(provinceContent, provinceExtractor);
            var countryResult = reader.ReadContent(countryContent, countryExtractor);

            Assert.Equal("FRA", provinceResult.Owner);
            Assert.Equal(3.0f, provinceResult.BaseTax);

            Assert.Equal("monarchy", countryResult.Government);
            Assert.Equal(183, countryResult.Capital);
        }

        [Fact]
        public void ReadContent_WithComments_IgnoresComments()
        {
            var reader = new ParadoxFileReader();
            var extractor = new ProvinceExtractor(1, "Test");
            var content = @"
                # This is a comment
                owner = FRA  # Inline comment
                /* Block comment */
                culture = french
                base_tax = 3  # Another comment
            ";

            var result = reader.ReadContent(content, extractor);

            Assert.Equal("FRA", result.Owner);
            Assert.Equal("french", result.Culture);
            Assert.Equal(3.0f, result.BaseTax);
        }

        [Fact]
        public void ReadContent_WithHistoricalEntries_ExtractsCorrectly()
        {
            var reader = new ParadoxFileReader();
            var extractor = new ProvinceExtractor(1, "Test");
            var content = @"
                owner = FRA
                culture = french
                1444.11.11 = {
                    owner = ENG
                    add_core = ENG
                }
                1500.1.1 = {
                    religion = reformed
                    remove_core = FRA
                }
            ";

            var result = reader.ReadContent(content, extractor);

            Assert.Equal("FRA", result.Owner); // Base owner
            Assert.Equal("french", result.Culture);
            Assert.Equal(2, result.HistoricalEntries.Count);

            var firstEntry = result.HistoricalEntries.First();
            Assert.Equal(new DateTime(1444, 11, 11), firstEntry.Date);
            Assert.Contains("owner", firstEntry.Changes.Keys);
            Assert.Contains("add_core", firstEntry.Changes.Keys);

            var secondEntry = result.HistoricalEntries.Last();
            Assert.Equal(new DateTime(1500, 1, 1), secondEntry.Date);
            Assert.Contains("religion", secondEntry.Changes.Keys);
            Assert.Contains("remove_core", secondEntry.Changes.Keys);
        }
    }
}