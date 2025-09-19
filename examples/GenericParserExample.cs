using System;
using System.IO;
using ParadoxDataLib.Core;
using ParadoxDataLib.Core.DataModels;
using ParadoxDataLib.Core.Extractors;

namespace ParadoxDataParser.Examples
{
    /// <summary>
    /// Example demonstrating how to use the generic Paradox parser architecture
    /// to parse different types of Paradox game files.
    /// </summary>
    public class GenericParserExample
    {
        /// <summary>
        /// Main example method demonstrating various parser usage patterns
        /// </summary>
        public static void RunExample()
        {
            var reader = new ParadoxFileReader();

            Console.WriteLine("=== Generic Paradox Parser Example ===\n");

            // Example 1: Parse a province file
            ParseProvinceFile(reader);

            // Example 2: Parse a country file
            ParseCountryFile(reader);

            // Example 3: Parse any file into generic structure
            ParseGenericFile(reader);

            // Example 4: Parse content from string
            ParseContentFromString(reader);

            // Example 5: Validate extractor compatibility
            ValidateExtractors(reader);
        }

        /// <summary>
        /// Example of parsing a province file using ProvinceExtractor
        /// </summary>
        private static void ParseProvinceFile(ParadoxFileReader reader)
        {
            Console.WriteLine("1. Province File Parsing:");

            // Create sample province content
            var provinceContent = @"
                owner = FRA
                controller = FRA
                culture = french
                religion = catholic
                base_tax = 3
                base_production = 2
                base_manpower = 1
                trade_goods = grain
                hre = yes
                fort_15th = yes
                discovered_by = western
                discovered_by = muslim
                add_core = FRA
                add_permanent_province_modifier = {
                    name = fertile_lands
                    local_tax_modifier = 0.1
                    duration = -1
                }
            ";

            try
            {
                var extractor = new ProvinceExtractor(1, "Paris");
                var province = reader.ReadContent(provinceContent, extractor);

                Console.WriteLine($"  Province: {province.Name} (ID: {province.Id})");
                Console.WriteLine($"  Owner: {province.Owner}");
                Console.WriteLine($"  Culture: {province.Culture}");
                Console.WriteLine($"  Religion: {province.Religion}");
                Console.WriteLine($"  Development: Tax={province.BaseTax}, Production={province.BaseProduction}, Manpower={province.BaseManpower}");
                Console.WriteLine($"  Buildings: {string.Join(", ", province.Buildings)}");
                Console.WriteLine($"  Modifiers: {province.Modifiers.Count}");
                Console.WriteLine($"  Discovered by: {string.Join(", ", province.DiscoveredBy)}");
                Console.WriteLine($"  Cores: {string.Join(", ", province.Cores)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {ex.Message}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Example of parsing a country file using CountryExtractor
        /// </summary>
        private static void ParseCountryFile(ParadoxFileReader reader)
        {
            Console.WriteLine("2. Country File Parsing:");

            // Create sample country content
            var countryContent = @"
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
                set_country_flag = formation_flag
            ";

            try
            {
                var extractor = new CountryExtractor("FRA", "France");
                var country = reader.ReadContent(countryContent, extractor);

                Console.WriteLine($"  Country: {country.Name} ({country.Tag})");
                Console.WriteLine($"  Government: {country.Government}");
                Console.WriteLine($"  Primary Culture: {country.PrimaryCulture}");
                Console.WriteLine($"  Religion: {country.Religion}");
                Console.WriteLine($"  Technology Group: {country.TechnologyGroup}");
                Console.WriteLine($"  Capital: {country.Capital}");
                Console.WriteLine($"  Government Reforms: {string.Join(", ", country.GovernmentReforms)}");
                Console.WriteLine($"  Ideas: {country.Ideas.Count}");
                Console.WriteLine($"  Policies: {string.Join(", ", country.Policies)}");
                Console.WriteLine($"  Historical Friends: {string.Join(", ", country.HistoricalFriends)}");
                Console.WriteLine($"  Historical Rivals: {string.Join(", ", country.HistoricalRivals)}");
                Console.WriteLine($"  Modifiers: {country.Modifiers.Count}");
                Console.WriteLine($"  Flags: {country.Flags.Count}");

                if (country.Monarch != null)
                {
                    var monarch = country.Monarch;
                    Console.WriteLine($"  Monarch: {monarch.Name} {monarch.Dynasty} ({monarch.ADM}/{monarch.DIP}/{monarch.MIL})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {ex.Message}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Example of parsing any file into generic ParadoxNode structure
        /// </summary>
        private static void ParseGenericFile(ParadoxFileReader reader)
        {
            Console.WriteLine("3. Generic Structure Parsing:");

            // Create sample content with various Paradox patterns
            var genericContent = @"
                title = ""Example File""
                version = 1.32.2
                date = 1444.11.11
                enabled = yes
                settings = {
                    difficulty = normal
                    lucky_nations = yes
                    custom_nation_difficulty = 0
                }
                countries = {
                    FRA = ""France""
                    ENG = ""England""
                    CAS = ""Castile""
                }
                1444.11.11 = {
                    add_ruler_modifier = {
                        name = diplomatic_ruler
                        duration = -1
                    }
                }
            ";

            try
            {
                var rootNode = reader.ParseContent(genericContent);

                Console.WriteLine($"  Root node has {rootNode.Children.Count} top-level entries:");
                foreach (var child in rootNode.Children.Values)
                {
                    Console.WriteLine($"    {child.Key}: {GetNodeDescription(child)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {ex.Message}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Example of parsing content from a string
        /// </summary>
        private static void ParseContentFromString(ParadoxFileReader reader)
        {
            Console.WriteLine("4. String Content Parsing:");

            var stringContent = @"
                mercenary_company = {
                    name = ""Free Company""
                    sprite = western_mercenary_sprite
                    cost_modifier = 1.0
                    discipline = 0.05
                    infantry = 8
                    cavalry = 4
                    artillery = 0
                    home_province = 183
                }
            ";

            try
            {
                var rootNode = reader.ParseContent(stringContent);
                var mercCompany = rootNode.GetChild("mercenary_company");

                if (mercCompany != null)
                {
                    Console.WriteLine($"  Mercenary Company: {mercCompany.GetValue<string>("name")}");
                    Console.WriteLine($"  Cost Modifier: {mercCompany.GetValue<float>("cost_modifier")}");
                    Console.WriteLine($"  Discipline: {mercCompany.GetValue<float>("discipline")}");
                    Console.WriteLine($"  Infantry: {mercCompany.GetValue<int>("infantry")}");
                    Console.WriteLine($"  Cavalry: {mercCompany.GetValue<int>("cavalry")}");
                    Console.WriteLine($"  Home Province: {mercCompany.GetValue<int>("home_province")}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {ex.Message}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Example of validating extractor compatibility
        /// </summary>
        private static void ValidateExtractors(ParadoxFileReader reader)
        {
            Console.WriteLine("5. Extractor Validation:");

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

            var provinceExtractor = new ProvinceExtractor(1, "Test Province");
            var countryExtractor = new CountryExtractor("TST", "Test Country");

            Console.WriteLine("  Testing province content:");
            Console.WriteLine($"    Province extractor: {(reader.ReadContent(provinceContent, provinceExtractor) != null ? "✓" : "✗")}");

            try
            {
                reader.ReadContent(provinceContent, countryExtractor);
                Console.WriteLine("    Country extractor: ✗ (should have failed)");
            }
            catch
            {
                Console.WriteLine("    Country extractor: ✓ (correctly rejected)");
            }

            Console.WriteLine("  Testing country content:");
            Console.WriteLine($"    Country extractor: {(reader.ReadContent(countryContent, countryExtractor) != null ? "✓" : "✗")}");

            try
            {
                reader.ReadContent(countryContent, provinceExtractor);
                Console.WriteLine("    Province extractor: ✗ (should have failed)");
            }
            catch
            {
                Console.WriteLine("    Province extractor: ✓ (correctly rejected)");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Helper method to describe a ParadoxNode
        /// </summary>
        private static string GetNodeDescription(ParadoxDataLib.Core.Common.ParadoxNode node)
        {
            return node.Type switch
            {
                ParadoxDataLib.Core.Common.NodeType.Scalar => $"'{node.Value}' ({node.Value?.GetType().Name})",
                ParadoxDataLib.Core.Common.NodeType.Object => $"Object with {node.Children.Count} children",
                ParadoxDataLib.Core.Common.NodeType.List => $"List with {node.Items.Count} items",
                ParadoxDataLib.Core.Common.NodeType.Date => $"Date '{node.Value}'",
                _ => "Unknown"
            };
        }
    }
}