using System;

namespace ParadoxDataLib.Utils
{
    public enum FileChangeType
    {
        Created,
        Modified,
        Deleted,
        Renamed
    }

    public enum FileCategory
    {
        Province,
        Country,
        Localization,
        Mod,
        Unknown
    }

    public class FileChange
    {
        public string FilePath { get; set; } = string.Empty;
        public FileChangeType ChangeType { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? OldPath { get; set; }
        public FileCategory Category { get; set; }

        public FileChange()
        {
        }

        public FileChange(string filePath, FileChangeType changeType)
        {
            FilePath = filePath;
            ChangeType = changeType;
            Category = DetermineCategory(filePath);
        }

        public FileChange(string filePath, FileChangeType changeType, string? oldPath) : this(filePath, changeType)
        {
            OldPath = oldPath;
        }

        public bool IsValidChange()
        {
            return !string.IsNullOrEmpty(FilePath) &&
                   (ChangeType != FileChangeType.Renamed || !string.IsNullOrEmpty(OldPath));
        }

        public bool IsSameFileAs(FileChange other)
        {
            if (other == null) return false;

            return string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase) ||
                   (ChangeType == FileChangeType.Renamed && string.Equals(OldPath, other.FilePath, StringComparison.OrdinalIgnoreCase)) ||
                   (other.ChangeType == FileChangeType.Renamed && string.Equals(FilePath, other.OldPath, StringComparison.OrdinalIgnoreCase));
        }

        public static FileCategory DetermineCategory(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return FileCategory.Unknown;

            var normalizedPath = filePath.Replace('\\', '/').ToLowerInvariant();

            if (normalizedPath.Contains("/history/provinces/") || normalizedPath.Contains("provinces") && normalizedPath.EndsWith(".txt"))
                return FileCategory.Province;

            if (normalizedPath.Contains("/history/countries/") || normalizedPath.Contains("/common/countries/") ||
                (normalizedPath.Contains("countries") && normalizedPath.EndsWith(".txt")))
                return FileCategory.Country;

            if (normalizedPath.Contains("/localisation/") || normalizedPath.EndsWith(".yml"))
                return FileCategory.Localization;

            if (normalizedPath.EndsWith(".mod") || normalizedPath.Contains("/mod/"))
                return FileCategory.Mod;

            return FileCategory.Unknown;
        }

        public override string ToString()
        {
            var operation = ChangeType switch
            {
                FileChangeType.Created => "Created",
                FileChangeType.Modified => "Modified",
                FileChangeType.Deleted => "Deleted",
                FileChangeType.Renamed => $"Renamed from {OldPath}",
                _ => "Unknown"
            };

            return $"{operation}: {FilePath} ({Category})";
        }

        public override bool Equals(object? obj)
        {
            if (obj is not FileChange other) return false;

            return string.Equals(FilePath, other.FilePath, StringComparison.OrdinalIgnoreCase) &&
                   ChangeType == other.ChangeType &&
                   string.Equals(OldPath, other.OldPath, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                FilePath?.ToLowerInvariant(),
                ChangeType,
                OldPath?.ToLowerInvariant()
            );
        }
    }
}