using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ParadoxDataLib.Core.DataModels;

namespace ParadoxDataLib.Json
{
    public class ParadoxJsonConverter
    {
        private readonly JsonExportOptions _options;
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonSerializerOptions JsonOptions => _jsonOptions;

        public ParadoxJsonConverter(JsonExportOptions options = null)
        {
            _options = options ?? JsonExportOptions.Pretty;
            _jsonOptions = CreateJsonSerializerOptions();
        }

        private JsonSerializerOptions CreateJsonSerializerOptions()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = _options.IndentJson,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = _options.IncludeNullValues ? JsonIgnoreCondition.Never : JsonIgnoreCondition.WhenWritingNull,
                MaxDepth = _options.MaxDepth,
                Encoder = _options.EscapeNonAscii ? System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping : System.Text.Encodings.Web.JavaScriptEncoder.Default
            };

            options.Converters.Add(new ProvinceDataJsonConverter(_options));
            options.Converters.Add(new CountryDataJsonConverter(_options));
            options.Converters.Add(new HistoricalEntryJsonConverter(_options));
            options.Converters.Add(new DateTimeJsonConverter(_options));

            if (_options.UseStringEnums)
            {
                options.Converters.Add(new JsonStringEnumConverter());
            }

            return options;
        }

        public string Serialize<T>(T obj)
        {
            if (obj == null)
                return _options.IncludeNullValues ? "null" : "{}";

            try
            {
                var json = JsonSerializer.Serialize(obj, _jsonOptions);

                if (_options.CompressOutput)
                {
                    // Remove unnecessary whitespace for compact format
                    json = json.Replace("\n", "").Replace("\r", "").Replace("  ", " ");
                }

                return json;
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException($"Failed to serialize {typeof(T).Name}: {ex.Message}", ex);
            }
        }

        public T Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default(T);

            try
            {
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException($"Failed to deserialize {typeof(T).Name}: {ex.Message}", ex);
            }
        }

        public string SerializeProvince(ProvinceData province)
        {
            return Serialize(province);
        }

        public string SerializeCountry(CountryData country)
        {
            return Serialize(country);
        }

        public string SerializeProvinces(IEnumerable<ProvinceData> provinces)
        {
            return Serialize(provinces);
        }

        public string SerializeCountries(IEnumerable<CountryData> countries)
        {
            return Serialize(countries);
        }

        public ProvinceData DeserializeProvince(string json)
        {
            return Deserialize<ProvinceData>(json);
        }

        public CountryData DeserializeCountry(string json)
        {
            return Deserialize<CountryData>(json);
        }

        public IEnumerable<ProvinceData> DeserializeProvinces(string json)
        {
            return Deserialize<IEnumerable<ProvinceData>>(json);
        }

        public IEnumerable<CountryData> DeserializeCountries(string json)
        {
            return Deserialize<IEnumerable<CountryData>>(json);
        }
    }

    public class ProvinceDataJsonConverter : JsonConverter<ProvinceData>
    {
        private readonly JsonExportOptions _options;

        public ProvinceDataJsonConverter(JsonExportOptions options)
        {
            _options = options;
        }

        public override ProvinceData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object");

            var province = new ProvinceData();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName?.ToLowerInvariant())
                {
                    case "provinceId":
                    case "provinceid":
                        province.ProvinceId = reader.GetInt32();
                        break;
                    case "owner":
                        province.Owner = reader.GetString() ?? string.Empty;
                        break;
                    case "controller":
                        province.Controller = reader.GetString() ?? string.Empty;
                        break;
                    case "culture":
                        province.Culture = reader.GetString() ?? string.Empty;
                        break;
                    case "religion":
                        province.Religion = reader.GetString() ?? string.Empty;
                        break;
                    case "tradegood":
                    case "tradeGood":
                        province.TradeGood = reader.GetString() ?? string.Empty;
                        break;
                    case "basetax":
                    case "baseTax":
                        province.BaseTax = reader.GetInt32();
                        break;
                    case "baseproduction":
                    case "baseProduction":
                        province.BaseProduction = reader.GetInt32();
                        break;
                    case "basemanpower":
                    case "baseManpower":
                        province.BaseManpower = reader.GetInt32();
                        break;
                    case "cores":
                        province.Cores = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? new List<string>();
                        break;
                    case "buildings":
                        province.Buildings = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? new List<string>();
                        break;
                    case "historicalentries":
                    case "historicalEntries":
                        province.HistoricalEntries = JsonSerializer.Deserialize<List<HistoricalEntry>>(ref reader, options) ?? new List<HistoricalEntry>();
                        break;
                }
            }

            return province;
        }

        public override void Write(Utf8JsonWriter writer, ProvinceData value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            WritePropertyIfShould(writer, "provinceId", value.ProvinceId, options);
            WritePropertyIfShould(writer, "owner", value.Owner, options);
            WritePropertyIfShould(writer, "controller", value.Controller, options);
            WritePropertyIfShould(writer, "culture", value.Culture, options);
            WritePropertyIfShould(writer, "religion", value.Religion, options);
            WritePropertyIfShould(writer, "tradeGood", value.TradeGood, options);
            WritePropertyIfShould(writer, "baseTax", value.BaseTax, options);
            WritePropertyIfShould(writer, "baseProduction", value.BaseProduction, options);
            WritePropertyIfShould(writer, "baseManpower", value.BaseManpower, options);
            WritePropertyIfShould(writer, "cores", value.Cores, options);
            WritePropertyIfShould(writer, "buildings", value.Buildings, options);
            WritePropertyIfShould(writer, "historicalEntries", value.HistoricalEntries, options);

            writer.WriteEndObject();
        }

        private void WritePropertyIfShould(Utf8JsonWriter writer, string propertyName, object value, JsonSerializerOptions options)
        {
            if (_options.ShouldIncludeProperty(propertyName, value))
            {
                if (value is string stringValue)
                {
                    writer.WriteString(propertyName, stringValue);
                }
                else if (value is int intValue)
                {
                    writer.WriteNumber(propertyName, intValue);
                }
                else if (value != null)
                {
                    writer.WritePropertyName(propertyName);
                    JsonSerializer.Serialize(writer, value, options);
                }
            }
        }
    }

    public class CountryDataJsonConverter : JsonConverter<CountryData>
    {
        private readonly JsonExportOptions _options;

        public CountryDataJsonConverter(JsonExportOptions options)
        {
            _options = options;
        }

        public override CountryData Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object");

            var country = new CountryData();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName?.ToLowerInvariant())
                {
                    case "tag":
                        country.Tag = reader.GetString() ?? string.Empty;
                        break;
                    case "government":
                        country.Government = reader.GetString() ?? string.Empty;
                        break;
                    case "primaryculture":
                    case "primaryCulture":
                        country.PrimaryCulture = reader.GetString() ?? string.Empty;
                        break;
                    case "religion":
                        country.Religion = reader.GetString() ?? string.Empty;
                        break;
                    case "technologygroup":
                    case "technologyGroup":
                        country.TechnologyGroup = reader.GetString() ?? string.Empty;
                        break;
                    case "capital":
                        country.Capital = reader.GetInt32();
                        break;
                    case "acceptedcultures":
                    case "acceptedCultures":
                        country.AcceptedCultures = JsonSerializer.Deserialize<List<string>>(ref reader, options) ?? new List<string>();
                        break;
                    case "historicalentries":
                    case "historicalEntries":
                        country.HistoricalEntries = JsonSerializer.Deserialize<List<HistoricalEntry>>(ref reader, options) ?? new List<HistoricalEntry>();
                        break;
                }
            }

            return country;
        }

        public override void Write(Utf8JsonWriter writer, CountryData value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            WritePropertyIfShould(writer, "tag", value.Tag, options);
            WritePropertyIfShould(writer, "government", value.Government, options);
            WritePropertyIfShould(writer, "primaryCulture", value.PrimaryCulture, options);
            WritePropertyIfShould(writer, "religion", value.Religion, options);
            WritePropertyIfShould(writer, "technologyGroup", value.TechnologyGroup, options);
            WritePropertyIfShould(writer, "capital", value.Capital, options);
            WritePropertyIfShould(writer, "acceptedCultures", value.AcceptedCultures, options);
            WritePropertyIfShould(writer, "historicalEntries", value.HistoricalEntries, options);

            writer.WriteEndObject();
        }

        private void WritePropertyIfShould(Utf8JsonWriter writer, string propertyName, object value, JsonSerializerOptions options)
        {
            if (_options.ShouldIncludeProperty(propertyName, value))
            {
                if (value is string stringValue)
                {
                    writer.WriteString(propertyName, stringValue);
                }
                else if (value is int intValue)
                {
                    writer.WriteNumber(propertyName, intValue);
                }
                else if (value != null)
                {
                    writer.WritePropertyName(propertyName);
                    JsonSerializer.Serialize(writer, value, options);
                }
            }
        }
    }

    public class HistoricalEntryJsonConverter : JsonConverter<HistoricalEntry>
    {
        private readonly JsonExportOptions _options;

        public HistoricalEntryJsonConverter(JsonExportOptions options)
        {
            _options = options;
        }

        public override HistoricalEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("Expected start of object");

            var entry = new HistoricalEntry();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName?.ToLowerInvariant())
                {
                    case "date":
                        if (_options.DateFormat == DateTimeFormat.ParadoxDate)
                        {
                            var dateStr = reader.GetString();
                            if (DateTime.TryParseExact(dateStr, "yyyy.M.d", null, System.Globalization.DateTimeStyles.None, out var paradoxDate))
                                entry.Date = paradoxDate;
                        }
                        else
                        {
                            entry.Date = JsonSerializer.Deserialize<DateTime>(ref reader, options);
                        }
                        break;
                    case "changes":
                        entry.Changes = JsonSerializer.Deserialize<Dictionary<string, object>>(ref reader, options) ?? new Dictionary<string, object>();
                        break;
                }
            }

            return entry;
        }

        public override void Write(Utf8JsonWriter writer, HistoricalEntry value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            if (_options.ShouldIncludeProperty("date", value.Date))
            {
                if (_options.DateFormat == DateTimeFormat.ParadoxDate)
                {
                    writer.WriteString("date", value.Date.ToString("yyyy.M.d"));
                }
                else
                {
                    writer.WritePropertyName("date");
                    JsonSerializer.Serialize(writer, value.Date, options);
                }
            }

            if (_options.ShouldIncludeProperty("changes", value.Changes))
            {
                writer.WritePropertyName("changes");
                JsonSerializer.Serialize(writer, value.Changes, options);
            }

            writer.WriteEndObject();
        }
    }

    public class DateTimeJsonConverter : JsonConverter<DateTime>
    {
        private readonly JsonExportOptions _options;

        public DateTimeJsonConverter(JsonExportOptions options)
        {
            _options = options;
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dateString = reader.GetString();
            if (string.IsNullOrEmpty(dateString))
                return default;

            if (_options.DateFormat == DateTimeFormat.ParadoxDate)
            {
                if (DateTime.TryParseExact(dateString, "yyyy.M.d", null, System.Globalization.DateTimeStyles.None, out var paradoxDate))
                    return paradoxDate;
            }

            if (DateTime.TryParse(dateString, out var isoDate))
                return isoDate;

            return default;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            string dateString = _options.DateFormat switch
            {
                DateTimeFormat.ParadoxDate => value.ToString("yyyy.M.d"),
                DateTimeFormat.ISO8601 => value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                DateTimeFormat.UnixTimestamp => ((DateTimeOffset)value).ToUnixTimeSeconds().ToString(),
                _ => value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            writer.WriteStringValue(dateString);
        }
    }

    public class JsonSerializationException : Exception
    {
        public JsonSerializationException(string message) : base(message) { }
        public JsonSerializationException(string message, Exception innerException) : base(message, innerException) { }
    }
}