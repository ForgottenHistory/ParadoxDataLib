using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using ParadoxDataLib.Core.DataModels;
using ParadoxDataLib.Validation;

namespace ParadoxDataLib.Json
{
    public class JsonImporter
    {
        private readonly ParadoxJsonConverter _converter;
        private readonly JsonExportOptions _options;
        private readonly DataValidator _validator;
        private readonly JsonSchemaValidator _schemaValidator;

        public JsonImporter(JsonExportOptions options = null)
        {
            _options = options ?? JsonExportOptions.Pretty;
            _converter = new ParadoxJsonConverter(_options);
            _validator = new DataValidator();
            _schemaValidator = new JsonSchemaValidator();
        }

        public async Task<JsonImportResult<ProvinceData>> ImportProvincesFromFileAsync(string filePath)
        {
            var result = new JsonImportResult<ProvinceData> { SourceType = "Provinces", FilePath = filePath };

            try
            {
                var json = await ReadJsonFileAsync(filePath);
                var importData = await ParseJsonDataAsync<ProvinceData>(json, "provinces");

                if (_options.ValidateOnExport) // Using same flag for import validation
                {
                    var validationResults = importData.Data.Select(p => _validator.ValidateProvince(p)).ToList();
                    var errors = validationResults.SelectMany(r => r.Issues.Where(i => i.Severity >= ValidationSeverity.Error)).ToList();

                    if (errors.Any())
                    {
                        result.ValidationErrors = errors.Select(e => e.Message).ToList();
                    }
                }

                result.Data = importData.Data;
                result.Metadata = importData.Metadata;
                result.Success = true;
                result.RecordCount = result.Data.Count();
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<JsonImportResult<CountryData>> ImportCountriesFromFileAsync(string filePath)
        {
            var result = new JsonImportResult<CountryData> { SourceType = "Countries", FilePath = filePath };

            try
            {
                var json = await ReadJsonFileAsync(filePath);
                var importData = await ParseJsonDataAsync<CountryData>(json, "countries");

                if (_options.ValidateOnExport)
                {
                    var validationResults = importData.Data.Select(c => _validator.ValidateCountry(c)).ToList();
                    var errors = validationResults.SelectMany(r => r.Issues.Where(i => i.Severity >= ValidationSeverity.Error)).ToList();

                    if (errors.Any())
                    {
                        result.ValidationErrors = errors.Select(e => e.Message).ToList();
                    }
                }

                result.Data = importData.Data;
                result.Metadata = importData.Metadata;
                result.Success = true;
                result.RecordCount = result.Data.Count();
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<JsonGameDataImportResult> ImportGameDataFromFileAsync(string filePath)
        {
            var result = new JsonGameDataImportResult { SourceType = "GameData", FilePath = filePath };

            try
            {
                var json = await ReadJsonFileAsync(filePath);
                var gameData = _converter.Deserialize<JsonGameData>(json);

                if (gameData == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to parse game data JSON";
                    return result;
                }

                if (_options.ValidateOnExport)
                {
                    var provinceValidation = gameData.Provinces.Select(p => _validator.ValidateProvince(p)).ToList();
                    var countryValidation = gameData.Countries.Select(c => _validator.ValidateCountry(c)).ToList();
                    var crossRefValidation = _validator.ValidateCrossReferences(gameData.Provinces, gameData.Countries);

                    var allErrors = provinceValidation.Concat(countryValidation).Append(crossRefValidation)
                        .SelectMany(r => r.Issues.Where(i => i.Severity >= ValidationSeverity.Error)).ToList();

                    if (allErrors.Any())
                    {
                        result.ValidationErrors = allErrors.Select(e => e.Message).ToList();
                    }
                }

                result.Provinces = gameData.Provinces;
                result.Countries = gameData.Countries;
                result.Metadata = gameData.Metadata;
                result.Success = true;
                result.RecordCount = gameData.Provinces.Count() + gameData.Countries.Count();
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public JsonImportResult<ProvinceData> ImportProvincesFromString(string json)
        {
            var result = new JsonImportResult<ProvinceData> { SourceType = "Provinces" };

            try
            {
                var importData = ParseJsonDataAsync<ProvinceData>(json, "provinces").Result;

                if (_options.ValidateOnExport)
                {
                    var validationResults = importData.Data.Select(p => _validator.ValidateProvince(p)).ToList();
                    var errors = validationResults.SelectMany(r => r.Issues.Where(i => i.Severity >= ValidationSeverity.Error)).ToList();

                    if (errors.Any())
                    {
                        result.ValidationErrors = errors.Select(e => e.Message).ToList();
                    }
                }

                result.Data = importData.Data;
                result.Metadata = importData.Metadata;
                result.Success = true;
                result.RecordCount = result.Data.Count();
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public JsonImportResult<CountryData> ImportCountriesFromString(string json)
        {
            var result = new JsonImportResult<CountryData> { SourceType = "Countries" };

            try
            {
                var importData = ParseJsonDataAsync<CountryData>(json, "countries").Result;

                if (_options.ValidateOnExport)
                {
                    var validationResults = importData.Data.Select(c => _validator.ValidateCountry(c)).ToList();
                    var errors = validationResults.SelectMany(r => r.Issues.Where(i => i.Severity >= ValidationSeverity.Error)).ToList();

                    if (errors.Any())
                    {
                        result.ValidationErrors = errors.Select(e => e.Message).ToList();
                    }
                }

                result.Data = importData.Data;
                result.Metadata = importData.Metadata;
                result.Success = true;
                result.RecordCount = result.Data.Count();
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<JsonSchemaValidationResult> ValidateWithSchemaAsync(string jsonFilePath, string schemaFilePath)
        {
            try
            {
                var json = await ReadJsonFileAsync(jsonFilePath);
                var schema = await File.ReadAllTextAsync(schemaFilePath);

                return await _schemaValidator.ValidateAsync(json, schema);
            }
            catch (Exception ex)
            {
                return new JsonSchemaValidationResult
                {
                    IsValid = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public JsonImportStatistics GetImportStatistics(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Import file not found: {filePath}");

            var fileInfo = new FileInfo(filePath);
            var isCompressed = filePath.EndsWith(".gz") || IsGzipFile(filePath);

            return new JsonImportStatistics
            {
                FilePath = filePath,
                FileSizeBytes = fileInfo.Length,
                IsCompressed = isCompressed,
                CreatedAt = fileInfo.CreationTimeUtc,
                ModifiedAt = fileInfo.LastWriteTimeUtc
            };
        }

        private async Task<string> ReadJsonFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"JSON file not found: {filePath}");

            if (IsGzipFile(filePath))
            {
                return await ReadCompressedJsonAsync(filePath);
            }

            return await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        }

        private async Task<string> ReadCompressedJsonAsync(string filePath)
        {
            using var fileStream = File.OpenRead(filePath);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            using var reader = new StreamReader(gzipStream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        private bool IsGzipFile(string filePath)
        {
            if (filePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                return true;

            try
            {
                using var fileStream = File.OpenRead(filePath);
                var buffer = new byte[2];
                var bytesRead = fileStream.Read(buffer, 0, 2);
                return bytesRead == 2 && buffer[0] == 0x1f && buffer[1] == 0x8b; // GZIP magic number
            }
            catch
            {
                return false;
            }
        }

        private async Task<JsonImportData<T>> ParseJsonDataAsync<T>(string json, string expectedDataType)
        {
            try
            {
                // Try to parse as structured data with metadata
                var document = JObject.Parse(json);

                if (document.ContainsKey("metadata") && document.ContainsKey("data"))
                {
                    var metadataToken = document["metadata"];
                    var dataToken = document["data"];

                    var metadata = metadataToken?.ToObject<Dictionary<string, object>>();
                    var data = dataToken?.ToObject<IEnumerable<T>>();

                    return new JsonImportData<T>
                    {
                        Data = data,
                        Metadata = metadata
                    };
                }

                // Try to parse as direct array/data
                if (document.Type == JTokenType.Array)
                {
                    var data = JsonConvert.DeserializeObject<IEnumerable<T>>(json, _converter.JsonOptions);
                    return new JsonImportData<T>
                    {
                        Data = data ?? Enumerable.Empty<T>(),
                        Metadata = new Dictionary<string, object>()
                    };
                }

                // Try to find data in expected property
                if (document.ContainsKey(expectedDataType))
                {
                    var expectedDataToken = document[expectedDataType];
                    var data = expectedDataToken?.ToObject<IEnumerable<T>>();
                    var metadata = new Dictionary<string, object>();

                    if (document.ContainsKey("metadata"))
                    {
                        var metadataToken = document["metadata"];
                        metadata = metadataToken?.ToObject<Dictionary<string, object>>() ?? metadata;
                    }

                    return new JsonImportData<T>
                    {
                        Data = data,
                        Metadata = metadata
                    };
                }

                throw new JsonException($"Could not find {expectedDataType} data in JSON structure");
            }
            catch (JsonException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new JsonException($"Failed to parse JSON data: {ex.Message}", ex);
            }
        }
    }

    public class JsonImportResult<T>
    {
        public bool Success { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public int RecordCount { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

        public bool HasValidationErrors => ValidationErrors.Any();
        public bool IsValid => Success && !HasValidationErrors;

        public string GetSummary()
        {
            if (!Success)
            {
                return HasValidationErrors
                    ? $"Import failed with {ValidationErrors.Count} validation errors"
                    : $"Import failed: {ErrorMessage}";
            }

            var warningText = HasValidationErrors ? $" ({ValidationErrors.Count} validation warnings)" : "";
            return $"Successfully imported {RecordCount} {SourceType} records{warningText}";
        }
    }

    public class JsonGameDataImportResult
    {
        public bool Success { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public IEnumerable<ProvinceData> Provinces { get; set; } = Enumerable.Empty<ProvinceData>();
        public IEnumerable<CountryData> Countries { get; set; } = Enumerable.Empty<CountryData>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public int RecordCount { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

        public bool HasValidationErrors => ValidationErrors.Any();
        public bool IsValid => Success && !HasValidationErrors;

        public string GetSummary()
        {
            if (!Success)
            {
                return HasValidationErrors
                    ? $"Game data import failed with {ValidationErrors.Count} validation errors"
                    : $"Game data import failed: {ErrorMessage}";
            }

            var warningText = HasValidationErrors ? $" ({ValidationErrors.Count} validation warnings)" : "";
            return $"Successfully imported {Provinces.Count()} provinces and {Countries.Count()} countries{warningText}";
        }
    }

    public class JsonImportData<T>
    {
        public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class JsonGameData
    {
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public IEnumerable<ProvinceData> Provinces { get; set; } = Enumerable.Empty<ProvinceData>();
        public IEnumerable<CountryData> Countries { get; set; } = Enumerable.Empty<CountryData>();
    }

    public class JsonImportStatistics
    {
        public string FilePath { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public bool IsCompressed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }

        public double FileSizeKB => FileSizeBytes / 1024.0;
        public double FileSizeMB => FileSizeBytes / (1024.0 * 1024.0);

        public override string ToString()
        {
            var sizeStr = FileSizeMB > 1 ? $"{FileSizeMB:F2} MB" : $"{FileSizeKB:F1} KB";
            var compressedStr = IsCompressed ? " (compressed)" : "";
            return $"{Path.GetFileName(FilePath)}: {sizeStr}{compressedStr}";
        }
    }
}