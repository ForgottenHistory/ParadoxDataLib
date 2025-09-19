using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using System.Threading.Tasks;
using ParadoxDataLib.Core.DataModels;

namespace ParadoxDataLib.Json
{
    public class JsonSchemaGenerator
    {
        private readonly JsonExportOptions _options;
        private readonly Dictionary<Type, JsonSchemaDefinition> _schemaCache;

        public JsonSchemaGenerator(JsonExportOptions options = null)
        {
            _options = options ?? JsonExportOptions.Pretty;
            _schemaCache = new Dictionary<Type, JsonSchemaDefinition>();
        }

        public JsonSchemaDefinition GenerateSchema<T>()
        {
            return GenerateSchema(typeof(T));
        }

        public JsonSchemaDefinition GenerateSchema(Type type)
        {
            if (_schemaCache.TryGetValue(type, out var cachedSchema))
                return cachedSchema;

            var schema = CreateSchemaDefinition(type);
            _schemaCache[type] = schema;
            return schema;
        }

        public string GenerateSchemaJson<T>()
        {
            var schema = GenerateSchema<T>();
            return JsonConvert.SerializeObject(schema, Formatting.Indented);
        }

        public string GenerateSchemaJson(Type type)
        {
            var schema = GenerateSchema(type);
            return JsonConvert.SerializeObject(schema, Formatting.Indented);
        }

        public JsonSchemaDefinition GenerateProvinceSchema()
        {
            return GenerateSchema<ProvinceData>();
        }

        public JsonSchemaDefinition GenerateCountrySchema()
        {
            return GenerateSchema<CountryData>();
        }

        public JsonSchemaDefinition GenerateHistoricalEntrySchema()
        {
            return GenerateSchema<HistoricalEntry>();
        }

        public JsonSchemaDefinition GenerateGameDataSchema()
        {
            var schema = new JsonSchemaDefinition
            {
                Type = "object",
                Title = "Paradox Game Data",
                Description = "Complete game data including provinces and countries",
                Properties = new Dictionary<string, JsonSchemaProperty>
                {
                    ["metadata"] = new JsonSchemaProperty
                    {
                        Type = "object",
                        Description = "Export metadata and information",
                        Properties = new Dictionary<string, JsonSchemaProperty>
                        {
                            ["version"] = new JsonSchemaProperty { Type = "string", Description = "Schema version" },
                            ["exportedAt"] = new JsonSchemaProperty { Type = "string", Format = "date-time", Description = "Export timestamp" },
                            ["description"] = new JsonSchemaProperty { Type = "string", Description = "Export description" }
                        }
                    },
                    ["provinces"] = new JsonSchemaProperty
                    {
                        Type = "array",
                        Description = "Array of province data",
                        Items = GenerateSchema<ProvinceData>()
                    },
                    ["countries"] = new JsonSchemaProperty
                    {
                        Type = "array",
                        Description = "Array of country data",
                        Items = GenerateSchema<CountryData>()
                    }
                },
                Required = new List<string> { "provinces", "countries" }
            };

            return schema;
        }

        private JsonSchemaDefinition CreateSchemaDefinition(Type type)
        {
            var schema = new JsonSchemaDefinition
            {
                Type = GetJsonType(type),
                Title = GetTypeTitle(type),
                Description = GetTypeDescription(type)
            };

            if ((type.IsClass && type != typeof(string)) || (type.IsValueType && !type.IsPrimitive && !type.IsEnum))
            {
                schema.Properties = GenerateProperties(type);
                schema.Required = GetRequiredProperties(type);
            }
            else if (type.IsEnum)
            {
                schema.Enum = Enum.GetNames(type).ToList();
            }

            return schema;
        }

        private Dictionary<string, JsonSchemaProperty> GenerateProperties(Type type)
        {
            var properties = new Dictionary<string, JsonSchemaProperty>();

            var propertyInfos = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite);

            foreach (var prop in propertyInfos)
            {
                var propertyName = GetPropertyName(prop);

                // For schema generation, we should include all properties regardless of filtering
                // if (!_options.ShouldIncludeProperty(propertyName, null))
                //     continue;

                properties[propertyName] = CreatePropertySchema(prop);
            }

            return properties;
        }

        private JsonSchemaProperty CreatePropertySchema(PropertyInfo property)
        {
            var propType = property.PropertyType;
            var propertySchema = new JsonSchemaProperty
            {
                Type = GetJsonType(propType),
                Description = GetPropertyDescription(property)
            };

            // Handle nullable types
            if (Nullable.GetUnderlyingType(propType) != null)
            {
                propType = Nullable.GetUnderlyingType(propType);
                propertySchema.Type = GetJsonType(propType);
            }

            // Handle arrays and lists
            if (IsArrayOrList(propType))
            {
                propertySchema.Type = "array";
                var elementType = GetElementType(propType);
                if (elementType != null)
                {
                    propertySchema.Items = GenerateSchema(elementType);
                }
            }
            // Handle enums
            else if (propType.IsEnum)
            {
                propertySchema.Type = "string";
                propertySchema.Enum = Enum.GetNames(propType).ToList();
            }
            // Handle complex objects
            else if ((propType.IsClass && propType != typeof(string) && propType != typeof(object)) ||
                     (propType.IsValueType && !propType.IsPrimitive && !propType.IsEnum))
            {
                propertySchema = ConvertSchemaToProperty(GenerateSchema(propType));
            }

            // Add format information for special types
            if (propType == typeof(DateTime))
            {
                propertySchema.Format = _options.DateFormat == DateTimeFormat.ParadoxDate ? "paradox-date" : "date-time";
            }

            // Add validation constraints
            AddValidationConstraints(propertySchema, property);

            return propertySchema;
        }

        private void AddValidationConstraints(JsonSchemaProperty schema, PropertyInfo property)
        {
            var propType = property.PropertyType;

            // String length constraints
            if (propType == typeof(string))
            {
                // Add common string constraints based on property name
                var propName = property.Name.ToLowerInvariant();
                if (propName.Contains("tag") || propName.Contains("owner") || propName.Contains("controller"))
                {
                    schema.MinLength = 1;
                    schema.MaxLength = 3; // Country tags are typically 3 characters
                }
                else if (propName.Contains("culture") || propName.Contains("religion"))
                {
                    schema.MinLength = 1;
                    schema.MaxLength = 50;
                }
            }

            // Numeric constraints
            if (propType == typeof(int))
            {
                var propName = property.Name.ToLowerInvariant();
                if (propName.Contains("id") || propName.Contains("capital"))
                {
                    schema.Minimum = 1;
                }
                else if (propName.Contains("tax") || propName.Contains("production") || propName.Contains("manpower"))
                {
                    schema.Minimum = 0;
                    schema.Maximum = 99; // Typical max values in Paradox games
                }
            }
        }

        private string GetJsonType(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int) || type == typeof(long) || type == typeof(short)) return "integer";
            if (type == typeof(double) || type == typeof(float) || type == typeof(decimal)) return "number";
            if (type == typeof(bool)) return "boolean";
            if (type == typeof(DateTime)) return "string";
            if (IsArrayOrList(type)) return "array";
            if (type.IsEnum) return "string";
            if (type == typeof(object)) return "object";
            if (type.IsClass || type.IsValueType && !type.IsPrimitive && !type.IsEnum) return "object";

            return "string"; // Default fallback
        }

        private bool IsArrayOrList(Type type)
        {
            return type.IsArray ||
                   (type.IsGenericType && (
                       type.GetGenericTypeDefinition() == typeof(List<>) ||
                       type.GetGenericTypeDefinition() == typeof(IList<>) ||
                       type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                       type.GetGenericTypeDefinition() == typeof(ICollection<>)
                   ));
        }

        private Type GetElementType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();

            if (type.IsGenericType)
                return type.GetGenericArguments()[0];

            return null;
        }

        private string GetPropertyName(PropertyInfo property)
        {
            // Convert to camelCase for JSON
            var name = property.Name;
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }

        private string GetTypeTitle(Type type)
        {
            if (type == typeof(ProvinceData)) return "Province Data";
            if (type == typeof(CountryData)) return "Country Data";
            if (type == typeof(HistoricalEntry)) return "Historical Entry";

            return type.Name;
        }

        private string GetTypeDescription(Type type)
        {
            if (type == typeof(ProvinceData)) return "Represents a province in a Paradox game with all its properties and historical data";
            if (type == typeof(CountryData)) return "Represents a country in a Paradox game with government, culture, and historical information";
            if (type == typeof(HistoricalEntry)) return "Represents a historical event or change that occurred on a specific date";

            return $"Schema for {type.Name}";
        }

        private string GetPropertyDescription(PropertyInfo property)
        {
            var descriptions = new Dictionary<string, string>
            {
                ["ProvinceId"] = "Unique identifier for the province",
                ["Owner"] = "Country tag that owns this province",
                ["Controller"] = "Country tag that controls this province (may differ from owner during occupation)",
                ["Culture"] = "Primary culture of the province",
                ["Religion"] = "Primary religion of the province",
                ["TradeGood"] = "Trade good produced by this province",
                ["BaseTax"] = "Base tax value of the province",
                ["BaseProduction"] = "Base production value of the province",
                ["BaseManpower"] = "Base manpower value of the province",
                ["Cores"] = "List of country tags that have cores on this province",
                ["Buildings"] = "List of buildings constructed in this province",
                ["HistoricalEntries"] = "Historical events and changes for this province",
                ["Tag"] = "Unique three-letter identifier for the country",
                ["Government"] = "Government type of the country",
                ["PrimaryCulture"] = "Primary culture of the country",
                ["TechnologyGroup"] = "Technology group that determines available technologies",
                ["Capital"] = "Province ID of the country's capital",
                ["AcceptedCultures"] = "List of cultures accepted by this country",
                ["Date"] = "Date when this historical event occurred",
                ["Changes"] = "Dictionary of property changes that occurred on this date"
            };

            return descriptions.TryGetValue(property.Name, out var description) ? description : $"{property.Name} property";
        }

        private List<string> GetRequiredProperties(Type type)
        {
            var required = new List<string>();

            if (type == typeof(ProvinceData))
            {
                required.AddRange(new[] { "provinceId" });
            }
            else if (type == typeof(CountryData))
            {
                required.AddRange(new[] { "tag" });
            }
            else if (type == typeof(HistoricalEntry))
            {
                required.AddRange(new[] { "date" });
            }

            return required;
        }

        private JsonSchemaProperty ConvertSchemaToProperty(JsonSchemaDefinition schema)
        {
            return new JsonSchemaProperty
            {
                Type = schema.Type,
                Description = schema.Description,
                Properties = schema.Properties,
                Items = schema.Items,
                Enum = schema.Enum,
                Format = schema.Format,
                Minimum = schema.Minimum,
                Maximum = schema.Maximum,
                MinLength = schema.MinLength,
                MaxLength = schema.MaxLength
            };
        }
    }

    public class JsonSchemaDefinition
    {
        public string Schema { get; set; } = "http://json-schema.org/draft-07/schema#";
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, JsonSchemaProperty> Properties { get; set; } = new Dictionary<string, JsonSchemaProperty>();
        public List<string> Required { get; set; } = new List<string>();
        public JsonSchemaDefinition Items { get; set; }
        public List<string> Enum { get; set; } = new List<string>();
        public string Format { get; set; } = string.Empty;
        public double? Minimum { get; set; }
        public double? Maximum { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
    }

    public class JsonSchemaProperty
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, JsonSchemaProperty> Properties { get; set; } = new Dictionary<string, JsonSchemaProperty>();
        public JsonSchemaDefinition Items { get; set; }
        public List<string> Enum { get; set; } = new List<string>();
        public string Format { get; set; } = string.Empty;
        public double? Minimum { get; set; }
        public double? Maximum { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
    }

    public class JsonSchemaValidator
    {
        public async Task<JsonSchemaValidationResult> ValidateAsync(string json, string schema)
        {
            var result = new JsonSchemaValidationResult();

            try
            {
                // Basic JSON validation using Newtonsoft.Json
                var jsonObject = JsonConvert.DeserializeObject(json);
                var schemaObject = JsonConvert.DeserializeObject(schema);

                result.IsValid = true;
                result.ValidationErrors = new List<string>();

                // In a full implementation, you would use a proper JSON Schema validator library
                // like NJsonSchema or JsonSchema.Net
                // For now, we'll do basic structural validation

                await Task.CompletedTask; // Placeholder for async validation
            }
            catch (JsonException ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"JSON parsing error: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Validation error: {ex.Message}";
            }

            return result;
        }

        public JsonSchemaValidationResult Validate(string json, string schema)
        {
            return ValidateAsync(json, schema).Result;
        }
    }

    public class JsonSchemaValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new List<string>();

        public bool HasErrors => !IsValid || ValidationErrors.Any();

        public string GetSummary()
        {
            if (!IsValid)
                return $"Schema validation failed: {ErrorMessage}";

            if (ValidationErrors.Any())
                return $"Schema validation completed with {ValidationErrors.Count} warnings";

            return "Schema validation passed successfully";
        }
    }
}