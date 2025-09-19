using System;
using System.Collections.Generic;

namespace ParadoxDataLib.Core.DataModels
{
    public struct Modifier
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ModifierType Type { get; set; }
        public Dictionary<string, float> Effects { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public bool IsTemporary => ExpirationDate.HasValue;

        public Modifier(string id, string name, ModifierType type)
        {
            Id = id;
            Name = name;
            Description = string.Empty;
            Type = type;
            Effects = new Dictionary<string, float>();
            ExpirationDate = null;
        }
    }

    public enum ModifierType
    {
        Permanent,
        Temporary,
        Event,
        Mission,
        Idea,
        Government,
        Technology,
        Religion,
        Culture
    }
}