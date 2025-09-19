using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ParadoxDataLib.Core.DataModels;
using ParadoxDataLib.Validation;

namespace ParadoxDataLib.Json
{
    public class JsonExporter
    {
        private readonly ParadoxJsonConverter _converter;
        private readonly JsonExportOptions _options;
        private readonly DataValidator _validator;

        public JsonExporter(JsonExportOptions options = null)
        {
            _options = options ?? JsonExportOptions.Pretty;
            _converter = new ParadoxJsonConverter(_options);
            _validator = new DataValidator();
        }

        public async Task<JsonExportResult> ExportProvincesToFileAsync(IEnumerable<ProvinceData> provinces, string filePath)
        {
            var result = new JsonExportResult { SourceType = "Provinces", FilePath = filePath };

            try
            {
                if (_options.ValidateOnExport)
                {
                    var validationResults = provinces.Select(p => _validator.ValidateProvince(p)).ToList();
                    var errors = validationResults.SelectMany(r => r.Issues.Where(i => i.Severity >= ValidationSeverity.Error)).ToList();

                    if (errors.Any())
                    {
                        result.ValidationErrors = errors.Select(e => e.Message).ToList();
                        result.Success = false;
                        return result;
                    }
                }

                var jsonData = CreateJsonDocument(provinces, "provinces");
                var json = _converter.Serialize(jsonData);

                if (_options.CompressOutput)
                {
                    await WriteCompressedJsonAsync(json, filePath);
                }
                else
                {
                    await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
                }

                result.Success = true;
                result.RecordCount = provinces.Count();
                result.FileSizeBytes = new FileInfo(filePath).Length;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<JsonExportResult> ExportCountriesToFileAsync(IEnumerable<CountryData> countries, string filePath)
        {
            var result = new JsonExportResult { SourceType = "Countries", FilePath = filePath };

            try
            {
                if (_options.ValidateOnExport)
                {
                    var validationResults = countries.Select(c => _validator.ValidateCountry(c)).ToList();
                    var errors = validationResults.SelectMany(r => r.Issues.Where(i => i.Severity >= ValidationSeverity.Error)).ToList();

                    if (errors.Any())
                    {
                        result.ValidationErrors = errors.Select(e => e.Message).ToList();
                        result.Success = false;
                        return result;
                    }
                }

                var jsonData = CreateJsonDocument(countries, "countries");
                var json = _converter.Serialize(jsonData);

                if (_options.CompressOutput)
                {
                    await WriteCompressedJsonAsync(json, filePath);
                }
                else
                {
                    await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
                }

                result.Success = true;
                result.RecordCount = countries.Count();
                result.FileSizeBytes = new FileInfo(filePath).Length;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<JsonExportResult> ExportGameDataToFileAsync(IEnumerable<ProvinceData> provinces, IEnumerable<CountryData> countries, string filePath)
        {
            var result = new JsonExportResult { SourceType = "GameData", FilePath = filePath };

            try
            {
                if (_options.ValidateOnExport)
                {
                    var provinceValidation = provinces.Select(p => _validator.ValidateProvince(p)).ToList();
                    var countryValidation = countries.Select(c => _validator.ValidateCountry(c)).ToList();
                    var crossRefValidation = _validator.ValidateCrossReferences(provinces, countries);

                    var allErrors = provinceValidation.Concat(countryValidation).Append(crossRefValidation)
                        .SelectMany(r => r.Issues.Where(i => i.Severity >= ValidationSeverity.Error)).ToList();

                    if (allErrors.Any())
                    {
                        result.ValidationErrors = allErrors.Select(e => e.Message).ToList();
                        result.Success = false;
                        return result;
                    }
                }

                var gameData = new
                {
                    metadata = CreateMetadata($"Provinces: {provinces.Count()}, Countries: {countries.Count()}"),
                    provinces = provinces,
                    countries = countries
                };

                var json = _converter.Serialize(gameData);

                if (_options.CompressOutput)
                {
                    await WriteCompressedJsonAsync(json, filePath);
                }
                else
                {
                    await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
                }

                result.Success = true;
                result.RecordCount = provinces.Count() + countries.Count();
                result.FileSizeBytes = new FileInfo(filePath).Length;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public string ExportProvincesToString(IEnumerable<ProvinceData> provinces)
        {
            var jsonData = CreateJsonDocument(provinces, "provinces");
            return _converter.Serialize(jsonData);
        }

        public string ExportCountriesToString(IEnumerable<CountryData> countries)
        {
            var jsonData = CreateJsonDocument(countries, "countries");
            return _converter.Serialize(jsonData);
        }

        public string ExportGameDataToString(IEnumerable<ProvinceData> provinces, IEnumerable<CountryData> countries)
        {
            var gameData = new
            {
                metadata = CreateMetadata($"Provinces: {provinces.Count()}, Countries: {countries.Count()}"),
                provinces = provinces,
                countries = countries
            };

            return _converter.Serialize(gameData);
        }

        public async Task<JsonExportResult> ExportWithSchemaAsync<T>(IEnumerable<T> data, string filePath, string dataType)
        {
            var result = new JsonExportResult { SourceType = dataType, FilePath = filePath };

            try
            {
                var schemaGenerator = new JsonSchemaGenerator(_options);
                var schema = schemaGenerator.GenerateSchema<T>();

                var exportData = new
                {
                    schema = schema,
                    metadata = CreateMetadata($"{dataType}: {data.Count()} records"),
                    data = data
                };

                var json = _converter.Serialize(exportData);

                if (_options.CompressOutput)
                {
                    await WriteCompressedJsonAsync(json, filePath);
                }
                else
                {
                    await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
                }

                result.Success = true;
                result.RecordCount = data.Count();
                result.FileSizeBytes = new FileInfo(filePath).Length;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private object CreateJsonDocument<T>(IEnumerable<T> data, string dataType)
        {
            if (!_options.IncludeMetadata)
            {
                return data;
            }

            return new
            {
                metadata = CreateMetadata($"{dataType}: {data.Count()} records"),
                data = data
            };
        }

        private object CreateMetadata(string description)
        {
            var metadata = new Dictionary<string, object>();

            if (_options.IncludeVersion)
            {
                metadata["version"] = "1.0.0";
                metadata["formatVersion"] = "1.0";
            }

            if (_options.IncludeTimestamp)
            {
                metadata["exportedAt"] = DateTime.UtcNow;
            }

            metadata["description"] = description;
            metadata["format"] = _options.Format.ToString();

            if (_options.IncludeTypeInformation)
            {
                metadata["generator"] = "ParadoxDataLib JsonExporter";
                metadata["exportOptions"] = new
                {
                    includeNullValues = _options.IncludeNullValues,
                    includeEmptyCollections = _options.IncludeEmptyCollections,
                    includeDefaultValues = _options.IncludeDefaultValues,
                    dateFormat = _options.DateFormat.ToString(),
                    compressed = _options.CompressOutput
                };
            }

            return metadata;
        }

        private async Task WriteCompressedJsonAsync(string json, string filePath)
        {
            var jsonBytes = Encoding.UTF8.GetBytes(json);

            using var fileStream = File.Create(filePath);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);
            await gzipStream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
        }

        public JsonExportStatistics GetExportStatistics(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Export file not found: {filePath}");

            var fileInfo = new FileInfo(filePath);
            var isCompressed = _options.CompressOutput || filePath.EndsWith(".gz");

            return new JsonExportStatistics
            {
                FilePath = filePath,
                FileSizeBytes = fileInfo.Length,
                IsCompressed = isCompressed,
                CreatedAt = fileInfo.CreationTimeUtc,
                ModifiedAt = fileInfo.LastWriteTimeUtc
            };
        }
    }

    public class JsonExportResult
    {
        public bool Success { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public int RecordCount { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

        public bool HasValidationErrors => ValidationErrors.Any();

        public string GetSummary()
        {
            if (!Success)
            {
                return HasValidationErrors
                    ? $"Export failed with {ValidationErrors.Count} validation errors"
                    : $"Export failed: {ErrorMessage}";
            }

            var sizeKb = FileSizeBytes / 1024.0;
            return $"Successfully exported {RecordCount} {SourceType} records ({sizeKb:F1} KB)";
        }
    }

    public class JsonExportStatistics
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