using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ParadoxDataLib.Utils
{
    public class FileWatcher : IDisposable
    {
        private readonly FileWatcherOptions _options;
        private readonly ChangeQueue _changeQueue;
        private readonly List<FileSystemWatcher> _watchers;
        private readonly object _lockObject = new object();
        private bool _disposed;
        private bool _isStarted;

        public event EventHandler<BatchChangeEventArgs>? BatchReady
        {
            add => _changeQueue.BatchReady += value;
            remove => _changeQueue.BatchReady -= value;
        }

        public event EventHandler<FileWatcherErrorEventArgs>? Error;

        public bool IsStarted => _isStarted && !_disposed;

        public FileWatcher(FileWatcherOptions? options = null)
        {
            _options = options ?? FileWatcherOptions.Default;
            _options.Validate();

            _changeQueue = new ChangeQueue(_options);
            _watchers = new List<FileSystemWatcher>();
        }

        public async Task StartAsync(string rootPath, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FileWatcher));

            if (_isStarted)
                throw new InvalidOperationException("FileWatcher is already started");

            if (!Directory.Exists(rootPath))
                throw new DirectoryNotFoundException($"Directory not found: {rootPath}");

            if (!_options.EnableHotReload)
            {
                if (_options.LogFileChanges)
                    Console.WriteLine("[FileWatcher] Hot reload is disabled");
                return;
            }

            lock (_lockObject)
            {
                if (_isStarted) return;

                try
                {
                    CreateWatchers(rootPath);
                    StartWatchers();
                    _isStarted = true;

                    if (_options.LogFileChanges)
                        Console.WriteLine($"[FileWatcher] Started watching: {rootPath}");
                }
                catch (Exception ex)
                {
                    StopWatchers();
                    OnError(new FileWatcherErrorEventArgs("Failed to start file watcher", ex));
                    throw;
                }
            }

            await Task.CompletedTask;
        }

        public void Start(string rootPath)
        {
            StartAsync(rootPath).GetAwaiter().GetResult();
        }

        public void Stop()
        {
            if (!_isStarted || _disposed)
                return;

            lock (_lockObject)
            {
                if (!_isStarted) return;

                StopWatchers();
                _changeQueue.Complete();
                _isStarted = false;

                if (_options.LogFileChanges)
                    Console.WriteLine("[FileWatcher] Stopped watching");
            }
        }

        private void CreateWatchers(string rootPath)
        {
            foreach (var filter in _options.FileFilters)
            {
                var watcher = new FileSystemWatcher(rootPath, filter)
                {
                    IncludeSubdirectories = _options.WatchSubdirectories,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime
                };

                watcher.Created += OnFileEvent;
                watcher.Changed += OnFileEvent;
                watcher.Deleted += OnFileEvent;
                watcher.Renamed += OnFileRenamed;
                watcher.Error += OnWatcherError;

                _watchers.Add(watcher);
            }
        }

        private void StartWatchers()
        {
            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = true;
            }
        }

        private void StopWatchers()
        {
            foreach (var watcher in _watchers)
            {
                try
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }
                catch (Exception ex)
                {
                    if (_options.LogFileChanges)
                        Console.WriteLine($"[FileWatcher] Error stopping watcher: {ex.Message}");
                }
            }
            _watchers.Clear();
        }

        private void OnFileEvent(object sender, FileSystemEventArgs e)
        {
            if (_disposed || !_isStarted)
                return;

            try
            {
                var changeType = e.ChangeType switch
                {
                    WatcherChangeTypes.Created => FileChangeType.Created,
                    WatcherChangeTypes.Changed => FileChangeType.Modified,
                    WatcherChangeTypes.Deleted => FileChangeType.Deleted,
                    _ => (FileChangeType?)null
                };

                if (changeType.HasValue)
                {
                    var change = new FileChange(e.FullPath, changeType.Value);
                    _ = _changeQueue.TryEnqueueChange(change);
                }
            }
            catch (Exception ex)
            {
                OnError(new FileWatcherErrorEventArgs($"Error processing file event for {e.FullPath}", ex));
            }
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            if (_disposed || !_isStarted)
                return;

            try
            {
                var change = new FileChange(e.FullPath, FileChangeType.Renamed, e.OldFullPath);
                _ = _changeQueue.TryEnqueueChange(change);
            }
            catch (Exception ex)
            {
                OnError(new FileWatcherErrorEventArgs($"Error processing rename event for {e.FullPath}", ex));
            }
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            var message = "FileSystemWatcher error occurred";
            OnError(new FileWatcherErrorEventArgs(message, e.GetException()));

            // Try to restart the watcher
            if (_isStarted && !_disposed)
            {
                try
                {
                    var watcher = (FileSystemWatcher)sender;
                    watcher.EnableRaisingEvents = false;
                    Thread.Sleep(1000); // Brief pause before restart
                    watcher.EnableRaisingEvents = true;

                    if (_options.LogFileChanges)
                        Console.WriteLine("[FileWatcher] Restarted watcher after error");
                }
                catch (Exception restartEx)
                {
                    OnError(new FileWatcherErrorEventArgs("Failed to restart watcher after error", restartEx));
                }
            }
        }

        private void OnError(FileWatcherErrorEventArgs args)
        {
            if (_options.NotifyOnErrors)
            {
                Error?.Invoke(this, args);
            }

            if (_options.LogFileChanges)
            {
                Console.WriteLine($"[FileWatcher] Error: {args.Message}");
                if (args.Exception != null)
                    Console.WriteLine($"[FileWatcher] Exception: {args.Exception}");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            Stop();
            _changeQueue?.Dispose();
            _disposed = true;
        }
    }

    public class FileWatcherErrorEventArgs : EventArgs
    {
        public string Message { get; }
        public Exception? Exception { get; }
        public DateTime Timestamp { get; }

        public FileWatcherErrorEventArgs(string message, Exception? exception = null)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Exception = exception;
            Timestamp = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return Exception != null
                ? $"{Message}: {Exception.Message}"
                : Message;
        }
    }
}