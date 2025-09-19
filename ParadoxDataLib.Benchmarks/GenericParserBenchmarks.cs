using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using ParadoxDataLib.Core.Common;
using ParadoxDataLib.Core.DataModels;
using ParadoxDataLib.Core.Extractors;
using ParadoxDataLib.Core.Parsers;
using ParadoxDataLib.Core;
using System.Text;

namespace ParadoxDataLib.Benchmarks
{
    /// <summary>
    /// Benchmarks for the generic parser architecture
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80)]
    public class GenericParserBenchmarks
    {
        private string _sampleProvinceContent = "";
        private string _sampleCountryContent = "";
        private string _complexProvinceContent = "";
        private List<string> _multipleProvinceContents = new();
        private GenericParadoxParser _parser = new();
        private ProvinceExtractor _provinceExtractor = new(1, "TestProvince");
        private CountryExtractor _countryExtractor = new("TST", "TestCountry");
        private ParadoxFileReader _fileReader = new();

        [GlobalSetup]
        public void Setup()
        {
            // Simple province content
            _sampleProvinceContent = @"
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
";

            // Simple country content
            _sampleCountryContent = @"
government = monarchy
primary_culture = french
religion = catholic
technology_group = western
capital = 183
";

            // Complex province with historical entries and modifiers
            _complexProvinceContent = @"
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
marketplace = yes
temple = yes
add_core = FRA
add_core = ENG
discovered_by = western
discovered_by = muslim
add_permanent_province_modifier = {
    name = fertile_lands
    local_tax_modifier = 0.1
    duration = -1
}
add_permanent_province_modifier = {
    name = trading_post
    local_trade_power = 0.15
    duration = -1
}
1444.11.11 = {
    owner = ENG
    controller = ENG
    add_core = ENG
    religion = anglican
    discovered_by = western
}
1500.1.1 = {
    religion = reformed
    remove_core = FRA
    add_core = NED
}
1600.3.15 = {
    owner = FRA
    controller = FRA
    culture = francien
}
";

            // Generate multiple province contents for batch testing
            for (int i = 1; i <= 1000; i++)
            {
                var content = $@"
owner = {(i % 2 == 0 ? "FRA" : "ENG")}
controller = {(i % 2 == 0 ? "FRA" : "ENG")}
culture = {(i % 3 == 0 ? "french" : i % 3 == 1 ? "english" : "german")}
religion = {(i % 2 == 0 ? "catholic" : "protestant")}
base_tax = {i % 10 + 1}
base_production = {i % 8 + 1}
base_manpower = {i % 6 + 1}
trade_goods = {(i % 4 == 0 ? "grain" : i % 4 == 1 ? "fish" : i % 4 == 2 ? "cloth" : "iron")}
hre = {(i % 2 == 0 ? "yes" : "no")}
is_city = {(i % 3 == 0 ? "yes" : "no")}
";
                _multipleProvinceContents.Add(content);
            }
        }

        [Benchmark]
        public ParadoxNode ParseSimpleProvince()
        {
            return _parser.Parse(_sampleProvinceContent);
        }

        [Benchmark]
        public ParadoxNode ParseSimpleCountry()
        {
            return _parser.Parse(_sampleCountryContent);
        }

        [Benchmark]
        public ParadoxNode ParseComplexProvince()
        {
            return _parser.Parse(_complexProvinceContent);
        }

        [Benchmark]
        public ProvinceData ExtractSimpleProvinceData()
        {
            var node = _parser.Parse(_sampleProvinceContent);
            return _provinceExtractor.Extract(node);
        }

        [Benchmark]
        public CountryData ExtractSimpleCountryData()
        {
            var node = _parser.Parse(_sampleCountryContent);
            return _countryExtractor.Extract(node);
        }

        [Benchmark]
        public ProvinceData ExtractComplexProvinceData()
        {
            var node = _parser.Parse(_complexProvinceContent);
            return _provinceExtractor.Extract(node);
        }

        [Benchmark]
        public List<ProvinceData> ParseAndExtractMultipleProvinces()
        {
            var results = new List<ProvinceData>();
            for (int i = 0; i < _multipleProvinceContents.Count; i++)
            {
                var extractor = new ProvinceExtractor(i + 1, $"Province{i + 1}");
                var node = _parser.Parse(_multipleProvinceContents[i]);
                results.Add(extractor.Extract(node));
            }
            return results;
        }

        [Benchmark]
        public ProvinceData UseFileReaderAPI()
        {
            var extractor = new ProvinceExtractor(1, "TestProvince");
            return _fileReader.ReadContent(_sampleProvinceContent, extractor);
        }

        [Benchmark]
        public List<ParadoxNode> ParseMultipleFiles()
        {
            var results = new List<ParadoxNode>();
            foreach (var content in _multipleProvinceContents.Take(100))
            {
                results.Add(_parser.Parse(content));
            }
            return results;
        }

        [Benchmark]
        public int CountTokensInComplexFile()
        {
            var node = _parser.Parse(_complexProvinceContent);
            return CountNodes(node);
        }

        private int CountNodes(ParadoxNode node)
        {
            int count = 1;
            foreach (var child in node.Children.Values)
            {
                count += CountNodes(child);
            }
            foreach (var item in node.Items)
            {
                count += CountNodes(item);
            }
            return count;
        }

        // Memory efficiency benchmarks
        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1000)]
        public List<ProvinceData> ParseMultipleProvincesWithCount(int count)
        {
            var results = new List<ProvinceData>();
            for (int i = 0; i < count && i < _multipleProvinceContents.Count; i++)
            {
                var extractor = new ProvinceExtractor(i + 1, $"Province{i + 1}");
                var node = _parser.Parse(_multipleProvinceContents[i]);
                results.Add(extractor.Extract(node));
            }
            return results;
        }

        // Parser vs extraction performance comparison
        [Benchmark]
        public List<ParadoxNode> ParseOnly()
        {
            var results = new List<ParadoxNode>();
            foreach (var content in _multipleProvinceContents.Take(100))
            {
                results.Add(_parser.Parse(content));
            }
            return results;
        }

        [Benchmark]
        public List<ProvinceData> ParseAndExtract()
        {
            var results = new List<ProvinceData>();
            for (int i = 0; i < 100 && i < _multipleProvinceContents.Count; i++)
            {
                var extractor = new ProvinceExtractor(i + 1, $"Province{i + 1}");
                var node = _parser.Parse(_multipleProvinceContents[i]);
                results.Add(extractor.Extract(node));
            }
            return results;
        }

        // String handling benchmarks
        [Benchmark]
        public ParadoxNode ParseWithUnicodeContent()
        {
            var unicodeContent = @"
owner = ""🇫🇷""
name = ""Île-de-France""
culture = français
religion = catholique
description = ""Une province très importante avec des caractères spéciaux""
";
            return _parser.Parse(unicodeContent);
        }

        [Benchmark]
        public ParadoxNode ParseLargeStringValues()
        {
            var largeStringContent = @"
owner = FRA
description = """ + new string('A', 1000) + @"""
long_name = """ + new string('B', 500) + @"""
tooltip = """ + new string('C', 750) + @"""
";
            return _parser.Parse(largeStringContent);
        }
    }

    /// <summary>
    /// Configuration for running specific benchmark categories
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80)]
    public class ParsingOnlyBenchmarks
    {
        private string _complexContent = "";
        private GenericParadoxParser _parser = new();

        [GlobalSetup]
        public void Setup()
        {
            _complexContent = @"
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
        treasury = 50
    }
    modifiers = {
        infantry_power = 0.1
        cavalry_power = 0.05
        discipline = 0.02
    }
    historical_data = {
        1444.11.11 = {
            available = yes
            cost = 0.8
        }
        1500.1.1 = {
            available = no
            disbanded = yes
        }
    }
}
";
        }

        [Benchmark]
        public ParadoxNode ParseNestedStructure()
        {
            return _parser.Parse(_complexContent);
        }

        [Benchmark]
        public int CountNestedLevels()
        {
            var node = _parser.Parse(_complexContent);
            return GetMaxDepth(node, 0);
        }

        private int GetMaxDepth(ParadoxNode node, int currentDepth)
        {
            int maxDepth = currentDepth;
            foreach (var child in node.Children.Values)
            {
                maxDepth = Math.Max(maxDepth, GetMaxDepth(child, currentDepth + 1));
            }
            foreach (var item in node.Items)
            {
                maxDepth = Math.Max(maxDepth, GetMaxDepth(item, currentDepth + 1));
            }
            return maxDepth;
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("ParadoxDataLib Generic Parser Benchmarks");
            Console.WriteLine("=======================================");
            Console.WriteLine();
            Console.WriteLine("Available benchmark categories:");
            Console.WriteLine("1. Generic Parser Benchmarks (comprehensive)");
            Console.WriteLine("2. Parsing Only Benchmarks (parsing performance focus)");
            Console.WriteLine();

            if (args.Length > 0 && args[0] == "parsing-only")
            {
                Console.WriteLine("Running Parsing Only Benchmarks...");
                BenchmarkRunner.Run<ParsingOnlyBenchmarks>();
            }
            else
            {
                Console.WriteLine("Running Generic Parser Benchmarks...");
                BenchmarkRunner.Run<GenericParserBenchmarks>();
            }
        }
    }
}