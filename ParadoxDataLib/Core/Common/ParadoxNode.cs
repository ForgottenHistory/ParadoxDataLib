using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParadoxDataLib.Core.Common
{
    /// <summary>
    /// Represents the type of data stored in a ParadoxNode
    /// </summary>
    public enum NodeType
    {
        /// <summary>A simple scalar value (string, number, boolean)</summary>
        Scalar,

        /// <summary>A list of values or nodes</summary>
        List,

        /// <summary>An object containing key-value pairs</summary>
        Object,

        /// <summary>A date-based historical entry</summary>
        Date
    }

    /// <summary>
    /// Generic data structure representing any parsed Paradox game file content.
    /// Can represent scalar values, lists, objects, or date-based historical entries.
    /// </summary>
    public class ParadoxNode
    {
        /// <summary>
        /// The key name for this node (empty for root or list items)
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The scalar value for simple nodes (string, number, boolean, date)
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// The type of data this node represents
        /// </summary>
        public NodeType Type { get; set; }

        /// <summary>
        /// Child nodes for object-type nodes (key-value pairs)
        /// </summary>
        public Dictionary<string, ParadoxNode> Children { get; set; }

        /// <summary>
        /// Items for list-type nodes
        /// </summary>
        public List<ParadoxNode> Items { get; set; }

        /// <summary>
        /// Initializes a new ParadoxNode
        /// </summary>
        public ParadoxNode()
        {
            Key = string.Empty;
            Children = new Dictionary<string, ParadoxNode>();
            Items = new List<ParadoxNode>();
            Type = NodeType.Scalar;
        }

        /// <summary>
        /// Creates a scalar node with the specified key and value
        /// </summary>
        public static ParadoxNode CreateScalar(string key, object value)
        {
            return new ParadoxNode
            {
                Key = key,
                Value = value,
                Type = NodeType.Scalar
            };
        }

        /// <summary>
        /// Creates an object node with the specified key
        /// </summary>
        public static ParadoxNode CreateObject(string key)
        {
            return new ParadoxNode
            {
                Key = key,
                Type = NodeType.Object
            };
        }

        /// <summary>
        /// Creates a list node with the specified key
        /// </summary>
        public static ParadoxNode CreateList(string key)
        {
            return new ParadoxNode
            {
                Key = key,
                Type = NodeType.List
            };
        }

        /// <summary>
        /// Creates a date node with the specified date and key
        /// </summary>
        public static ParadoxNode CreateDate(string key, DateTime date)
        {
            return new ParadoxNode
            {
                Key = key,
                Value = date,
                Type = NodeType.Date
            };
        }

        /// <summary>
        /// Adds a child node to this object or date node (replaces existing child with same key)
        /// </summary>
        public void AddChild(ParadoxNode child)
        {
            if (Type != NodeType.Object && Type != NodeType.Date)
                throw new InvalidOperationException("Cannot add child to non-object/non-date node");

            Children[child.Key] = child;
        }

        /// <summary>
        /// Adds a child node to this object, accumulating multiple nodes with the same key into a list
        /// </summary>
        public void AddChildAccumulating(ParadoxNode child)
        {
            if (Type != NodeType.Object && Type != NodeType.Date)
                throw new InvalidOperationException("Cannot add child to non-object/non-date node");

            // Handle multiple nodes with the same key by converting to list
            if (Children.ContainsKey(child.Key))
            {
                var existing = Children[child.Key];
                if (existing.Type != NodeType.List)
                {
                    // Convert existing single value to list
                    var listNode = CreateList(child.Key);
                    listNode.Items.Add(existing);
                    listNode.Items.Add(child);
                    Children[child.Key] = listNode;
                }
                else
                {
                    // Add to existing list
                    existing.Items.Add(child);
                }
            }
            else
            {
                Children[child.Key] = child;
            }
        }

        /// <summary>
        /// Adds an item to this list node
        /// </summary>
        public void AddItem(ParadoxNode item)
        {
            if (Type != NodeType.List)
                throw new InvalidOperationException("Cannot add item to non-list node");

            Items.Add(item);
        }

        /// <summary>
        /// Gets a child node by key
        /// </summary>
        public ParadoxNode GetChild(string key)
        {
            return Children.TryGetValue(key, out var child) ? child : null;
        }

        /// <summary>
        /// Gets all children with the specified key (handles both single values and lists)
        /// </summary>
        public IEnumerable<ParadoxNode> GetChildren(string key)
        {
            if (!Children.ContainsKey(key))
                return Enumerable.Empty<ParadoxNode>();

            var child = Children[key];
            if (child.Type == NodeType.List)
                return child.Items;
            else
                return new[] { child };
        }

        /// <summary>
        /// Gets a scalar value by key with type conversion
        /// </summary>
        public T GetValue<T>(string key, T defaultValue = default(T))
        {
            var child = GetChild(key);
            if (child == null || child.Value == null)
                return defaultValue;

            try
            {
                // Special handling for boolean conversion to support Paradox format
                if (typeof(T) == typeof(bool) && child.Value is string stringValue)
                {
                    var lowerValue = stringValue.ToLower();
                    if (lowerValue == "yes" || lowerValue == "true")
                        return (T)(object)true;
                    if (lowerValue == "no" || lowerValue == "false")
                        return (T)(object)false;
                }

                // Special handling for float conversion with invariant culture
                if (typeof(T) == typeof(float) && child.Value is string floatString)
                {
                    if (float.TryParse(floatString, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var floatValue))
                        return (T)(object)floatValue;
                }

                // Special handling for nullable types
                var targetType = typeof(T);
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    var underlyingType = Nullable.GetUnderlyingType(targetType);
                    var convertedValue = Convert.ChangeType(child.Value, underlyingType);
                    return (T)convertedValue;
                }

                return (T)Convert.ChangeType(child.Value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Gets all scalar values for a key (useful for lists like "add_core = FRA")
        /// </summary>
        public IEnumerable<T> GetValues<T>(string key)
        {
            return GetChildren(key)
                .Where(n => n.Value != null)
                .Select(n =>
                {
                    try
                    {
                        // Special handling for boolean conversion to support Paradox format
                        if (typeof(T) == typeof(bool) && n.Value is string stringValue)
                        {
                            var lowerValue = stringValue.ToLower();
                            if (lowerValue == "yes" || lowerValue == "true")
                                return (T)(object)true;
                            if (lowerValue == "no" || lowerValue == "false")
                                return (T)(object)false;
                        }

                        // Special handling for float conversion with invariant culture
                        if (typeof(T) == typeof(float) && n.Value is string floatString)
                        {
                            if (float.TryParse(floatString, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var floatValue))
                                return (T)(object)floatValue;
                        }

                        // Special handling for nullable types
                        var targetType = typeof(T);
                        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            var underlyingType = Nullable.GetUnderlyingType(targetType);
                            var convertedValue = Convert.ChangeType(n.Value, underlyingType);
                            return (T)convertedValue;
                        }

                        return (T)Convert.ChangeType(n.Value, typeof(T));
                    }
                    catch
                    {
                        return default(T);
                    }
                })
                .Where(v => v != null);
        }

        /// <summary>
        /// Checks if this node has a child with the specified key
        /// </summary>
        public bool HasChild(string key)
        {
            return Children.ContainsKey(key);
        }

        /// <summary>
        /// Returns a debug-friendly string representation of this node
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            ToString(sb, 0);
            return sb.ToString();
        }

        private void ToString(StringBuilder sb, int indent)
        {
            var indentStr = new string(' ', indent * 2);

            switch (Type)
            {
                case NodeType.Scalar:
                    sb.AppendLine($"{indentStr}{Key} = {Value}");
                    break;

                case NodeType.Date:
                    sb.AppendLine($"{indentStr}{Key} = {{ # Date: {Value} }}");
                    break;

                case NodeType.Object:
                    sb.AppendLine($"{indentStr}{Key} = {{");
                    foreach (var child in Children.Values)
                    {
                        child.ToString(sb, indent + 1);
                    }
                    sb.AppendLine($"{indentStr}}}");
                    break;

                case NodeType.List:
                    sb.AppendLine($"{indentStr}{Key} = [");
                    foreach (var item in Items)
                    {
                        item.ToString(sb, indent + 1);
                    }
                    sb.AppendLine($"{indentStr}]");
                    break;
            }
        }
    }
}