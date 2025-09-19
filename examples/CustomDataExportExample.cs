using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using ParadoxDataLib.Core.DataManagers;

/// <summary>
/// Demonstrates various data export formats and custom export scenarios
/// </summary>
class CustomDataExportExample
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Custom Data Export Example ===\n");

        // Load sample data
        var provinceManager = new ProvinceManager();
        var countryManager = new CountryManager();

        // Try to load real data, fallback to sample data
        try
        {
            var basePath = @"C:\path\to\eu4";
            await provinceManager.LoadFromDirectoryAsync(Path.Combine(basePath, "history", "provinces"));
            await countryManager.LoadFromDirectoryAsync(Path.Combine(basePath, "history", "countries"));
            Console.WriteLine($"Loaded {provinceManager.Count} provinces and {countryManager.Count} countries from game data");
        }
        catch
        {
            CreateSampleData(provinceManager, countryManager);
            Console.WriteLine("Using sample data for demonstration");
        }

        // Create output directory
        var outputDir = "exports";
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("\n=== Export Formats ===");

        // Export 1: JSON Summary Report
        await ExportJsonSummary(provinceManager, countryManager, Path.Combine(outputDir, "summary.json"));

        // Export 2: CSV for Excel Analysis
        await ExportCsvData(provinceManager, Path.Combine(outputDir, "provinces.csv"));

        // Export 3: XML for Integration
        await ExportXmlData(provinceManager, countryManager, Path.Combine(outputDir, "gamedata.xml"));

        // Export 4: Custom Format for Modders
        await ExportModderFormat(provinceManager, Path.Combine(outputDir, "mod_template.txt"));

        // Export 5: Statistics Dashboard Data
        await ExportDashboardData(provinceManager, countryManager, Path.Combine(outputDir, "dashboard.json"));

        Console.WriteLine("\nAll exports completed successfully!");
        Console.WriteLine($"Files saved to: {Path.GetFullPath(outputDir)}");
    }

    /// <summary>
    /// Exports a comprehensive JSON summary
    /// </summary>
    static async Task ExportJsonSummary(ProvinceManager provinceManager, CountryManager countryManager, string filePath)
    {
        Console.WriteLine("Exporting JSON summary...");

        var summary = new
        {
            GeneratedAt = DateTime.UtcNow,
            Summary = new
            {
                TotalProvinces = provinceManager.Count,
                TotalCountries = countryManager.Count,
                DataSources = new[] { "EU4 Game Files" }
            },
            Statistics = new
            {
                Provinces = provinceManager.GetStatistics(),
                Countries = countryManager.GetStatistics()
            },
            TopLists = new
            {
                RichestProvinces = provinceManager.Values
                    .OrderByDescending(p => p.BaseTax + p.BaseProduction)
                    .Take(10)
                    .Select(p => new { p.ProvinceId, p.Name, p.Owner,
                                     TotalValue = p.BaseTax + p.BaseProduction }),

                LargestCountriesByProvinces = provinceManager.Values
                    .Where(p => !string.IsNullOrEmpty(p.Owner))
                    .GroupBy(p => p.Owner)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => new { CountryTag = g.Key, ProvinceCount = g.Count() }),

                MostCommonCultures = provinceManager.Values
                    .Where(p => !string.IsNullOrEmpty(p.Culture))
                    .GroupBy(p => p.Culture)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => new { Culture = g.Key, ProvinceCount = g.Count() })
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(summary, options);
        await File.WriteAllTextAsync(filePath, json);

        Console.WriteLine($"✅ JSON summary exported to {filePath}");
    }

    /// <summary>
    /// Exports province data in CSV format for Excel analysis
    /// </summary>
    static async Task ExportCsvData(ProvinceManager provinceManager, string filePath)
    {
        Console.WriteLine("Exporting CSV data...");

        var csv = new System.Text.StringBuilder();

        // Header
        csv.AppendLine("ProvinceId,Name,Owner,Controller,Culture,Religion,TradeGood,BaseTax,BaseProduction,BaseManpower,TotalDevelopment,IsHre,Buildings,Cores");

        // Data rows
        foreach (var province in provinceManager.Values.OrderBy(p => p.ProvinceId))
        {
            var totalDev = province.BaseTax + province.BaseProduction + province.BaseManpower;
            var buildings = string.Join(";", province.Buildings);
            var cores = string.Join(";", province.Cores);

            csv.AppendLine($"{province.ProvinceId}," +
                          $"\"{province.Name}\"," +
                          $"\"{province.Owner ?? ""}\"," +
                          $"\"{province.Controller ?? ""}\"," +
                          $"\"{province.Culture ?? ""}\"," +
                          $"\"{province.Religion ?? ""}\"," +
                          $"\"{province.TradeGood ?? ""}\"," +
                          $"{province.BaseTax}," +
                          $"{province.BaseProduction}," +
                          $"{province.BaseManpower}," +
                          $"{totalDev:F1}," +
                          $"{(province.IsHre ? "TRUE" : "FALSE")}," +
                          $"\"{buildings}\"," +
                          $"\"{cores}\"");
        }

        await File.WriteAllTextAsync(filePath, csv.ToString());

        Console.WriteLine($"✅ CSV data exported to {filePath}");
    }

    /// <summary>
    /// Exports data in XML format for system integration
    /// </summary>
    static async Task ExportXmlData(ProvinceManager provinceManager, CountryManager countryManager, string filePath)
    {
        Console.WriteLine("Exporting XML data...");

        var root = new XElement("GameData",
            new XAttribute("GeneratedAt", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")),
            new XAttribute("ProvinceCount", provinceManager.Count),
            new XAttribute("CountryCount", countryManager.Count));

        // Add provinces
        var provincesElement = new XElement("Provinces");
        foreach (var province in provinceManager.Values.Take(100)) // Limit for demo
        {
            var provinceElement = new XElement("Province",
                new XAttribute("id", province.ProvinceId),
                new XAttribute("name", province.Name ?? ""),
                new XElement("Owner", province.Owner ?? ""),
                new XElement("Culture", province.Culture ?? ""),
                new XElement("Religion", province.Religion ?? ""),
                new XElement("TradeGood", province.TradeGood ?? ""),
                new XElement("Development",
                    new XAttribute("tax", province.BaseTax),
                    new XAttribute("production", province.BaseProduction),
                    new XAttribute("manpower", province.BaseManpower),
                    new XAttribute("total", province.BaseTax + province.BaseProduction + province.BaseManpower)),
                new XElement("Buildings",
                    province.Buildings.Select(b => new XElement("Building", b))),
                new XElement("Cores",
                    province.Cores.Select(c => new XElement("Core", c)))
            );
            provincesElement.Add(provinceElement);
        }
        root.Add(provincesElement);

        // Add countries
        var countriesElement = new XElement("Countries");
        foreach (var country in countryManager.Values.Take(50)) // Limit for demo
        {
            var countryElement = new XElement("Country",
                new XAttribute("tag", country.Tag),
                new XAttribute("name", country.Name ?? ""),
                new XElement("Government", country.Government ?? ""),
                new XElement("PrimaryCulture", country.PrimaryCulture ?? ""),
                new XElement("Religion", country.Religion ?? ""),
                new XElement("TechnologyGroup", country.TechnologyGroup ?? ""),
                new XElement("Capital", country.Capital),
                new XElement("AcceptedCultures",
                    country.AcceptedCultures.Select(c => new XElement("Culture", c)))
            );
            countriesElement.Add(countryElement);
        }
        root.Add(countriesElement);

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            root);

        await using var writer = new StringWriter();
        document.Save(writer);
        await File.WriteAllTextAsync(filePath, writer.ToString());

        Console.WriteLine($"✅ XML data exported to {filePath}");
    }

    /// <summary>
    /// Exports data in a format useful for modders
    /// </summary>
    static async Task ExportModderFormat(ProvinceManager provinceManager, string filePath)
    {
        Console.WriteLine("Exporting modder template...");

        var content = new System.Text.StringBuilder();

        content.AppendLine("# Paradox Data Parser - Modder Template");
        content.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        content.AppendLine($"# Total Provinces: {provinceManager.Count}");
        content.AppendLine();

        content.AppendLine("# High Development Provinces (Good targets for buildings)");
        var highDevProvinces = provinceManager.Values
            .Where(p => p.BaseTax + p.BaseProduction + p.BaseManpower >= 15)
            .OrderByDescending(p => p.BaseTax + p.BaseProduction + p.BaseManpower)
            .Take(20);

        foreach (var province in highDevProvinces)
        {
            var totalDev = province.BaseTax + province.BaseProduction + province.BaseManpower;
            content.AppendLine($"# {province.ProvinceId} - {province.Name} (Dev: {totalDev:F0}, Owner: {province.Owner ?? "None"})");
        }

        content.AppendLine();
        content.AppendLine("# Province Modification Template");
        content.AppendLine("# Copy and modify the section below for your mod");
        content.AppendLine();

        // Create a template for the first high-dev province
        var templateProvince = highDevProvinces.FirstOrDefault();
        if (templateProvince != null)
        {
            content.AppendLine($"# {templateProvince.ProvinceId} - {templateProvince.Name}");
            content.AppendLine($"owner = {templateProvince.Owner ?? "---"}");
            content.AppendLine($"controller = {templateProvince.Controller ?? templateProvince.Owner ?? "---"}");
            content.AppendLine($"culture = {templateProvince.Culture ?? "---"}");
            content.AppendLine($"religion = {templateProvince.Religion ?? "---"}");
            content.AppendLine($"base_tax = {templateProvince.BaseTax}");
            content.AppendLine($"base_production = {templateProvince.BaseProduction}");
            content.AppendLine($"base_manpower = {templateProvince.BaseManpower}");
            content.AppendLine($"trade_goods = {templateProvince.TradeGood ?? "grain"}");
            content.AppendLine($"hre = {(templateProvince.IsHre ? "yes" : "no")}");

            if (templateProvince.Buildings.Any())
            {
                content.AppendLine();
                content.AppendLine("# Buildings");
                foreach (var building in templateProvince.Buildings)
                {
                    content.AppendLine($"{building} = yes");
                }
            }

            if (templateProvince.Cores.Any())
            {
                content.AppendLine();
                content.AppendLine("# Cores");
                foreach (var core in templateProvince.Cores)
                {
                    content.AppendLine($"add_core = {core}");
                }
            }
        }

        content.AppendLine();
        content.AppendLine("# Useful Statistics for Modding:");

        var stats = provinceManager.GetStatistics();
        content.AppendLine($"# Average Base Tax: {stats.TotalBaseTax / stats.TotalProvinces:F1}");
        content.AppendLine($"# Average Base Production: {stats.TotalBaseProduction / stats.TotalProvinces:F1}");
        content.AppendLine($"# Average Base Manpower: {stats.TotalBaseManpower / stats.TotalProvinces:F1}");

        await File.WriteAllTextAsync(filePath, content.ToString());

        Console.WriteLine($"✅ Modder template exported to {filePath}");
    }

    /// <summary>
    /// Exports data for dashboard visualization
    /// </summary>
    static async Task ExportDashboardData(ProvinceManager provinceManager, CountryManager countryManager, string filePath)
    {
        Console.WriteLine("Exporting dashboard data...");

        var dashboardData = new
        {
            metadata = new
            {
                generated_at = DateTime.UtcNow,
                data_version = "1.0",
                total_provinces = provinceManager.Count,
                total_countries = countryManager.Count
            },
            charts = new
            {
                development_distribution = provinceManager.Values
                    .Select(p => p.BaseTax + p.BaseProduction + p.BaseManpower)
                    .GroupBy(dev => Math.Floor(dev / 5) * 5) // Group by 5-dev buckets
                    .OrderBy(g => g.Key)
                    .Select(g => new { development_range = $"{g.Key}-{g.Key + 4}", count = g.Count() }),

                religion_distribution = provinceManager.Values
                    .Where(p => !string.IsNullOrEmpty(p.Religion))
                    .GroupBy(p => p.Religion)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => new { religion = g.Key, provinces = g.Count() }),

                culture_distribution = provinceManager.Values
                    .Where(p => !string.IsNullOrEmpty(p.Culture))
                    .GroupBy(p => p.Culture)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => new { culture = g.Key, provinces = g.Count() }),

                trade_goods_distribution = provinceManager.Values
                    .Where(p => !string.IsNullOrEmpty(p.TradeGood))
                    .GroupBy(p => p.TradeGood)
                    .OrderByDescending(g => g.Count())
                    .Select(g => new { trade_good = g.Key, provinces = g.Count() }),

                country_sizes = provinceManager.Values
                    .Where(p => !string.IsNullOrEmpty(p.Owner))
                    .GroupBy(p => p.Owner)
                    .Select(g => new {
                        country = g.Key,
                        provinces = g.Count(),
                        total_development = g.Sum(p => p.BaseTax + p.BaseProduction + p.BaseManpower)
                    })
                    .OrderByDescending(x => x.provinces)
                    .Take(20)
            },
            summary_stats = new
            {
                total_development = provinceManager.Values.Sum(p => p.BaseTax + p.BaseProduction + p.BaseManpower),
                average_development = provinceManager.Values.Average(p => p.BaseTax + p.BaseProduction + p.BaseManpower),
                hre_provinces = provinceManager.Values.Count(p => p.IsHre),
                unique_cultures = provinceManager.Values.Where(p => !string.IsNullOrEmpty(p.Culture)).Select(p => p.Culture).Distinct().Count(),
                unique_religions = provinceManager.Values.Where(p => !string.IsNullOrEmpty(p.Religion)).Select(p => p.Religion).Distinct().Count(),
                unique_trade_goods = provinceManager.Values.Where(p => !string.IsNullOrEmpty(p.TradeGood)).Select(p => p.TradeGood).Distinct().Count()
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        var json = JsonSerializer.Serialize(dashboardData, options);
        await File.WriteAllTextAsync(filePath, json);

        Console.WriteLine($"✅ Dashboard data exported to {filePath}");
    }

    /// <summary>
    /// Creates sample data for demonstration
    /// </summary>
    static void CreateSampleData(ProvinceManager provinceManager, CountryManager countryManager)
    {
        var random = new Random(42);
        var cultures = new[] { "french", "english", "castilian", "german", "italian" };
        var religions = new[] { "catholic", "orthodox", "protestant", "reformed" };
        var tradeGoods = new[] { "grain", "livestock", "fish", "cloth", "wine", "iron" };
        var countries = new[] { "FRA", "ENG", "CAS", "HAB", "ITA" };

        // Create sample countries
        for (int i = 0; i < countries.Length; i++)
        {
            var country = new CountryData(countries[i], $"Country {countries[i]}")
            {
                Government = "monarchy",
                PrimaryCulture = cultures[i],
                Religion = religions[i % religions.Length],
                Capital = i * 100 + 1
            };
            countryManager.Add(country.Tag, country);
        }

        // Create sample provinces
        for (int i = 1; i <= 500; i++)
        {
            var owner = countries[random.Next(countries.Length)];
            var province = new ProvinceData(i, $"Province {i}")
            {
                Owner = owner,
                Controller = owner,
                Culture = cultures[random.Next(cultures.Length)],
                Religion = religions[random.Next(religions.Length)],
                TradeGood = tradeGoods[random.Next(tradeGoods.Length)],
                BaseTax = random.Next(1, 12),
                BaseProduction = random.Next(1, 10),
                BaseManpower = random.Next(1, 8),
                IsHre = random.NextDouble() > 0.7
            };

            province.Cores.Add(owner);
            if (random.NextDouble() > 0.8)
            {
                province.Cores.Add(countries[random.Next(countries.Length)]);
            }

            provinceManager.Add(i, province);
        }
    }
}