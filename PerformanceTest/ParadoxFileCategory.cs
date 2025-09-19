using System;
using System.IO;
using System.Linq;

namespace ParadoxDataLib.PerformanceTest
{
    /// <summary>
    /// Categorizes Paradox game files by type and purpose
    /// </summary>
    public enum ParadoxFileType
    {
        Text,
        Csv,
        Bitmap
    }

    /// <summary>
    /// Represents a category of Paradox game files
    /// </summary>
    public class ParadoxFileCategory
    {
        public string Name { get; }
        public ParadoxFileType Type { get; }
        public string Description { get; }
        public int FileCount { get; set; }
        public long TotalSize { get; set; }

        public ParadoxFileCategory(string name, ParadoxFileType type, string description)
        {
            Name = name;
            Type = type;
            Description = description;
            FileCount = 0;
            TotalSize = 0;
        }

        /// <summary>
        /// Determines the file category from file path
        /// </summary>
        public static ParadoxFileCategory DetermineCategory(string filePath)
        {
            var fileName = Path.GetFileName(filePath).ToLower();
            var extension = Path.GetExtension(filePath).ToLower();
            var directory = Path.GetDirectoryName(filePath)?.Replace('\\', '/');

            // Bitmap files
            if (extension == ".bmp")
            {
                if (fileName.Contains("provinces"))
                    return new ParadoxFileCategory("Province Maps", ParadoxFileType.Bitmap, "RGB province mapping bitmaps");
                if (fileName.Contains("rivers"))
                    return new ParadoxFileCategory("Rivers", ParadoxFileType.Bitmap, "River system bitmaps");
                if (fileName.Contains("terrain"))
                    return new ParadoxFileCategory("Terrain", ParadoxFileType.Bitmap, "Terrain type bitmaps");
                if (fileName.Contains("heightmap"))
                    return new ParadoxFileCategory("Heightmaps", ParadoxFileType.Bitmap, "Elevation data bitmaps");

                return new ParadoxFileCategory("Other Bitmaps", ParadoxFileType.Bitmap, "Miscellaneous bitmap files");
            }

            // CSV files
            if (extension == ".csv")
            {
                if (fileName.Contains("definition"))
                    return new ParadoxFileCategory("Province Definitions", ParadoxFileType.Csv, "Province RGB definitions");
                if (fileName.Contains("adjacencies"))
                    return new ParadoxFileCategory("Adjacencies", ParadoxFileType.Csv, "Province adjacency data");

                return new ParadoxFileCategory("Other CSV", ParadoxFileType.Csv, "Miscellaneous CSV data");
            }

            // Text files - categorize by directory structure
            if (extension == ".txt")
            {
                if (directory?.Contains("/history/provinces") == true)
                    return new ParadoxFileCategory("Province History", ParadoxFileType.Text, "Individual province history files");
                if (directory?.Contains("/history/countries") == true)
                    return new ParadoxFileCategory("Country History", ParadoxFileType.Text, "Country configuration files");
                if (directory?.Contains("/history/wars") == true)
                    return new ParadoxFileCategory("Wars", ParadoxFileType.Text, "Historical war definitions");
                if (directory?.Contains("/history/diplomacy") == true)
                    return new ParadoxFileCategory("Diplomacy", ParadoxFileType.Text, "Diplomatic relations");
                if (directory?.Contains("/history/advisors") == true)
                    return new ParadoxFileCategory("Advisors", ParadoxFileType.Text, "Historical advisor definitions");

                if (directory?.Contains("/common/buildings") == true)
                    return new ParadoxFileCategory("Buildings", ParadoxFileType.Text, "Building definitions");
                if (directory?.Contains("/common/technologies") == true)
                    return new ParadoxFileCategory("Technologies", ParadoxFileType.Text, "Technology trees");
                if (directory?.Contains("/common/ideas") == true)
                    return new ParadoxFileCategory("Ideas", ParadoxFileType.Text, "National and idea group definitions");
                if (directory?.Contains("/common/religions") == true)
                    return new ParadoxFileCategory("Religions", ParadoxFileType.Text, "Religion and denomination definitions");
                if (directory?.Contains("/common/governments") == true)
                    return new ParadoxFileCategory("Governments", ParadoxFileType.Text, "Government type definitions");
                if (directory?.Contains("/common/trade_goods") == true)
                    return new ParadoxFileCategory("Trade Goods", ParadoxFileType.Text, "Trade good definitions");
                if (directory?.Contains("/common/cultures") == true)
                    return new ParadoxFileCategory("Cultures", ParadoxFileType.Text, "Culture and culture group definitions");
                if (directory?.Contains("/common/countries") == true)
                    return new ParadoxFileCategory("Country Tags", ParadoxFileType.Text, "Country tag definitions");

                if (directory?.Contains("/common/") == true)
                {
                    var commonType = directory.Split('/').LastOrDefault(x => x != "");
                    return new ParadoxFileCategory($"Common - {commonType}", ParadoxFileType.Text, $"Common game data: {commonType}");
                }

                return new ParadoxFileCategory("Other Text", ParadoxFileType.Text, "Miscellaneous text files");
            }

            return new ParadoxFileCategory("Unknown", ParadoxFileType.Text, "Unrecognized file type");
        }

        public override string ToString() => $"{Name} ({FileCount} files, {TotalSize / (1024.0 * 1024.0):F1}MB)";
    }
}