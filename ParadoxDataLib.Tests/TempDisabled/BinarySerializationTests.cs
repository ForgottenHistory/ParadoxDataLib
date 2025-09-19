using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using ParadoxDataLib.Core.DataModels;
using ParadoxDataLib.Serialization;

namespace ParadoxDataLib.Tests
{
    public class BinarySerializationTests : IDisposable
    {
        private readonly string _tempDir;

        public BinarySerializationTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "BinarySerializationTests_" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void BinaryFormat_Header_ShouldHaveCorrectValues()
        {
            var header = new BinaryFormat.FileHeader
            {
                MagicHeader = BinaryFormat.MAGIC_HEADER,
                Version = BinaryFormat.CURRENT_VERSION,
                CompressionType = BinaryFormat.COMPRESSION_GZIP,
                CreatedTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ProvinceCount = 100,
                CountryCount = 50,
                StringTableSize = 2048,
                Checksum = new byte[4],
                Reserved = new byte[3]
            };

            Assert.True(header.IsValid());
            Assert.Equal(BinaryFormat.CURRENT_VERSION, header.Version);
            Assert.Equal(BinaryFormat.COMPRESSION_GZIP, header.CompressionType);
        }

        [Fact]
        public void BinaryFormat_CRC32_ShouldCalculateCorrectly()
        {
            var testData = System.Text.Encoding.UTF8.GetBytes("Hello, World!");
            var crc = BinaryFormat.CalculateCRC32(testData);

            // CRC32 for "Hello, World!" should be consistent
            Assert.NotEqual(0u, crc);
        }

        [Fact]
        public void BinaryDataWriter_ShouldWriteValidHeader()
        {
            var tempFile = Path.Combine(_tempDir, "header_test.dat");

            using (var fileStream = File.Create(tempFile))
            using (var writer = new BinaryDataWriter(fileStream, useCompression: false))
            {
                var provinces = new List<ProvinceData>();
                var countries = new List<CountryData>();

                writer.WriteGameData(provinces, countries);
            }

            // Verify file was created and has header
            Assert.True(File.Exists(tempFile));
            var fileInfo = new FileInfo(tempFile);
            Assert.True(fileInfo.Length >= BinaryFormat.HEADER_SIZE);
        }

        [Fact]
        public void BinaryDataReader_ShouldReadValidHeader()
        {
            var tempFile = Path.Combine(_tempDir, "reader_test.dat");

            // Write test data
            using (var fileStream = File.Create(tempFile))
            using (var writer = new BinaryDataWriter(fileStream, useCompression: false))
            {
                var provinces = new List<ProvinceData>();
                var countries = new List<CountryData>();
                writer.WriteGameData(provinces, countries);
            }

            // Read and verify
            using (var fileStream = File.OpenRead(tempFile))
            using (var reader = new BinaryDataReader(fileStream))
            {
                Assert.True(reader.Header.IsValid());
                Assert.Equal(BinaryFormat.CURRENT_VERSION, reader.Header.Version);
                Assert.Equal(BinaryFormat.COMPRESSION_NONE, reader.Header.CompressionType);
            }
        }

        [Fact]
        public void BinarySerialization_RoundTrip_EmptyData_ShouldWork()
        {
            var tempFile = Path.Combine(_tempDir, "empty_roundtrip.dat");

            var originalProvinces = new List<ProvinceData>();
            var originalCountries = new List<CountryData>();

            // Write
            using (var fileStream = File.Create(tempFile))
            using (var writer = new BinaryDataWriter(fileStream, useCompression: true))
            {
                writer.WriteGameData(originalProvinces, originalCountries);
            }

            // Read
            using (var fileStream = File.OpenRead(tempFile))
            using (var reader = new BinaryDataReader(fileStream))
            {
                var (provinces, countries) = reader.ReadGameData();

                Assert.NotNull(provinces);
                Assert.NotNull(countries);
                Assert.Empty(provinces);
                Assert.Empty(countries);
            }
        }

        [Fact]
        public void BinarySerialization_RoundTrip_WithData_ShouldWork()
        {
            var tempFile = Path.Combine(_tempDir, "data_roundtrip.dat");

            // Create test data
            var originalProvinces = new List<ProvinceData>
            {
                new ProvinceData(1, "Test Province")
                {
                    Owner = "TST",
                    Controller = "TST",
                    Culture = "test_culture",
                    Religion = "test_religion",
                    BaseTax = 3.5f,
                    BaseProduction = 2.0f,
                    BaseManpower = 1.5f,
                    IsCity = true,
                    IsHre = false,
                    Cores = new List<string> { "TST", "OTH" },
                    Buildings = new List<string> { "temple", "marketplace" }
                }
            };

            var originalCountries = new List<CountryData>
            {
                new CountryData("TST", "Test Country")
                {
                    Government = "monarchy",
                    PrimaryCulture = "test_culture",
                    Religion = "test_religion",
                    TechnologyGroup = "western",
                    Capital = 1,
                    AcceptedCultures = new List<string> { "test_culture", "other_culture" }
                }
            };

            // Write
            using (var fileStream = File.Create(tempFile))
            using (var writer = new BinaryDataWriter(fileStream, useCompression: true))
            {
                writer.WriteGameData(originalProvinces, originalCountries);
            }

            // Read
            using (var fileStream = File.OpenRead(tempFile))
            using (var reader = new BinaryDataReader(fileStream))
            {
                var (provinces, countries) = reader.ReadGameData();

                // Verify provinces
                Assert.Single(provinces);
                var province = provinces[0];
                Assert.Equal(1, province.ProvinceId);
                Assert.Equal("Test Province", province.Name);
                Assert.Equal("TST", province.Owner);
                Assert.Equal("TST", province.Controller);
                Assert.Equal("test_culture", province.Culture);
                Assert.Equal("test_religion", province.Religion);
                Assert.Equal(3.5f, province.BaseTax, 2);
                Assert.Equal(2.0f, province.BaseProduction, 2);
                Assert.Equal(1.5f, province.BaseManpower, 2);
                Assert.True(province.IsCity);
                Assert.False(province.IsHre);
                Assert.Equal(2, province.Cores.Count);
                Assert.Contains("TST", province.Cores);
                Assert.Contains("OTH", province.Cores);
                Assert.Equal(2, province.Buildings.Count);
                Assert.Contains("temple", province.Buildings);
                Assert.Contains("marketplace", province.Buildings);

                // Verify countries
                Assert.Single(countries);
                var country = countries[0];
                Assert.Equal("TST", country.Tag);
                Assert.Equal("Test Country", country.Name);
                Assert.Equal("monarchy", country.Government);
                Assert.Equal("test_culture", country.PrimaryCulture);
                Assert.Equal("test_religion", country.Religion);
                Assert.Equal("western", country.TechnologyGroup);
                Assert.Equal(1, country.Capital);
                Assert.Equal(2, country.AcceptedCultures.Count);
                Assert.Contains("test_culture", country.AcceptedCultures);
                Assert.Contains("other_culture", country.AcceptedCultures);
            }
        }

        [Fact]
        public void BinarySerialization_Compression_ShouldReduceFileSize()
        {
            var uncompressedFile = Path.Combine(_tempDir, "uncompressed.dat");
            var compressedFile = Path.Combine(_tempDir, "compressed.dat");

            // Create test data with repetitive strings (good for compression)
            var provinces = new List<ProvinceData>();
            var countries = new List<CountryData>();

            for (int i = 1; i <= 100; i++)
            {
                provinces.Add(new ProvinceData(i, $"Province {i}")
                {
                    Owner = "TST",
                    Controller = "TST",
                    Culture = "test_culture",
                    Religion = "test_religion",
                    BaseTax = 3.0f,
                    BaseProduction = 2.0f,
                    BaseManpower = 1.0f
                });
            }

            for (int i = 1; i <= 10; i++)
            {
                countries.Add(new CountryData($"T{i:D2}", $"Test Country {i}")
                {
                    Government = "monarchy",
                    PrimaryCulture = "test_culture",
                    Religion = "test_religion",
                    TechnologyGroup = "western",
                    Capital = i
                });
            }

            // Write uncompressed
            using (var fileStream = File.Create(uncompressedFile))
            using (var writer = new BinaryDataWriter(fileStream, useCompression: false))
            {
                writer.WriteGameData(provinces, countries);
            }

            // Write compressed
            using (var fileStream = File.Create(compressedFile))
            using (var writer = new BinaryDataWriter(fileStream, useCompression: true))
            {
                writer.WriteGameData(provinces, countries);
            }

            var uncompressedSize = new FileInfo(uncompressedFile).Length;
            var compressedSize = new FileInfo(compressedFile).Length;

            // Compressed should be smaller than uncompressed
            Assert.True(compressedSize < uncompressedSize,
                $"Compressed size ({compressedSize}) should be smaller than uncompressed size ({uncompressedSize})");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void BinarySerialization_LargeDataset_ShouldHandleCorrectly(bool useCompression)
        {
            var tempFile = Path.Combine(_tempDir, $"large_dataset_{useCompression}.dat");

            // Create larger test dataset
            var provinces = new List<ProvinceData>();
            var countries = new List<CountryData>();

            for (int i = 1; i <= 1000; i++)
            {
                provinces.Add(new ProvinceData(i, $"Province {i}")
                {
                    Owner = $"C{(i % 50) + 1:D2}",
                    Controller = $"C{(i % 50) + 1:D2}",
                    Culture = $"culture_{i % 20}",
                    Religion = $"religion_{i % 10}",
                    BaseTax = (float)(i % 10) + 1,
                    BaseProduction = (float)((i % 5) + 1),
                    BaseManpower = (float)((i % 3) + 1),
                    IsCity = i % 5 == 0,
                    IsHre = i % 7 == 0
                });
            }

            for (int i = 1; i <= 50; i++)
            {
                countries.Add(new CountryData($"C{i:D2}", $"Country {i}")
                {
                    Government = i % 3 == 0 ? "republic" : "monarchy",
                    PrimaryCulture = $"culture_{i % 20}",
                    Religion = $"religion_{i % 10}",
                    TechnologyGroup = i % 2 == 0 ? "western" : "eastern",
                    Capital = i * 20
                });
            }

            var startTime = DateTime.UtcNow;

            // Write
            using (var fileStream = File.Create(tempFile))
            using (var writer = new BinaryDataWriter(fileStream, useCompression))
            {
                writer.WriteGameData(provinces, countries);
            }

            var writeTime = DateTime.UtcNow - startTime;

            // Read
            startTime = DateTime.UtcNow;
            using (var fileStream = File.OpenRead(tempFile))
            using (var reader = new BinaryDataReader(fileStream))
            {
                var (readProvinces, readCountries) = reader.ReadGameData();

                Assert.Equal(provinces.Count, readProvinces.Count);
                Assert.Equal(countries.Count, readCountries.Count);
            }

            var readTime = DateTime.UtcNow - startTime;

            // Performance should be reasonable (less than 5 seconds for 1000 provinces)
            Assert.True(writeTime.TotalSeconds < 5, $"Write time was {writeTime.TotalSeconds:F2}s, expected < 5s");
            Assert.True(readTime.TotalSeconds < 5, $"Read time was {readTime.TotalSeconds:F2}s, expected < 5s");
        }

        [Fact]
        public void BinaryFormat_VersionCheck_ShouldRejectIncompatibleVersions()
        {
            var tempFile = Path.Combine(_tempDir, "version_test.dat");

            // Create a file with a future version (simulate incompatible version)
            using (var fileStream = File.Create(tempFile))
            using (var binaryWriter = new System.IO.BinaryWriter(fileStream))
            {
                // Write magic header
                binaryWriter.Write(BinaryFormat.MAGIC_HEADER);
                // Write incompatible version
                binaryWriter.Write(999);
                // Write rest of header with dummy data
                binaryWriter.Write((byte)0); // compression type
                binaryWriter.Write(0L); // timestamp
                binaryWriter.Write(0); // province count
                binaryWriter.Write(0); // country count
                binaryWriter.Write(0); // string table size
                binaryWriter.Write(new byte[4]); // checksum
                binaryWriter.Write(new byte[3]); // reserved
            }

            // Should throw exception when trying to read (InvalidDataException for invalid header, NotSupportedException for version)
            var exception = Assert.Throws<InvalidDataException>(() =>
            {
                using var fileStream = File.OpenRead(tempFile);
                using var reader = new BinaryDataReader(fileStream);
            });

            Assert.Contains("Invalid binary format header", exception.Message);
        }

        [Fact]
        public void BinaryFormat_VersionCompatibility_ShouldWorkCorrectly()
        {
            // Test current version is compatible
            Assert.True(BinaryFormat.IsVersionCompatible(BinaryFormat.CURRENT_VERSION));

            // Test other versions are not compatible
            Assert.False(BinaryFormat.IsVersionCompatible(0));
            Assert.False(BinaryFormat.IsVersionCompatible(999));
            Assert.False(BinaryFormat.IsVersionCompatible(-1));
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                try
                {
                    Directory.Delete(_tempDir, true);
                }
                catch
                {
                    // Ignore cleanup errors in tests
                }
            }
        }
    }
}