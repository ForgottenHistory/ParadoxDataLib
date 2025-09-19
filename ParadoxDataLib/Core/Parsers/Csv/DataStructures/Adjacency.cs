using System;

namespace ParadoxDataLib.Core.Parsers.Csv.DataStructures
{
    /// <summary>
    /// Represents an adjacency connection between two provinces from the map/adjacencies.csv file.
    /// Defines how provinces connect to each other (sea, land, rivers, etc.)
    /// </summary>
    public readonly struct Adjacency : IEquatable<Adjacency>
    {
        /// <summary>
        /// Source province ID
        /// </summary>
        public int From { get; }

        /// <summary>
        /// Destination province ID
        /// </summary>
        public int To { get; }

        /// <summary>
        /// Type of adjacency connection (sea, land, river, impassable)
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// ID of the province this connection passes through (for straits, canals)
        /// </summary>
        public int Through { get; }

        /// <summary>
        /// Starting X coordinate for the connection
        /// </summary>
        public int StartX { get; }

        /// <summary>
        /// Starting Y coordinate for the connection
        /// </summary>
        public int StartY { get; }

        /// <summary>
        /// Ending X coordinate for the connection
        /// </summary>
        public int StopX { get; }

        /// <summary>
        /// Ending Y coordinate for the connection
        /// </summary>
        public int StopY { get; }

        /// <summary>
        /// Human-readable comment describing this adjacency
        /// </summary>
        public string Comment { get; }

        /// <summary>
        /// Creates a new adjacency definition
        /// </summary>
        /// <param name="from">Source province ID</param>
        /// <param name="to">Destination province ID</param>
        /// <param name="type">Type of connection</param>
        /// <param name="through">Province ID this connection passes through</param>
        /// <param name="startX">Starting X coordinate</param>
        /// <param name="startY">Starting Y coordinate</param>
        /// <param name="stopX">Ending X coordinate</param>
        /// <param name="stopY">Ending Y coordinate</param>
        /// <param name="comment">Description of this adjacency</param>
        public Adjacency(int from, int to, string type, int through = -1,
                        int startX = -1, int startY = -1, int stopX = -1, int stopY = -1,
                        string comment = "")
        {
            From = from;
            To = to;
            Type = type ?? string.Empty;
            Through = through;
            StartX = startX;
            StartY = startY;
            StopX = stopX;
            StopY = stopY;
            Comment = comment ?? string.Empty;
        }

        /// <summary>
        /// Checks if this adjacency has valid province IDs
        /// </summary>
        public bool IsValid => From > 0 && To > 0 && !string.IsNullOrEmpty(Type);

        /// <summary>
        /// Checks if this adjacency is bidirectional (can be traversed both ways)
        /// </summary>
        public bool IsBidirectional => Type.Equals("sea", StringComparison.OrdinalIgnoreCase) ||
                                      Type.Equals("land", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if this adjacency has coordinate information
        /// </summary>
        public bool HasCoordinates => StartX >= 0 && StartY >= 0 && StopX >= 0 && StopY >= 0;

        /// <summary>
        /// Gets the reverse adjacency (swapping From and To)
        /// </summary>
        public Adjacency Reverse()
        {
            return new Adjacency(To, From, Type, Through, StopX, StopY, StartX, StartY, Comment);
        }

        public bool Equals(Adjacency other)
        {
            return From == other.From &&
                   To == other.To &&
                   Type == other.Type &&
                   Through == other.Through &&
                   StartX == other.StartX &&
                   StartY == other.StartY &&
                   StopX == other.StopX &&
                   StopY == other.StopY &&
                   Comment == other.Comment;
        }

        public override bool Equals(object obj)
        {
            return obj is Adjacency other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(From);
            hash.Add(To);
            hash.Add(Type);
            hash.Add(Through);
            hash.Add(StartX);
            hash.Add(StartY);
            hash.Add(StopX);
            hash.Add(StopY);
            hash.Add(Comment);
            return hash.ToHashCode();
        }

        public static bool operator ==(Adjacency left, Adjacency right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Adjacency left, Adjacency right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            var coordInfo = HasCoordinates ? $" [{StartX},{StartY}->{StopX},{StopY}]" : "";
            var throughInfo = Through > 0 ? $" via {Through}" : "";
            var commentInfo = !string.IsNullOrEmpty(Comment) ? $" ({Comment})" : "";

            return $"{From} -> {To} ({Type}){throughInfo}{coordInfo}{commentInfo}";
        }
    }
}