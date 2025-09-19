# Paradox Data Parser - Usage Examples

This directory contains comprehensive examples demonstrating various use cases and scenarios for the Paradox Data Parser library using the new **Generic Parser Architecture**.

## Available Examples

### 1. GenericParserExample.cs ⭐ **NEW**
**Focus**: Universal Paradox file parsing with the new architecture
- Generic parser for any Paradox game format
- Data extraction patterns with custom extractors
- Province and country data parsing
- Historical entries and complex nested structures

**Best for**: Understanding the new parser architecture, all use cases

### 2. BasicProvinceLoading.cs
**Focus**: Fundamental data loading and querying
- Loading province data from game files using generic parser
- Basic queries (by owner, culture, religion, trade goods)
- Displaying detailed province information
- Generating basic statistics

**Best for**: First-time users, basic integration scenarios

### 3. DataValidationExample.cs
**Focus**: Comprehensive data validation and error checking
- Province and country data validation
- Cross-reference integrity checking
- Error reporting and categorization
- Sample data creation for testing

**Best for**: Quality assurance, mod validation, data integrity checks

### 4. CustomDataExportExample.cs
**Focus**: Various data export formats and scenarios
- JSON export for web applications
- CSV export for Excel analysis
- XML export for system integration
- Custom format export for modders
- Dashboard data preparation

**Best for**: Data analysis, reporting, integration with other tools

## Running the Examples

### Prerequisites
- .NET 8.0 or later
- Paradox Data Parser library
- Any Paradox game files (EU4, CK3, Stellaris, etc.) or sample data

### Basic Setup
1. Update file paths in examples to point to your game installation
2. Build and run individual examples:
```bash
dotnet run --file GenericParserExample.cs
```

### Common Paths
Update these paths in the examples to match your game installation:
- **EU4 Steam**: `C:\Program Files (x86)\Steam\steamapps\common\Europa Universalis IV`
- **CK3 Steam**: `C:\Program Files (x86)\Steam\steamapps\common\Crusader Kings III`
- **Stellaris Steam**: `C:\Program Files (x86)\Steam\steamapps\common\Stellaris`
- **Linux Steam**: `~/.steam/steam/steamapps/common/[GAME NAME]`

## Example Output

### BasicProvinceLoading.cs
```
=== Basic Province Loading Example ===

Loading province data...
Successfully loaded 4927 provinces

=== Basic Queries ===
French provinces: 147
French culture provinces: 89
Catholic provinces: 1834
Grain-producing provinces: 423

=== Province Details ===
Province: Paris (ID: 183)
Owner: FRA
Culture: french
Religion: catholic
Trade Good: cloth
Base Tax: 6.0
Base Production: 8.0
Base Manpower: 5.0
Buildings: marketplace, workshop, temple
Cores: FRA
```

### DataValidationExample.cs
```
=== Data Validation Example ===

Loading game data for validation...
Loaded 4927 provinces and 441 countries

=== Province Validation ===
✅ Province 1 (Stockholm): No issues found
⚠️  Province 2 (Västergötland): Base tax (12.5) is unusually high (>20)
❌ Province 999 (Invalid): Invalid country tag format: 'INVALID'. Must be 3 uppercase letters.

Validation Report:
Total Issues Found:
  Provinces: 23 errors, 45 warnings
  Countries: 5 errors, 12 warnings
  Cross-references: 3 errors
```

## Advanced Usage Patterns

### Error Handling
```csharp
try
{
    await provinceManager.LoadFromDirectoryAsync(provincesPath);
}
catch (DirectoryNotFoundException)
{
    Console.WriteLine("Game directory not found - using sample data");
    CreateSampleData();
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
    // Handle or log appropriately
}
```

### Asynchronous Processing
```csharp
var loadTasks = new[]
{
    provinceManager.LoadFromDirectoryAsync(provincesPath),
    countryManager.LoadFromDirectoryAsync(countriesPath)
};

await Task.WhenAll(loadTasks);
```

### Memory Management
```csharp
// For large datasets, consider using statements
using var provinceManager = new ProvinceManager();
// Manager will be disposed automatically

// Or explicit cleanup
provinceManager.Clear();
GC.Collect(); // Force cleanup if needed
```

## Integration Examples

### Web API Integration
```csharp
[HttpGet("provinces")]
public async Task<IActionResult> GetProvinces()
{
    var provinces = _provinceManager.GetAll();
    var dto = provinces.Select(p => new ProvinceDTO
    {
        Id = p.Key,
        Name = p.Value.Name,
        Owner = p.Value.Owner,
        Development = p.Value.BaseTax + p.Value.BaseProduction + p.Value.BaseManpower
    });
    return Ok(dto);
}
```

### Background Service
```csharp
public class GameDataService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await LoadAndProcessGameData();
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
```

## Performance Tips

1. **Use async methods** for I/O operations
2. **Batch process** large datasets
3. **Filter early** to reduce memory usage
4. **Use parallel processing** for CPU-intensive operations
5. **Cache results** when appropriate
6. **Dispose resources** properly

## Common Issues and Solutions

### Issue: "Directory not found"
**Solution**: Update file paths or enable sample data creation

### Issue: "Out of memory"
**Solution**: Use batch processing (see PerformanceOptimizationExample.cs)

### Issue: "Encoding problems"
**Solution**: Check file encoding, try different encodings

### Issue: "Slow performance"
**Solution**: Use parallel processing and selective loading

## Contributing

To add new examples:
1. Create a new `.cs` file following the naming pattern
2. Include comprehensive XML documentation
3. Add error handling and sample data fallbacks
4. Update this README with a description
5. Test with both real and sample data

## Related Documentation

- [MODDING_GUIDE.md](../MODDING_GUIDE.md) - Comprehensive modding guide
- [paradox-parser-guide.md](../paradox-parser-guide.md) - Generic parser architecture guide
- [PERFORMANCE_GUIDELINES.md](../PERFORMANCE_GUIDELINES.md) - Performance optimization
- [API Documentation](../docs/) - Detailed API reference