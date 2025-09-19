namespace ParadoxDataLib.Core.Parsers.Bitmap
{
    /// <summary>
    /// Defines different modes for bitmap processing to optimize performance
    /// </summary>
    public enum BitmapProcessingMode
    {
        /// <summary>
        /// Only validate the bitmap header, don't process pixel data
        /// Fast validation for file integrity checking
        /// </summary>
        HeaderOnly,

        /// <summary>
        /// Process header and sample a few pixels to verify readability
        /// Good for quick format validation
        /// </summary>
        Sampling,

        /// <summary>
        /// Process all pixels and store interpreted data
        /// Full processing for complete bitmap analysis
        /// </summary>
        FullProcessing,

        /// <summary>
        /// Process pixels on-demand as they are accessed
        /// Memory-efficient for large files
        /// </summary>
        LazyLoading
    }
}