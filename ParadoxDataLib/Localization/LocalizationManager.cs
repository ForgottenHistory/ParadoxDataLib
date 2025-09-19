using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ParadoxDataLib.Localization
{
    public class LocalizationManager
    {
        private readonly ConcurrentDictionary<string, LocalizationEntry> _entries;
        private readonly HashSet<string> _supportedLanguages;
        private readonly object _lock = new object();

        public string DefaultLanguage { get; set; } = "english";
        public string FallbackLanguage { get; set; } = "english";

        public IReadOnlyCollection<string> SupportedLanguages
        {
            get
            {
                lock (_lock)
                {
                    return _supportedLanguages.ToList();
                }
            }
        }

        public int EntryCount => _entries.Count;

        public LocalizationManager()
        {
            _entries = new ConcurrentDictionary<string, LocalizationEntry>();
            _supportedLanguages = new HashSet<string>();
        }

        public void AddEntry(LocalizationEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Key))
                return;

            _entries.AddOrUpdate(entry.Key, entry, (key, existing) =>
            {
                // Merge translations from the new entry into the existing one
                foreach (var translation in entry.Translations)
                {
                    existing.AddTranslation(translation.Key, translation.Value);
                }
                return existing;
            });

            lock (_lock)
            {
                foreach (var language in entry.GetSupportedLanguages())
                {
                    _supportedLanguages.Add(language);
                }
            }
        }

        public void AddEntry(string key, string language, string value, int version = 1)
        {
            var entry = new LocalizationEntry(key, language, value, version);
            AddEntry(entry);
        }

        public string GetText(string key, string language = null, string fallback = null)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            language = language ?? DefaultLanguage;
            fallback = fallback ?? FallbackLanguage;

            if (_entries.TryGetValue(key, out LocalizationEntry entry))
            {
                return entry.GetTranslation(language, fallback);
            }

            return key; // Return the key itself if no localization found
        }

        public bool HasKey(string key)
        {
            return !string.IsNullOrEmpty(key) && _entries.ContainsKey(key);
        }

        public bool HasTranslation(string key, string language)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(language))
                return false;

            return _entries.TryGetValue(key, out LocalizationEntry entry) && entry.HasTranslation(language);
        }

        public LocalizationEntry GetEntry(string key)
        {
            _entries.TryGetValue(key, out LocalizationEntry entry);
            return entry;
        }

        public IEnumerable<LocalizationEntry> GetAllEntries()
        {
            return _entries.Values;
        }

        public IEnumerable<LocalizationEntry> GetEntriesForLanguage(string language)
        {
            return _entries.Values.Where(e => e.HasTranslation(language));
        }

        public IEnumerable<string> GetMissingTranslations(string language)
        {
            return _entries.Values
                .Where(e => !e.HasTranslation(language))
                .Select(e => e.Key);
        }

        public Dictionary<string, int> GetLanguageStatistics()
        {
            var stats = new Dictionary<string, int>();

            foreach (var entry in _entries.Values)
            {
                foreach (var language in entry.GetSupportedLanguages())
                {
                    stats[language] = stats.GetValueOrDefault(language, 0) + 1;
                }
            }

            return stats;
        }

        public void LoadFromDirectory(string directoryPath, string pattern = "*.yml")
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Localization directory not found: {directoryPath}");

            var files = Directory.GetFiles(directoryPath, pattern, SearchOption.AllDirectories);
            LoadFromFiles(files);
        }

        public void LoadFromFiles(params string[] filePaths)
        {
            var parser = new LocalizationParser();

            Parallel.ForEach(filePaths, filePath =>
            {
                try
                {
                    var entries = parser.ParseFile(filePath);
                    foreach (var entry in entries)
                    {
                        AddEntry(entry);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to load localization file '{filePath}': {ex.Message}", ex);
                }
            });
        }

        public async Task LoadFromDirectoryAsync(string directoryPath, string pattern = "*.yml")
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Localization directory not found: {directoryPath}");

            var files = Directory.GetFiles(directoryPath, pattern, SearchOption.AllDirectories);
            await LoadFromFilesAsync(files);
        }

        public async Task LoadFromFilesAsync(params string[] filePaths)
        {
            var parser = new LocalizationParser();
            var tasks = filePaths.Select(async filePath =>
            {
                try
                {
                    var entries = await parser.ParseFileAsync(filePath);
                    foreach (var entry in entries)
                    {
                        AddEntry(entry);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to load localization file '{filePath}': {ex.Message}", ex);
                }
            });

            await Task.WhenAll(tasks);
        }

        public void Clear()
        {
            _entries.Clear();
            lock (_lock)
            {
                _supportedLanguages.Clear();
            }
        }

        public void RemoveEntry(string key)
        {
            _entries.TryRemove(key, out _);
        }

        public void SetDefaultLanguage(string language)
        {
            if (string.IsNullOrEmpty(language))
                throw new ArgumentException("Language cannot be null or empty", nameof(language));

            DefaultLanguage = language;
        }

        public void SetFallbackLanguage(string language)
        {
            if (string.IsNullOrEmpty(language))
                throw new ArgumentException("Language cannot be null or empty", nameof(language));

            FallbackLanguage = language;
        }

        public LocalizationValidationReport ValidateCompleteness()
        {
            var report = new LocalizationValidationReport();
            var allLanguages = SupportedLanguages.ToList();

            foreach (var language in allLanguages)
            {
                var missing = GetMissingTranslations(language).ToList();
                if (missing.Count > 0)
                {
                    report.MissingTranslations[language] = missing;
                }
            }

            var stats = GetLanguageStatistics();
            if (stats.Count > 0)
            {
                var maxCount = stats.Values.Max();
                report.TotalKeys = _entries.Count;

                foreach (var language in allLanguages)
                {
                    var count = stats.GetValueOrDefault(language, 0);
                    report.CompletionRates[language] = _entries.Count > 0 ? (double)count / _entries.Count * 100 : 0;
                }
            }

            return report;
        }
    }

    public class LocalizationValidationReport
    {
        public Dictionary<string, List<string>> MissingTranslations { get; set; }
        public Dictionary<string, double> CompletionRates { get; set; }
        public int TotalKeys { get; set; }

        public LocalizationValidationReport()
        {
            MissingTranslations = new Dictionary<string, List<string>>();
            CompletionRates = new Dictionary<string, double>();
        }

        public bool IsComplete => MissingTranslations.Count == 0;

        public override string ToString()
        {
            var lines = new List<string>();
            lines.Add($"Localization Validation Report");
            lines.Add($"Total Keys: {TotalKeys}");

            if (IsComplete)
            {
                lines.Add("✓ All languages have complete translations");
            }
            else
            {
                lines.Add($"✗ {MissingTranslations.Count} language(s) have missing translations");

                foreach (var kvp in MissingTranslations)
                {
                    var completion = CompletionRates.GetValueOrDefault(kvp.Key, 0);
                    lines.Add($"  {kvp.Key}: {kvp.Value.Count} missing ({completion:F1}% complete)");
                }
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}