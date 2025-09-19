using System;
using System.Collections.Generic;
using ParadoxDataLib.Core.Common;

namespace ParadoxDataLib.Core.DataModels
{
    public class CountryData : IGameEntity, IModifiable, IHistorical
    {
        public string Tag { get; set; }
        public string Name { get; set; }
        public string Government { get; set; }
        public List<string> GovernmentReforms { get; set; }
        public string PrimaryCulture { get; set; }
        public List<string> AcceptedCultures { get; set; }
        public string Religion { get; set; }
        public string TechnologyGroup { get; set; }
        public int Capital { get; set; }
        public int? FixedCapital { get; set; }

        public List<string> HistoricalFriends { get; set; }
        public List<string> HistoricalRivals { get; set; }
        public List<string> HistoricalEnemies { get; set; }

        public Dictionary<string, int> Ideas { get; set; }
        public List<string> Policies { get; set; }

        public Ruler Monarch { get; set; }
        public Ruler Heir { get; set; }
        public Ruler Queen { get; set; }

        public List<Modifier> Modifiers { get; set; }
        public List<HistoricalEntry> HistoricalEntries { get; set; }

        public Dictionary<string, object> Flags { get; set; }
        public List<string> EstatePrivileges { get; set; }

        public string Id => Tag;

        public CountryData()
        {
            GovernmentReforms = new List<string>();
            AcceptedCultures = new List<string>();
            HistoricalFriends = new List<string>();
            HistoricalRivals = new List<string>();
            HistoricalEnemies = new List<string>();
            Ideas = new Dictionary<string, int>();
            Policies = new List<string>();
            Modifiers = new List<Modifier>();
            HistoricalEntries = new List<HistoricalEntry>();
            Flags = new Dictionary<string, object>();
            EstatePrivileges = new List<string>();
        }

        public CountryData(string tag, string name) : this()
        {
            Tag = tag;
            Name = name;
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Tag) && !string.IsNullOrEmpty(Name);
        }

        public void Validate()
        {
            if (!IsValid())
            {
                throw new InvalidOperationException($"Country {Tag} is not valid");
            }
        }

        public void ApplyModifier(Modifier modifier)
        {
            Modifiers.Add(modifier);
        }

        public void RemoveModifier(string modifierId)
        {
            Modifiers.RemoveAll(m => m.Id == modifierId);
        }

        public void ClearModifiers()
        {
            Modifiers.Clear();
        }

        public void AddHistoricalEntry(HistoricalEntry entry)
        {
            HistoricalEntries.Add(entry);
        }

        public HistoricalEntry GetEntryAtDate(DateTime date)
        {
            HistoricalEntry lastEntry = null;
            foreach (var entry in HistoricalEntries)
            {
                if (entry.Date <= date)
                {
                    lastEntry = entry;
                }
                else
                {
                    break;
                }
            }
            return lastEntry;
        }

        public void ApplyHistoryUpToDate(DateTime date)
        {
            foreach (var entry in HistoricalEntries)
            {
                if (entry.Date > date) break;

                foreach (var change in entry.Changes)
                {
                    ApplyHistoricalChange(change.Key, change.Value);
                }
            }
        }

        private void ApplyHistoricalChange(string key, object value)
        {
            switch (key.ToLower())
            {
                case "government":
                    Government = value.ToString();
                    break;
                case "primary_culture":
                    PrimaryCulture = value.ToString();
                    break;
                case "religion":
                    Religion = value.ToString();
                    break;
                case "capital":
                    Capital = Convert.ToInt32(value);
                    break;
                case "add_accepted_culture":
                    if (!AcceptedCultures.Contains(value.ToString()))
                        AcceptedCultures.Add(value.ToString());
                    break;
                case "remove_accepted_culture":
                    AcceptedCultures.Remove(value.ToString());
                    break;
                case "monarch":
                    if (value is Ruler ruler)
                        Monarch = ruler;
                    break;
                case "heir":
                    if (value is Ruler heirRuler)
                        Heir = heirRuler;
                    break;
                case "queen":
                    if (value is Ruler queenRuler)
                        Queen = queenRuler;
                    break;
            }
        }
    }

    public struct Ruler
    {
        public string Name { get; set; }
        public string Dynasty { get; set; }
        public DateTime BirthDate { get; set; }
        public DateTime? DeathDate { get; set; }
        public int ADM { get; set; }
        public int DIP { get; set; }
        public int MIL { get; set; }
        public bool Female { get; set; }
        public bool IsRegent { get; set; }
        public string Culture { get; set; }
        public string Religion { get; set; }
        public List<string> Personalities { get; set; }
        public string CountryOfOrigin { get; set; }

        public Ruler(string name, string dynasty)
        {
            Name = name;
            Dynasty = dynasty;
            BirthDate = default;
            DeathDate = null;
            ADM = 0;
            DIP = 0;
            MIL = 0;
            Female = false;
            IsRegent = false;
            Culture = null;
            Religion = null;
            Personalities = new List<string>();
            CountryOfOrigin = null;
        }
    }
}