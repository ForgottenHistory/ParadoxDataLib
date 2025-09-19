using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ParadoxDataLib.Core.DataModels;

namespace ParadoxDataLib.Json
{
    public class ParadoxJsonConverter
    {
        private readonly JsonExportOptions _options;
        private readonly JsonSerializerSettings _jsonOptions;

        public JsonSerializerSettings JsonOptions => _jsonOptions;

        public ParadoxJsonConverter(JsonExportOptions options = null)
        {
            _options = options ?? JsonExportOptions.Pretty;
            _jsonOptions = CreateJsonSerializerSettings();
        }

        private JsonSerializerSettings CreateJsonSerializerSettings()
        {
            var options = new JsonSerializerSettings
            {
                Formatting = _options.IndentJson ? Formatting.Indented : Formatting.None,
                NullValueHandling = _options.IncludeNullValues ? NullValueHandling.Include : NullValueHandling.Ignore,
                MaxDepth = _options.MaxDepth,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            };

            options.Converters.Add(new ProvinceDataJsonConverter(_options));
            options.Converters.Add(new CountryDataJsonConverter(_options));
            options.Converters.Add(new HistoricalEntryJsonConverter(_options));
            options.Converters.Add(new DateTimeJsonConverter(_options));

            if (_options.UseStringEnums)
            {
                options.Converters.Add(new StringEnumConverter());
            }

            return options;
        }

        public string Serialize<T>(T obj)
        {
            if (obj == null)
                return _options.IncludeNullValues ? "null" : "{}";

            try
            {
                var json = JsonConvert.SerializeObject(obj, _jsonOptions);

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
                return default;

            try
            {
                return JsonConvert.DeserializeObject<T>(json, _jsonOptions);
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

        public override ProvinceData ReadJson(JsonReader reader, Type objectType, ProvinceData existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return default(ProvinceData);

            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException("Expected start of object");

            var province = new ProvinceData();

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType != JsonToken.PropertyName)
                    continue;

                var propertyName = reader.Value?.ToString();
                reader.Read();

                switch (propertyName?.ToLowerInvariant())
                {
                    case "provinceId":
                    case "provinceid":
                        province.ProvinceId = Convert.ToInt32(reader.Value);
                        break;
                    case "owner":
                        province.Owner = reader.Value?.ToString() ?? string.Empty;
                        break;
                    case "controller":
                        province.Controller = reader.Value?.ToString() ?? string.Empty;
                        break;
                    case "culture":
                        province.Culture = reader.Value?.ToString() ?? string.Empty;
                        break;
                    case "religion":
                        province.Religion = reader.Value?.ToString() ?? string.Empty;
                        break;
                    case "tradegood":
                    case "tradeGood":
                        province.TradeGood = reader.Value?.ToString() ?? string.Empty;
                        break;
                    case "basetax":
                    case "baseTax":
                        province.BaseTax = Convert.ToInt32(reader.Value);
                        break;
                    case "baseproduction":
                    case "baseProduction":
                        province.BaseProduction = Convert.ToInt32(reader.Value);
                        break;
                    case "basemanpower":
                    case "baseManpower":
                        province.BaseManpower = Convert.ToInt32(reader.Value);
                        break;
                    case "cores":
                        province.Cores = serializer.Deserialize<List<string>>(reader) ?? new List<string>();
                        break;
                    case "buildings":
                        province.Buildings = serializer.Deserialize<List<string>>(reader) ?? new List<string>();
                        break;
                    case "historicalentries":
                    case "historicalEntries":
                        province.HistoricalEntries = serializer.Deserialize<List<HistoricalEntry>>(reader) ?? new List<HistoricalEntry>();
                        break;
                }
            }

            return province;
        }

        public override void WriteJson(JsonWriter writer, ProvinceData value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            WritePropertyIfShould(writer, serializer, "provinceId", value.ProvinceId);
            WritePropertyIfShould(writer, serializer, "owner", value.Owner);
            WritePropertyIfShould(writer, serializer, "controller", value.Controller);
            WritePropertyIfShould(writer, serializer, "culture", value.Culture);
            WritePropertyIfShould(writer, serializer, "religion", value.Religion);
            WritePropertyIfShould(writer, serializer, "tradeGood", value.TradeGood);
            WritePropertyIfShould(writer, serializer, "baseTax", value.BaseTax);
            WritePropertyIfShould(writer, serializer, "baseProduction", value.BaseProduction);
            WritePropertyIfShould(writer, serializer, "baseManpower", value.BaseManpower);
            WritePropertyIfShould(writer, serializer, "cores", value.Cores);
            WritePropertyIfShould(writer, serializer, "buildings", value.Buildings);
            WritePropertyIfShould(writer, serializer, "historicalEntries", value.HistoricalEntries);

            writer.WriteEndObject();
        }

        private void WritePropertyIfShould(JsonWriter writer, JsonSerializer serializer, string propertyName, object value)
        {
            if (_options.ShouldIncludeProperty(propertyName, value))
            {
                writer.WritePropertyName(propertyName);
                serializer.Serialize(writer, value);
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

        public override CountryData ReadJson(JsonReader reader, Type objectType, CountryData existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return default(CountryData);

            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException("Expected start of object");

            var country = new CountryData();

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType != JsonToken.PropertyName)
                    continue;

                var propertyName = reader.Value?.ToString();
                reader.Read();

                switch (propertyName?.ToLowerInvariant())
                {
                    case "tag":
                        country.Tag = reader.Value?.ToString() ?? string.Empty;
                        break;
                    case "government":
                        country.Government = reader.Value?.ToString() ?? string.Empty;
                        break;
                    case "primaryculture":
                    case "primaryCulture":
                        country.PrimaryCulture = reader.Value?.ToString() ?? string.Empty;
                        break;
                    case "religion":
                        country.Religion = reader.Value?.ToString() ?? string.Empty;
                        break;
                    case "technologygroup":
                    case "technologyGroup":
                        country.TechnologyGroup = reader.Value?.ToString() ?? string.Empty;
                        break;
                    case "capital":
                        country.Capital = Convert.ToInt32(reader.Value);
                        break;
                    case "acceptedcultures":
                    case "acceptedCultures":
                        country.AcceptedCultures = serializer.Deserialize<List<string>>(reader) ?? new List<string>();
                        break;
                    case "historicalentries":
                    case "historicalEntries":
                        country.HistoricalEntries = serializer.Deserialize<List<HistoricalEntry>>(reader) ?? new List<HistoricalEntry>();
                        break;
                }
            }

            return country;
        }

        public override void WriteJson(JsonWriter writer, CountryData value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            WritePropertyIfShould(writer, serializer, "tag", value.Tag);
            WritePropertyIfShould(writer, serializer, "government", value.Government);
            WritePropertyIfShould(writer, serializer, "primaryCulture", value.PrimaryCulture);
            WritePropertyIfShould(writer, serializer, "religion", value.Religion);
            WritePropertyIfShould(writer, serializer, "technologyGroup", value.TechnologyGroup);
            WritePropertyIfShould(writer, serializer, "capital", value.Capital);
            WritePropertyIfShould(writer, serializer, "acceptedCultures", value.AcceptedCultures);
            WritePropertyIfShould(writer, serializer, "historicalEntries", value.HistoricalEntries);

            writer.WriteEndObject();
        }

        private void WritePropertyIfShould(JsonWriter writer, JsonSerializer serializer, string propertyName, object value)
        {
            if (_options.ShouldIncludeProperty(propertyName, value))
            {
                writer.WritePropertyName(propertyName);
                serializer.Serialize(writer, value);
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

        public override HistoricalEntry ReadJson(JsonReader reader, Type objectType, HistoricalEntry existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return default(HistoricalEntry);

            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonException("Expected start of object");

            var entry = new HistoricalEntry();

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject)
                    break;

                if (reader.TokenType != JsonToken.PropertyName)
                    continue;

                var propertyName = reader.Value?.ToString();
                reader.Read();

                switch (propertyName?.ToLowerInvariant())
                {
                    case "date":
                        if (_options.DateFormat == DateTimeFormat.ParadoxDate)
                        {
                            var dateStr = reader.Value?.ToString();
                            if (DateTime.TryParseExact(dateStr, "yyyy.M.d", null, System.Globalization.DateTimeStyles.None, out var paradoxDate))
                                entry.Date = paradoxDate;
                        }
                        else
                        {
                            entry.Date = serializer.Deserialize<DateTime>(reader);
                        }
                        break;
                    case "changes":
                        entry.Changes = serializer.Deserialize<Dictionary<string, object>>(reader) ?? new Dictionary<string, object>();
                        break;
                }
            }

            return entry;
        }

        public override void WriteJson(JsonWriter writer, HistoricalEntry value, JsonSerializer serializer)
        {
            writer.WriteStartObject();

            if (_options.ShouldIncludeProperty("date", value.Date))
            {
                writer.WritePropertyName("date");
                if (_options.DateFormat == DateTimeFormat.ParadoxDate)
                {
                    writer.WriteValue(value.Date.ToString("yyyy.M.d"));
                }
                else
                {
                    serializer.Serialize(writer, value.Date);
                }
            }

            if (_options.ShouldIncludeProperty("changes", value.Changes))
            {
                writer.WritePropertyName("changes");
                serializer.Serialize(writer, value.Changes);
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

        public override DateTime ReadJson(JsonReader reader, Type objectType, DateTime existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var dateString = reader.Value?.ToString();
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

        public override void WriteJson(JsonWriter writer, DateTime value, JsonSerializer serializer)
        {
            string dateString = _options.DateFormat switch
            {
                DateTimeFormat.ParadoxDate => value.ToString("yyyy.M.d"),
                DateTimeFormat.ISO8601 => value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                DateTimeFormat.UnixTimestamp => ((DateTimeOffset)value).ToUnixTimeSeconds().ToString(),
                _ => value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            writer.WriteValue(dateString);
        }
    }

    public class JsonSerializationException : Exception
    {
        public JsonSerializationException(string message) : base(message) { }
        public JsonSerializationException(string message, Exception innerException) : base(message, innerException) { }
    }
}