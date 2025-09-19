using System;
using System.IO;
using System.Threading.Tasks;
using ParadoxDataLib.Core.DataManagers;
using ParadoxDataLib.Core.Parsers;

/// <summary>
/// Demonstrates basic province data loading and querying
/// </summary>
class BasicProvinceLoading
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Basic Province Loading Example ===\n");

        // Initialize the province manager
        var provinceManager = new ProvinceManager();

        // Load province data from a directory
        var provincesPath = @"C:\path\to\eu4\history\provinces";

        if (!Directory.Exists(provincesPath))
        {
            Console.WriteLine($"Province directory not found: {provincesPath}");
            Console.WriteLine("Please update the path to point to your EU4 installation.");
            return;
        }

        try
        {
            Console.WriteLine("Loading province data...");
            await provinceManager.LoadFromDirectoryAsync(provincesPath);
            Console.WriteLine($"Successfully loaded {provinceManager.Count} provinces\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading provinces: {ex.Message}");
            return;
        }

        // Basic queries
        Console.WriteLine("=== Basic Queries ===");

        // Get provinces by owner
        var frenchProvinces = provinceManager.GetProvincesByOwner("FRA");
        Console.WriteLine($"French provinces: {frenchProvinces.Count()}");

        // Get provinces by culture
        var frenchCultureProvinces = provinceManager.GetProvincesByCulture("french");
        Console.WriteLine($"French culture provinces: {frenchCultureProvinces.Count()}");

        // Get provinces by religion
        var catholicProvinces = provinceManager.GetProvincesByReligion("catholic");
        Console.WriteLine($"Catholic provinces: {catholicProvinces.Count()}");

        // Get provinces by trade good
        var grainProvinces = provinceManager.GetProvincesByTradeGood("grain");
        Console.WriteLine($"Grain-producing provinces: {grainProvinces.Count()}\n");

        // Show detailed information for a specific province
        Console.WriteLine("=== Province Details ===");
        if (provinceManager.TryGet(183, out var paris)) // Paris province ID
        {
            Console.WriteLine($"Province: {paris.Name} (ID: {paris.ProvinceId})");
            Console.WriteLine($"Owner: {paris.Owner ?? "None"}");
            Console.WriteLine($"Culture: {paris.Culture ?? "Unknown"}");
            Console.WriteLine($"Religion: {paris.Religion ?? "Unknown"}");
            Console.WriteLine($"Trade Good: {paris.TradeGood ?? "None"}");
            Console.WriteLine($"Base Tax: {paris.BaseTax}");
            Console.WriteLine($"Base Production: {paris.BaseProduction}");
            Console.WriteLine($"Base Manpower: {paris.BaseManpower}");
            Console.WriteLine($"Buildings: {string.Join(", ", paris.Buildings)}");
            Console.WriteLine($"Cores: {string.Join(", ", paris.Cores)}");
        }
        else
        {
            Console.WriteLine("Paris province (183) not found in data.");
        }

        // Statistics
        Console.WriteLine("\n=== Statistics ===");
        var stats = provinceManager.GetStatistics();
        Console.WriteLine($"Total provinces: {stats.TotalProvinces}");
        Console.WriteLine($"Total base tax: {stats.TotalBaseTax:F1}");
        Console.WriteLine($"Total base production: {stats.TotalBaseProduction:F1}");
        Console.WriteLine($"Total base manpower: {stats.TotalBaseManpower:F1}");

        Console.WriteLine($"\nTop 5 countries by province count:");
        var topOwners = stats.ProvincesByOwner
            .OrderByDescending(kvp => kvp.Value)
            .Take(5);

        foreach (var owner in topOwners)
        {
            Console.WriteLine($"  {owner.Key}: {owner.Value} provinces");
        }

        Console.WriteLine("\nExample completed successfully!");
    }
}