using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ParadoxDataLib.Core.Performance
{
    /// <summary>
    /// Collects and tracks performance metrics during parsing operations
    /// </summary>
    public class ParsingMetrics
    {
        /// <summary>
        /// Total time spent on tokenization
        /// </summary>
        public TimeSpan TokenizationTime { get; set; }

        /// <summary>
        /// Total time spent on parsing tokens into data structures
        /// </summary>
        public TimeSpan ParsingTime { get; set; }

        /// <summary>
        /// Total time spent on file I/O operations
        /// </summary>
        public TimeSpan FileIOTime { get; set; }

        /// <summary>
        /// Total number of tokens processed
        /// </summary>
        public int TokensProcessed { get; set; }

        /// <summary>
        /// Number of lines processed in the source file
        /// </summary>
        public int LinesProcessed { get; set; }

        /// <summary>
        /// Size of the input content in bytes
        /// </summary>
        public long InputSizeBytes { get; set; }

        /// <summary>
        /// Memory usage before parsing started (in bytes)
        /// </summary>
        public long MemoryBefore { get; set; }

        /// <summary>
        /// Memory usage after parsing completed (in bytes)
        /// </summary>
        public long MemoryAfter { get; set; }

        /// <summary>
        /// Number of errors encountered during parsing
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// Number of warnings encountered during parsing
        /// </summary>
        public int WarningCount { get; set; }

        /// <summary>
        /// Additional timing measurements for specific operations
        /// </summary>
        public Dictionary<string, TimeSpan> CustomTimings { get; } = new Dictionary<string, TimeSpan>();

        /// <summary>
        /// Counters for specific parsing operations
        /// </summary>
        public Dictionary<string, int> Counters { get; } = new Dictionary<string, int>();

        /// <summary>
        /// Gets the total parsing time (tokenization + parsing)
        /// </summary>
        public TimeSpan TotalParsingTime => TokenizationTime + ParsingTime;

        /// <summary>
        /// Gets the total time including I/O operations
        /// </summary>
        public TimeSpan TotalTime => TokenizationTime + ParsingTime + FileIOTime;

        /// <summary>
        /// Gets the parsing throughput in tokens per second
        /// </summary>
        public double TokensPerSecond => TokensProcessed / Math.Max(TotalParsingTime.TotalSeconds, 0.001);

        /// <summary>
        /// Gets the parsing throughput in bytes per second
        /// </summary>
        public double BytesPerSecond => InputSizeBytes / Math.Max(TotalTime.TotalSeconds, 0.001);

        /// <summary>
        /// Gets the memory delta (memory used during parsing)
        /// </summary>
        public long MemoryDelta => MemoryAfter - MemoryBefore;

        /// <summary>
        /// Resets all metrics to their default values
        /// </summary>
        public void Reset()
        {
            TokenizationTime = TimeSpan.Zero;
            ParsingTime = TimeSpan.Zero;
            FileIOTime = TimeSpan.Zero;
            TokensProcessed = 0;
            LinesProcessed = 0;
            InputSizeBytes = 0;
            MemoryBefore = 0;
            MemoryAfter = 0;
            ErrorCount = 0;
            WarningCount = 0;
            CustomTimings.Clear();
            Counters.Clear();
        }

        /// <summary>
        /// Returns a formatted string with key performance metrics
        /// </summary>
        public override string ToString()
        {
            return $"Parsing Metrics: " +
                   $"Total Time: {TotalTime.TotalMilliseconds:F1}ms, " +
                   $"Tokens: {TokensProcessed}, " +
                   $"Throughput: {TokensPerSecond:F0} tokens/sec, " +
                   $"Errors: {ErrorCount}, " +
                   $"Warnings: {WarningCount}";
        }

        /// <summary>
        /// Returns detailed performance metrics as a formatted string
        /// </summary>
        public string ToDetailedString()
        {
            var result = new System.Text.StringBuilder();
            result.AppendLine("=== Parsing Performance Metrics ===");
            result.AppendLine($"Total Time: {TotalTime.TotalMilliseconds:F1}ms");
            result.AppendLine($"  - File I/O: {FileIOTime.TotalMilliseconds:F1}ms");
            result.AppendLine($"  - Tokenization: {TokenizationTime.TotalMilliseconds:F1}ms");
            result.AppendLine($"  - Parsing: {ParsingTime.TotalMilliseconds:F1}ms");
            result.AppendLine($"Input Size: {InputSizeBytes:N0} bytes");
            result.AppendLine($"Tokens Processed: {TokensProcessed:N0}");
            result.AppendLine($"Lines Processed: {LinesProcessed:N0}");
            result.AppendLine($"Throughput: {TokensPerSecond:F0} tokens/sec, {BytesPerSecond / 1024:F1} KB/sec");
            result.AppendLine($"Memory Delta: {MemoryDelta / 1024:F1} KB");
            result.AppendLine($"Errors: {ErrorCount}, Warnings: {WarningCount}");

            if (CustomTimings.Count > 0)
            {
                result.AppendLine("Custom Timings:");
                foreach (var timing in CustomTimings)
                {
                    result.AppendLine($"  - {timing.Key}: {timing.Value.TotalMilliseconds:F1}ms");
                }
            }

            if (Counters.Count > 0)
            {
                result.AppendLine("Counters:");
                foreach (var counter in Counters)
                {
                    result.AppendLine($"  - {counter.Key}: {counter.Value:N0}");
                }
            }

            return result.ToString();
        }
    }

    /// <summary>
    /// Helper class for timing operations during parsing
    /// </summary>
    public class PerformanceTimer : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly Action<TimeSpan> _onComplete;

        /// <summary>
        /// Creates a new performance timer that will call the provided action when disposed
        /// </summary>
        /// <param name="onComplete">Action to call with the elapsed time when the timer is disposed</param>
        public PerformanceTimer(Action<TimeSpan> onComplete)
        {
            _onComplete = onComplete ?? throw new ArgumentNullException(nameof(onComplete));
            _stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Gets the elapsed time since the timer was created
        /// </summary>
        public TimeSpan Elapsed => _stopwatch.Elapsed;

        /// <summary>
        /// Stops the timer and calls the completion action
        /// </summary>
        public void Dispose()
        {
            _stopwatch.Stop();
            _onComplete(_stopwatch.Elapsed);
        }
    }
}