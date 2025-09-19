using System.Collections.Generic;

namespace ParadoxDataLib.Utils
{
    public class FileWatcherOptions
    {
        public bool EnableHotReload { get; set; } = true;
        public int DebounceMilliseconds { get; set; } = 100;
        public List<string> FileFilters { get; set; } = new List<string> { "*.txt", "*.yml", "*.mod" };
        public bool WatchSubdirectories { get; set; } = true;
        public int MaxQueueSize { get; set; } = 100;
        public int MaxConcurrentReloads { get; set; } = 3;
        public bool LogFileChanges { get; set; } = false;
        public bool NotifyOnErrors { get; set; } = true;
        public List<string> ExcludedDirectories { get; set; } = new List<string> { ".git", ".vs", "bin", "obj", "cache" };
        public List<string> ExcludedFilePatterns { get; set; } = new List<string> { "*.tmp", "*.bak", "*~" };

        public static FileWatcherOptions Default => new FileWatcherOptions();

        public static FileWatcherOptions Development => new FileWatcherOptions
        {
            EnableHotReload = true,
            DebounceMilliseconds = 50,
            LogFileChanges = true,
            NotifyOnErrors = true
        };

        public static FileWatcherOptions Production => new FileWatcherOptions
        {
            EnableHotReload = false,
            DebounceMilliseconds = 500,
            LogFileChanges = false,
            NotifyOnErrors = false
        };

        public static FileWatcherOptions Testing => new FileWatcherOptions
        {
            EnableHotReload = true,
            DebounceMilliseconds = 10,
            MaxQueueSize = 50,
            LogFileChanges = true
        };

        public bool ShouldWatchFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var fileName = System.IO.Path.GetFileName(filePath);
            var directory = System.IO.Path.GetDirectoryName(filePath) ?? "";

            // Check excluded directories
            foreach (var excludedDir in ExcludedDirectories)
            {
                if (directory.Contains(excludedDir, System.StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Check excluded file patterns
            foreach (var pattern in ExcludedFilePatterns)
            {
                if (IsPatternMatch(fileName, pattern))
                    return false;
            }

            // Check included file filters
            foreach (var filter in FileFilters)
            {
                if (IsPatternMatch(fileName, filter))
                    return true;
            }

            return false;
        }

        private static bool IsPatternMatch(string fileName, string pattern)
        {
            if (pattern == "*") return true;
            if (pattern.StartsWith("*."))
            {
                var extension = pattern.Substring(1);
                return fileName.EndsWith(extension, System.StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(fileName, pattern, System.StringComparison.OrdinalIgnoreCase);
        }

        public FileWatcherOptions Clone()
        {
            return new FileWatcherOptions
            {
                EnableHotReload = EnableHotReload,
                DebounceMilliseconds = DebounceMilliseconds,
                FileFilters = new List<string>(FileFilters),
                WatchSubdirectories = WatchSubdirectories,
                MaxQueueSize = MaxQueueSize,
                MaxConcurrentReloads = MaxConcurrentReloads,
                LogFileChanges = LogFileChanges,
                NotifyOnErrors = NotifyOnErrors,
                ExcludedDirectories = new List<string>(ExcludedDirectories),
                ExcludedFilePatterns = new List<string>(ExcludedFilePatterns)
            };
        }

        public void Validate()
        {
            if (DebounceMilliseconds < 0)
                throw new System.ArgumentException("DebounceMilliseconds must be non-negative");

            if (MaxQueueSize <= 0)
                throw new System.ArgumentException("MaxQueueSize must be positive");

            if (MaxConcurrentReloads <= 0)
                throw new System.ArgumentException("MaxConcurrentReloads must be positive");

            if (FileFilters.Count == 0)
                throw new System.ArgumentException("At least one file filter must be specified");
        }
    }
}