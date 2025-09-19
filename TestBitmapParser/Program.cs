using System;
using System.Collections.Generic;
using ParadoxDataLib.Core.Parsers.Bitmap.DataStructures;
using ParadoxDataLib.Core.Parsers.Bitmap.Interpreters;
using ParadoxDataLib.Core.Parsers.Bitmap.Specialized;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Bitmap Parser Test ===");

        // Test 1: Create sample RGB to Province mapping
        Console.WriteLine("\n1. Testing RGB to Province mapping...");
        var rgbMap = new Dictionary<int, int>
        {
            { 0x000000, 0 },     // Black = No province
            { 0xFF0000, 1 },     // Red = Province 1
            { 0x00FF00, 2 },     // Green = Province 2
            { 0x0000FF, 3 },     // Blue = Province 3
            { 0xFFFF00, 4 },     // Yellow = Province 4
            { 0xFF00FF, 5 }      // Magenta = Province 5
        };

        var interpreter = new RgbToProvinceInterpreter(rgbMap);
        Console.WriteLine($"✓ Created interpreter with {interpreter.MappingCount} mappings");

        // Test pixel interpretation
        var testPixel = new Pixel(255, 0, 0, 255, 0, 0); // Red pixel
        var provinceId = interpreter.InterpretPixel(testPixel);
        Console.WriteLine($"✓ Red pixel (255,0,0) maps to province {provinceId}");

        // Test 2: Height interpreter
        Console.WriteLine("\n2. Testing height interpreter...");
        var heightInterpreter = GrayscaleToHeightInterpreter.CreateEarthHeights();
        Console.WriteLine($"✓ Created height interpreter: {heightInterpreter.Description}");

        var grayPixel = new Pixel(128, 128, 128, 255, 0, 0); // Mid-gray
        var height = heightInterpreter.InterpretPixel(grayPixel);
        Console.WriteLine($"✓ Gray pixel (128,128,128) maps to {height:F1}m elevation");

        // Test 3: Binary mask interpreter
        Console.WriteLine("\n3. Testing binary mask interpreter...");
        var riverDetector = BinaryMaskInterpreter.CreateRiverDetector();
        Console.WriteLine($"✓ Created river detector: {riverDetector.Description}");

        var darkPixel = new Pixel(32, 32, 32, 255, 0, 0); // Dark pixel
        var isRiver = riverDetector.InterpretPixel(darkPixel);
        Console.WriteLine($"✓ Dark pixel (32,32,32) is river: {isRiver}");

        // Test 4: Specialized readers
        Console.WriteLine("\n4. Testing specialized readers...");

        var provinceReader = new ProvinceMapReader(rgbMap);
        Console.WriteLine($"✓ Created province reader: {provinceReader.GetMappingSummary()}");

        var heightReader = HeightmapReader.CreateEarthHeights();
        Console.WriteLine($"✓ Created height reader: {heightReader.GetHeightRangeDescription()}");

        var terrainReader = TerrainMapReader.CreateRiverDetector();
        Console.WriteLine($"✓ Created terrain reader: {terrainReader.GetDetectionDescription()}");

        // Test 5: Batch processing performance
        Console.WriteLine("\n5. Testing batch processing performance...");
        var pixels = new Pixel[10000];
        var random = new Random(42);

        for (int i = 0; i < pixels.Length; i++)
        {
            var r = (byte)random.Next(256);
            var g = (byte)random.Next(256);
            var b = (byte)random.Next(256);
            pixels[i] = new Pixel(r, g, b, 255, i % 100, i / 100);
        }

        var startTime = DateTime.UtcNow;
        var results = interpreter.InterpretPixels(pixels);
        var elapsed = DateTime.UtcNow - startTime;

        Console.WriteLine($"✓ Processed {pixels.Length:N0} pixels in {elapsed.TotalMilliseconds:F1}ms");
        Console.WriteLine($"✓ Rate: {pixels.Length / elapsed.TotalSeconds:N0} pixels/second");

        // Test 6: Statistics
        Console.WriteLine("\n6. Testing statistics...");
        var stats = interpreter.GetStatistics();
        Console.WriteLine($"✓ Interpreter stats: {stats}");

        // Test 7: Error handling
        Console.WriteLine("\n7. Testing error handling...");
        var errorPixel = new Pixel(123, 45, 67, 255, 0, 0); // Unmapped color
        var success = interpreter.TryInterpretPixel(errorPixel, out var result, out var errorMessage);
        Console.WriteLine($"✓ Unmapped pixel returns default: {result} (success: {success})");

        // Test 8: Pixel format conversions
        Console.WriteLine("\n8. Testing pixel format conversions...");
        var testRgb = new Pixel(200, 150, 100, 255, 10, 20);
        Console.WriteLine($"✓ Pixel RGB: 0x{testRgb.ToRgb():X6}");
        Console.WriteLine($"✓ Pixel Grayscale: {testRgb.ToGrayscale()}");
        Console.WriteLine($"✓ Pixel Position: ({testRgb.X}, {testRgb.Y})");

        Console.WriteLine("\n=== All tests completed successfully! ===");
    }
}
