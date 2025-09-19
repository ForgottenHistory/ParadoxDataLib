using System;
using System.Collections.Generic;
using System.Linq;
using ParadoxDataLib.Core.Common;
using ParadoxDataLib.Core.DataModels;

namespace ParadoxDataLib.Core.Extractors
{
    /// <summary>
    /// Base class for data extractors providing common functionality for
    /// converting ParadoxNode trees into specific data types.
    /// </summary>
    /// <typeparam name="T">The type of data this extractor produces</typeparam>
    public abstract class BaseDataExtractor<T> : IDataExtractor<T>
    {
        /// <summary>
        /// Collection of error messages encountered during extraction
        /// </summary>
        protected readonly List<string> _errors;

        /// <summary>
        /// Collection of warning messages encountered during extraction
        /// </summary>
        protected readonly List<string> _warnings;

        /// <summary>
        /// Gets the list of errors encountered during extraction
        /// </summary>
        public IReadOnlyList<string> Errors => _errors;

        /// <summary>
        /// Gets the list of warnings encountered during extraction
        /// </summary>
        public IReadOnlyList<string> Warnings => _warnings;

        /// <summary>
        /// Gets whether any errors were encountered during extraction
        /// </summary>
        public bool HasErrors => _errors.Count > 0;

        /// <summary>
        /// Gets whether any warnings were encountered during extraction
        /// </summary>
        public bool HasWarnings => _warnings.Count > 0;

        /// <summary>
        /// Initializes a new instance of the BaseDataExtractor
        /// </summary>
        protected BaseDataExtractor()
        {
            _errors = new List<string>();
            _warnings = new List<string>();
        }

        /// <summary>
        /// Extracts typed data from a generic ParadoxNode tree
        /// </summary>
        /// <param name="node">The root node containing parsed Paradox data</param>
        /// <returns>The extracted data object</returns>
        public abstract T Extract(ParadoxNode node);

        /// <summary>
        /// Validates that the node structure is compatible with this extractor
        /// </summary>
        /// <param name="node">The node to validate</param>
        /// <returns>True if the node can be processed by this extractor</returns>
        public abstract bool CanExtract(ParadoxNode node);

        /// <summary>
        /// Adds an error message to the error collection
        /// </summary>
        /// <param name="message">The error message</param>
        protected void AddError(string message)
        {
            _errors.Add(message);
        }

        /// <summary>
        /// Adds a warning message to the warning collection
        /// </summary>
        /// <param name="message">The warning message</param>
        protected void AddWarning(string message)
        {
            _warnings.Add(message);
        }

        /// <summary>
        /// Extracts a list of modifiers from a ParadoxNode
        /// </summary>
        /// <param name="node">The node to extract modifiers from</param>
        /// <returns>List of extracted modifiers</returns>
        protected List<Modifier> ExtractModifiers(ParadoxNode node)
        {
            var modifiers = new List<Modifier>();

            // Extract modifier blocks
            foreach (var modifierNode in node.GetChildren("add_permanent_province_modifier"))
            {
                var modifier = ExtractModifierFromNode(modifierNode, ModifierType.Permanent);
                if (modifier.HasValue)
                    modifiers.Add(modifier.Value);
            }

            foreach (var modifierNode in node.GetChildren("add_province_modifier"))
            {
                var modifier = ExtractModifierFromNode(modifierNode, ModifierType.Temporary);
                if (modifier.HasValue)
                    modifiers.Add(modifier.Value);
            }

            foreach (var modifierNode in node.GetChildren("add_country_modifier"))
            {
                var modifier = ExtractModifierFromNode(modifierNode, ModifierType.Permanent);
                if (modifier.HasValue)
                    modifiers.Add(modifier.Value);
            }

            return modifiers;
        }

        /// <summary>
        /// Extracts a modifier from a modifier node
        /// </summary>
        /// <param name="modifierNode">The node containing modifier data</param>
        /// <param name="defaultType">The default modifier type</param>
        /// <returns>The extracted modifier</returns>
        protected Modifier? ExtractModifierFromNode(ParadoxNode modifierNode, ModifierType defaultType)
        {
            if (modifierNode.Type != NodeType.Object)
                return null;

            var name = modifierNode.GetValue<string>("name", "unnamed_modifier");
            var modifier = new Modifier(name, name, defaultType);

            // Extract description
            modifier.Description = modifierNode.GetValue<string>("desc",
                modifierNode.GetValue<string>("description", ""));

            // Extract duration for temporary modifiers
            if (defaultType == ModifierType.Temporary)
            {
                var duration = modifierNode.GetValue<float>("duration", 0);
                if (duration > 0)
                {
                    modifier.ExpirationDate = DateTime.Now.AddDays(duration);
                }
            }

            // Extract all other properties as effects
            foreach (var child in modifierNode.Children)
            {
                var key = child.Key.ToLower();
                if (key != "name" && key != "desc" && key != "description" && key != "duration")
                {
                    if (child.Value.Type == NodeType.Scalar && child.Value.Value != null)
                    {
                        try
                        {
                            var effectValue = Convert.ToSingle(child.Value.Value);
                            modifier.Effects[key] = effectValue;
                        }
                        catch
                        {
                            AddWarning($"Could not convert modifier effect '{key}' value '{child.Value.Value}' to number");
                        }
                    }
                }
            }

            return modifier;
        }

        /// <summary>
        /// Extracts historical entries from a ParadoxNode
        /// </summary>
        /// <param name="node">The node to extract historical entries from</param>
        /// <returns>List of extracted historical entries</returns>
        protected List<HistoricalEntry> ExtractHistoricalEntries(ParadoxNode node)
        {
            var entries = new List<HistoricalEntry>();

            foreach (var child in node.Children.Values)
            {
                if (child.Type == NodeType.Date && child.Value is DateTime date)
                {
                    var entry = new HistoricalEntry(date);

                    // Extract all changes from the date block
                    foreach (var change in child.Children)
                    {
                        if (change.Value.Type == NodeType.Scalar)
                        {
                            entry.AddChange(change.Key, change.Value.Value);
                        }
                        else if (change.Value.Type == NodeType.List)
                        {
                            // Handle list values like multiple add_core entries
                            foreach (var item in change.Value.Items)
                            {
                                if (item.Type == NodeType.Scalar)
                                {
                                    entry.AddChange(change.Key, item.Value);
                                }
                            }
                        }
                    }

                    entries.Add(entry);
                }
            }

            // Sort by date
            entries.Sort((a, b) => a.Date.CompareTo(b.Date));

            return entries;
        }

        /// <summary>
        /// Safely converts a value to the specified type
        /// </summary>
        /// <typeparam name="TValue">The target type</typeparam>
        /// <param name="value">The value to convert</param>
        /// <param name="defaultValue">The default value if conversion fails</param>
        /// <returns>The converted value or default value</returns>
        protected TValue SafeConvert<TValue>(object value, TValue defaultValue = default(TValue))
        {
            if (value == null)
                return defaultValue;

            try
            {
                return (TValue)Convert.ChangeType(value, typeof(TValue));
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}