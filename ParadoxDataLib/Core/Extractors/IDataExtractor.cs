using ParadoxDataLib.Core.Common;

namespace ParadoxDataLib.Core.Extractors
{
    /// <summary>
    /// Interface for extracting specific data types from generic ParadoxNode trees.
    /// Extractors convert the generic parsed structure into strongly-typed game objects.
    /// </summary>
    /// <typeparam name="T">The type of data this extractor produces</typeparam>
    public interface IDataExtractor<T>
    {
        /// <summary>
        /// Extracts typed data from a generic ParadoxNode tree
        /// </summary>
        /// <param name="node">The root node containing parsed Paradox data</param>
        /// <returns>The extracted data object</returns>
        T Extract(ParadoxNode node);

        /// <summary>
        /// Validates that the node structure is compatible with this extractor
        /// </summary>
        /// <param name="node">The node to validate</param>
        /// <returns>True if the node can be processed by this extractor</returns>
        bool CanExtract(ParadoxNode node);
    }
}