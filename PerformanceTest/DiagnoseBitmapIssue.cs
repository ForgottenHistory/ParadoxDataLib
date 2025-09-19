using System;
using System.IO;
using System.Linq;
using ParadoxDataLib.Core.Parsers.Bitmap;
using ParadoxDataLib.Core.Parsers.Bitmap.Specialized;
using ParadoxDataLib.Core.Parsers.Bitmap.Interpreters;
using System.Collections.Generic;

namespace ParadoxDataLib.PerformanceTest
{
    public class DiagnoseBitmapIssue
    {
        public static void DiagnoseIssues()
        {
            Console.WriteLine("=== BITMAP PARSER DIAGNOSIS ===");

            // Find the EU IV Files directory
            var baseDir = FindEuivFilesDirectory();
            if (string.IsNullOrEmpty(baseDir))
            {
                Console.WriteLine("❌ EU IV Files directory not found");
                return;
            }

            Console.WriteLine($"✓ Found EU IV Files directory: {baseDir}");

            // Test 1: Check if any bitmap files exist
            var mapDir = Path.Combine(baseDir, "map");
            if (!Directory.Exists(mapDir))
            {
                Console.WriteLine("❌ Map directory not found");
                return;
            }

            var bitmapFiles = Directory.GetFiles(mapDir, "*.bmp", SearchOption.AllDirectories);
            Console.WriteLine($"✓ Found {bitmapFiles.Length} bitmap files");

            if (bitmapFiles.Length == 0)
            {
                Console.WriteLine("❌ No bitmap files found to test");
                return;
            }

            // Test 2: Try reading bitmap headers with low-level reader
            Console.WriteLine("\n=== Testing BmpReader directly ===");
            var bmpReader = new BmpReader();

            foreach (var bitmapFile in bitmapFiles.Take(3)) // Test first 3 files
            {
                Console.WriteLine($"\nTesting: {Path.GetFileName(bitmapFile)}");
                try
                {
                    bmpReader.Open(bitmapFile);
                    var header = bmpReader.Header;
                    Console.WriteLine($"✓ Header read successfully: {header.Width}x{header.Height}, {header.BitsPerPixel}bpp");

                    // Try reading one pixel
                    var pixel = bmpReader.GetPixel(0, 0);
                    Console.WriteLine($"✓ First pixel: R={pixel.R}, G={pixel.G}, B={pixel.B}");

                    bmpReader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ BmpReader failed: {ex.Message}");
                    Console.WriteLine($"   Inner: {ex.InnerException?.Message}");
                }
            }

            // Test 3: Test with specialized readers
            Console.WriteLine("\n=== Testing Specialized Readers ===");

            // Create a simple RGB mapping for testing
            var testMapping = new Dictionary<int, int>
            {
                { 0x000000, 0 }, // Black
                { 0xFF0000, 1 }, // Red
                { 0x00FF00, 2 }, // Green
                { 0x0000FF, 3 }  // Blue
            };

            foreach (var bitmapFile in bitmapFiles.Take(2))
            {
                var fileName = Path.GetFileName(bitmapFile).ToLower();
                Console.WriteLine($"\nTesting specialized reader for: {fileName}");

                try
                {
                    if (fileName.Contains("provinces"))
                    {
                        Console.WriteLine("Using ProvinceMapReader...");
                        var provinceReader = new ProvinceMapReader(testMapping);
                        var result = provinceReader.ReadProvinceMap(bitmapFile);
                        Console.WriteLine($"✓ ProvinceMapReader success: {result.Width}x{result.Height}");
                    }
                    else
                    {
                        Console.WriteLine("Using HeightmapReader...");
                        var heightReader = new HeightmapReader();
                        var result = heightReader.ReadHeightmap(bitmapFile);
                        Console.WriteLine($"✓ HeightmapReader success: {result.Width}x{result.Height}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Specialized reader failed: {ex.Message}");
                    Console.WriteLine($"   Inner: {ex.InnerException?.Message}");
                    Console.WriteLine($"   Stack: {ex.StackTrace}");
                }
            }

            Console.WriteLine("\n=== Diagnosis Complete ===");
        }

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