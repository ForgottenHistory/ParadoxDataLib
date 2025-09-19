using System;
using System.Collections.Generic;
using System.Linq;
using ParadoxDataLib.Core.DataModels;
using ParadoxDataLib.Validation;

namespace ParadoxDataLib.Utils
{
    public delegate void EntityReloadedEventHandler(object sender, EntityReloadedEventArgs e);
    public delegate void EntitiesReloadedEventHandler(object sender, EntitiesReloadedEventArgs e);
    public delegate void ReloadErrorEventHandler(object sender, ReloadErrorEventArgs e);
    public delegate void FileProcessedEventHandler(object sender, FileProcessedEventArgs e);

    public interface IHotReloadEventPublisher
    {
        event EntityReloadedEventHandler? EntityReloaded;
        event EntitiesReloadedEventHandler? EntitiesReloaded;
        event ReloadErrorEventHandler? ReloadError;
        event FileProcessedEventHandler? FileProcessed;
    }

    public class HotReloadEventBus : IHotReloadEventPublisher
    {
        public event EntityReloadedEventHandler? EntityReloaded;
        public event EntitiesReloadedEventHandler? EntitiesReloaded;
        public event ReloadErrorEventHandler? ReloadError;
        public event FileProcessedEventHandler? FileProcessed;

        public void PublishEntityReloaded(EntityType entityType, string entityId, object? entityData = null, string? sourceFile = null)
        {
            var args = new EntityReloadedEventArgs(entityType, entityId, entityData, sourceFile);
            EntityReloaded?.Invoke(this, args);
        }

        public void PublishEntitiesReloaded(IEnumerable<EntityUpdate> updates, IEnumerable<FileChange> sourceChanges)
        {
            var args = new EntitiesReloadedEventArgs(updates, sourceChanges);
            EntitiesReloaded?.Invoke(this, args);
        }

        public void PublishReloadError(string message, Exception? exception = null, string? sourceFile = null, EntityType? entityType = null, string? entityId = null)
        {
            var args = new ReloadErrorEventArgs(message, exception, sourceFile, entityType, entityId);
            ReloadError?.Invoke(this, args);
        }

        public void PublishFileProcessed(string filePath, FileChangeType changeType, bool success, string? errorMessage = null, TimeSpan? processingTime = null)
        {
            var args = new FileProcessedEventArgs(filePath, changeType, success, errorMessage, processingTime);
            FileProcessed?.Invoke(this, args);
        }
    }

    public class EntityReloadedEventArgs : EventArgs
    {
        public EntityType EntityType { get; }
        public string EntityId { get; }
        public object? EntityData { get; }
        public string? SourceFile { get; }
        public DateTime Timestamp { get; }

        public EntityReloadedEventArgs(EntityType entityType, string entityId, object? entityData = null, string? sourceFile = null)
        {
            EntityType = entityType;
            EntityId = entityId ?? throw new ArgumentNullException(nameof(entityId));
            EntityData = entityData;
            SourceFile = sourceFile;
            Timestamp = DateTime.UtcNow;
        }

        public T? GetEntityData<T>() where T : class
        {
            return EntityData as T;
        }

        public ProvinceData? GetProvinceData() => EntityData is ProvinceData province ? province : null;
        public CountryData? GetCountryData() => GetEntityData<CountryData>();

        public override string ToString()
        {
            var source = !string.IsNullOrEmpty(SourceFile) ? $" from {SourceFile}" : "";
            return $"{EntityType} '{EntityId}' reloaded{source}";
        }
    }

    public class EntitiesReloadedEventArgs : EventArgs
    {
        public IReadOnlyList<EntityUpdate> Updates { get; }
        public IReadOnlyList<FileChange> SourceChanges { get; }
        public DateTime Timestamp { get; }

        public int UpdateCount => Updates.Count;
        public int SourceChangeCount => SourceChanges.Count;

        public EntitiesReloadedEventArgs(IEnumerable<EntityUpdate> updates, IEnumerable<FileChange> sourceChanges)
        {
            Updates = updates?.ToList() ?? throw new ArgumentNullException(nameof(updates));
            SourceChanges = sourceChanges?.ToList() ?? throw new ArgumentNullException(nameof(sourceChanges));
            Timestamp = DateTime.UtcNow;
        }

        public IEnumerable<EntityUpdate> GetUpdatesForType(EntityType entityType)
        {
            return Updates.Where(u => u.EntityType == entityType);
        }

        public IEnumerable<EntityUpdate> GetUpdatesForReason(UpdateReason reason)
        {
            return Updates.Where(u => u.UpdateReason == reason);
        }

        public IEnumerable<EntityUpdate> GetDirectUpdates()
        {
            return GetUpdatesForReason(UpdateReason.Direct);
        }

        public IEnumerable<EntityUpdate> GetCascadingUpdates()
        {
            return GetUpdatesForReason(UpdateReason.Cascading);
        }

        public IEnumerable<FileChange> GetChangesForCategory(FileCategory category)
        {
            return SourceChanges.Where(c => c.Category == category);
        }

        public bool HasUpdatesForEntity(EntityType entityType, string entityId)
        {
            return Updates.Any(u => u.EntityType == entityType &&
                                   string.Equals(u.EntityId, entityId, StringComparison.OrdinalIgnoreCase));
        }

        public EntityUpdateSummary GetSummary()
        {
            return new EntityUpdateSummary(Updates, SourceChanges);
        }

        public override string ToString()
        {
            return $"Batch reload: {UpdateCount} entity updates from {SourceChangeCount} file changes";
        }
    }

    public class ReloadErrorEventArgs : EventArgs
    {
        public string Message { get; }
        public Exception? Exception { get; }
        public string? SourceFile { get; }
        public EntityType? EntityType { get; }
        public string? EntityId { get; }
        public DateTime Timestamp { get; }

        public ReloadErrorEventArgs(string message, Exception? exception = null, string? sourceFile = null, EntityType? entityType = null, string? entityId = null)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Exception = exception;
            SourceFile = sourceFile;
            EntityType = entityType;
            EntityId = entityId;
            Timestamp = DateTime.UtcNow;
        }

        public string GetFullErrorMessage()
        {
            var parts = new List<string> { Message };

            if (EntityType.HasValue && !string.IsNullOrEmpty(EntityId))
                parts.Add($"Entity: {EntityType} '{EntityId}'");

            if (!string.IsNullOrEmpty(SourceFile))
                parts.Add($"File: {SourceFile}");

            if (Exception != null)
                parts.Add($"Exception: {Exception.Message}");

            return string.Join(" | ", parts);
        }

        public override string ToString()
        {
            return GetFullErrorMessage();
        }
    }

    public class FileProcessedEventArgs : EventArgs
    {
        public string FilePath { get; }
        public FileChangeType ChangeType { get; }
        public bool Success { get; }
        public string? ErrorMessage { get; }
        public TimeSpan? ProcessingTime { get; }
        public DateTime Timestamp { get; }

        public FileProcessedEventArgs(string filePath, FileChangeType changeType, bool success, string? errorMessage = null, TimeSpan? processingTime = null)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            ChangeType = changeType;
            Success = success;
            ErrorMessage = errorMessage;
            ProcessingTime = processingTime;
            Timestamp = DateTime.UtcNow;
        }

        public override string ToString()
        {
            var status = Success ? "Success" : "Failed";
            var time = ProcessingTime.HasValue ? $" ({ProcessingTime.Value.TotalMilliseconds:F1}ms)" : "";
            var error = !Success && !string.IsNullOrEmpty(ErrorMessage) ? $": {ErrorMessage}" : "";

            return $"{ChangeType} {FilePath} - {status}{time}{error}";
        }
    }

    public class EntityUpdateSummary
    {
        public int TotalUpdates { get; }
        public int DirectUpdates { get; }
        public int CascadingUpdates { get; }
        public int ValidationUpdates { get; }
        public int DependencyUpdates { get; }

        public Dictionary<EntityType, int> UpdatesByType { get; }
        public Dictionary<FileCategory, int> ChangesByCategory { get; }
        public Dictionary<FileChangeType, int> ChangesByType { get; }

        public EntityUpdateSummary(IEnumerable<EntityUpdate> updates, IEnumerable<FileChange> changes)
        {
            var updateList = updates.ToList();
            var changeList = changes.ToList();

            TotalUpdates = updateList.Count;
            DirectUpdates = updateList.Count(u => u.UpdateReason == UpdateReason.Direct);
            CascadingUpdates = updateList.Count(u => u.UpdateReason == UpdateReason.Cascading);
            ValidationUpdates = updateList.Count(u => u.UpdateReason == UpdateReason.Validation);
            DependencyUpdates = updateList.Count(u => u.UpdateReason == UpdateReason.Dependency);

            UpdatesByType = updateList
                .GroupBy(u => u.EntityType)
                .ToDictionary(g => g.Key, g => g.Count());

            ChangesByCategory = changeList
                .GroupBy(c => c.Category)
                .ToDictionary(g => g.Key, g => g.Count());

            ChangesByType = changeList
                .GroupBy(c => c.ChangeType)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public override string ToString()
        {
            var typeSummary = string.Join(", ", UpdatesByType.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
            return $"Summary: {TotalUpdates} updates ({DirectUpdates} direct, {CascadingUpdates} cascading) - {typeSummary}";
        }
    }

    public static class HotReloadEventExtensions
    {
        public static void SubscribeToAll(this IHotReloadEventPublisher publisher, IHotReloadEventSubscriber subscriber)
        {
            publisher.EntityReloaded += subscriber.OnEntityReloaded;
            publisher.EntitiesReloaded += subscriber.OnEntitiesReloaded;
            publisher.ReloadError += subscriber.OnReloadError;
            publisher.FileProcessed += subscriber.OnFileProcessed;
        }

        public static void UnsubscribeFromAll(this IHotReloadEventPublisher publisher, IHotReloadEventSubscriber subscriber)
        {
            publisher.EntityReloaded -= subscriber.OnEntityReloaded;
            publisher.EntitiesReloaded -= subscriber.OnEntitiesReloaded;
            publisher.ReloadError -= subscriber.OnReloadError;
            publisher.FileProcessed -= subscriber.OnFileProcessed;
        }
    }

    public interface IHotReloadEventSubscriber
    {
        void OnEntityReloaded(object sender, EntityReloadedEventArgs e);
        void OnEntitiesReloaded(object sender, EntitiesReloadedEventArgs e);
        void OnReloadError(object sender, ReloadErrorEventArgs e);
        void OnFileProcessed(object sender, FileProcessedEventArgs e);
    }

    public abstract class HotReloadEventSubscriberBase : IHotReloadEventSubscriber
    {
        public virtual void OnEntityReloaded(object sender, EntityReloadedEventArgs e) { }
        public virtual void OnEntitiesReloaded(object sender, EntitiesReloadedEventArgs e) { }
        public virtual void OnReloadError(object sender, ReloadErrorEventArgs e) { }
        public virtual void OnFileProcessed(object sender, FileProcessedEventArgs e) { }
    }

    public class ConsoleHotReloadLogger : HotReloadEventSubscriberBase
    {
        private readonly bool _logSuccess;
        private readonly bool _logErrors;
        private readonly bool _logFileProcessing;

        public ConsoleHotReloadLogger(bool logSuccess = true, bool logErrors = true, bool logFileProcessing = false)
        {
            _logSuccess = logSuccess;
            _logErrors = logErrors;
            _logFileProcessing = logFileProcessing;
        }

        public override void OnEntityReloaded(object sender, EntityReloadedEventArgs e)
        {
            if (_logSuccess)
                Console.WriteLine($"[HotReload] {e}");
        }

        public override void OnEntitiesReloaded(object sender, EntitiesReloadedEventArgs e)
        {
            if (_logSuccess)
                Console.WriteLine($"[HotReload] {e}");
        }

        public override void OnReloadError(object sender, ReloadErrorEventArgs e)
        {
            if (_logErrors)
                Console.WriteLine($"[HotReload ERROR] {e.GetFullErrorMessage()}");
        }

        public override void OnFileProcessed(object sender, FileProcessedEventArgs e)
        {
            if (_logFileProcessing)
                Console.WriteLine($"[HotReload File] {e}");
        }
    }
}