using System;
using System.Collections.Generic;

namespace ParadoxDataLib.Core.DataModels
{
    public class HistoricalEntry
    {
        public DateTime Date { get; set; }
        public Dictionary<string, object> Changes { get; set; }
        public string Comment { get; set; }

        public HistoricalEntry()
        {
            Changes = new Dictionary<string, object>();
        }

        public HistoricalEntry(DateTime date) : this()
        {
            Date = date;
        }

        public void AddChange(string key, object value)
        {
            Changes[key] = value;
        }

        public T GetChange<T>(string key)
        {
            if (Changes.TryGetValue(key, out var value))
            {
                return (T)value;
            }
            return default(T);
        }

        public bool HasChange(string key)
        {
            return Changes.ContainsKey(key);
        }
    }
}