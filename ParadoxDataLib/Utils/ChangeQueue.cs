using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ParadoxDataLib.Utils
{
    public class ChangeQueue : IDisposable
    {
        private readonly ConcurrentQueue<FileChange> _queue;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentDictionary<string, FileChange> _pendingChanges;
        private readonly Timer _debounceTimer;
        private readonly FileWatcherOptions _options;
        private readonly object _lockObject = new object();
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _disposed;

        public event EventHandler<BatchChangeEventArgs>? BatchReady;

        public ChangeQueue(FileWatcherOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _queue = new ConcurrentQueue<FileChange>();
            _semaphore = new SemaphoreSlim(0);
            _pendingChanges = new ConcurrentDictionary<string, FileChange>();
            _cancellationTokenSource = new CancellationTokenSource();

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
                _queue.Enqueue(change);
                _semaphore.Release();
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public bool TryEnqueueChange(FileChange change)
        {
            if (_disposed || change == null || !change.IsValidChange())
                return false;

            if (!_options.ShouldWatchFile(change.FilePath))
                return false;

            _queue.Enqueue(change);
            _semaphore.Release();
            return true;
        }

        private async Task ProcessChangesAsync()
        {
            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await _semaphore.WaitAsync(_cancellationTokenSource.Token);

                    if (_queue.TryDequeue(out var change))
                    {
                        await ProcessSingleChangeAsync(change);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when disposing
            }
        }

        private async Task ProcessSingleChangeAsync(FileChange change)
        {
            if (_disposed || change == null)
                return;

            lock (_lockObject)
            {
                _pendingChanges[change.FilePath] = change;
            }

            // Reset debounce timer
            _debounceTimer.Change(_options.DebounceInterval, Timeout.InfiniteTimeSpan);
        }

        private void OnDebounceTimerElapsed(object? state)
        {
            if (_disposed)
                return;

            List<FileChange> batchedChanges;
            lock (_lockObject)
            {
                batchedChanges = _pendingChanges.Values.ToList();
                _pendingChanges.Clear();
            }

            if (batchedChanges.Count > 0)
            {
                var eventArgs = new BatchChangeEventArgs(batchedChanges);
                BatchReady?.Invoke(this, eventArgs);
            }
        }

        public int PendingChangesCount => _pendingChanges.Count;
        public int QueuedChangesCount => _queue.Count;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _cancellationTokenSource.Cancel();
            _debounceTimer?.Dispose();
            _semaphore?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}