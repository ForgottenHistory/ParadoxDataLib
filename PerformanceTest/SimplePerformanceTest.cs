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
using ParadoxDataLib.Core.Parsers.Csv.Specialized;
using ParadoxDataLib.Core.Parsers.Csv;
using ParadoxDataLib.Core.Parsers.Bitmap.Specialized;

namespace ParadoxDataLib.PerformanceTest
{
    /// <summary>
    /// Simplified comprehensive performance testing suite
    /// </summary>
    public class SimplePerformanceTest
    {
        /// <summary>
        /// Runs all performance tests
        /// </summary>
        public async Task RunAllTests()
        {
            Console.WriteLine("=== COMPREHENSIVE PARADOX DATA PARSER PERFORMANCE TEST ===");
            Console.WriteLine("Testing: Generic Parser, CSV Parser, and Bitmap Parser");
            Console.WriteLine();

            var baseDir = FindEuivFilesDirectory();
            if (string.IsNullOrEmpty(baseDir))
            {
                Console.WriteLine("EU IV Files directory not found!");
                Console.WriteLine("Testing with generic parser only...");
                await RunGenericParserTests();
                return;
            }

            Console.WriteLine($"EU IV Files found at: {baseDir}");
            Console.WriteLine();

            await RunCsvTests(baseDir);
            await RunBitmapTests(baseDir);
            await RunGenericParserTests(baseDir);

            Console.WriteLine("=== PERFORMANCE TEST COMPLETE ===");
        }

        private async Task RunCsvTests(string baseDir)
        {
            Console.WriteLine("=== CSV PARSER PERFORMANCE TESTS ===");

            var definitionCsv = Path.Combine(baseDir, "map", "definition.csv");
            var adjacenciesCsv = Path.Combine(baseDir, "map", "adjacencies.csv");

            if (File.Exists(definitionCsv))
            {
                await TestCsvPerformance(definitionCsv, "Province Definitions");
            }

            if (File.Exists(adjacenciesCsv))
            {
                await TestCsvPerformance(adjacenciesCsv, "Adjacencies");
            }

            Console.WriteLine();
        }

        private async Task TestCsvPerformance(string csvFile, string description)
        {
            Console.WriteLine($"{description} ({Path.GetFileName(csvFile)}):");

            try
            {
                if (csvFile.Contains("definition"))
                {
                    var reader = new ProvinceDefinitionReader();
                    var stopwatch = Stopwatch.StartNew();
                    var data = reader.ReadFile(csvFile, out var stats);
                    stopwatch.Stop();

                    Console.WriteLine($"  Rows parsed: {data.Count:N0}");
                    Console.WriteLine($"  Parse time: {stopwatch.ElapsedMilliseconds:N0}ms");
                    Console.WriteLine($"  Throughput: {data.Count / stopwatch.Elapsed.TotalSeconds:F0} rows/second");
                    Console.WriteLine($"  Success/Total: {stats.SuccessfulRows}/{stats.TotalRows}");
                }
                else if (csvFile.Contains("adjacencies"))
                {
                    var reader = new AdjacenciesReader();
                    var stopwatch = Stopwatch.StartNew();
                    var data = reader.ReadFile(csvFile, out var stats);
                    stopwatch.Stop();

                    Console.WriteLine($"  Rows parsed: {data.Count:N0}");
                    Console.WriteLine($"  Parse time: {stopwatch.ElapsedMilliseconds:N0}ms");
                    Console.WriteLine($"  Throughput: {data.Count / stopwatch.Elapsed.TotalSeconds:F0} rows/second");
                    Console.WriteLine($"  Success/Total: {stats.SuccessfulRows}/{stats.TotalRows}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {ex.Message}");
            }
        }

        private async Task RunBitmapTests(string baseDir)
        {
            Console.WriteLine("=== BITMAP PARSER PERFORMANCE TESTS ===");

            var mapDir = Path.Combine(baseDir, "map");
            var bitmapFiles = new[]
            {
                Path.Combine(mapDir, "provinces.bmp"),
                Path.Combine(mapDir, "rivers.bmp"),
                Path.Combine(mapDir, "terrain.bmp")
            }.Where(File.Exists).ToArray();

            if (bitmapFiles.Length == 0)
            {
                Console.WriteLine("No bitmap files found - skipping bitmap tests");
                return;
            }

            // Load province definitions for RGB mapping if available
            Dictionary<int, int> rgbToProvinceMap = null;
            var definitionCsv = Path.Combine(mapDir, "definition.csv");
            if (File.Exists(definitionCsv))
            {
                try
                {
                    var definitionReader = new ProvinceDefinitionReader();
                    var definitions = definitionReader.ReadFile(definitionCsv);

                    // Use last-wins approach for duplicate RGB values
                    rgbToProvinceMap = new Dictionary<int, int>();
                    var duplicateCount = 0;

                    foreach (var definition in definitions)
                    {
                        var rgbValue = (definition.Red << 16) | (definition.Green << 8) | definition.Blue;
                        if (rgbToProvinceMap.ContainsKey(rgbValue))
                        {
                            duplicateCount++;
                            if (duplicateCount <= 3) // Show first few duplicates
                            {
                                Console.WriteLine($"  Warning: RGB {definition.RgbString} shared by provinces {rgbToProvinceMap[rgbValue]} and {definition.ProvinceId} (using {definition.ProvinceId})");
                            }
                        }
                        rgbToProvinceMap[rgbValue] = definition.ProvinceId; // Last wins
                    }

                    Console.WriteLine($"Loaded {rgbToProvinceMap.Count:N0} RGB→Province mappings");
                    if (duplicateCount > 0)
                    {
                        Console.WriteLine($"  Found {duplicateCount} duplicate RGB values (using last-wins approach)");
                        if (duplicateCount > 3)
                        {
                            Console.WriteLine($"  (and {duplicateCount - 3} more duplicates...)");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load RGB mappings: {ex.Message}");
                }
            }

            foreach (var bitmapFile in bitmapFiles)
            {
                await TestBitmapFile(bitmapFile, rgbToProvinceMap);
            }

            Console.WriteLine();
        }

        private async Task TestBitmapFile(string bitmapFile, Dictionary<int, int> rgbToProvinceMap)
        {
            Console.WriteLine($"Testing {Path.GetFileName(bitmapFile)}:");

            var fileSize = new FileInfo(bitmapFile).Length;
            Console.WriteLine($"  File size: {fileSize / (1024.0 * 1024.0):F1}MB");

            try
            {
                var fileName = Path.GetFileName(bitmapFile).ToLower();

                if (fileName.Contains("provinces") && rgbToProvinceMap != null)
                {
                    var provinceMapReader = new ProvinceMapReader(rgbToProvinceMap);

                    var stopwatch = Stopwatch.StartNew();
                    var provinceData = await provinceMapReader.ReadProvinceMapAsync(bitmapFile);
                    stopwatch.Stop();

                    var pixelCount = provinceData.Width * provinceData.Height;
                    var pixelsPerSecond = pixelCount / stopwatch.Elapsed.TotalSeconds;

                    Console.WriteLine($"  Dimensions: {provinceData.Width}x{provinceData.Height} ({pixelCount:N0} pixels)");
                    Console.WriteLine($"  Parse time: {stopwatch.ElapsedMilliseconds:N0}ms");
                    Console.WriteLine($"  Throughput: {pixelsPerSecond:F0} pixels/second");
                    Console.WriteLine($"  Memory usage: ~{(pixelCount * 4) / (1024.0 * 1024.0):F1}MB");
                }
                else
                {
                    // For other bitmap files, use the heightmap reader as a general test
                    var heightmapReader = new HeightmapReader();

                    var stopwatch = Stopwatch.StartNew();
                    var heightmapData = heightmapReader.ReadHeightmap(bitmapFile);
                    stopwatch.Stop();

                    var pixelCount = heightmapData.Width * heightmapData.Height;
                    var pixelsPerSecond = pixelCount / stopwatch.Elapsed.TotalSeconds;

                    Console.WriteLine($"  Dimensions: {heightmapData.Width}x{heightmapData.Height} ({pixelCount:N0} pixels)");
                    Console.WriteLine($"  Parse time: {stopwatch.ElapsedMilliseconds:N0}ms");
                    Console.WriteLine($"  Throughput: {pixelsPerSecond:F0} pixels/second");
                    Console.WriteLine($"  Memory usage: ~{(pixelCount * 4) / (1024.0 * 1024.0):F1}MB");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error: {ex.Message}");
            }
        }

        private async Task RunGenericParserTests(string baseDir = null)
        {
            Console.WriteLine("=== GENERIC PARSER PERFORMANCE TESTS ===");

            if (baseDir == null)
            {
                await RunMicroBenchmark();
                return;
            }

            var provincesDir = Path.Combine(baseDir, "history", "provinces");
            var countriesDir = Path.Combine(baseDir, "history", "countries");

            if (!Directory.Exists(provincesDir))
            {
                Console.WriteLine("Province files not found - running micro-benchmark only");
                await RunMicroBenchmark();
                return;
            }

            var provinceFiles = Directory.GetFiles(provincesDir, "*.txt");
            var countryFiles = Directory.Exists(countriesDir) ? Directory.GetFiles(countriesDir, "*.txt") : Array.Empty<string>();

            Console.WriteLine($"Testing {provinceFiles.Length} province files");
            Console.WriteLine($"Testing {countryFiles.Length} country files");

            // Test province parsing
            await TestGenericProvinceParsing(provinceFiles);

            // Test country parsing
            if (countryFiles.Length > 0)
            {
                await TestGenericCountryParsing(countryFiles);
            }

            // Run micro-benchmark
            await RunMicroBenchmark();

            Console.WriteLine();
        }

        private async Task TestGenericProvinceParsing(string[] provinceFiles)
        {
            var parser = new GenericParadoxParser();
            var provinces = new Dictionary<int, ProvinceData>();

            GC.Collect();
            var startMemory = GC.GetTotalMemory(false);
            var stopwatch = Stopwatch.StartNew();

            await Task.Run(() =>
            {
                foreach (var file in provinceFiles)
                {
                    try
                    {
                        var content = File.ReadAllText(file);
                        var filename = Path.GetFileNameWithoutExtension(file);

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
                    catch (Exception)
                    {
                        // Skip files that can't be parsed
                    }
                }
            });

            stopwatch.Stop();
            var endMemory = GC.GetTotalMemory(false);
            var memoryUsedMB = (endMemory - startMemory) / (1024.0 * 1024.0);

            Console.WriteLine($"Province parsing results:");
            Console.WriteLine($"  Files processed: {provinceFiles.Length:N0}");
            Console.WriteLine($"  Provinces loaded: {provinces.Count:N0}");
            Console.WriteLine($"  Parse time: {stopwatch.ElapsedMilliseconds:N0}ms ({stopwatch.Elapsed.TotalSeconds:F1}s)");
            Console.WriteLine($"  Memory used: {memoryUsedMB:F1}MB");

            if (provinces.Count > 0)
            {
                var timePerProvince = (double)stopwatch.ElapsedMilliseconds / provinces.Count;
                var provincesPerSecond = provinces.Count / stopwatch.Elapsed.TotalSeconds;
                Console.WriteLine($"  Time per province: {timePerProvince:F3}ms");
                Console.WriteLine($"  Throughput: {provincesPerSecond:F0} provinces/second");

                // Project to 13k target
                var projected13kTime = timePerProvince * 13000;
                var projected13kMemory = (memoryUsedMB / provinces.Count) * 13000;
                Console.WriteLine($"  Projected 13k time: {projected13kTime:F0}ms ({projected13kTime/1000:F1}s)");
                Console.WriteLine($"  Projected 13k memory: {projected13kMemory:F0}MB");

                var targetTime = 5000; // 5 seconds
                var targetMemory = 500; // 500MB
                var speedPass = projected13kTime <= targetTime;
                var memoryPass = projected13kMemory <= targetMemory;
                Console.WriteLine($"  Speed vs target (5s): {(speedPass ? "✅ PASS" : "❌ FAIL")} ({projected13kTime/1000:F1}s)");
                Console.WriteLine($"  Memory vs target (500MB): {(memoryPass ? "✅ PASS" : "❌ FAIL")} ({projected13kMemory:F0}MB)");
            }
        }

        private async Task TestGenericCountryParsing(string[] countryFiles)
        {
            var parser = new GenericParadoxParser();
            var countries = new Dictionary<string, CountryData>();

            var stopwatch = Stopwatch.StartNew();

            await Task.Run(() =>
            {
                foreach (var file in countryFiles)
                {
                    try
                    {
                        var content = File.ReadAllText(file);
                        var filename = Path.GetFileNameWithoutExtension(file);
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
                    catch (Exception)
                    {
                        // Skip files that can't be parsed
                    }
                }
            });

            stopwatch.Stop();

            Console.WriteLine($"Country parsing results:");
            Console.WriteLine($"  Files processed: {countryFiles.Length:N0}");
            Console.WriteLine($"  Countries loaded: {countries.Count:N0}");
            Console.WriteLine($"  Parse time: {stopwatch.ElapsedMilliseconds:N0}ms");

            if (countries.Count > 0)
            {
                var timePerCountry = (double)stopwatch.ElapsedMilliseconds / countries.Count;
                var countriesPerSecond = countries.Count / stopwatch.Elapsed.TotalSeconds;
                Console.WriteLine($"  Time per country: {timePerCountry:F3}ms");
                Console.WriteLine($"  Throughput: {countriesPerSecond:F0} countries/second");
            }
        }

        private async Task RunMicroBenchmark()
        {
            Console.WriteLine($"Micro-benchmark (single province, 10,000 iterations):");

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
            var parser = new GenericParadoxParser();
            var extractor = new ProvinceExtractor(120, "Test");

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var node = parser.Parse(sampleContent);
                var result = extractor.Extract(node);
            }
            stopwatch.Stop();

            var avgTime = (double)stopwatch.ElapsedMilliseconds / iterations;
            var throughput = iterations / stopwatch.Elapsed.TotalSeconds;

            Console.WriteLine($"  Average time: {avgTime:F3}ms per province");
            Console.WriteLine($"  Throughput: {throughput:F0} provinces/second");
        }

        /// <summary>
        /// Finds the EU IV Files directory in common locations
        /// </summary>
        private static string FindEuivFilesDirectory()
        {
            var possiblePaths = new[]
            {
                "EU IV Files",
                "./EU IV Files",
                "../EU IV Files",
                "../../EU IV Files"
            };

            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    return Path.GetFullPath(path);
                }
            }

            return null;
        }
    }
}