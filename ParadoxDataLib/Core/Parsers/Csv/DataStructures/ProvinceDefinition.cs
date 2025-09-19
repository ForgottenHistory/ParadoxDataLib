using System;

namespace ParadoxDataLib.Core.Parsers.Csv.DataStructures
{
    /// <summary>
    /// Represents a province definition from the map/definition.csv file.
    /// Contains the province ID, RGB color values, and name mapping.
    /// </summary>
    public readonly struct ProvinceDefinition : IEquatable<ProvinceDefinition>
    {
        /// <summary>
        /// The unique province identifier
        /// </summary>
        public int ProvinceId { get; }

        /// <summary>
        /// Red component of the province color (0-255)
        /// </summary>
        public byte Red { get; }

        /// <summary>
        /// Green component of the province color (0-255)
        /// </summary>
        public byte Green { get; }

        /// <summary>
        /// Blue component of the province color (0-255)
        /// </summary>
        public byte Blue { get; }

        /// <summary>
        /// The display name of the province
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Unused field from the CSV (typically 'x')
        /// </summary>
        public string Unused { get; }

        /// <summary>
        /// Creates a new province definition
        /// </summary>
        /// <param name="provinceId">The unique province identifier</param>
        /// <param name="red">Red color component (0-255)</param>
        /// <param name="green">Green color component (0-255)</param>
        /// <param name="blue">Blue color component (0-255)</param>
        /// <param name="name">Province display name</param>
        /// <param name="unused">Unused field value</param>
        public ProvinceDefinition(int provinceId, byte red, byte green, byte blue, string name, string unused = "x")
        {
            ProvinceId = provinceId;
            Red = red;
            Green = green;
            Blue = blue;
            Name = name ?? string.Empty;
            Unused = unused ?? "x";
        }

        /// <summary>
        /// Gets the RGB color as a packed integer value
        /// </summary>
        public int RgbValue => (Red << 16) | (Green << 8) | Blue;

        /// <summary>
        /// Gets a string representation of the RGB color
        /// </summary>
        public string RgbString => $"({Red},{Green},{Blue})";

        /// <summary>
        /// Checks if this definition has a valid province ID
        /// </summary>
        public bool IsValid => ProvinceId > 0 && !string.IsNullOrEmpty(Name);

        public bool Equals(ProvinceDefinition other)
        {
            return ProvinceId == other.ProvinceId &&
                   Red == other.Red &&
                   Green == other.Green &&
                   Blue == other.Blue &&
                   Name == other.Name &&
                   Unused == other.Unused;
        }

        public override bool Equals(object obj)
        {
            return obj is ProvinceDefinition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ProvinceId, Red, Green, Blue, Name, Unused);
        }

        public static bool operator ==(ProvinceDefinition left, ProvinceDefinition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProvinceDefinition left, ProvinceDefinition right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{ProvinceId}: {Name} {RgbString}";
        }
    }
}