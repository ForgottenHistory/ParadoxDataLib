using System.Collections.Generic;
using System.Linq;

namespace ParadoxDataLib.Localization
{
    public class LocalizationEntry
    {
        public string Key { get; set; }
        public Dictionary<string, string> Translations { get; set; }
        public int Version { get; set; }
        public string SourceFile { get; set; }
        public int LineNumber { get; set; }

        public LocalizationEntry()
        {
            Translations = new Dictionary<string, string>();
            Version = 1;
        }

        public LocalizationEntry(string key, string language, string value, int version = 1) : this()
        {
            Key = key;
            Translations[language] = value;
            Version = version;
        }

        public string GetTranslation(string language, string fallbackLanguage = "english")
        {
            if (Translations.TryGetValue(language, out string value))
                return value;

            if (!string.IsNullOrEmpty(fallbackLanguage) && Translations.TryGetValue(fallbackLanguage, out string fallback))
                return fallback;

            return Translations.Count > 0 ? Translations.Values.First() : Key;
        }

        public bool HasTranslation(string language)
        {
            return Translations.ContainsKey(language);
        }

        public void AddTranslation(string language, string value)
        {
            Translations[language] = value;
        }

        public void RemoveTranslation(string language)
        {
            Translations.Remove(language);
        }

        public IEnumerable<string> GetSupportedLanguages()
        {
            return Translations.Keys;
        }

        public override string ToString()
        {
            return $"{Key} ({Translations.Count} languages)";
        }
    }
}