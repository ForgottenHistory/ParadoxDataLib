using System.Collections.Generic;
using System.Linq;
using Xunit;
using ParadoxDataLib.Validation;
using ParadoxDataLib.Core.DataModels;

namespace ParadoxDataLib.Tests
{
    public class ValidationTests
    {
        private readonly DataValidator _validator;

        public ValidationTests()
        {
            _validator = new DataValidator();
        }

        [Fact]
        public void ProvinceValidator_ValidProvince_ShouldPass()
        {
            var province = new ProvinceData(1, "Test Province")
            {
                Owner = "FRA",
                Controller = "FRA",
                Culture = "french",
                Religion = "catholic",
                BaseTax = 5,
                BaseProduction = 3,
                BaseManpower = 4,
                TradeGood = "grain",
                Cores = new List<string> { "FRA" }
            };

            var result = _validator.ValidateProvince(province);

            Assert.True(result.IsValid);
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void ProvinceValidator_InvalidId_ShouldFail()
        {
            var province = new ProvinceData(-1, "Test Province")
            {
                Owner = "FRA",
                Controller = "FRA"
            };

            var result = _validator.ValidateProvince(province);

            Assert.False(result.IsValid);
            Assert.True(result.HasErrors);
            Assert.Contains(result.Issues, i => i.PropertyName == "ProvinceId" && i.Severity == ValidationSeverity.Error);
        }

        [Fact]
        public void ProvinceValidator_InvalidCountryTag_ShouldFail()
        {
            var province = new ProvinceData(1, "Test Province")
            {
                Owner = "FRANCE", // Invalid - too long
                Controller = "FR" // Invalid - too short
            };

            var result = _validator.ValidateProvince(province);

            Assert.False(result.IsValid);
            Assert.True(result.HasErrors);
            Assert.Contains(result.Issues, i => i.PropertyName == "Owner" && i.Severity == ValidationSeverity.Error);
            Assert.Contains(result.Issues, i => i.PropertyName == "Controller" && i.Severity == ValidationSeverity.Error);
        }

        [Fact]
        public void ProvinceValidator_NegativeEconomicValues_ShouldFail()
        {
            var province = new ProvinceData(1, "Test Province")
            {
                BaseTax = -1,
                BaseProduction = -2,
                BaseManpower = -3
            };

            var result = _validator.ValidateProvince(province);

            Assert.False(result.IsValid);
            Assert.True(result.HasErrors);
            Assert.Equal(3, result.ErrorCount);
        }

        [Fact]
        public void ProvinceValidator_HighEconomicValues_ShouldWarn()
        {
            var province = new ProvinceData(1, "Test Province")
            {
                BaseTax = 25,
                BaseProduction = 25,
                BaseManpower = 25
            };

            var result = _validator.ValidateProvince(province);

            Assert.True(result.IsValid); // Should be valid but with warnings
            Assert.True(result.HasWarnings);
            Assert.Equal(3, result.WarningCount);
        }

        [Fact]
        public void ProvinceValidator_InvalidTradeGood_ShouldWarn()
        {
            var province = new ProvinceData(1, "Test Province")
            {
                TradeGood = "unknown_trade_good"
            };

            var result = _validator.ValidateProvince(province);

            Assert.True(result.IsValid);
            Assert.True(result.HasWarnings);
            Assert.Contains(result.Issues, i => i.PropertyName == "TradeGood" && i.Severity == ValidationSeverity.Warning);
        }

        [Fact]
        public void CountryValidator_ValidCountry_ShouldPass()
        {
            var country = new CountryData("FRA", "France")
            {
                Government = "monarchy",
                PrimaryCulture = "french",
                Religion = "catholic",
                TechnologyGroup = "western",
                Capital = 183
            };

            var result = _validator.ValidateCountry(country);

            Assert.True(result.IsValid);
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void CountryValidator_InvalidTag_ShouldFail()
        {
            var country = new CountryData("FRANCE", "France") // Invalid - too long
            {
                Government = "monarchy"
            };

            var result = _validator.ValidateCountry(country);

            Assert.False(result.IsValid);
            Assert.True(result.HasErrors);
            Assert.Contains(result.Issues, i => i.PropertyName == "Tag" && i.Severity == ValidationSeverity.Error);
        }

        [Fact]
        public void CountryValidator_EmptyTag_ShouldFail()
        {
            var country = new CountryData("", "")
            {
                Government = "monarchy"
            };

            var result = _validator.ValidateCountry(country);

            Assert.False(result.IsValid);
            Assert.True(result.HasErrors);
            Assert.Contains(result.Issues, i => i.PropertyName == "Tag" && i.Severity == ValidationSeverity.Error);
        }

        [Fact]
        public void CountryValidator_UnknownGovernment_ShouldWarn()
        {
            var country = new CountryData("FRA", "France")
            {
                Government = "unknown_government",
                PrimaryCulture = "french",
                Religion = "catholic"
            };

            var result = _validator.ValidateCountry(country);

            Assert.True(result.IsValid);
            Assert.True(result.HasWarnings);
            Assert.Contains(result.Issues, i => i.PropertyName == "Government" && i.Severity == ValidationSeverity.Warning);
        }

        [Fact]
        public void CountryValidator_UnknownTechnologyGroup_ShouldWarn()
        {
            var country = new CountryData("FRA", "France")
            {
                Government = "monarchy",
                TechnologyGroup = "unknown_tech_group",
                PrimaryCulture = "french",
                Religion = "catholic"
            };

            var result = _validator.ValidateCountry(country);

            Assert.True(result.IsValid);
            Assert.True(result.HasWarnings);
            Assert.Contains(result.Issues, i => i.PropertyName == "TechnologyGroup" && i.Severity == ValidationSeverity.Warning);
        }

        [Fact]
        public void CrossReferenceValidator_ValidReferences_ShouldPass()
        {
            var provinces = new List<ProvinceData>
            {
                new ProvinceData(1, "Paris") { Owner = "FRA", Controller = "FRA", Cores = new List<string> { "FRA", "ENG" } },
                new ProvinceData(2, "London") { Owner = "ENG", Controller = "ENG", Cores = new List<string> { "ENG" } }
            };

            var countries = new List<CountryData>
            {
                new CountryData("FRA", "France") { Capital = 1 },
                new CountryData("ENG", "England") { Capital = 2 }
            };

            var result = _validator.ValidateCrossReferences(provinces, countries);

            Assert.True(result.IsValid);
            Assert.False(result.HasErrors);
        }

        [Fact]
        public void CrossReferenceValidator_MissingCountry_ShouldFail()
        {
            var provinces = new List<ProvinceData>
            {
                new ProvinceData(1, "Paris") { Owner = "FRA", Controller = "SPA", Cores = new List<string> { "FRA", "ENG" } }
            };

            var countries = new List<CountryData>
            {
                new CountryData("FRA", "France")
                // Missing ENG and SPA
            };

            var result = _validator.ValidateCrossReferences(provinces, countries);

            Assert.False(result.IsValid);
            Assert.True(result.HasErrors);
            Assert.Equal(2, result.ErrorCount); // Missing SPA (controller), ENG (core)
        }

        [Fact]
        public void CrossReferenceValidator_InvalidCapital_ShouldFail()
        {
            var provinces = new List<ProvinceData>
            {
                new ProvinceData(1, "Paris")
            };

            var countries = new List<CountryData>
            {
                new CountryData("FRA", "France") { Capital = 999 } // Province 999 doesn't exist
            };

            var result = _validator.ValidateCrossReferences(provinces, countries);

            Assert.False(result.IsValid);
            Assert.True(result.HasErrors);
            Assert.Contains(result.Issues, i => i.PropertyName == "CrossReference" && i.Message.Contains("capital province 999 does not exist"));
        }

        [Fact]
        public void ValidationResult_ToString_ShouldFormatCorrectly()
        {
            var result = new ValidationResult();
            result.AddError("TestProperty", "Test error message");
            result.AddWarning("TestProperty", "Test warning message", "TestContext", 42);
            result.AddInfo("TestProperty", "Test info message");

            var output = result.ToString();

            Assert.Contains("[ERROR]", output);
            Assert.Contains("[WARN]", output);
            Assert.Contains("[INFO]", output);
            Assert.Contains("Line 42", output);
            Assert.Contains("TestContext", output);
            Assert.Contains("Test error message", output);
        }

        [Fact]
        public void ValidationResult_Merge_ShouldCombineResults()
        {
            var result1 = new ValidationResult();
            result1.AddError("Property1", "Error 1");
            result1.AddWarning("Property1", "Warning 1");

            var result2 = new ValidationResult();
            result2.AddError("Property2", "Error 2");
            result2.AddInfo("Property2", "Info 2");

            result1.Merge(result2);

            Assert.Equal(2, result1.ErrorCount);
            Assert.Equal(1, result1.WarningCount);
            Assert.Equal(4, result1.Issues.Count);
        }

        [Fact]
        public void ValidationResult_GetIssuesBySeverity_ShouldFilter()
        {
            var result = new ValidationResult();
            result.AddError("Property1", "Error");
            result.AddWarning("Property2", "Warning");
            result.AddInfo("Property3", "Info");

            var errors = result.GetIssuesBySeverity(ValidationSeverity.Error);
            var warnings = result.GetIssuesBySeverity(ValidationSeverity.Warning);

            Assert.Equal(1, errors.Issues.Count);
            Assert.Equal(1, warnings.Issues.Count);
            Assert.Equal(ValidationSeverity.Error, errors.Issues[0].Severity);
            Assert.Equal(ValidationSeverity.Warning, warnings.Issues[0].Severity);
        }

        [Fact]
        public void ValidationResult_GetIssuesForProperty_ShouldFilter()
        {
            var result = new ValidationResult();
            result.AddError("Property1", "Error 1");
            result.AddWarning("Property1", "Warning 1");
            result.AddError("Property2", "Error 2");

            var property1Issues = result.GetIssuesForProperty("Property1");

            Assert.Equal(2, property1Issues.Issues.Count);
            Assert.All(property1Issues.Issues, issue => Assert.Equal("Property1", issue.PropertyName));
        }
    }
}