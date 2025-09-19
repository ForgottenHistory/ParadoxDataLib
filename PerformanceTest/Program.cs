using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ParadoxDataLib.Core.Common;
using ParadoxDataLib.Core.DataModels;
using ParadoxDataLib.Core.Extractors;
using ParadoxDataLib.Core.Parsers;

Console.WriteLine("=== PARADOX DATA PARSER PERFORMANCE TEST ===");
Console.WriteLine();

var provincesDir = @"D:\Stuff\Coding\ParadoxDataParser\EU IV Files\history\provinces";
var countriesDir = @"D:\Stuff\Coding\ParadoxDataParser\EU IV Files\history\countries";

if (!Directory.Exists(provincesDir))
{
    Console.WriteLine("EU IV Files not found!");
    return;
}

// Count files
var provinceFiles = Directory.GetFiles(provincesDir, "*.txt");
var countryFiles = Directory.Exists(countriesDir) ? Directory.GetFiles(countriesDir, "*.txt") : Array.Empty<string>();

Console.WriteLine($"Found {provinceFiles.Length} province files");
Console.WriteLine($"Found {countryFiles.Length} country files");
Console.WriteLine();

// Force garbage collection for accurate memory measurement
GC.Collect();
GC.WaitForPendingFinalizers();
GC.Collect();

var startMemory = GC.GetTotalMemory(false);
var totalStopwatch = Stopwatch.StartNew();

// Test province loading with generic parser
var parser = new GenericParadoxParser();
var provinces = new Dictionary<int, ProvinceData>();
var countries = new Dictionary<string, CountryData>();

var provinceStopwatch = Stopwatch.StartNew();

await Task.Run(() =>
{
    foreach (var file in provinceFiles)
    {
        try
        {
            var content = File.ReadAllText(file);
            var filename = Path.GetFileNameWithoutExtension(file);

            // Extract province ID from filename (e.g., "1 - Stockholm.txt" -> 1)
            if (int.TryParse(filename.Split(' ')[0], out var provinceId))
            {
                var provinceName = filename.Contains(" - ") ? filename.Split(" - ")[1] : $"Province{provinceId}";
                var extractor = new ProvinceExtractor(provinceId, provinceName);
                var node = parser.Parse(content);

                if (extractor.CanExtract(node))
                {
                    var provinceData = extractor.Extract(node);
                    provinces[provinceId] = provinceData;
                }
            }
        }
        catch (Exception ex)
        {
            // Skip files that can't be parsed
            Console.WriteLine($"Warning: Could not parse {Path.GetFileName(file)}: {ex.Message}");
        }
    }
});

provinceStopwatch.Stop();

// Test country loading
var countryStopwatch = Stopwatch.StartNew();

if (Directory.Exists(countriesDir))
{
    await Task.Run(() =>
    {
        foreach (var file in countryFiles)
        {
            try
            {
                var content = File.ReadAllText(file);
                var filename = Path.GetFileNameWithoutExtension(file);

                // Extract country tag from filename (e.g., "FRA - France.txt" -> "FRA")
                var countryTag = filename.Split(' ')[0];
                var countryName = filename.Contains(" - ") ? filename.Split(" - ")[1] : countryTag;

                var extractor = new CountryExtractor(countryTag, countryName);
                var node = parser.Parse(content);

                if (extractor.CanExtract(node))
                {
                    var countryData = extractor.Extract(node);
                    countries[countryTag] = countryData;
                }
            }
            catch (Exception ex)
            {
                // Skip files that can't be parsed
                Console.WriteLine($"Warning: Could not parse {Path.GetFileName(file)}: {ex.Message}");
            }
        }
    });
}

countryStopwatch.Stop();
totalStopwatch.Stop();

var endMemory = GC.GetTotalMemory(false);
var memoryUsedBytes = endMemory - startMemory;
var memoryUsedMB = memoryUsedBytes / (1024.0 * 1024.0);

// Calculate statistics
var provinceStats = CalculateProvinceStatistics(provinces.Values);

Console.WriteLine("=== PERFORMANCE RESULTS ===");
Console.WriteLine($"Provinces loaded: {provinces.Count:N0}");
Console.WriteLine($"Countries loaded: {countries.Count:N0}");
Console.WriteLine($"Province load time: {provinceStopwatch.ElapsedMilliseconds:N0}ms ({provinceStopwatch.Elapsed.TotalSeconds:F2}s)");
Console.WriteLine($"Country load time: {countryStopwatch.ElapsedMilliseconds:N0}ms ({countryStopwatch.Elapsed.TotalSeconds:F2}s)");
Console.WriteLine($"Total load time: {totalStopwatch.ElapsedMilliseconds:N0}ms ({totalStopwatch.Elapsed.TotalSeconds:F2}s)");
Console.WriteLine($"Memory used: {memoryUsedMB:F1}MB ({memoryUsedBytes:N0} bytes)");
Console.WriteLine();

if (provinces.Count > 0)
{
    var timePerProvince = (double)provinceStopwatch.ElapsedMilliseconds / provinces.Count;
    var provincesPerSecond = provinces.Count / provinceStopwatch.Elapsed.TotalSeconds;

    // Project to 13k provinces (target from guide)
    var projected13kTime = timePerProvince * 13000;
    var projected13kMemory = (memoryUsedMB / provinces.Count) * 13000;

    Console.WriteLine("=== THROUGHPUT ANALYSIS ===");
    Console.WriteLine($"Time per province: {timePerProvince:F3}ms");
    Console.WriteLine($"Provinces per second: {provincesPerSecond:F0}");
    Console.WriteLine($"Memory per province: {(memoryUsedMB / provinces.Count):F3}MB");
    Console.WriteLine();

    Console.WriteLine("=== SCALING PROJECTION ===");
    Console.WriteLine($"Projected 13k province load time: {projected13kTime:F0}ms ({projected13kTime/1000:F1}s)");
    Console.WriteLine($"Projected 13k province memory: {projected13kMemory:F0}MB");
    Console.WriteLine();

    // Compare to targets
    var targetTime = 5000; // 5 seconds
    var targetMemory = 500; // 500MB

    Console.WriteLine("=== PERFORMANCE GOALS ===");
    Console.WriteLine($"Target: Load 13k provinces in <{targetTime}ms (5s)");
    Console.WriteLine($"Projected: {projected13kTime:F0}ms ({projected13kTime/1000:F1}s)");
    var speedPass = projected13kTime <= targetTime;
    var speedMultiplier = speedPass ? targetTime/projected13kTime : projected13kTime/targetTime;
    Console.WriteLine($"Speed result: {(speedPass ? "✅ PASS" : "❌ FAIL")} " +
                    $"({speedMultiplier:F1}x {(speedPass ? "faster" : "slower")})");
    Console.WriteLine();

    Console.WriteLine($"Target: Use <{targetMemory}MB RAM");
    Console.WriteLine($"Projected: {projected13kMemory:F0}MB");
    var memoryPass = projected13kMemory <= targetMemory;
    var memoryMultiplier = memoryPass ? targetMemory/projected13kMemory : projected13kMemory/targetMemory;
    Console.WriteLine($"Memory result: {(memoryPass ? "✅ PASS" : "❌ FAIL")} " +
                    $"({memoryMultiplier:F1}x {(memoryPass ? "better" : "worse")})");
    Console.WriteLine();

    Console.WriteLine("=== DATA STATISTICS ===");
    Console.WriteLine($"Total base tax: {provinceStats.TotalBaseTax:F0}");
    Console.WriteLine($"Total base production: {provinceStats.TotalBaseProduction:F0}");
    Console.WriteLine($"Total base manpower: {provinceStats.TotalBaseManpower:F0}");
    Console.WriteLine($"Unique owners: {provinceStats.ProvincesByOwner.Count}");
    Console.WriteLine($"Unique cultures: {provinceStats.ProvincesByCulture.Count}");
    Console.WriteLine($"Unique religions: {provinceStats.ProvincesByReligion.Count}");
    Console.WriteLine($"Unique trade goods: {provinceStats.ProvincesByTradeGood.Count}");
    Console.WriteLine();

    Console.WriteLine("=== TOP STATISTICS ===");
    Console.WriteLine("Top 5 owners by province count:");
    foreach(var kvp in provinceStats.ProvincesByOwner.OrderByDescending(x => x.Value).Take(5))
    {
        Console.WriteLine($"  {kvp.Key}: {kvp.Value} provinces");
    }
    Console.WriteLine();

    Console.WriteLine("Top 5 cultures by province count:");
    foreach(var kvp in provinceStats.ProvincesByCulture.OrderByDescending(x => x.Value).Take(5))
    {
        Console.WriteLine($"  {kvp.Key}: {kvp.Value} provinces");
    }
}

// Single province parsing speed test
Console.WriteLine();
Console.WriteLine("=== MICRO-BENCHMARK ===");
var sampleContent = @"#120 - Abbruzzi
owner = NAP
controller = NAP
culture = neapolitan
religion = catholic
hre = no
base_tax = 5
base_production = 5
trade_goods = wine
base_manpower = 3
capital = ""L'Aquila""
is_city = yes
add_core = NAP
discovered_by = western
1494.1.1 = { add_core = FRA }
1495.2.22 = { controller = FRA }";

var iterations = 10000;
var microParser = new GenericParadoxParser();
var microExtractor = new ProvinceExtractor(120, "Test");

var microStopwatch = Stopwatch.StartNew();
for (int i = 0; i < iterations; i++)
{
    var node = microParser.Parse(sampleContent);
    var parseResult = microExtractor.Extract(node);
}
microStopwatch.Stop();

var avgParseTime = (double)microStopwatch.ElapsedMilliseconds / iterations;
var singleProvinceThroughput = iterations / microStopwatch.Elapsed.TotalSeconds;

Console.WriteLine($"Single province parse + extract (average of {iterations:N0} runs):");
Console.WriteLine($"  Time: {avgParseTime:F3}ms per province");
Console.WriteLine($"  Throughput: {singleProvinceThroughput:F0} provinces/second");

// Test generic parser features
Console.WriteLine();
Console.WriteLine("=== GENERIC PARSER FEATURES TEST ===");
var detailedContent = @"
owner = TST
culture = test
religion = test_faith
base_tax = 5

add_permanent_province_modifier = {
    name = ""test_modifier""
    local_tax_modifier = 0.25
    local_defensiveness = 0.15
}

local_production_efficiency = 0.2
local_manpower_modifier = 0.1

1444.11.11 = {
    owner = FRA
    add_core = FRA
}
";

var detailedParser = new GenericParadoxParser();
var detailedExtractor = new ProvinceExtractor(999, "Detailed Test");

var detailedStopwatch = Stopwatch.StartNew();
var detailedNode = detailedParser.Parse(detailedContent);
var detailedResult = detailedExtractor.Extract(detailedNode);
detailedStopwatch.Stop();

Console.WriteLine($"Complex province parsing time: {detailedStopwatch.ElapsedMilliseconds:F2}ms");
Console.WriteLine($"Province ID: {detailedResult.ProvinceId} ({detailedResult.Name})");
Console.WriteLine($"Base stats: {detailedResult.BaseTax} tax, {detailedResult.BaseProduction} production");
Console.WriteLine($"Modifiers parsed: {detailedResult.Modifiers.Count}");
Console.WriteLine($"Historical entries: {detailedResult.HistoricalEntries.Count}");

if (detailedResult.Modifiers.Count > 0)
{
    Console.WriteLine("Modifier details:");
    foreach (var modifier in detailedResult.Modifiers)
    {
        Console.WriteLine($"  {modifier.Name}: {modifier.Effects.Count} effects");
        foreach (var effect in modifier.Effects)
        {
            Console.WriteLine($"    {effect.Key}: {effect.Value:+0.##;-0.##;0}");
        }
    }
}

if (detailedResult.HistoricalEntries.Count > 0)
{
    Console.WriteLine("Historical entries:");
    foreach (var entry in detailedResult.HistoricalEntries)
    {
        Console.WriteLine($"  {entry.Date:yyyy.MM.dd}: {entry.Changes.Count} changes");
    }
}

// Helper method to calculate province statistics
static (float TotalBaseTax, float TotalBaseProduction, float TotalBaseManpower,
        Dictionary<string, int> ProvincesByOwner, Dictionary<string, int> ProvincesByCulture,
        Dictionary<string, int> ProvincesByReligion, Dictionary<string, int> ProvincesByTradeGood)
        CalculateProvinceStatistics(IEnumerable<ProvinceData> provinces)
{
    var stats = (
        TotalBaseTax: 0f,
        TotalBaseProduction: 0f,
        TotalBaseManpower: 0f,
        ProvincesByOwner: new Dictionary<string, int>(),
        ProvincesByCulture: new Dictionary<string, int>(),
        ProvincesByReligion: new Dictionary<string, int>(),
        ProvincesByTradeGood: new Dictionary<string, int>()
    );

    foreach (var province in provinces)
    {
        stats.TotalBaseTax += province.BaseTax;
        stats.TotalBaseProduction += province.BaseProduction;
        stats.TotalBaseManpower += province.BaseManpower;

        // Count by owner
        if (!string.IsNullOrEmpty(province.Owner))
        {
            stats.ProvincesByOwner[province.Owner] = stats.ProvincesByOwner.GetValueOrDefault(province.Owner, 0) + 1;
        }

        // Count by culture
        if (!string.IsNullOrEmpty(province.Culture))
        {
            stats.ProvincesByCulture[province.Culture] = stats.ProvincesByCulture.GetValueOrDefault(province.Culture, 0) + 1;
        }

        // Count by religion
        if (!string.IsNullOrEmpty(province.Religion))
        {
            stats.ProvincesByReligion[province.Religion] = stats.ProvincesByReligion.GetValueOrDefault(province.Religion, 0) + 1;
        }

        // Count by trade good
        if (!string.IsNullOrEmpty(province.TradeGood))
        {
            stats.ProvincesByTradeGood[province.TradeGood] = stats.ProvincesByTradeGood.GetValueOrDefault(province.TradeGood, 0) + 1;
        }
    }

    return stats;
}
