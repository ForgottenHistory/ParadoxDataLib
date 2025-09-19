using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ParadoxDataLib.Core.Common;
using ParadoxDataLib.Core.DataModels;

namespace ParadoxDataLib.Serialization
{
    /// <summary>
    /// High-performance binary writer for Paradox game data
    /// </summary>
    public class BinaryDataWriter : IDisposable
    {
        private readonly Stream _baseStream;
        private readonly BinaryWriter _writer;
        private readonly bool _useCompression;
        private readonly Dictionary<string, int> _stringTable;
        private bool _disposed;

        public BinaryDataWriter(Stream stream, bool useCompression = true)
        {
            _baseStream = stream ?? throw new ArgumentNullException(nameof(stream));
            _useCompression = useCompression;
            _stringTable = new Dictionary<string, int>();

            // Create writer with appropriate stream
            if (useCompression)
            {
                var gzipStream = new GZipStream(stream, CompressionMode.Compress, leaveOpen: true);
                _writer = new BinaryWriter(gzipStream);
            }
            else
            {
                _writer = new BinaryWriter(stream);
            }
        }

        /// <summary>
        /// Writes complete game data to binary format
        /// </summary>
        public void WriteGameData(IEnumerable<ProvinceData> provinces, IEnumerable<CountryData> countries)
        {
            var provinceList = provinces.ToList();
            var countryList = countries.ToList();

            // Pre-build string table
            BuildStringTable(provinceList, countryList);

            // Write header
            WriteHeader(provinceList.Count, countryList.Count);

            // Write string table
            WriteStringTable();

            // Write data sections
            WriteProvinces(provinceList);
            WriteCountries(countryList);

            // Write end marker
            _writer.Write(BinaryFormat.TYPE_END_MARKER);

            _writer.Flush();
        }

        private void WriteHeader(int provinceCount, int countryCount)
        {
            var header = new BinaryFormat.FileHeader
            {
                Version = BinaryFormat.CURRENT_VERSION,
                CompressionType = _useCompression ? BinaryFormat.COMPRESSION_GZIP : BinaryFormat.COMPRESSION_NONE,
                CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ProvinceCount = provinceCount,
                CountryCount = countryCount,
                StringTableSize = EstimateStringTableSize(),
                Checksum = new byte[4], // Will be calculated later if needed
                Reserved = new byte[3]
            };

            // Write header to base stream (not compressed)
            var originalPosition = _baseStream.Position;
            using (var headerWriter = new BinaryWriter(_baseStream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                header.WriteTo(headerWriter);
            }
        }

        private void BuildStringTable(IEnumerable<ProvinceData> provinces, IEnumerable<CountryData> countries)
        {
            var strings = new HashSet<string>();

            // Collect all strings from provinces
            foreach (var province in provinces)
            {
                AddStringIfNotNull(strings, province.Name);
                AddStringIfNotNull(strings, province.Owner);
                AddStringIfNotNull(strings, province.Controller);
                AddStringIfNotNull(strings, province.Culture);
                AddStringIfNotNull(strings, province.Religion);
                AddStringIfNotNull(strings, province.Capital);
                AddStringIfNotNull(strings, province.TradeGood);
                AddStringIfNotNull(strings, province.Terrain);
                AddStringIfNotNull(strings, province.Climate);
                AddStringIfNotNull(strings, province.TradeNode);

                // Add strings from collections
                foreach (var core in province.Cores)
                    AddStringIfNotNull(strings, core);
                foreach (var building in province.Buildings)
                    AddStringIfNotNull(strings, building);
                foreach (var discovered in province.DiscoveredBy)
                    AddStringIfNotNull(strings, discovered);
            }

            // Collect all strings from countries
            foreach (var country in countries)
            {
                AddStringIfNotNull(strings, country.Tag);
                AddStringIfNotNull(strings, country.Name);
                AddStringIfNotNull(strings, country.Government);
                AddStringIfNotNull(strings, country.PrimaryCulture);
                AddStringIfNotNull(strings, country.Religion);
                AddStringIfNotNull(strings, country.TechnologyGroup);
                // Capital and FixedCapital are integers, not strings - skip them for string table

                // Add strings from collections
                foreach (var culture in country.AcceptedCultures)
                    AddStringIfNotNull(strings, culture);
            }

            // Build lookup table
            int index = 0;
            foreach (var str in strings.OrderBy(s => s)) // Sort for better compression
            {
                _stringTable[str] = index++;
            }
        }

        private void AddStringIfNotNull(HashSet<string> strings, string? value)
        {
            if (!string.IsNullOrEmpty(value))
                strings.Add(value);
        }

        private void WriteStringTable()
        {
            _writer.Write(BinaryFormat.TYPE_STRING_TABLE);
            _writer.Write(_stringTable.Count);

            foreach (var kvp in _stringTable.OrderBy(x => x.Value))
            {
                _writer.Write(kvp.Value); // Index
                _writer.Write(kvp.Key);   // String value
            }
        }

        private void WriteProvinces(IEnumerable<ProvinceData> provinces)
        {
            _writer.Write(BinaryFormat.TYPE_PROVINCE);
            _writer.Write(provinces.Count());

            foreach (var province in provinces)
            {
                WriteProvince(province);
            }
        }

        private void WriteProvince(ProvinceData province)
        {
            _writer.Write(province.ProvinceId);
            WriteString(province.Name);
            WriteString(province.Owner);
            WriteString(province.Controller);
            WriteString(province.Culture);
            WriteString(province.Religion);
            WriteString(province.Capital);
            WriteString(province.TradeGood);
            WriteString(province.Terrain);
            WriteString(province.Climate);
            WriteString(province.TradeNode);

            _writer.Write(province.IsHre);
            _writer.Write(province.IsCity);
            _writer.Write(province.BaseTax);
            _writer.Write(province.BaseProduction);
            _writer.Write(province.BaseManpower);
            _writer.Write(province.ExtraCost);
            _writer.Write(province.CenterOfTrade);

            // Write collections
            WriteStringCollection(province.Cores);
            WriteStringCollection(province.Buildings);
            WriteStringCollection(province.DiscoveredBy);

            // Write historical entries count (implementation details can be added later)
            _writer.Write(province.HistoricalEntries?.Count ?? 0);
        }

        private void WriteCountries(IEnumerable<CountryData> countries)
        {
            _writer.Write(BinaryFormat.TYPE_COUNTRY);
            _writer.Write(countries.Count());

            foreach (var country in countries)
            {
                WriteCountry(country);
            }
        }

        private void WriteCountry(CountryData country)
        {
            WriteString(country.Tag);
            WriteString(country.Name);
            WriteString(country.Government);
            WriteString(country.PrimaryCulture);
            WriteString(country.Religion);
            WriteString(country.TechnologyGroup);
            _writer.Write(country.Capital);
            _writer.Write(country.FixedCapital ?? -1);

            WriteStringCollection(country.AcceptedCultures);

            // Write historical entries count (implementation details can be added later)
            _writer.Write(country.HistoricalEntries?.Count ?? 0);
        }

        private void WriteString(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                _writer.Write(-1); // Null marker
            }
            else
            {
                _writer.Write(_stringTable[value]); // String table index
            }
        }

        private void WriteStringCollection(IList<string> collection)
        {
            _writer.Write(collection?.Count ?? 0);
            if (collection != null)
            {
                foreach (var item in collection)
                {
                    WriteString(item);
                }
            }
        }

        private int EstimateStringTableSize()
        {
            // Rough estimate: 4 bytes per index + average 20 characters per string
            return _stringTable.Count * (4 + 20);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _writer?.Dispose();
                _disposed = true;
            }
        }
    }
}