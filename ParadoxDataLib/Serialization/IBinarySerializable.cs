using System.IO;

namespace ParadoxDataLib.Serialization
{
    /// <summary>
    /// Interface for objects that can be serialized to/from binary format
    /// </summary>
    public interface IBinarySerializable
    {
        /// <summary>
        /// Writes the object to a binary writer
        /// </summary>
        void WriteTo(BinaryWriter writer);

        /// <summary>
        /// Reads the object from a binary reader
        /// </summary>
        void ReadFrom(BinaryReader reader);

        /// <summary>
        /// Gets the version number for this serializable type
        /// </summary>
        int SerializationVersion { get; }
    }

    /// <summary>
    /// Interface for factories that can create objects from binary data
    /// </summary>
    /// <typeparam name="T">The type to deserialize</typeparam>
    public interface IBinaryDeserializable<T> where T : IBinarySerializable
    {
        /// <summary>
        /// Creates an instance from binary data
        /// </summary>
        T CreateFrom(BinaryReader reader, int version);
    }
}