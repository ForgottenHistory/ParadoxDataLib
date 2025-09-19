using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ParadoxDataLib.Core.Parsers;
using ParadoxDataLib.Core.Parsers.Csv.Specialized;
using ParadoxDataLib.Core.Parsers.Bitmap;
using ParadoxDataLib.Core.Parsers.Bitmap.Specialized;
using ParadoxDataLib.Core.Parsers.Bitmap.Interpreters;

namespace ParadoxDataLib.PerformanceTest
{
    /// <summary>
    /// Comprehensive test runner for all Paradox file types (txt, csv, bitmap)
    /// </summary>
    public class MassiveParadoxTest
    {
        private readonly string _baseDir;
        private readonly ParsingStatistics _stats;
        private readonly GenericParadoxParser _textParser;
        private Dictionary<int, int> _rgbToProvinceMap;

        public MassiveParadoxTest()
        {
            _baseDir = FindEuivFilesDirectory();
            if (string.IsNullOrEmpty(_baseDir))
                throw new DirectoryNotFoundException("EU IV Files directory not found");

            _stats = new ParsingStatistics();
            _textParser = new GenericParadoxParser();
        }

        /// <summary>
        /// Runs the complete massive test on all file types
        /// </summary>
        public async Task RunCompleteTest()
        {
            Console.WriteLine("=== MASSIVE PARADOX DATA TEST ===");
            Console.WriteLine("Testing TXT, CSV, and BITMAP files across history and common folders...");
            Console.WriteLine();

            // Discover all files
            var allFiles = DiscoverAllFiles();
            Console.WriteLine($"Discovered {allFiles.Count:N0} files to test");

            // Show breakdown by type
            var byType = allFiles.GroupBy(f => Path.GetExtension(f).ToLower());
            foreach (var group in byType.OrderByDescending(g => g.Count()))
            {
                Console.WriteLine($"  {group.Key}: {group.Count():N0} files");
            }
            Console.WriteLine();

            _stats.Start();

            // Load RGB mappings for bitmap files
            await LoadRgbMappings();

            // Process files in parallel batches
            var batchSize = 100;
            var batches = allFiles.Chunk(batchSize).ToList();
            var processedFiles = 0;

            foreach (var batch in batches)
            {
                await ProcessBatch(batch);
                processedFiles += batch.Length;

                // Show progress
                var progress = (double)processedFiles / allFiles.Count * 100;
                var bar = CreateProgressBar(progress);
                Console.Write($"\r{bar} {progress:F1}% - {processedFiles:N0}/{allFiles.Count:N0} files");
            }

            Console.WriteLine();
            Console.WriteLine();

            _stats.Stop();

            // Generate and display report
            var report = _stats.GenerateReport();
            Console.WriteLine(report);
        }

        /// <summary>
        /// Discovers all testable files in the EU IV directory
        /// </summary>
        private List<string> DiscoverAllFiles()
        {
            var files = new List<string>();

            // Add history files
            var historyDir = Path.Combine(_baseDir, "history");
            if (Directory.Exists(historyDir))
            {
                files.AddRange(Directory.GetFiles(historyDir, "*.txt", SearchOption.AllDirectories));
            }

            // Add common files
            var commonDir = Path.Combine(_baseDir, "common");
            if (Directory.Exists(commonDir))
            {
                files.AddRange(Directory.GetFiles(commonDir, "*.txt", SearchOption.AllDirectories));
            }

            // Add map files (CSV and BMP)
            var mapDir = Path.Combine(_baseDir, "map");
            if (Directory.Exists(mapDir))
            {
                files.AddRange(Directory.GetFiles(mapDir, "*.csv", SearchOption.AllDirectories));
                files.AddRange(Directory.GetFiles(mapDir, "*.bmp", SearchOption.AllDirectories));
            }

            return files.OrderBy(f => f).ToList();
        }

        /// <summary>
        /// Loads RGB to province mappings for bitmap parsing
        /// </summary>
        private async Task LoadRgbMappings()
        {
            var definitionCsv = Path.Combine(_baseDir, "map", "definition.csv");
            if (!File.Exists(definitionCsv))
            {
                Console.WriteLine("Warning: definition.csv not found - bitmap parsing will be limited");
                return;
            }

            try
            {
                var definitionReader = new ProvinceDefinitionReader();
                var definitions = definitionReader.ReadFile(definitionCsv);

                _rgbToProvinceMap = new Dictionary<int, int>();
                var duplicateCount = 0;

                foreach (var definition in definitions)
                {
                    var rgbValue = (definition.Red << 16) | (definition.Green << 8) | definition.Blue;
                    if (_rgbToProvinceMap.ContainsKey(rgbValue))
                    {
                        duplicateCount++;
                    }
                    _rgbToProvinceMap[rgbValue] = definition.ProvinceId; // Last wins
                }

                Console.WriteLine($"Loaded {_rgbToProvinceMap.Count:N0} RGB→Province mappings");
                if (duplicateCount > 0)
                {
                    Console.WriteLine($"  Found {duplicateCount} duplicate RGB values (using last-wins approach)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load RGB mappings: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes a batch of files in parallel
        /// </summary>
        private async Task ProcessBatch(string[] batch)
        {
            var tasks = batch.Select(ProcessSingleFile).ToArray();
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Processes a single file based on its type
        /// </summary>
        private async Task ProcessSingleFile(string filePath)
        {
            var stopwatch = Stopwatch.StartNew();
            var success = false;
            Exception error = null;

            try
            {
                var extension = Path.GetExtension(filePath).ToLower();

                switch (extension)
                {
                    case ".txt":
                        success = await ProcessTextFile(filePath);
                        break;
                    case ".csv":
                        success = await ProcessCsvFile(filePath);
                        break;
                    case ".bmp":
                        success = await ProcessBitmapFile(filePath);
                        break;
                    default:
                        success = false;
                        error = new NotSupportedException($"Unsupported file type: {extension}");
                        break;
                }
            }
            catch (Exception ex)
            {
                success = false;
                error = ex;
            }
            finally
            {
                stopwatch.Stop();
                _stats.RecordFile(filePath, success, stopwatch.Elapsed, error);
            }
        }

        /// <summary>
        /// Processes a text file using the generic parser
        /// </summary>
        private async Task<bool> ProcessTextFile(string filePath)
        {
            return await Task.Run(() =>
            {
                var content = File.ReadAllText(filePath);
                var node = _textParser.Parse(content);
                return node != null;
            });
        }

        /// <summary>
        /// Processes a CSV file using appropriate CSV parser
        /// </summary>
        private async Task<bool> ProcessCsvFile(string filePath)
        {
            return await Task.Run(() =>
            {
                var fileName = Path.GetFileName(filePath).ToLower();

                if (fileName.Contains("definition"))
                {
                    var reader = new ProvinceDefinitionReader();
                    var results = reader.ReadFile(filePath);
                    return results != null && results.Count > 0;
                }
                else if (fileName.Contains("adjacencies"))
                {
                    var reader = new AdjacenciesReader();
                    var results = reader.ReadFile(filePath);
                    return results != null && results.Count > 0;
                }
                else
                {
                    // For unknown CSV files, just try to read them as province definitions
                    try
                    {
                        var reader = new ProvinceDefinitionReader();
                        var results = reader.ReadFile(filePath);
                        return results != null;
                    }
                    catch
                    {
                        // If that fails, try adjacencies
                        var reader = new AdjacenciesReader();
                        var results = reader.ReadFile(filePath);
                        return results != null;
                    }
                }
            });
        }

        /// <summary>
        /// Processes a bitmap file using appropriate bitmap parser
        /// </summary>
        private async Task<bool> ProcessBitmapFile(string filePath)
        {
            return await Task.Run(() =>
            {
                // For performance testing, just validate the bitmap header instead of processing all pixels
                try
                {
                    using var bmpReader = new BmpReader();
                    bmpReader.Open(filePath);
                    var header = bmpReader.Header;

                    // Basic validation: ensure we can read the header and it has reasonable dimensions
                    var isValid = header.Width > 0 &&
                                  header.Height > 0 &&
                                  header.BitsPerPixel >= 8 &&
                                  header.IsValid();

                    bmpReader.Close();
                    return isValid;
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// Creates a progress bar string
        /// </summary>
        private string CreateProgressBar(double percentage, int width = 40)
        {
            var filled = (int)(percentage / 100.0 * width);
            var empty = width - filled;
            return $"[{new string('■', filled)}{new string('□', empty)}]";
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

    /// <summary>
    /// Extension method to chunk arrays (for .NET Framework compatibility)
    /// </summary>
    public static class ArrayExtensions
    {
        public static IEnumerable<T[]> Chunk<T>(this IEnumerable<T> source, int size)
        {
            var list = source.ToList();
            for (int i = 0; i < list.Count; i += size)
            {
                yield return list.Skip(i).Take(size).ToArray();
            }
        }
    }
}