# Paradox Data Parser - Modding Guide

This guide helps modders and end users understand how to use the Paradox Data Parser to work with modded game files and create custom data processing workflows.

## Table of Contents

1. [Quick Start](#quick-start)
2. [Mod Loading](#mod-loading)
3. [Working with Mod Data](#working-with-mod-data)
4. [Custom Parsing Scenarios](#custom-parsing-scenarios)
5. [Performance Considerations](#performance-considerations)
6. [Troubleshooting](#troubleshooting)
7. [Advanced Usage](#advanced-usage)

## Quick Start

### Basic Mod Processing

```csharp
using ParadoxDataLib.ModSupport;
using ParadoxDataLib.Core.DataManagers;

// Initialize the mod manager
var modManager = new ModManager(@"C:\path\to\eu4", @"C:\path\to\mods");

// Load mods from directory
modManager.LoadModsFromDirectory();

// Set up data managers
var provinceManager = new ProvinceManager();
var countryManager = new CountryManager();

// Load base game data first
await provinceManager.LoadFromDirectoryAsync(@"C:\path\to\eu4\history\provinces");
await countryManager.LoadFromDirectoryAsync(@"C:\path\to\eu4\history\countries");

// Process mod data (example with a specific mod)
foreach (var mod in modManager.LoadedMods)
{
    if (mod.IsEnabled)
    {
        var modProvincesPath = Path.Combine(mod.GetModDirectory(), "history", "provinces");
        var modCountriesPath = Path.Combine(mod.GetModDirectory(), "history", "countries");

        if (Directory.Exists(modProvincesPath))
        {
            await provinceManager.LoadFromDirectoryAsync(modProvincesPath);
        }

        if (Directory.Exists(modCountriesPath))
        {
            await countryManager.LoadFromDirectoryAsync(modCountriesPath);
        }
    }
}
```

## Mod Loading

### Understanding Mod Structure

The parser supports the standard Paradox mod structure:

```
ModName/
├── descriptor.mod          # Mod metadata
├── history/
│   ├── provinces/         # Province modifications
│   └── countries/         # Country modifications
├── common/
│   ├── cultures/          # Culture definitions
│   └── religions/         # Religion definitions
└── localisation/          # Text translations
```

### Mod Descriptor Format

The parser reads `.mod` files with the following format:

```
name="My Custom Mod"
path="mod/my_custom_mod"
dependencies={"Base Game"}
tags={"Historical" "Map"}
version="1.0.0"
supported_version="1.35.*"
```

### Loading Mods Programmatically

```csharp
// Create mod manager
var modManager = new ModManager(gameDataPath: @"C:\SteamLibrary\steamapps\common\Europa Universalis IV");

// Option 1: Load all mods from default directory
modManager.LoadModsFromDirectory();

// Option 2: Load mods from custom directory
modManager.LoadModsFromDirectory(@"D:\MyCustomMods");

// Option 3: Load specific mod file
var customMod = ModDescriptor.ParseFromFile(@"D:\MyMod\descriptor.mod");
modManager.AddMod(customMod);

// Check loaded mods
Console.WriteLine($"Loaded {modManager.ModCount} mods:");
foreach (var mod in modManager.LoadedMods)
{
    Console.WriteLine($"- {mod.Name} (Version: {mod.Version})");
}
```

## Working with Mod Data

### Handling Overrides

When working with mods, data can override base game files. The parser handles this through load order:

```csharp
// Load in correct order: base game first, then mods
var dataManager = new GameDataManager();

// 1. Load base game data
await dataManager.LoadBaseGameData(@"C:\EU4\history");

// 2. Load mods in order
foreach (var mod in modManager.LoadedMods.OrderBy(m => m.LoadOrder))
{
    if (mod.IsEnabled)
    {
        await dataManager.LoadModData(mod);
    }
}

// The final data will have mod overrides applied
```

### Checking for Mod-Specific Files

```csharp
public async Task ProcessModSpecificContent(ModDescriptor mod)
{
    var modDir = mod.GetModDirectory();

    // Check for custom provinces
    var provincesPath = Path.Combine(modDir, "history", "provinces");
    if (Directory.Exists(provincesPath))
    {
        var files = Directory.GetFiles(provincesPath, "*.txt");
        Console.WriteLine($"Mod '{mod.Name}' modifies {files.Length} provinces");

        foreach (var file in files)
        {
            var parser = new ProvinceParser(Path.GetFileName(file));
            var province = await parser.ParseFileAsync(file);

            if (parser.HasErrors)
            {
                Console.WriteLine($"Errors in {file}: {string.Join(", ", parser.Errors)}");
            }
        }
    }

    // Check for new countries
    var countriesPath = Path.Combine(modDir, "history", "countries");
    if (Directory.Exists(countriesPath))
    {
        // Process country files...
    }
}
```

### Data Validation for Mods

```csharp
public void ValidateModData(ModDescriptor mod, ProvinceManager provinceManager, CountryManager countryManager)
{
    var validator = new DataValidator();
    var allErrors = new List<string>();

    // Validate all provinces
    foreach (var province in provinceManager.Values)
    {
        var result = validator.ValidateProvince(province, $"Mod: {mod.Name}");
        if (result.HasErrors)
        {
            allErrors.AddRange(result.Errors.Select(e => $"Province {province.ProvinceId}: {e}"));
        }
    }

    // Validate all countries
    foreach (var country in countryManager.Values)
    {
        var result = validator.ValidateCountry(country, $"Mod: {mod.Name}");
        if (result.HasErrors)
        {
            allErrors.AddRange(result.Errors.Select(e => $"Country {country.Tag}: {e}"));
        }
    }

    // Cross-reference validation
    var crossRefResult = validator.ValidateCrossReferences(
        provinceManager.Values,
        countryManager.Values,
        $"Mod: {mod.Name}");

    if (crossRefResult.HasErrors)
    {
        allErrors.AddRange(crossRefResult.Errors);
    }

    if (allErrors.Any())
    {
        Console.WriteLine($"Validation errors for mod '{mod.Name}':");
        foreach (var error in allErrors.Take(10)) // Show first 10 errors
        {
            Console.WriteLine($"  - {error}");
        }

        if (allErrors.Count > 10)
        {
            Console.WriteLine($"  ... and {allErrors.Count - 10} more errors");
        }
    }
}
```

## Custom Parsing Scenarios

### Parsing Custom File Formats

If your mod uses non-standard file formats, you can extend the parser:

```csharp
public class CustomModParser : BaseParser<CustomModData>
{
    protected override CustomModData ParseTokens()
    {
        var data = new CustomModData();

        while (!IsEndOfFile())
        {
            SkipComments();

            if (IsEndOfFile()) break;

            var token = CurrentToken();

            if (token.Type == TokenType.Identifier)
            {
                var key = token.Value.ToLower();
                ConsumeToken();

                if (!ExpectToken(TokenType.Equals))
                {
                    SkipToNextStatement();
                    continue;
                }

                // Parse your custom attributes
                switch (key)
                {
                    case "custom_attribute":
                        data.CustomAttribute = CurrentToken().Value;
                        ConsumeToken();
                        break;

                    default:
                        AddWarning($"Unknown attribute: {key}");
                        ConsumeToken();
                        break;
                }
            }
            else
            {
                ConsumeToken();
            }
        }

        return data;
    }
}
```

### Handling Mod-Specific Encoding

Some mods might use different text encodings:

```csharp
public class ModAwareParser : ProvinceParser
{
    private readonly Encoding _modEncoding;

    public ModAwareParser(string fileName, Encoding encoding = null) : base(fileName)
    {
        _modEncoding = encoding ?? Encoding.UTF8;
    }

    protected override Encoding GetFileEncoding(string filePath)
    {
        // Use mod-specific encoding if provided
        if (_modEncoding != null)
            return _modEncoding;

        return base.GetFileEncoding(filePath);
    }
}
```

## Performance Considerations

### Efficient Mod Processing

```csharp
public class OptimizedModProcessor
{
    public async Task ProcessModsInParallel(IEnumerable<ModDescriptor> mods)
    {
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount);
        var tasks = mods.Select(async mod =>
        {
            await semaphore.WaitAsync();
            try
            {
                await ProcessSingleMod(mod);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task ProcessSingleMod(ModDescriptor mod)
    {
        // Process mod data efficiently
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var provinceManager = new ProvinceManager();
            await provinceManager.LoadFromDirectoryAsync(
                Path.Combine(mod.GetModDirectory(), "history", "provinces"));

            Console.WriteLine($"Processed mod '{mod.Name}' in {stopwatch.ElapsedMilliseconds}ms " +
                             $"({provinceManager.Count} provinces)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing mod '{mod.Name}': {ex.Message}");
        }
    }
}
```

### Memory Management

```csharp
public class MemoryEfficientModProcessor : IDisposable
{
    private readonly Dictionary<string, WeakReference<ProvinceManager>> _provinceManagers;

    public MemoryEfficientModProcessor()
    {
        _provinceManagers = new Dictionary<string, WeakReference<ProvinceManager>>();
    }

    public ProvinceManager GetOrCreateProvinceManager(string modName)
    {
        if (_provinceManagers.TryGetValue(modName, out var weakRef) &&
            weakRef.TryGetTarget(out var existingManager))
        {
            return existingManager;
        }

        var newManager = new ProvinceManager();
        _provinceManagers[modName] = new WeakReference<ProvinceManager>(newManager);
        return newManager;
    }

    public void Dispose()
    {
        _provinceManagers.Clear();
        GC.Collect(); // Force cleanup
    }
}
```

## Troubleshooting

### Common Issues and Solutions

#### 1. Mod Files Not Loading

**Problem**: Mod files are not being parsed correctly.

**Solutions**:
```csharp
// Check if mod directory exists
if (!Directory.Exists(mod.GetModDirectory()))
{
    Console.WriteLine($"Mod directory not found: {mod.GetModDirectory()}");
    return;
}

// Verify file permissions
try
{
    var testFile = Directory.GetFiles(mod.GetModDirectory(), "*.txt").FirstOrDefault();
    if (testFile != null)
    {
        File.ReadAllText(testFile);
    }
}
catch (UnauthorizedAccessException)
{
    Console.WriteLine("Insufficient permissions to read mod files");
}
```

#### 2. Encoding Problems

**Problem**: Non-ASCII characters appear as question marks or garbage.

**Solution**:
```csharp
// Try different encodings
var encodings = new[]
{
    Encoding.UTF8,
    Encoding.GetEncoding(1252), // Windows-1252
    Encoding.Unicode,
    Encoding.Default
};

foreach (var encoding in encodings)
{
    try
    {
        var parser = new ModAwareParser(fileName, encoding);
        var result = parser.ParseFile(filePath);

        if (!parser.HasErrors)
        {
            Console.WriteLine($"Successfully parsed with {encoding.EncodingName}");
            break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed with {encoding.EncodingName}: {ex.Message}");
    }
}
```

#### 3. Performance Issues

**Problem**: Mod processing is too slow.

**Solutions**:
```csharp
// Profile your mod processing
var stopwatch = Stopwatch.StartNew();

// Option 1: Use async processing
await ProcessModsAsync(mods);

// Option 2: Limit parallel processing
var parallelOptions = new ParallelOptions
{
    MaxDegreeOfParallelism = Environment.ProcessorCount / 2
};

Parallel.ForEach(mods, parallelOptions, ProcessMod);

// Option 3: Use selective loading
var importantMods = mods.Where(m => m.Tags.Contains("Essential"));
ProcessMods(importantMods);

Console.WriteLine($"Processing completed in {stopwatch.ElapsedMilliseconds}ms");
```

### Debug Mode

Enable detailed logging for troubleshooting:

```csharp
public class DebugModManager : ModManager
{
    private readonly bool _debugMode;

    public DebugModManager(string gameDataPath, string modsPath, bool debugMode = false)
        : base(gameDataPath, modsPath)
    {
        _debugMode = debugMode;
    }

    protected void Log(string message)
    {
        if (_debugMode)
        {
            Console.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss} - {message}");
        }
    }
}
```

## Advanced Usage

### Creating Mod Compatibility Checker

```csharp
public class ModCompatibilityChecker
{
    public CompatibilityReport CheckCompatibility(IEnumerable<ModDescriptor> mods)
    {
        var report = new CompatibilityReport();
        var modList = mods.ToList();

        for (int i = 0; i < modList.Count; i++)
        {
            for (int j = i + 1; j < modList.Count; j++)
            {
                var conflicts = FindConflicts(modList[i], modList[j]);
                if (conflicts.Any())
                {
                    report.AddConflict(modList[i], modList[j], conflicts);
                }
            }
        }

        return report;
    }

    private List<string> FindConflicts(ModDescriptor mod1, ModDescriptor mod2)
    {
        var conflicts = new List<string>();

        // Check for file overwrites
        var mod1Files = GetModifiedFiles(mod1);
        var mod2Files = GetModifiedFiles(mod2);

        var overlapping = mod1Files.Intersect(mod2Files).ToList();
        if (overlapping.Any())
        {
            conflicts.Add($"Both mods modify: {string.Join(", ", overlapping.Take(5))}");
        }

        return conflicts;
    }

    private HashSet<string> GetModifiedFiles(ModDescriptor mod)
    {
        var files = new HashSet<string>();
        var modDir = mod.GetModDirectory();

        if (Directory.Exists(modDir))
        {
            var allFiles = Directory.GetFiles(modDir, "*.txt", SearchOption.AllDirectories);
            foreach (var file in allFiles)
            {
                var relativePath = Path.GetRelativePath(modDir, file);
                files.Add(relativePath);
            }
        }

        return files;
    }
}
```

### Custom Mod Export

```csharp
public class ModExporter
{
    public async Task ExportModData(ModDescriptor mod, string outputPath)
    {
        var modData = new
        {
            ModInfo = new
            {
                mod.Name,
                mod.Version,
                mod.SupportedVersion,
                mod.Tags
            },
            Statistics = await GatherModStatistics(mod),
            ValidationResults = await ValidateModData(mod)
        };

        var json = JsonSerializer.Serialize(modData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(outputPath, json);
    }

    private async Task<object> GatherModStatistics(ModDescriptor mod)
    {
        var provinceManager = new ProvinceManager();
        var countryManager = new CountryManager();

        // Load mod data
        var modDir = mod.GetModDirectory();
        var provincesPath = Path.Combine(modDir, "history", "provinces");
        var countriesPath = Path.Combine(modDir, "history", "countries");

        if (Directory.Exists(provincesPath))
            await provinceManager.LoadFromDirectoryAsync(provincesPath);

        if (Directory.Exists(countriesPath))
            await countryManager.LoadFromDirectoryAsync(countriesPath);

        return new
        {
            ProvinceCount = provinceManager.Count,
            CountryCount = countryManager.Count,
            ProvinceStats = provinceManager.GetStatistics(),
            CountryStats = countryManager.GetStatistics()
        };
    }
}
```

## Conclusion

This guide covers the essential aspects of using the Paradox Data Parser with mods. The parser provides robust support for:

- Loading and managing multiple mods
- Handling data overrides and conflicts
- Validating mod data integrity
- Processing custom file formats
- Performance optimization for large mod collections

For more advanced scenarios, refer to the API documentation and consider extending the base classes to meet your specific modding needs.

### Getting Help

- Check the [API Documentation](./API_DOCUMENTATION.md) for detailed class references
- Review [Usage Examples](./examples/) for more code samples
- Report issues on the project's GitHub page
- Join the community discord for modding discussions

Happy modding! 🎮