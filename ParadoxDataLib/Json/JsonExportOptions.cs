using System;
using System.Collections.Generic;

namespace ParadoxDataLib.Json
{
    public class JsonExportOptions
    {
        public JsonFormat Format { get; set; } = JsonFormat.Pretty;
        public bool IncludeNullValues { get; set; } = false;
        public bool IncludeEmptyCollections { get; set; } = false;
        public bool IncludeDefaultValues { get; set; } = false;
        public bool IndentJson { get; set; } = true;
        public string IndentString { get; set; } = "  ";
        public int MaxDepth { get; set; } = 50;
        public bool UseStringEnums { get; set; } = true;
        public bool IncludeTypeInformation { get; set; } = false;
        public bool ValidateOnExport { get; set; } = true;
        public bool CompressOutput { get; set; } = false;
        public bool IncludeMetadata { get; set; } = true;
        public bool IncludeTimestamp { get; set; } = true;
        public bool IncludeVersion { get; set; } = true;
        public HashSet<string> ExcludedProperties { get; set; } = new HashSet<string>();
        public HashSet<string> IncludedPropertiesOnly { get; set; } = new HashSet<string>();
        public int DecimalPlaces { get; set; } = 2;
        public DateTimeFormat DateFormat { get; set; } = DateTimeFormat.ISO8601;
        public bool SortProperties { get; set; } = false;
        public bool EscapeNonAscii { get; set; } = false;

        public static JsonExportOptions Pretty => new JsonExportOptions
        {
            Format = JsonFormat.Pretty,
            IndentJson = true,
            IncludeMetadata = true,
            SortProperties = true,
            ValidateOnExport = true
        };

        public static JsonExportOptions Minimal => new JsonExportOptions
        {
            Format = JsonFormat.Minimal,
            IndentJson = false,
            IncludeNullValues = false,
            IncludeEmptyCollections = false,
            IncludeDefaultValues = false,
            IncludeMetadata = false,
            IncludeTimestamp = false,
            IncludeVersion = false,
            ValidateOnExport = false
        };

        public static JsonExportOptions Debug => new JsonExportOptions
        {
            Format = JsonFormat.Debug,
            IndentJson = true,
            IncludeNullValues = true,
            IncludeEmptyCollections = true,
            IncludeDefaultValues = true,
            IncludeMetadata = true,
            IncludeTypeInformation = true,
            ValidateOnExport = true,
            SortProperties = true
        };

        public static JsonExportOptions Compact => new JsonExportOptions
        {
            Format = JsonFormat.Compact,
            IndentJson = false,
            IncludeNullValues = false,
            IncludeEmptyCollections = false,
            IncludeDefaultValues = false,
            IncludeMetadata = false,
            CompressOutput = true
        };

        public bool ShouldIncludeProperty(string propertyName, object value)
        {
            if (IncludedPropertiesOnly.Count > 0)
            {
                return IncludedPropertiesOnly.Contains(propertyName);
            }

            if (ExcludedProperties.Contains(propertyName))
            {
                return false;
            }

            if (!IncludeNullValues && value == null)
            {
                return false;
            }

            if (!IncludeEmptyCollections && value is System.Collections.ICollection collection && collection.Count == 0)
            {
                return false;
            }

            if (!IncludeDefaultValues && IsDefaultValue(value))
            {
                return false;
            }

            return true;
        }

        private bool IsDefaultValue(object value)
        {
            if (value == null) return true;

            var type = value.GetType();

            if (type == typeof(string))
                return string.IsNullOrEmpty((string)value);

            if (type == typeof(int))
                return (int)value == 0;

            if (type == typeof(double))
                return Math.Abs((double)value) < 0.001;

            if (type == typeof(bool))
                return (bool)value == false;

            if (type == typeof(DateTime))
                return (DateTime)value == DateTime.MinValue;

            return false;
        }

        public JsonExportOptions Clone()
        {
            return new JsonExportOptions
            {
                Format = Format,
                IncludeNullValues = IncludeNullValues,
                IncludeEmptyCollections = IncludeEmptyCollections,
                IncludeDefaultValues = IncludeDefaultValues,
                IndentJson = IndentJson,
                IndentString = IndentString,
                MaxDepth = MaxDepth,
                UseStringEnums = UseStringEnums,
                IncludeTypeInformation = IncludeTypeInformation,
                ValidateOnExport = ValidateOnExport,
                CompressOutput = CompressOutput,
                IncludeMetadata = IncludeMetadata,
                IncludeTimestamp = IncludeTimestamp,
                IncludeVersion = IncludeVersion,
                ExcludedProperties = new HashSet<string>(ExcludedProperties),
                IncludedPropertiesOnly = new HashSet<string>(IncludedPropertiesOnly),
                DecimalPlaces = DecimalPlaces,
                DateFormat = DateFormat,
                SortProperties = SortProperties,
                EscapeNonAscii = EscapeNonAscii
            };
        }
    }

    public enum JsonFormat
    {
        Pretty,
        Minimal,
        Compact,
        Debug
    }

    public enum DateTimeFormat
    {
        ISO8601,
        ParadoxDate,
        UnixTimestamp,
        Custom
    }
}