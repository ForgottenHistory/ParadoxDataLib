using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ParadoxDataLib.Serialization
{
    /// <summary>
    /// Defines the binary format structure and constants
    /// </summary>
    public static class BinaryFormat
    {
        // File format constants
        public static readonly byte[] MAGIC_HEADER = Encoding.ASCII.GetBytes("PDLB"); // ParaDox Library Binary
        public const int CURRENT_VERSION = 1;
        public const int HEADER_SIZE = 32; // Fixed header size for fast parsing

        // Data type identifiers
        public const byte TYPE_PROVINCE = 0x01;
        public const byte TYPE_COUNTRY = 0x02;
        public const byte TYPE_STRING_TABLE = 0x03;
        public const byte TYPE_CROSS_REFERENCES = 0x04;
        public const byte TYPE_END_MARKER = 0xFF;

        // Compression flags
        public const byte COMPRESSION_NONE = 0x00;
        public const byte COMPRESSION_GZIP = 0x01;
        public const byte COMPRESSION_BROTLI = 0x02;

        /// <summary>
        /// Binary file header structure
        /// </summary>
        public struct FileHeader
        {
            public byte[] MagicHeader;      // 4 bytes: "PDLB"
            public int Version;             // 4 bytes: File format version
            public byte CompressionType;    // 1 byte: Compression algorithm used
            public long CreatedTimestamp;   // 8 bytes: UTC timestamp when created
            public int ProvinceCount;       // 4 bytes: Number of provinces
            public int CountryCount;        // 4 bytes: Number of countries
            public int StringTableSize;     // 4 bytes: Size of string table in bytes
            public byte[] Checksum;         // 4 bytes: CRC32 of data section
            public byte[] Reserved;         // 3 bytes: Reserved for future use

            public void WriteTo(BinaryWriter writer)
            {
                writer.Write(MagicHeader ?? MAGIC_HEADER);
                writer.Write(Version);
                writer.Write(CompressionType);
                writer.Write(CreatedTimestamp);
                writer.Write(ProvinceCount);
                writer.Write(CountryCount);
                writer.Write(StringTableSize);
                writer.Write(Checksum ?? new byte[4]);
                writer.Write(Reserved ?? new byte[3]);
            }

            public static FileHeader ReadFrom(BinaryReader reader)
            {
                var header = new FileHeader();
                header.MagicHeader = reader.ReadBytes(4);
                header.Version = reader.ReadInt32();
                header.CompressionType = reader.ReadByte();
                header.CreatedTimestamp = reader.ReadInt64();
                header.ProvinceCount = reader.ReadInt32();
                header.CountryCount = reader.ReadInt32();
                header.StringTableSize = reader.ReadInt32();
                header.Checksum = reader.ReadBytes(4);
                header.Reserved = reader.ReadBytes(3);
                return header;
            }

            public bool IsValid()
            {
                if (MagicHeader == null || MagicHeader.Length != 4)
                    return false;

                for (int i = 0; i < 4; i++)
                {
                    if (MagicHeader[i] != MAGIC_HEADER[i])
                        return false;
                }

                return Version > 0 && Version <= CURRENT_VERSION;
            }
        }

        /// <summary>
        /// Calculates CRC32 checksum for data integrity
        /// </summary>
        public static uint CalculateCRC32(byte[] data)
        {
            const uint polynomial = 0xEDB88320;
            var table = new uint[256];

            // Build CRC table
            for (uint i = 0; i < 256; i++)
            {
                uint crc = i;
                for (int j = 0; j < 8; j++)
                {
                    crc = (crc & 1) == 1 ? (crc >> 1) ^ polynomial : crc >> 1;
                }
                table[i] = crc;
            }

            // Calculate CRC
            uint result = 0xFFFFFFFF;
            foreach (byte b in data)
            {
                result = table[(result ^ b) & 0xFF] ^ (result >> 8);
            }

            return ~result;
        }

        /// <summary>
        /// Validates file format version compatibility
        /// </summary>
        public static bool IsVersionCompatible(int fileVersion)
        {
            // For now, only support exact version match
            // In the future, we can implement backward compatibility logic
            return fileVersion == CURRENT_VERSION;
        }
    }
}