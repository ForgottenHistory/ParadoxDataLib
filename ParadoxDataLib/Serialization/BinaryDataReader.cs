using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ParadoxDataLib.Core.DataModels;

namespace ParadoxDataLib.Serialization
{
    /// <summary>
    /// High-performance binary reader for Paradox game data
    /// </summary>
    public class BinaryDataReader : IDisposable
    {
        private readonly Stream _baseStream;
        private readonly BinaryReader _reader;
        private readonly BinaryFormat.FileHeader _header;
        private readonly Dictionary<int, string> _stringTable;
        private bool _disposed;

        public BinaryFormat.FileHeader Header => _header;

        public BinaryDataReader(Stream stream)
        {
            _baseStream = stream ?? throw new ArgumentNullException(nameof(stream));

            // Read header from base stream
            _header = BinaryFormat.FileHeader.ReadFrom(new BinaryReader(stream));

            if (!_header.IsValid())
                throw new InvalidDataException("Invalid binary format header");

            if (!BinaryFormat.IsVersionCompatible(_header.Version))
                throw new NotSupportedException($"Binary format version {_header.Version} is not supported");

            // Create reader with appropriate stream
            if (_header.CompressionType == BinaryFormat.COMPRESSION_GZIP)
            {
                var gzipStream = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
                _reader = new BinaryReader(gzipStream);
            }
            else if (_header.CompressionType == BinaryFormat.COMPRESSION_NONE)
            {
                _reader = new BinaryReader(stream);
            }
            else
            {
                throw new NotSupportedException($"Compression type {_header.CompressionType} is not supported");
            }

            _stringTable = new Dictionary<int, string>();
        }

        /// <summary>
        /// Reads complete game data from binary format
        /// </summary>
        public (List<ProvinceData> Provinces, List<CountryData> Countries) ReadGameData()
        {
            var provinces = new List<ProvinceData>();
            var countries = new List<CountryData>();

            try
            {
                while (true)
                {
                    var sectionType = _reader.ReadByte();

                    switch (sectionType)
                    {
                        case BinaryFormat.TYPE_STRING_TABLE:
                            ReadStringTable();
                            break;

                        case BinaryFormat.TYPE_PROVINCE:
                            provinces = ReadProvinces();
                            break;

                        case BinaryFormat.TYPE_COUNTRY:
                            countries = ReadCountries();
                            break;

                        case BinaryFormat.TYPE_CROSS_REFERENCES:
                            // Skip cross-reference data for now (can be rebuilt)
                            SkipSection();
                            break;

                        case BinaryFormat.TYPE_END_MARKER:
                            return (provinces, countries);

                        default:
                            throw new InvalidDataException($"Unknown section type: 0x{sectionType:X2}");
                    }
                }
            }
            catch (EndOfStreamException)
            {
                // File ended without end marker - assume valid but incomplete
                return (provinces, countries);
            }
        }

        private void ReadStringTable()
        {
            var count = _reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                var index = _reader.ReadInt32();
                var value = _reader.ReadString();
                _stringTable[index] = value;
            }
        }

        private List<ProvinceData> ReadProvinces()
        {
            var count = _reader.ReadInt32();
            var provinces = new List<ProvinceData>(count);

            for (int i = 0; i < count; i++)
            {
                provinces.Add(ReadProvince());
            }

            return provinces;
        }

        private ProvinceData ReadProvince()
        {
            var provinceId = _reader.ReadInt32();
            var name = ReadString() ?? "";
            var province = new ProvinceData(provinceId, name);

            province.Owner = ReadString();
            province.Controller = ReadString();
            province.Culture = ReadString();
            province.Religion = ReadString();
            province.Capital = ReadString();
            province.TradeGood = ReadString();
            province.Terrain = ReadString();
            province.Climate = ReadString();
            province.TradeNode = ReadString();

            province.IsHre = _reader.ReadBoolean();
            province.IsCity = _reader.ReadBoolean();
            province.BaseTax = _reader.ReadSingle();
            province.BaseProduction = _reader.ReadSingle();
            province.BaseManpower = _reader.ReadSingle();
            province.ExtraCost = _reader.ReadSingle();
            province.CenterOfTrade = _reader.ReadInt32();

            // Read collections
            province.Cores = ReadStringList();
            province.Buildings = ReadStringList();
            province.DiscoveredBy = ReadStringList();

            // Read historical entries count (skip data for now)
            var historicalCount = _reader.ReadInt32();
            // TODO: Read historical entries when implemented

            return province;
        }

        private List<CountryData> ReadCountries()
        {
            var count = _reader.ReadInt32();
            var countries = new List<CountryData>(count);

            for (int i = 0; i < count; i++)
            {
                countries.Add(ReadCountry());
            }

            return countries;
        }

        private CountryData ReadCountry()
        {
            var tag = ReadString() ?? "";
            var name = ReadString() ?? "";
            var country = new CountryData(tag, name);

            country.Government = ReadString();
            country.PrimaryCulture = ReadString();
            country.Religion = ReadString();
            country.TechnologyGroup = ReadString();
            country.Capital = _reader.ReadInt32();
            var fixedCapitalValue = _reader.ReadInt32();
            country.FixedCapital = fixedCapitalValue == -1 ? (int?)null : fixedCapitalValue;

            country.AcceptedCultures = ReadStringList();

            // Read historical entries count (skip data for now)
            var historicalCount = _reader.ReadInt32();
            // TODO: Read historical entries when implemented

            return country;
        }

        private string? ReadString()
        {
            var index = _reader.ReadInt32();
            if (index == -1)
                return null;

            if (_stringTable.TryGetValue(index, out var value))
                return value;

            throw new InvalidDataException($"String table index {index} not found");
        }

        private List<string> ReadStringList()
        {
            var count = _reader.ReadInt32();
            var list = new List<string>(count);

            for (int i = 0; i < count; i++)
            {
                var value = ReadString();
                if (value != null)
                    list.Add(value);
            }

            return list;
        }

        private void SkipSection()
        {
            // Skip section by reading until next known section type or end
            // This is a simple implementation - could be more sophisticated
            while (_reader.BaseStream.Position < _reader.BaseStream.Length)
            {
                var nextByte = _reader.ReadByte();
                if (nextByte == BinaryFormat.TYPE_PROVINCE ||
                    nextByte == BinaryFormat.TYPE_COUNTRY ||
                    nextByte == BinaryFormat.TYPE_STRING_TABLE ||
                    nextByte == BinaryFormat.TYPE_END_MARKER)
                {
                    // Rewind one byte
                    _reader.BaseStream.Position--;
                    break;
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _reader?.Dispose();
                _disposed = true;
            }
        }
    }
}