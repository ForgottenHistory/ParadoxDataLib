using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ParadoxDataLib.Core.DataManagers;
using ParadoxDataLib.Validation;

/// <summary>
/// Demonstrates comprehensive data validation for game files
/// </summary>
class DataValidationExample
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Data Validation Example ===\n");

        // Initialize managers
        var provinceManager = new ProvinceManager();
        var countryManager = new CountryManager();
        var validator = new DataValidator();

        // Load data
        var basePath = @"C:\path\to\eu4";
        var provincesPath = Path.Combine(basePath, "history", "provinces");
        var countriesPath = Path.Combine(basePath, "history", "countries");

        Console.WriteLine("Loading game data for validation...");

        try
        {
            if (Directory.Exists(provincesPath))
            {
                await provinceManager.LoadFromDirectoryAsync(provincesPath);
                Console.WriteLine($"Loaded {provinceManager.Count} provinces");
            }

            if (Directory.Exists(countriesPath))
            {
                await countryManager.LoadFromDirectoryAsync(countriesPath);
                Console.WriteLine($"Loaded {countryManager.Count} countries");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading data: {ex.Message}");
            Console.WriteLine("Using sample data for demonstration...");
            CreateSampleDataWithErrors(provinceManager, countryManager);
        }

        Console.WriteLine("\n=== Province Validation ===");

        var provinceErrors = 0;
        var provinceWarnings = 0;

        foreach (var province in provinceManager.Values.Take(10)) // Validate first 10 provinces
        {
            var result = validator.ValidateProvince(province, $"Province {province.ProvinceId}");

            if (result.HasErrors)
            {
                provinceErrors += result.Errors.Count;
                Console.WriteLine($"\nERRORS in Province {province.ProvinceId} ({province.Name}):");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  ❌ {error}");
                }
            }

            if (result.HasWarnings)
            {
                provinceWarnings += result.Warnings.Count;
                Console.WriteLine($"\nWARNINGS in Province {province.ProvinceId} ({province.Name}):");
                foreach (var warning in result.Warnings.Take(3)) // Show max 3 warnings
                {
                    Console.WriteLine($"  ⚠️  {warning}");
                }
            }

            if (result.HasInfo)
            {
                Console.WriteLine($"\nINFO for Province {province.ProvinceId} ({province.Name}):");
                foreach (var info in result.InfoMessages.Take(2)) // Show max 2 info messages
                {
                    Console.WriteLine($"  ℹ️  {info}");
                }
            }
        }

        Console.WriteLine($"\nProvince validation summary: {provinceErrors} errors, {provinceWarnings} warnings");

        Console.WriteLine("\n=== Country Validation ===");

        var countryErrors = 0;
        var countryWarnings = 0;

        foreach (var country in countryManager.Values.Take(10)) // Validate first 10 countries
        {
            var result = validator.ValidateCountry(country, $"Country {country.Tag}");

            if (result.HasErrors)
            {
                countryErrors += result.Errors.Count;
                Console.WriteLine($"\nERRORS in Country {country.Tag} ({country.Name}):");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  ❌ {error}");
                }
            }

            if (result.HasWarnings)
            {
                countryWarnings += result.Warnings.Count;
                Console.WriteLine($"\nWARNINGS in Country {country.Tag} ({country.Name}):");
                foreach (var warning in result.Warnings.Take(3))
                {
                    Console.WriteLine($"  ⚠️  {warning}");
                }
            }
        }

        Console.WriteLine($"\nCountry validation summary: {countryErrors} errors, {countryWarnings} warnings");

        Console.WriteLine("\n=== Cross-Reference Validation ===");

        var crossRefResult = validator.ValidateCrossReferences(
            provinceManager.Values,
            countryManager.Values,
            "Global Cross-Reference Check");

        if (crossRefResult.HasErrors)
        {
            Console.WriteLine("Cross-reference ERRORS found:");
            foreach (var error in crossRefResult.Errors.Take(10)) // Show max 10 errors
            {
                Console.WriteLine($"  ❌ {error}");
            }

            if (crossRefResult.Errors.Count > 10)
            {
                Console.WriteLine($"  ... and {crossRefResult.Errors.Count - 10} more errors");
            }
        }
        else
        {
            Console.WriteLine("✅ No cross-reference errors found!");
        }

        Console.WriteLine("\n=== Validation Report ===");
        Console.WriteLine($"Total Issues Found:");
        Console.WriteLine($"  Provinces: {provinceErrors} errors, {provinceWarnings} warnings");
        Console.WriteLine($"  Countries: {countryErrors} errors, {countryWarnings} warnings");
        Console.WriteLine($"  Cross-references: {crossRefResult.Errors.Count} errors");

        var totalIssues = provinceErrors + countryErrors + crossRefResult.Errors.Count;
        if (totalIssues == 0)
        {
            Console.WriteLine("🎉 All data validation passed successfully!");
        }
        else
        {
            Console.WriteLine($"⚠️  Total issues requiring attention: {totalIssues}");
        }

        Console.WriteLine("\nValidation example completed!");
    }

    /// <summary>
    /// Creates sample data with intentional errors for demonstration
    /// </summary>
    private static void CreateSampleDataWithErrors(ProvinceManager provinceManager, CountryManager countryManager)
    {
        // Add sample provinces with various issues
        var sampleProvinces = new[]
        {
            new ProvinceData(1, "Valid Province") { Owner = "FRA", BaseTax = 3, BaseProduction = 2, BaseManpower = 1 },
            new ProvinceData(-1, "Invalid ID Province") { Owner = "FRA" }, // Invalid ID
            new ProvinceData(2, "Invalid Owner Province") { Owner = "XXX" }, // Invalid owner tag
            new ProvinceData(3, "High Values Province") { Owner = "FRA", BaseTax = 25, BaseProduction = 30, BaseManpower = 40 }, // Suspiciously high values
            new ProvinceData(4, "Negative Values Province") { Owner = "FRA", BaseTax = -1, BaseProduction = -2 }, // Negative values
        };

        foreach (var province in sampleProvinces)
        {
            provinceManager.Add(province.ProvinceId, province);
        }

        // Add sample countries
        var sampleCountries = new[]
        {
            new CountryData("FRA", "France") { Government = "monarchy", PrimaryCulture = "french", Religion = "catholic", Capital = 183 },
            new CountryData("XXX", "Invalid Country") { Government = "invalid_government" }, // Invalid government
            new CountryData("", "Empty Tag Country"), // Empty tag
        };

        foreach (var country in sampleCountries)
        {
            if (!string.IsNullOrEmpty(country.Tag))
            {
                countryManager.Add(country.Tag, country);
            }
        }

        Console.WriteLine("Created sample data with intentional errors for demonstration.");
    }
}