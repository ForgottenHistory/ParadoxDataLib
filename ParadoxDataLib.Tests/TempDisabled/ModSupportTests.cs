using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ParadoxDataLib.ModSupport;

namespace ParadoxDataLib.Tests
{
    public class ModSupportTests : IDisposable
    {
        private readonly string _testDirectory;
        private readonly string _gameDataPath;
        private readonly string _modsPath;

        public ModSupportTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _gameDataPath = Path.Combine(_testDirectory, "game");
            _modsPath = Path.Combine(_testDirectory, "mods");

            Directory.CreateDirectory(_testDirectory);
            Directory.CreateDirectory(_gameDataPath);
            Directory.CreateDirectory(_modsPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, true);
            }
        }

        [Fact]
        public void ModDescriptor_ParseFromContent_ShouldParseBasicMod()
        {
            var content = @"name=""Test Mod""
path=""mod/test_mod""
version=""1.0.0""
supported_version=""1.35.*""
tags={ ""Alternative History"" ""Gameplay"" }";

            var mod = ModDescriptor.ParseFromContent(content);

            Assert.Equal("Test Mod", mod.Name);
            Assert.Equal("mod/test_mod", mod.Path);
            Assert.Equal("1.0.0", mod.Version);
            Assert.Equal("1.35.*", mod.SupportedVersion);
            Assert.Equal(2, mod.Tags.Count);
            Assert.Contains("Alternative History", mod.Tags);
            Assert.Contains("Gameplay", mod.Tags);
        }

        [Fact]
        public void ModDescriptor_ParseFromContent_ShouldParseDependencies()
        {
            var content = @"name=""Dependent Mod""
dependencies={ ""Base Mod"" ""Another Mod"" }";

            var mod = ModDescriptor.ParseFromContent(content);

            Assert.Equal("Dependent Mod", mod.Name);
            Assert.Equal(2, mod.Dependencies.Count);
            Assert.Contains("Base Mod", mod.Dependencies);
            Assert.Contains("Another Mod", mod.Dependencies);
        }

        [Fact]
        public void ModDescriptor_IsCompatibleWith_ShouldCheckVersionCorrectly()
        {
            var mod1 = new ModDescriptor { SupportedVersion = "1.35.0" };
            var mod2 = new ModDescriptor { SupportedVersion = "1.35.*" };
            var mod3 = new ModDescriptor { SupportedVersion = "" };

            Assert.True(mod1.IsCompatibleWith("1.35.0"));
            Assert.False(mod1.IsCompatibleWith("1.36.0"));

            Assert.True(mod2.IsCompatibleWith("1.35.0"));
            Assert.True(mod2.IsCompatibleWith("1.35.5"));

            Assert.True(mod3.IsCompatibleWith("1.35.0")); // Empty version = compatible
        }

        [Fact]
        public void ModManager_AddMod_ShouldStoreModCorrectly()
        {
            var manager = new ModManager(_gameDataPath, _modsPath);
            var mod = new ModDescriptor { Name = "Test Mod", Version = "1.0.0" };

            manager.AddMod(mod);

            Assert.Equal(1, manager.ModCount);
            Assert.True(manager.IsModLoaded("Test Mod"));
            Assert.Equal(mod, manager.GetMod("Test Mod"));
        }

        [Fact]
        public void ModManager_AddMod_ShouldReplaceExistingMod()
        {
            var manager = new ModManager(_gameDataPath, _modsPath);
            var mod1 = new ModDescriptor { Name = "Test Mod", Version = "1.0.0" };
            var mod2 = new ModDescriptor { Name = "Test Mod", Version = "1.1.0" };

            manager.AddMod(mod1);
            manager.AddMod(mod2);

            Assert.Equal(1, manager.ModCount);
            Assert.Equal("1.1.0", manager.GetMod("Test Mod").Version);
        }

        [Fact]
        public void ModManager_EnableDisableMod_ShouldWorkCorrectly()
        {
            var manager = new ModManager(_gameDataPath, _modsPath);
            var mod = new ModDescriptor { Name = "Test Mod", IsEnabled = false };

            manager.AddMod(mod);
            Assert.False(manager.GetMod("Test Mod").IsEnabled);

            manager.EnableMod("Test Mod");
            Assert.True(manager.GetMod("Test Mod").IsEnabled);

            manager.DisableMod("Test Mod");
            Assert.False(manager.GetMod("Test Mod").IsEnabled);
        }

        [Fact]
        public void ModManager_CheckCompatibility_ShouldDetectMissingDependencies()
        {
            var manager = new ModManager(_gameDataPath, _modsPath);
            var mod = new ModDescriptor
            {
                Name = "Dependent Mod",
                Dependencies = new List<string> { "Missing Mod" },
                IsEnabled = true
            };

            manager.AddMod(mod);
            var issues = manager.CheckCompatibility("1.35.0");

            Assert.Single(issues);
            Assert.Equal(CompatibilityIssueType.MissingDependency, issues[0].IssueType);
            Assert.Equal(CompatibilitySeverity.Error, issues[0].Severity);
        }

        [Fact]
        public void ModManager_CheckCompatibility_ShouldDetectVersionMismatch()
        {
            var manager = new ModManager(_gameDataPath, _modsPath);
            var mod = new ModDescriptor
            {
                Name = "Version Mod",
                SupportedVersion = "1.34.0",
                IsEnabled = true
            };

            manager.AddMod(mod);
            var issues = manager.CheckCompatibility("1.35.0");

            Assert.Single(issues);
            Assert.Equal(CompatibilityIssueType.VersionMismatch, issues[0].IssueType);
            Assert.Equal(CompatibilitySeverity.Warning, issues[0].Severity);
        }

        [Fact]
        public void ModManager_CheckCompatibility_ShouldDetectDisabledDependencies()
        {
            var manager = new ModManager(_gameDataPath, _modsPath);
            var baseMod = new ModDescriptor { Name = "Base Mod", IsEnabled = false };
            var dependentMod = new ModDescriptor
            {
                Name = "Dependent Mod",
                Dependencies = new List<string> { "Base Mod" },
                IsEnabled = true
            };

            manager.AddMod(baseMod);
            manager.AddMod(dependentMod);

            var issues = manager.CheckCompatibility("1.35.0");

            Assert.Single(issues);
            Assert.Equal(CompatibilityIssueType.DisabledDependency, issues[0].IssueType);
            Assert.Equal(CompatibilitySeverity.Warning, issues[0].Severity);
        }

        [Fact]
        public void ModManager_GetModFilesForPath_ShouldReturnInLoadOrder()
        {
            // Create test files
            var baseFile = Path.Combine(_gameDataPath, "test.txt");
            var mod1Dir = Path.Combine(_modsPath, "mod1");
            var mod2Dir = Path.Combine(_modsPath, "mod2");
            var mod1File = Path.Combine(mod1Dir, "test.txt");
            var mod2File = Path.Combine(mod2Dir, "test.txt");

            Directory.CreateDirectory(mod1Dir);
            Directory.CreateDirectory(mod2Dir);
            File.WriteAllText(baseFile, "base");
            File.WriteAllText(mod1File, "mod1");
            File.WriteAllText(mod2File, "mod2");

            var manager = new ModManager(_gameDataPath, _modsPath);
            var mod1 = new ModDescriptor { Name = "Mod 1", Path = "mod1", IsEnabled = true };
            var mod2 = new ModDescriptor { Name = "Mod 2", Path = "mod2", IsEnabled = true };

            manager.AddMod(mod1);
            manager.AddMod(mod2);

            var files = manager.GetModFilesForPath("test.txt");

            Assert.Equal(3, files.Count);
            Assert.Equal(baseFile, files[0]); // Base game first
            Assert.True(files.Contains(mod1File));
            Assert.True(files.Contains(mod2File));
        }

        [Fact]
        public void ModManager_GetEffectiveFileForPath_ShouldReturnLastInOrder()
        {
            // Create test files
            var baseFile = Path.Combine(_gameDataPath, "test.txt");
            var mod1Dir = Path.Combine(_modsPath, "mod1");
            var mod1File = Path.Combine(mod1Dir, "test.txt");

            Directory.CreateDirectory(mod1Dir);
            File.WriteAllText(baseFile, "base");
            File.WriteAllText(mod1File, "mod1");

            var manager = new ModManager(_gameDataPath, _modsPath);
            var mod1 = new ModDescriptor { Name = "Mod 1", Path = "mod1", IsEnabled = true };

            manager.AddMod(mod1);

            var effectiveFile = manager.GetEffectiveFileForPath("test.txt");

            Assert.Equal(mod1File, effectiveFile); // Mod file should override base
        }

        [Fact]
        public void ModManager_GetSummary_ShouldReturnCorrectCounts()
        {
            var manager = new ModManager(_gameDataPath, _modsPath);
            var mod1 = new ModDescriptor { Name = "Mod 1", IsEnabled = true };
            var mod2 = new ModDescriptor
            {
                Name = "Mod 2",
                IsEnabled = false,
                Dependencies = new List<string> { "Mod 1" }
            };

            manager.AddMod(mod1);
            manager.AddMod(mod2);

            var summary = manager.GetSummary();

            Assert.Equal(2, summary.TotalMods);
            Assert.Equal(1, summary.EnabledMods);
            Assert.Equal(1, summary.DisabledMods);
            Assert.Equal(1, summary.ModsWithDependencies);
            Assert.Single(summary.LoadOrder);
            Assert.Contains("Mod 1", summary.LoadOrder);
        }

        [Fact]
        public async Task ModManager_LoadModsFromDirectoryAsync_ShouldLoadCorrectly()
        {
            // Create test mod file
            var modFile = Path.Combine(_modsPath, "test_mod.mod");
            var modContent = @"name=""Test Mod""
version=""1.0.0""";

            await File.WriteAllTextAsync(modFile, modContent);

            var manager = new ModManager(_gameDataPath, _modsPath);
            await manager.LoadModsFromDirectoryAsync();

            Assert.Equal(1, manager.ModCount);
            Assert.True(manager.IsModLoaded("Test Mod"));
        }

        [Fact]
        public void ModManager_ResolveDependencyOrder_ShouldOrderCorrectly()
        {
            var manager = new ModManager(_gameDataPath, _modsPath);

            var baseMod = new ModDescriptor { Name = "Base", IsEnabled = true };
            var mod1 = new ModDescriptor
            {
                Name = "Mod1",
                Dependencies = new List<string> { "Base" },
                IsEnabled = true
            };
            var mod2 = new ModDescriptor
            {
                Name = "Mod2",
                Dependencies = new List<string> { "Mod1" },
                IsEnabled = true
            };

            // Add in reverse order to test sorting
            manager.AddMod(mod2);
            manager.AddMod(mod1);
            manager.AddMod(baseMod);

            var summary = manager.GetSummary();

            Assert.Equal(3, summary.LoadOrder.Count);
            Assert.Equal("Base", summary.LoadOrder[0]);
            Assert.Equal("Mod1", summary.LoadOrder[1]);
            Assert.Equal("Mod2", summary.LoadOrder[2]);
        }

        [Fact]
        public void ModDataMerger_ValidateModData_ShouldDetectMissingDirectory()
        {
            var manager = new ModManager(_gameDataPath, _modsPath);
            var mod = new ModDescriptor { Name = "Missing Mod", Path = "nonexistent", IsEnabled = true };

            manager.AddMod(mod);

            var merger = new ModDataMerger(manager);
            var result = merger.ValidateModData();

            Assert.False(result.IsValid);
            Assert.Single(result.Issues);
            Assert.Equal(ModDataIssueType.MissingDirectory, result.Issues[0].IssueType);
            Assert.Equal(ModDataSeverity.Error, result.Issues[0].Severity);
        }

        [Fact]
        public void ModDataMerger_GetDataOverviewByType_ShouldCategorizeFiles()
        {
            var mod1Dir = Path.Combine(_modsPath, "mod1");
            Directory.CreateDirectory(Path.Combine(mod1Dir, "history"));
            Directory.CreateDirectory(Path.Combine(mod1Dir, "common"));
            Directory.CreateDirectory(Path.Combine(mod1Dir, "localisation"));

            File.WriteAllText(Path.Combine(mod1Dir, "history", "provinces.txt"), "");
            File.WriteAllText(Path.Combine(mod1Dir, "common", "countries.txt"), "");
            File.WriteAllText(Path.Combine(mod1Dir, "localisation", "text.yml"), "");

            var manager = new ModManager(_gameDataPath, _modsPath);
            var mod = new ModDescriptor { Name = "Test Mod", Path = "mod1", IsEnabled = true };
            manager.AddMod(mod);

            var merger = new ModDataMerger(manager);
            var overview = merger.GetDataOverviewByType();

            Assert.True(overview.ContainsKey("History"));
            Assert.True(overview.ContainsKey("Common"));
            Assert.True(overview.ContainsKey("Localization"));
            Assert.Contains("history/provinces.txt", overview["History"]);
            Assert.Contains("common/countries.txt", overview["Common"]);
            Assert.Contains("localisation/text.yml", overview["Localization"]);
        }

        [Fact]
        public void ModDescriptor_GetModDirectory_ShouldReturnCorrectPath()
        {
            var mod1 = new ModDescriptor { Path = "test_mod" };
            var mod2 = new ModDescriptor { Archive = "test_mod.zip" };

            var dir1 = mod1.GetModDirectory();
            var dir2 = mod2.GetModDirectory();

            Assert.Contains("test_mod", dir1);
            Assert.Contains("test_mod", dir2);
        }

        [Fact]
        public void ModDescriptor_HasDependencies_ShouldReturnCorrectValue()
        {
            var mod1 = new ModDescriptor { Dependencies = new List<string>() };
            var mod2 = new ModDescriptor { Dependencies = new List<string> { "Dep1" } };

            Assert.False(mod1.HasDependencies());
            Assert.True(mod2.HasDependencies());
        }

        [Fact]
        public void ModDescriptor_IsArchived_ShouldDetectArchive()
        {
            var mod1 = new ModDescriptor { Path = "test" };
            var mod2 = new ModDescriptor { Archive = "test.zip" };

            Assert.False(mod1.IsArchived());
            Assert.True(mod2.IsArchived());
        }

        [Fact]
        public void ModManager_Clear_ShouldRemoveAllMods()
        {
            var manager = new ModManager(_gameDataPath, _modsPath);
            manager.AddMod(new ModDescriptor { Name = "Mod1" });
            manager.AddMod(new ModDescriptor { Name = "Mod2" });

            Assert.Equal(2, manager.ModCount);

            manager.Clear();

            Assert.Equal(0, manager.ModCount);
            Assert.False(manager.IsModLoaded("Mod1"));
            Assert.False(manager.IsModLoaded("Mod2"));
        }
    }
}