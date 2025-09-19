using System;
using System.Collections.Generic;
using System.Linq;

namespace ParadoxDataLib.Utils
{
    public enum EntityType
    {
        Province,
        Country,
        Culture,
        Religion,
        TradeGood,
        Government,
        Localization,
        Mod
    }

    public enum UpdateReason
    {
        Direct,
        Cascading,
        Validation,
        Dependency
    }

    public enum RequiredAction
    {
        Reload,
        Revalidate,
        Recalculate,
        Remove,
        Create
    }

    public class EntityUpdate
    {
        public EntityType EntityType { get; set; }
        public string EntityId { get; set; } = string.Empty;
        public UpdateReason UpdateReason { get; set; }
        public List<RequiredAction> RequiredActions { get; set; } = new List<RequiredAction>();
        public string SourceFilePath { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> AdditionalData { get; set; } = new Dictionary<string, object>();

        public EntityUpdate()
        {
        }

        public EntityUpdate(EntityType entityType, string entityId, UpdateReason reason)
        {
            EntityType = entityType;
            EntityId = entityId ?? throw new ArgumentNullException(nameof(entityId));
            UpdateReason = reason;
        }

        public EntityUpdate(EntityType entityType, string entityId, UpdateReason reason, string sourceFilePath)
            : this(entityType, entityId, reason)
        {
            SourceFilePath = sourceFilePath;
        }

        public void AddAction(RequiredAction action)
        {
            if (!RequiredActions.Contains(action))
            {
                RequiredActions.Add(action);
            }
        }

        public void AddActions(params RequiredAction[] actions)
        {
            foreach (var action in actions)
            {
                AddAction(action);
            }
        }

        public bool HasAction(RequiredAction action)
        {
            return RequiredActions.Contains(action);
        }

        public bool RequiresReload()
        {
            return HasAction(RequiredAction.Reload) || HasAction(RequiredAction.Create);
        }

        public bool RequiresValidation()
        {
            return HasAction(RequiredAction.Revalidate) || RequiresReload();
        }

        public void SetAdditionalData(string key, object value)
        {
            AdditionalData[key] = value;
        }

        public T? GetAdditionalData<T>(string key, T? defaultValue = default)
        {
            if (AdditionalData.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        public bool IsSameEntityAs(EntityUpdate other)
        {
            if (other == null) return false;
            return EntityType == other.EntityType &&
                   string.Equals(EntityId, other.EntityId, StringComparison.OrdinalIgnoreCase);
        }

        public EntityUpdate Clone()
        {
            return new EntityUpdate
            {
                EntityType = EntityType,
                EntityId = EntityId,
                UpdateReason = UpdateReason,
                RequiredActions = new List<RequiredAction>(RequiredActions),
                SourceFilePath = SourceFilePath,
                Timestamp = Timestamp,
                AdditionalData = new Dictionary<string, object>(AdditionalData)
            };
        }

        public override string ToString()
        {
            var actions = string.Join(", ", RequiredActions);
            var reasonText = UpdateReason switch
            {
                UpdateReason.Direct => "Direct",
                UpdateReason.Cascading => "Cascading",
                UpdateReason.Validation => "Validation",
                UpdateReason.Dependency => "Dependency",
                _ => "Unknown"
            };

            return $"{EntityType} {EntityId} ({reasonText}): [{actions}]";
        }

        public override bool Equals(object? obj)
        {
            if (obj is not EntityUpdate other) return false;
            return IsSameEntityAs(other) &&
                   UpdateReason == other.UpdateReason &&
                   SourceFilePath == other.SourceFilePath;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EntityType, EntityId.ToLowerInvariant(), UpdateReason);
        }
    }

    public static class EntityUpdateExtensions
    {
        public static IEnumerable<EntityUpdate> GetUpdatesForType(this IEnumerable<EntityUpdate> updates, EntityType entityType)
        {
            return updates.Where(u => u.EntityType == entityType);
        }

        public static IEnumerable<EntityUpdate> GetUpdatesForReason(this IEnumerable<EntityUpdate> updates, UpdateReason reason)
        {
            return updates.Where(u => u.UpdateReason == reason);
        }

        public static IEnumerable<EntityUpdate> GetUpdatesWithAction(this IEnumerable<EntityUpdate> updates, RequiredAction action)
        {
            return updates.Where(u => u.HasAction(action));
        }

        public static IEnumerable<EntityUpdate> GetDirectUpdates(this IEnumerable<EntityUpdate> updates)
        {
            return updates.GetUpdatesForReason(UpdateReason.Direct);
        }

        public static IEnumerable<EntityUpdate> GetCascadingUpdates(this IEnumerable<EntityUpdate> updates)
        {
            return updates.GetUpdatesForReason(UpdateReason.Cascading);
        }

        public static bool HasUpdatesForEntity(this IEnumerable<EntityUpdate> updates, EntityType entityType, string entityId)
        {
            return updates.Any(u => u.EntityType == entityType &&
                                   string.Equals(u.EntityId, entityId, StringComparison.OrdinalIgnoreCase));
        }

        public static List<EntityUpdate> MergeUpdates(this IEnumerable<EntityUpdate> updates)
        {
            var mergedUpdates = new Dictionary<string, EntityUpdate>();

            foreach (var update in updates)
            {
                var key = $"{update.EntityType}:{update.EntityId.ToLowerInvariant()}";

                if (mergedUpdates.TryGetValue(key, out var existing))
                {
                    // Merge the updates
                    foreach (var action in update.RequiredActions)
                    {
                        existing.AddAction(action);
                    }

                    // Use the most recent timestamp
                    if (update.Timestamp > existing.Timestamp)
                    {
                        existing.Timestamp = update.Timestamp;
                        existing.SourceFilePath = update.SourceFilePath;
                    }

                    // Merge additional data
                    foreach (var kvp in update.AdditionalData)
                    {
                        existing.AdditionalData[kvp.Key] = kvp.Value;
                    }

                    // Use the highest priority reason
                    if (GetReasonPriority(update.UpdateReason) > GetReasonPriority(existing.UpdateReason))
                    {
                        existing.UpdateReason = update.UpdateReason;
                    }
                }
                else
                {
                    mergedUpdates[key] = update.Clone();
                }
            }

            return mergedUpdates.Values.ToList();
        }

        private static int GetReasonPriority(UpdateReason reason)
        {
            return reason switch
            {
                UpdateReason.Direct => 4,
                UpdateReason.Dependency => 3,
                UpdateReason.Cascading => 2,
                UpdateReason.Validation => 1,
                _ => 0
            };
        }
    }
}