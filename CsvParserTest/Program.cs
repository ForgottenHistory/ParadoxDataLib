using System;
using System.IO;
using ParadoxDataLib.Core.Parsers.Csv.Specialized;

Console.WriteLine("=== Testing CSV Parser ===");

try
{
    // Test Province Definition Parser
    var provinceReader = new ProvinceDefinitionReader();
    var definitionPath = @"D:\Stuff\Coding\ParadoxDataLib\EU IV Files\map\definition.csv";

    Console.WriteLine($"Reading province definitions from: {definitionPath}");
    var provinces = provinceReader.ReadFile(definitionPath, out var provinceStats);

    Console.WriteLine($"Province Parsing Results:");
    Console.WriteLine($"  Total rows: {provinceStats.TotalRows}");
    Console.WriteLine($"  Successful: {provinceStats.SuccessfulRows}");
    Console.WriteLine($"  Failed: {provinceStats.FailedRows}");
    Console.WriteLine($"  Time: {provinceStats.ElapsedTime.TotalMilliseconds:F1}ms");
    Console.WriteLine($"  Rate: {provinceStats.RowsPerSecond:F0} rows/sec");

    if (provinces.Count > 0)
    {
        Console.WriteLine($"First province: {provinces[0]}");
        Console.WriteLine($"Last province: {provinces[provinces.Count - 1]}");
    }

    Console.WriteLine();

    // Test Adjacencies Parser
    var adjacencyReader = new AdjacenciesReader();
    var adjacencyPath = @"D:\Stuff\Coding\ParadoxDataLib\EU IV Files\map\adjacencies.csv";

    Console.WriteLine($"Reading adjacencies from: {adjacencyPath}");
    var adjacencies = adjacencyReader.ReadFile(adjacencyPath, out var adjacencyStats);

    Console.WriteLine($"Adjacency Parsing Results:");
    Console.WriteLine($"  Total rows: {adjacencyStats.TotalRows}");
    Console.WriteLine($"  Successful: {adjacencyStats.SuccessfulRows}");
    Console.WriteLine($"  Failed: {adjacencyStats.FailedRows}");
    Console.WriteLine($"  Time: {adjacencyStats.ElapsedTime.TotalMilliseconds:F1}ms");
    Console.WriteLine($"  Rate: {adjacencyStats.RowsPerSecond:F0} rows/sec");

    if (adjacencies.Count > 0)
    {
        Console.WriteLine($"First adjacency: {adjacencies[0]}");
        Console.WriteLine($"Last adjacency: {adjacencies[adjacencies.Count - 1]}");
    }

    Console.WriteLine("\n=== Test Complete ===");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}
