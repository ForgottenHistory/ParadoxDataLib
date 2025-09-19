using System;
using System.IO;
using System.Linq;
using ParadoxDataLib.Core.Common;
using ParadoxDataLib.Core.Parsers;
using Xunit;

namespace ParadoxDataLib.Tests
{
    /// <summary>
    /// Unit tests for the GenericParadoxParser class
    /// </summary>
    public class GenericParadoxParserTests
    {
        [Fact]
        public void ParseString_SimpleKeyValue_ParsesCorrectly()
        {
            var parser = new GenericParadoxParser();
            var content = "owner = FRA";

            var result = ParseContentWithTempFile(parser, content);

            Assert.Equal("root", result.Key);
            Assert.Equal(NodeType.Object, result.Type);
            Assert.Single(result.Children);
            Assert.True(result.Children.ContainsKey("owner"));
            Assert.Equal("FRA", result.Children["owner"].Value);
        }

        [Fact]
        public void ParseString_MultipleKeyValues_ParsesCorrectly()
        {
            var parser = new GenericParadoxParser();
            var content = @"
                owner = FRA
                culture = french
                religion = catholic
                base_tax = 3
            ";

            var result = ParseContentWithTempFile(parser, content);

            Assert.Equal(4, result.Children.Count);
            Assert.Equal("FRA", result.GetValue<string>("owner"));
            Assert.Equal("french", result.GetValue<string>("culture"));
            Assert.Equal("catholic", result.GetValue<string>("religion"));
            Assert.Equal(3, result.GetValue<int>("base_tax"));
        }

        [Fact]
        public void ParseString_ObjectBlock_ParsesCorrectly()
        {
            var parser = new GenericParadoxParser();
            var content = @"
                settings = {
                    difficulty = normal
                    lucky_nations = yes
                    custom_difficulty = 2
                }
            ";

            var result = ParseContentWithTempFile(parser, content);

            Assert.Single(result.Children);
            var settings = result.GetChild("settings");
            Assert.NotNull(settings);
            Assert.Equal(NodeType.Object, settings.Type);
            Assert.Equal(3, settings.Children.Count);
            Assert.Equal("normal", settings.GetValue<string>("difficulty"));
            Assert.Equal(true, settings.GetValue<bool>("lucky_nations"));
            Assert.Equal(2, settings.GetValue<int>("custom_difficulty"));
        }

        [Fact]
        public void ParseString_NestedObjects_ParsesCorrectly()
        {
            var parser = new GenericParadoxParser();
            var content = @"
                country = {
                    government = monarchy
                    monarch = {
                        name = ""Louis XI""
                        dynasty = ""de Valois""
                        adm = 5
                        dip = 4
                        mil = 6
                    }
                }
            ";

            var result = ParseContentWithTempFile(parser, content);

            var country = result.GetChild("country");
            Assert.NotNull(country);
            Assert.Equal("monarchy", country.GetValue<string>("government"));

            var monarch = country.GetChild("monarch");
            Assert.NotNull(monarch);
            Assert.Equal("Louis XI", monarch.GetValue<string>("name"));
            Assert.Equal("de Valois", monarch.GetValue<string>("dynasty"));
            Assert.Equal(5, monarch.GetValue<int>("adm"));
            Assert.Equal(4, monarch.GetValue<int>("dip"));
            Assert.Equal(6, monarch.GetValue<int>("mil"));
        }

        [Fact]
        public void ParseString_DateEntries_ParsesCorrectly()
        {
            var parser = new GenericParadoxParser();
            var content = @"
                1444.11.11 = {
                    owner = FRA
                    add_core = FRA
                }
                1500.1.1 = {
                    religion = reformed
                }
            ";

            var result = ParseContentWithTempFile(parser, content);

            Assert.Equal(2, result.Children.Count);

            var date1 = result.GetChild("1444.11.11");
            Assert.NotNull(date1);
            Assert.Equal(NodeType.Date, date1.Type);
            Assert.IsType<DateTime>(date1.Value);
            Assert.Equal("FRA", date1.GetValue<string>("owner"));
            Assert.Equal("FRA", date1.GetValue<string>("add_core"));

            var date2 = result.GetChild("1500.1.1");
            Assert.NotNull(date2);
            Assert.Equal(NodeType.Date, date2.Type);
            Assert.Equal("reformed", date2.GetValue<string>("religion"));
        }

        [Fact]
        public void ParseString_DateWithSimpleValue_ParsesCorrectly()
        {
            var parser = new GenericParadoxParser();
            var content = "birth_date = 1444.11.11";

            var result = ParseContentWithTempFile(parser, content);

            Assert.Single(result.Children);
            var birthDate = result.GetChild("birth_date");
            Assert.NotNull(birthDate);
            Assert.Equal(NodeType.Scalar, birthDate.Type);
            Assert.IsType<DateTime>(birthDate.Value);
        }

        [Fact]
        public void ParseString_BooleanValues_ParsesCorrectly()
        {
            var parser = new GenericParadoxParser();
            var content = @"
                hre = yes
                is_city = no
                enabled = true
                disabled = false
            ";

            var result = ParseContentWithTempFile(parser, content);

            Assert.True(result.GetValue<bool>("hre"));
            Assert.False(result.GetValue<bool>("is_city"));
            Assert.True(result.GetValue<bool>("enabled"));
            Assert.False(result.GetValue<bool>("disabled"));
        }

        [Fact]
        public void ParseString_NumericValues_ParsesCorrectly()
        {
            var parser = new GenericParadoxParser();
            var content = @"
                integer_value = 42
                float_value = 3.14
                negative_value = -10
                zero_value = 0
            ";

            var result = ParseContentWithTempFile(parser, content);

            Assert.Equal(42, result.GetValue<int>("integer_value"));
            Assert.Equal(3.14f, result.GetValue<float>("float_value"), 2);
            Assert.Equal(-10, result.GetValue<int>("negative_value"));
            Assert.Equal(0, result.GetValue<int>("zero_value"));
        }

        [Fact]
        public void ParseString_StringValues_ParsesCorrectly()
        {
            var parser = new GenericParadoxParser();
            var content = @"
                simple_string = test_value
                quoted_string = ""Test String""
                empty_string = """"
            ";

            var result = ParseContentWithTempFile(parser, content);

            Assert.Equal("test_value", result.GetValue<string>("simple_string"));
            Assert.Equal("Test String", result.GetValue<string>("quoted_string"));
            Assert.Equal("", result.GetValue<string>("empty_string"));
        }

        [Fact]
        public void ParseString_Comments_IgnoresComments()
        {
            var parser = new GenericParadoxParser();
            var content = @"
                # This is a comment
                owner = FRA  # Inline comment
                # Another comment
                culture = french
                /* Block comment */
                religion = catholic
            ";

            var result = ParseContentWithTempFile(parser, content);

            Assert.Equal(3, result.Children.Count);
            Assert.Equal("FRA", result.GetValue<string>("owner"));
            Assert.Equal("french", result.GetValue<string>("culture"));
            Assert.Equal("catholic", result.GetValue<string>("religion"));
        }

        [Fact]
        public void ParseString_ComplexFile_ParsesCorrectly()
        {
            var parser = new GenericParadoxParser();
            var content = @"
                # Province file example
                owner = FRA
                controller = FRA
                culture = french
                religion = catholic
                base_tax = 3
                base_production = 2
                base_manpower = 1
                trade_goods = grain
                hre = yes
                discovered_by = western
                discovered_by = muslim
                fort_15th = yes
                add_core = FRA
                add_permanent_province_modifier = {
                    name = fertile_lands
                    local_tax_modifier = 0.1
                    duration = -1
                }
                1444.11.11 = {
                    add_core = FRA
                    discovered_by = western
                }
                1500.1.1 = {
                    religion = reformed
                    remove_core = FRA
                }
            ";

            var result = ParseContentWithTempFile(parser, content);

            Assert.True(result.Children.Count >= 10);
            Assert.Equal("FRA", result.GetValue<string>("owner"));
            Assert.Equal("french", result.GetValue<string>("culture"));
            Assert.Equal(3, result.GetValue<int>("base_tax"));
            Assert.True(result.GetValue<bool>("hre"));

            var modifier = result.GetChild("add_permanent_province_modifier");
            Assert.NotNull(modifier);
            Assert.Equal("fertile_lands", modifier.GetValue<string>("name"));
            Assert.Equal(0.1f, modifier.GetValue<float>("local_tax_modifier"), 2);

            var date1 = result.GetChild("1444.11.11");
            Assert.NotNull(date1);
            Assert.Equal(NodeType.Date, date1.Type);

            var date2 = result.GetChild("1500.1.1");
            Assert.NotNull(date2);
            Assert.Equal("reformed", date2.GetValue<string>("religion"));
        }

        [Fact]
        public void ParseString_MalformedContent_HandlesGracefully()
        {
            var parser = new GenericParadoxParser();
            var content = @"
                owner = FRA
                invalid_line_without_equals
                culture = french
                = invalid_value_without_key
                religion = catholic
            ";

            var result = ParseContentWithTempFile(parser, content);

            // Should still parse valid entries
            Assert.Equal("FRA", result.GetValue<string>("owner"));
            Assert.Equal("french", result.GetValue<string>("culture"));
            Assert.Equal("catholic", result.GetValue<string>("religion"));
        }

        [Fact]
        public void ParseString_EmptyContent_ReturnsEmptyRoot()
        {
            var parser = new GenericParadoxParser();
            var content = "";

            var result = ParseContentWithTempFile(parser, content);

            Assert.Equal("root", result.Key);
            Assert.Equal(NodeType.Object, result.Type);
            Assert.Empty(result.Children);
        }

        [Fact]
        public void ParseString_OnlyComments_ReturnsEmptyRoot()
        {
            var parser = new GenericParadoxParser();
            var content = @"
                # This is a comment
                # Another comment
                /* Block comment */
            ";

            var result = ParseContentWithTempFile(parser, content);

            Assert.Equal("root", result.Key);
            Assert.Equal(NodeType.Object, result.Type);
            Assert.Empty(result.Children);
        }

        [Fact]
        public void ParseString_DuplicateKeys_OverwritesPrevious()
        {
            var parser = new GenericParadoxParser();
            var content = @"
                owner = FRA
                owner = ENG
                owner = CAS
            ";

            var result = ParseContentWithTempFile(parser, content);

            Assert.Single(result.Children);
            Assert.Equal("CAS", result.GetValue<string>("owner"));
        }

        [Fact]
        public void ParseString_BalancedBraces_ParsesCorrectly()
        {
            var parser = new GenericParadoxParser();
            var content = @"
                nested = {
                    level1 = {
                        level2 = {
                            value = deep_value
                        }
                        another_value = test
                    }
                    simple_value = simple
                }
            ";

            var result = ParseContentWithTempFile(parser, content);

            var nested = result.GetChild("nested");
            Assert.NotNull(nested);

            var level1 = nested.GetChild("level1");
            Assert.NotNull(level1);
            Assert.Equal("test", level1.GetValue<string>("another_value"));

            var level2 = level1.GetChild("level2");
            Assert.NotNull(level2);
            Assert.Equal("deep_value", level2.GetValue<string>("value"));

            Assert.Equal("simple", nested.GetValue<string>("simple_value"));
        }

        [Theory]
        [InlineData("1444.11.11")]
        [InlineData("1500.1.1")]
        [InlineData("1.1.1")]
        [InlineData("2023.12.31")]
        public void ParseString_ValidDates_ParsesCorrectly(string dateString)
        {
            var parser = new GenericParadoxParser();
            var content = $"{dateString} = {{ test = value }}";

            var result = ParseContentWithTempFile(parser, content);

            Assert.Single(result.Children);
            var dateNode = result.Children.Values.First();
            Assert.Equal(NodeType.Date, dateNode.Type);
            Assert.IsType<DateTime>(dateNode.Value);
            Assert.Equal("value", dateNode.GetValue<string>("test"));
        }

        /// <summary>
        /// Helper method to parse content using a temporary file
        /// </summary>
        private ParadoxNode ParseContentWithTempFile(GenericParadoxParser parser, string content)
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, content);
                return parser.ParseFile(tempFile);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }
}