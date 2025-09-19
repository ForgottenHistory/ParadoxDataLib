using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ParadoxDataLib.Utils
{
    public class ChangeQueue : IDisposable
    {
        private readonly Channel<FileChange> _channel;
        private readonly ChannelWriter<FileChange> _writer;
        private readonly ChannelReader<FileChange> _reader;
        private readonly ConcurrentDictionary<string, FileChange> _pendingChanges;
        private readonly Timer _debounceTimer;
        private readonly FileWatcherOptions _options;
        private readonly object _lockObject = new object();
        private bool _disposed;

        public event EventHandler<BatchChangeEventArgs>? BatchReady;

        public ChangeQueue(FileWatcherOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            var channelOptions = new BoundedChannelOptions(_options.MaxQueueSize)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };

            _channel = Channel.CreateBounded<FileChange>(channelOptions);
            _writer = _channel.Writer;
            _reader = _channel.Reader;
            _pendingChanges = new ConcurrentDictionary<string, FileChange>();

            _debounceTimer = new Timer(OnDebounceTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);

            // Start processing changes
            _ = Task.Run(ProcessChangesAsync);
        }

        public async Task<bool> EnqueueChangeAsync(FileChange change, CancellationToken cancellationToken = default)
        {
            if (_disposed || change == null || !change.IsValidChange())
                return false;

            if (!_options.ShouldWatchFile(change.FilePath))
                return false;

            try
            {
                await _writer.WriteAsync(change, cancellationToken);
                return true;
            }
            catch (InvalidOperationException)
            {
                // Channel is closed
                return false;
            }
        }

        public bool TryEnqueueChange(FileChange change)
        {
            if (_disposed || change == null || !change.IsValidChange())
                return false;

            if (!_options.ShouldWatchFile(change.FilePath))
                return false;

            return _writer.TryWrite(change);
        }

        private async Task ProcessChangesAsync()
        {
            try
            {
                await foreach (var change in _reader.ReadAllAsync())
                {
                    if (_disposed) break;

                    ProcessSingleChange(change);
                    RestartDebounceTimer();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when shutting down
            }
        }

        private void ProcessSingleChange(FileChange change)
        {
            lock (_lockObject)
            {
                var key = change.FilePath.ToLowerInvariant();

                // Check for duplicate/superseding changes
                if (_pendingChanges.TryGetValue(key, out var existingChange))
                {
                    var mergedChange = MergeChanges(existingChange, change);
                    if (mergedChange != null)
                    {
                        _pendingChanges[key] = mergedChange;
                    }
                    else
                    {
                        // Changes cancel each other out
                        _pendingChanges.TryRemove(key, out _);
                    }
                }
                else
                {
                    _pendingChanges[key] = change;
                }

                if (_options.LogFileChanges)
                {
                    Console.WriteLine($"[FileWatcher] Queued: {change}");
                }
            }
        }

        private FileChange? MergeChanges(FileChange existing, FileChange newChange)
        {
            // Same file, merge the changes intelligently
            if (existing.ChangeType == FileChangeType.Created && newChange.ChangeType == FileChangeType.Deleted)
            {
                // Create then delete = no change
                return null;
            }

            if (existing.ChangeType == FileChangeType.Created && newChange.ChangeType == FileChangeType.Modified)
            {
                // Create then modify = still a create
                return existing;
            }

            if (existing.ChangeType == FileChangeType.Modified && newChange.ChangeType == FileChangeType.Modified)
            {
                // Multiple modifications = single modification with latest timestamp
                return new FileChange(newChange.FilePath, FileChangeType.Modified)
                {
                    Timestamp = newChange.Timestamp,
                    Category = newChange.Category
                };
            }

            if (existing.ChangeType == FileChangeType.Modified && newChange.ChangeType == FileChangeType.Deleted)
            {
                // Modify then delete = delete
                return newChange;
            }

            if (existing.ChangeType == FileChangeType.Renamed && newChange.ChangeType == FileChangeType.Modified)
            {
                // Rename then modify = rename with modification
                return new FileChange(newChange.FilePath, FileChangeType.Renamed, existing.OldPath)
                {
                    Timestamp = newChange.Timestamp,
                    Category = newChange.Category
                };
            }

            // Default: keep the newer change
            return newChange;
        }

        private void RestartDebounceTimer()
        {
            _debounceTimer.Change(_options.DebounceMilliseconds, Timeout.Infinite);
        }

        private void OnDebounceTimerElapsed(object? state)
        {
            List<FileChange> batch;

            lock (_lockObject)
            {
                if (_pendingChanges.IsEmpty)
                    return;

                // Create ordered batch by priority and timestamp
                batch = _pendingChanges.Values
                    .OrderBy(GetChangePriority)
                    .ThenBy(c => c.Timestamp)
                    .ToList();

                _pendingChanges.Clear();
            }

            if (batch.Count > 0)
            {
                var eventArgs = new BatchChangeEventArgs(batch);
                BatchReady?.Invoke(this, eventArgs);

                if (_options.LogFileChanges)
                {
                    Console.WriteLine($"[FileWatcher] Batch ready: {batch.Count} changes");
                }
            }
        }

        private static int GetChangePriority(FileChange change)
        {
            // Lower number = higher priority
            return change.Category switch
            {
                FileCategory.Province => 1,
                FileCategory.Country => 2,
                FileCategory.Localization => 3,
                FileCategory.Mod => 4,
                _ => 5
            };
        }

        public void Complete()
        {
            _writer.Complete();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _debounceTimer?.Dispose();
            _writer.Complete();

            // Process any remaining pending changes
            OnDebounceTimerElapsed(null);
        }
    }

    public class BatchChangeEventArgs : EventArgs
    {
        public IReadOnlyList<FileChange> Changes { get; }
        public DateTime BatchTimestamp { get; }

        public BatchChangeEventArgs(IReadOnlyList<FileChange> changes)
        {
            Changes = changes ?? throw new ArgumentNullException(nameof(changes));
            BatchTimestamp = DateTime.UtcNow;
        }

        public IEnumerable<FileChange> GetChangesByCategory(FileCategory category)
        {
            return Changes.Where(c => c.Category == category);
        }

        public IEnumerable<FileChange> GetChangesByType(FileChangeType changeType)
        {
            return Changes.Where(c => c.ChangeType == changeType);
        }

        public bool HasChangesFor(FileCategory category)
        {
            return Changes.Any(c => c.Category == category);
        }

        public override string ToString()
        {
            var summary = Changes.GroupBy(c => c.Category)
                .Select(g => $"{g.Key}: {g.Count()}")
                .ToList();

            return $"Batch ({Changes.Count} changes): {string.Join(", ", summary)}";
        }
    }
}