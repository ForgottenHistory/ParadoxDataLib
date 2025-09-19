using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ParadoxDataLib.PerformanceTest
{
    /// <summary>
    /// Tracks parsing statistics across all file types and categories
    /// </summary>
    public class ParsingStatistics
    {
        private readonly object _lock = new object();
        private readonly Stopwatch _totalTimer = new Stopwatch();
        private readonly ConcurrentDictionary<string, ParadoxFileCategory> _categories = new();
        private readonly ConcurrentBag<ParseResult> _results = new();
        private readonly ConcurrentBag<string> _errors = new();

        public int TotalFiles { get; private set; }
        public int SuccessfulFiles => _results.Count(r => r.Success);
        public int FailedFiles => _results.Count(r => !r.Success);
        public double SuccessRate => TotalFiles > 0 ? (double)SuccessfulFiles / TotalFiles * 100 : 0;

        public TimeSpan TotalTime => _totalTimer.Elapsed;
        public double FilesPerSecond => TotalTime.TotalSeconds > 0 ? TotalFiles / TotalTime.TotalSeconds : 0;

        public long StartMemory { get; private set; }
        public long CurrentMemory => GC.GetTotalMemory(false);
        public long PeakMemory { get; private set; }
        public long MemoryUsed => CurrentMemory - StartMemory;

        /// <summary>
        /// Starts timing and memory tracking
        /// </summary>
        public void Start()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            StartMemory = GC.GetTotalMemory(false);
            PeakMemory = StartMemory;
            _totalTimer.Start();
        }

        /// <summary>
        /// Stops timing
        /// </summary>
        public void Stop()
        {
            _totalTimer.Stop();
        }

        /// <summary>
        /// Records a file parsing attempt
        /// </summary>
        public void RecordFile(string filePath, bool success, TimeSpan parseTime, Exception error = null)
        {
            lock (_lock)
            {
                TotalFiles++;

                var category = ParadoxFileCategory.DetermineCategory(filePath);
                var categoryKey = $"{category.Type}_{category.Name}";

                var existingCategory = _categories.GetOrAdd(categoryKey, category);
                existingCategory.FileCount++;

                _results.Add(new ParseResult
                {
                    FilePath = filePath,
                    Category = category,
                    Success = success,
                    ParseTime = parseTime,
                    Error = error?.Message
                });

                if (!success && error != null)
                {
                    _errors.Add($"{filePath}: {error.Message}");
                }

                // Update peak memory
                var currentMem = GC.GetTotalMemory(false);
                if (currentMem > PeakMemory)
                    PeakMemory = currentMem;
            }
        }

        /// <summary>
        /// Gets statistics by category
        /// </summary>
        public Dictionary<string, CategoryStats> GetCategoryStats()
        {
            var stats = new Dictionary<string, CategoryStats>();

            foreach (var category in _categories.Values)
            {
                var categoryResults = _results.Where(r =>
                    r.Category.Type == category.Type && r.Category.Name == category.Name).ToList();

                var categoryStats = new CategoryStats
                {
                    Category = category,
                    TotalFiles = categoryResults.Count,
                    SuccessfulFiles = categoryResults.Count(r => r.Success),
                    FailedFiles = categoryResults.Count(r => !r.Success),
                    AverageParseTime = categoryResults.Any() ?
                        TimeSpan.FromTicks((long)categoryResults.Average(r => r.ParseTime.Ticks)) : TimeSpan.Zero,
                    TotalParseTime = TimeSpan.FromTicks(categoryResults.Sum(r => r.ParseTime.Ticks))
                };

                categoryStats.SuccessRate = categoryStats.TotalFiles > 0 ?
                    (double)categoryStats.SuccessfulFiles / categoryStats.TotalFiles * 100 : 0;

                stats[category.Name] = categoryStats;
            }

            return stats;
        }

        /// <summary>
        /// Gets the slowest files
        /// </summary>
        public List<ParseResult> GetSlowestFiles(int count = 10)
        {
            return _results
                .Where(r => r.Success)
                .OrderByDescending(r => r.ParseTime)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Gets the most common errors
        /// </summary>
        public Dictionary<string, int> GetTopErrors(int count = 10)
        {
            return _errors
                .GroupBy(e => ExtractErrorType(e))
                .ToDictionary(g => g.Key, g => g.Count())
                .OrderByDescending(kvp => kvp.Value)
                .Take(count)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private string ExtractErrorType(string fullError)
        {
            // Extract just the error type/message, not the file path
            var colonIndex = fullError.IndexOf(": ");
            if (colonIndex >= 0 && colonIndex < fullError.Length - 2)
            {
                var error = fullError.Substring(colonIndex + 2);
                // Truncate very long errors
                return error.Length > 100 ? error.Substring(0, 100) + "..." : error;
            }
            return fullError;
        }

        /// <summary>
        /// Generates a comprehensive report
        /// </summary>
        public string GenerateReport()
        {
            var report = new System.Text.StringBuilder();

            report.AppendLine("=== MASSIVE PARADOX DATA TEST RESULTS ===");
            report.AppendLine($"Total files processed: {TotalFiles:N0}");
            report.AppendLine();

            report.AppendLine("OVERALL RESULTS:");
            report.AppendLine($"✅ Successfully parsed: {SuccessfulFiles:N0} files ({SuccessRate:F1}%)");
            report.AppendLine($"❌ Failed: {FailedFiles:N0} files ({100 - SuccessRate:F1}%)");
            report.AppendLine();

            report.AppendLine("PERFORMANCE:");
            report.AppendLine($"- Total time: {TotalTime.TotalSeconds:F1} seconds");
            report.AppendLine($"- Throughput: {FilesPerSecond:F0} files/second");
            report.AppendLine($"- Memory used: {MemoryUsed / (1024.0 * 1024.0):F1}MB (peak: {PeakMemory / (1024.0 * 1024.0):F1}MB)");
            report.AppendLine();

            // Category breakdown
            report.AppendLine("BY CATEGORY:");
            var categoryStats = GetCategoryStats();
            foreach (var category in categoryStats.Values.OrderByDescending(c => c.TotalFiles))
            {
                var icon = category.SuccessRate >= 95 ? "✅" : category.SuccessRate >= 80 ? "⚠️" : "❌";
                report.AppendLine($"  {icon} {category.Category.Name}: {category.SuccessfulFiles}/{category.TotalFiles} ({category.SuccessRate:F1}%)");
            }
            report.AppendLine();

            // Slowest files
            var slowest = GetSlowestFiles(5);
            if (slowest.Any())
            {
                report.AppendLine("SLOWEST FILES:");
                foreach (var file in slowest)
                {
                    report.AppendLine($"  {file.ParseTime.TotalMilliseconds:F1}ms - {Path.GetFileName(file.FilePath)}");
                }
                report.AppendLine();
            }

            // Top errors
            var topErrors = GetTopErrors(5);
            if (topErrors.Any())
            {
                report.AppendLine("TOP ERRORS:");
                foreach (var error in topErrors)
                {
                    report.AppendLine($"  {error.Value}x - {error.Key}");
                }
                report.AppendLine();
            }

            return report.ToString();
        }
    }

    /// <summary>
    /// Statistics for a specific file category
    /// </summary>
    public class CategoryStats
    {
        public ParadoxFileCategory Category { get; set; }
        public int TotalFiles { get; set; }
        public int SuccessfulFiles { get; set; }
        public int FailedFiles { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan AverageParseTime { get; set; }
        public TimeSpan TotalParseTime { get; set; }
    }

    /// <summary>
    /// Result of parsing a single file
    /// </summary>
    public class ParseResult
    {
        public string FilePath { get; set; }
        public ParadoxFileCategory Category { get; set; }
        public bool Success { get; set; }
        public TimeSpan ParseTime { get; set; }
        public string Error { get; set; }
    }
}