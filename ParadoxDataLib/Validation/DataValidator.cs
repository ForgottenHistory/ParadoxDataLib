using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ParadoxDataLib.Core.DataModels;
using ParadoxDataLib.Core.Common;

namespace ParadoxDataLib.Validation
{
    /// <summary>
    /// Comprehensive validator for Paradox game data including provinces, countries, and cross-references.
    /// Performs structural validation, format checking, and logical consistency verification.
    /// </summary>
    public class DataValidator
    {
        /// <summary>
        /// Regex pattern for validating three-letter uppercase country tags (e.g., "POL", "FRA")
        /// </summary>
        private static readonly Regex CountryTagRegex = new Regex(@"^[A-Z]{3}$", RegexOptions.Compiled);

        /// <summary>
        /// Regex pattern for validating Paradox date format (YYYY.MM.DD)
        /// </summary>
        private static readonly Regex DateRegex = new Regex(@"^\d{1,4}\.\d{1,2}\.\d{1,2}$", RegexOptions.Compiled);

        /// <summary>
        /// Validates a province data structure for structural and logical consistency
        /// </summary>
        /// <param name="province">The province data to validate</param>
        /// <param name="context">Optional context information for error reporting</param>
        /// <returns>A ValidationResult containing any errors, warnings, or informational messages</returns>
        public ValidationResult ValidateProvince(ProvinceData province, string context = null)
        {
            var result = new ValidationResult();

            // ProvinceData is a struct, so it can't be null - skip null check

            ValidateProvinceId(province, result, context);
            ValidateProvinceOwner(province, result, context);
            ValidateProvinceController(province, result, context);
            ValidateProvinceEconomy(province, result, context);
            ValidateProvinceCulture(province, result, context);
            ValidateProvinceReligion(province, result, context);
            ValidateProvinceTradeGood(province, result, context);
            ValidateProvinceHistoricalEntries(province, result, context);
            ValidateProvinceCores(province, result, context);
            ValidateProvinceBuildings(province, result, context);

            return result;
        }

        /// <summary>
        /// Validates the province ID is within acceptable ranges
        /// </summary>
        /// <param name="province">The province data to validate</param>
        /// <param name="result">The validation result to add messages to</param>
        /// <param name="context">Optional context for error reporting</param>
        private void ValidateProvinceId(ProvinceData province, ValidationResult result, string context)
        {
            if (province.ProvinceId <= 0)
            {
                result.AddError("ProvinceId", "Province ID must be greater than 0", context);
            }

            if (province.ProvinceId > 999999)
            {
                result.AddWarning("ProvinceId", "Province ID is unusually high (>999999)", context);
            }
        }

        /// <summary>
        /// Validates the province owner tag format
        /// </summary>
        /// <param name="province">The province data to validate</param>
        /// <param name="result">The validation result to add messages to</param>
        /// <param name="context">Optional context for error reporting</param>
        private void ValidateProvinceOwner(ProvinceData province, ValidationResult result, string context)
        {
            if (!string.IsNullOrEmpty(province.Owner))
            {
                if (!CountryTagRegex.IsMatch(province.Owner))
                {
                    result.AddError("Owner", $"Invalid country tag format: '{province.Owner}'. Must be 3 uppercase letters.", context);
                }
            }
        }

        /// <summary>
        /// Validates the province controller tag format and checks for owner/controller mismatches
        /// </summary>
        /// <param name="province">The province data to validate</param>
        /// <param name="result">The validation result to add messages to</param>
        /// <param name="context">Optional context for error reporting</param>
        private void ValidateProvinceController(ProvinceData province, ValidationResult result, string context)
        {
            if (!string.IsNullOrEmpty(province.Controller))
            {
                if (!CountryTagRegex.IsMatch(province.Controller))
                {
                    result.AddError("Controller", $"Invalid controller tag format: '{province.Controller}'. Must be 3 uppercase letters.", context);
                }
            }

            if (!string.IsNullOrEmpty(province.Owner) && !string.IsNullOrEmpty(province.Controller))
            {
                if (province.Owner != province.Controller)
                {
                    result.AddInfo("Controller", $"Province owner ({province.Owner}) differs from controller ({province.Controller})", context);
                }
            }
        }

        /// <summary>
        /// Validates economic values (tax, production, manpower) for reasonable ranges
        /// </summary>
        /// <param name="province">The province data to validate</param>
        /// <param name="result">The validation result to add messages to</param>
        /// <param name="context">Optional context for error reporting</param>
        private void ValidateProvinceEconomy(ProvinceData province, ValidationResult result, string context)
        {
            if (province.BaseTax < 0)
            {
                result.AddError("BaseTax", "Base tax cannot be negative", context);
            }

            if (province.BaseProduction < 0)
            {
                result.AddError("BaseProduction", "Base production cannot be negative", context);
            }

            if (province.BaseManpower < 0)
            {
                result.AddError("BaseManpower", "Base manpower cannot be negative", context);
            }

            if (province.BaseTax > 20)
            {
                result.AddWarning("BaseTax", $"Base tax ({province.BaseTax}) is unusually high (>20)", context);
            }

            if (province.BaseProduction > 20)
            {
                result.AddWarning("BaseProduction", $"Base production ({province.BaseProduction}) is unusually high (>20)", context);
            }

            if (province.BaseManpower > 20)
            {
                result.AddWarning("BaseManpower", $"Base manpower ({province.BaseManpower}) is unusually high (>20)", context);
            }

            if (province.ExtraCost < 0)
            {
                result.AddError("ExtraCost", "Extra cost cannot be negative", context);
            }
        }

        /// <summary>
        /// Validates the province culture name for formatting issues
        /// </summary>
        /// <param name="province">The province data to validate</param>
        /// <param name="result">The validation result to add messages to</param>
        /// <param name="context">Optional context for error reporting</param>
        private void ValidateProvinceCulture(ProvinceData province, ValidationResult result, string context)
        {
            if (!string.IsNullOrEmpty(province.Culture))
            {
                if (province.Culture.Length > 50)
                {
                    result.AddWarning("Culture", $"Culture name is unusually long ({province.Culture.Length} characters)", context);
                }

                if (province.Culture.Contains(" "))
                {
                    result.AddWarning("Culture", "Culture name contains spaces, which may cause parsing issues", context);
                }
            }
        }

        /// <summary>
        /// Validates the province religion name for formatting issues
        /// </summary>
        /// <param name="province">The province data to validate</param>
        /// <param name="result">The validation result to add messages to</param>
        /// <param name="context">Optional context for error reporting</param>
        private void ValidateProvinceReligion(ProvinceData province, ValidationResult result, string context)
        {
            if (!string.IsNullOrEmpty(province.Religion))
            {
                if (province.Religion.Length > 50)
                {
                    result.AddWarning("Religion", $"Religion name is unusually long ({province.Religion.Length} characters)", context);
                }

                if (province.Religion.Contains(" "))
                {
                    result.AddWarning("Religion", "Religion name contains spaces, which may cause parsing issues", context);
                }
            }
        }

        /// <summary>
        /// Validates the province trade good against known EU4 trade goods
        /// </summary>
        /// <param name="province">The province data to validate</param>
        /// <param name="result">The validation result to add messages to</param>
        /// <param name="context">Optional context for error reporting</param>
        private void ValidateProvinceTradeGood(ProvinceData province, ValidationResult result, string context)
        {
            if (!string.IsNullOrEmpty(province.TradeGood))
            {
                var validTradeGoods = new HashSet<string>
                {
                    "grain", "livestock", "fish", "naval_supplies", "salt", "wine", "wool", "cloth",
                    "iron", "copper", "gold", "gems", "ivory", "fur", "spices", "silk", "dyes",
                    "sugar", "tobacco", "coffee", "cotton", "tea", "chinaware", "paper"
                };

                if (!validTradeGoods.Contains(province.TradeGood))
                {
                    result.AddWarning("TradeGood", $"Unknown trade good: '{province.TradeGood}'", context);
                }
            }
        }

        /// <summary>
        /// Validates historical entries for chronological order and date ranges
        /// </summary>
        /// <param name="province">The province data to validate</param>
        /// <param name="result">The validation result to add messages to</param>
        /// <param name="context">Optional context for error reporting</param>
        private void ValidateProvinceHistoricalEntries(ProvinceData province, ValidationResult result, string context)
        {
            if (province.HistoricalEntries?.Count > 0)
            {
                // HistoricalEntries is a List<HistoricalEntry>, not a dictionary
                var sortedEntries = province.HistoricalEntries.OrderBy(h => h.Date).ToList();

                for (int i = 0; i < sortedEntries.Count; i++)
                {
                    var entry = sortedEntries[i];
                    var date = entry.Date;

                    if (date.Year < 1000 || date.Year > 2100)
                    {
                        result.AddWarning("HistoricalEntries", $"Date {date} has unusual year ({date.Year})", context);
                    }

                    if (i > 0)
                    {
                        var prevDate = sortedEntries[i - 1].Date;
                        if (date <= prevDate)
                        {
                            result.AddWarning("HistoricalEntries", $"Historical entries may not be in chronological order: {prevDate} -> {date}", context);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates province cores for tag format, duplicates, and owner relationships
        /// </summary>
        /// <param name="province">The province data to validate</param>
        /// <param name="result">The validation result to add messages to</param>
        /// <param name="context">Optional context for error reporting</param>
        private void ValidateProvinceCores(ProvinceData province, ValidationResult result, string context)
        {
            if (province.Cores?.Count > 0)
            {
                foreach (var core in province.Cores)
                {
                    if (!CountryTagRegex.IsMatch(core))
                    {
                        result.AddError("Cores", $"Invalid core country tag format: '{core}'. Must be 3 uppercase letters.", context);
                    }
                }

                if (!string.IsNullOrEmpty(province.Owner) && !province.Cores.Contains(province.Owner))
                {
                    result.AddInfo("Cores", $"Province owner '{province.Owner}' does not have a core on this province", context);
                }

                var distinctCores = province.Cores.Distinct().ToList();
                if (distinctCores.Count != province.Cores.Count)
                {
                    result.AddWarning("Cores", "Duplicate cores detected", context);
                }
            }
        }

        /// <summary>
        /// Validates province buildings against known EU4 building types
        /// </summary>
        /// <param name="province">The province data to validate</param>
        /// <param name="result">The validation result to add messages to</param>
        /// <param name="context">Optional context for error reporting</param>
        private void ValidateProvinceBuildings(ProvinceData province, ValidationResult result, string context)
        {
            if (province.Buildings?.Count > 0)
            {
                var validBuildings = new HashSet<string>
                {
                    "fort_15th", "fort_16th", "fort_17th", "fort_18th", "fort_19th",
                    "marketplace", "workshop", "temple", "shipyard", "dock",
                    "drydock", "grand_shipyard", "admiralty", "conscription_center",
                    "training_fields", "regimental_camp", "barracks", "armory"
                };

                // Buildings is a List<string>, not a dictionary
                foreach (var building in province.Buildings)
                {
                    if (!validBuildings.Contains(building))
                    {
                        result.AddWarning("Buildings", $"Unknown building type: '{building}'", context);
                    }
                }
            }
        }

        /// <summary>
        /// Validates a country data structure for structural and logical consistency
        /// </summary>
        /// <param name="country">The country data to validate</param>
        /// <param name="context">Optional context information for error reporting</param>
        /// <returns>A ValidationResult containing any errors, warnings, or informational messages</returns>
        public ValidationResult ValidateCountry(CountryData country, string context = null)
        {
            var result = new ValidationResult();

            // CountryData is a struct, so it can't be null - skip null check

            ValidateCountryTag(country, result, context);
            ValidateCountryGovernment(country, result, context);
            ValidateCountryTechnology(country, result, context);
            ValidateCountryCulture(country, result, context);
            ValidateCountryReligion(country, result, context);
            ValidateCountryCapital(country, result, context);
            ValidateCountryHistoricalEntries(country, result, context);

            return result;
        }

        /// <summary>
        /// Validates the country tag format (must be 3 uppercase letters)
        /// </summary>
        /// <param name="country">The country data to validate</param>
        /// <param name="result">The validation result to add messages to</param>
        /// <param name="context">Optional context for error reporting</param>
        private void ValidateCountryTag(CountryData country, ValidationResult result, string context)
        {
            if (string.IsNullOrEmpty(country.Tag))
            {
                result.AddError("Tag", "Country tag cannot be null or empty", context);
            }
            else if (!CountryTagRegex.IsMatch(country.Tag))
            {
                result.AddError("Tag", $"Invalid country tag format: '{country.Tag}'. Must be 3 uppercase letters.", context);
            }
        }

        /// <summary>
        /// Validates the country government type against known EU4 government types
        /// </summary>
        /// <param name="country">The country data to validate</param>
        /// <param name="result">The validation result to add messages to</param>
        /// <param name="context">Optional context for error reporting</param>
        private void ValidateCountryGovernment(CountryData country, ValidationResult result, string context)
        {
            if (string.IsNullOrEmpty(country.Government))
            {
                result.AddWarning("Government", "Government type not specified", context);
            }
            else
            {
                var validGovernments = new HashSet<string>
                {
                    "monarchy", "republic", "tribal", "theocracy", "horde",
                    "despotic_monarchy", "feudal_monarchy", "administrative_monarchy",
                    "oligarchic_republic", "merchant_republic", "noble_republic",
                    "tribal_federation", "tribal_despotism", "tribal_kingdom"
                };

                if (!validGovernments.Contains(country.Government))
                {
                    result.AddWarning("Government", $"Unknown government type: '{country.Government}'", context);
                }
            }
        }

        /// <summary>
        /// Validates the country technology group against known EU4 technology groups
        /// </summary>
        /// <param name="country">The country data to validate</param>
        /// <param name="result">The validation result to add messages to</param>
        /// <param name="context">Optional context for error reporting</param>
        private void ValidateCountryTechnology(CountryData country, ValidationResult result, string context)
        {
            if (!string.IsNullOrEmpty(country.TechnologyGroup))
            {
                var validTechGroups = new HashSet<string>
                {
                    "western", "eastern", "ottoman", "muslim", "indian", "chinese",
                    "nomad_group", "sub_saharan", "north_american", "mesoamerican",
                    "south_american", "andean", "high_american"
                };

                if (!validTechGroups.Contains(country.TechnologyGroup))
                {
                    result.AddWarning("TechnologyGroup", $"Unknown technology group: '{country.TechnologyGroup}'", context);
                }
            }
        }

        /// <summary>
        /// Validates that the country has a primary culture specified
        /// </summary>
        /// <param name="country">The country data to validate</param>
        /// <param name="result">The validation result to add messages to</param>
        /// <param name="context">Optional context for error reporting</param>
        private void ValidateCountryCulture(CountryData country, ValidationResult result, string context)
        {
            if (string.IsNullOrEmpty(country.PrimaryCulture))
            {
                result.AddWarning("PrimaryCulture", "Primary culture not specified", context);
            }
        }

        /// <summary>
        /// Validates that the country has a religion specified
        /// </summary>
        /// <param name="country">The country data to validate</param>
        /// <param name="result">The validation result to add messages to</param>
        /// <param name="context">Optional context for error reporting</param>
        private void ValidateCountryReligion(CountryData country, ValidationResult result, string context)
        {
            if (string.IsNullOrEmpty(country.Religion))
            {
                result.AddWarning("Religion", "Religion not specified", context);
            }
        }

        /// <summary>
        /// Validates that the country has a valid capital province ID
        /// </summary>
        /// <param name="country">The country data to validate</param>
        /// <param name="result">The validation result to add messages to</param>
        /// <param name="context">Optional context for error reporting</param>
        private void ValidateCountryCapital(CountryData country, ValidationResult result, string context)
        {
            if (country.Capital <= 0)
            {
                result.AddWarning("Capital", "Capital province not specified or invalid", context);
            }
        }

        /// <summary>
        /// Validates country historical entries for reasonable date ranges
        /// </summary>
        /// <param name="country">The country data to validate</param>
        /// <param name="result">The validation result to add messages to</param>
        /// <param name="context">Optional context for error reporting</param>
        private void ValidateCountryHistoricalEntries(CountryData country, ValidationResult result, string context)
        {
            if (country.HistoricalEntries?.Count > 0)
            {
                // HistoricalEntries is a List<HistoricalEntry>, not a dictionary
                foreach (var entry in country.HistoricalEntries)
                {
                    if (entry.Date.Year < 1000 || entry.Date.Year > 2100)
                    {
                        result.AddWarning("HistoricalEntries", $"Date {entry.Date} has unusual year ({entry.Date.Year})", context);
                    }
                }
            }
        }

        /// <summary>
        /// Validates cross-references between provinces and countries, ensuring referential integrity.
        /// Checks that province owners, controllers, and cores reference existing countries,
        /// and that country capitals reference existing provinces.
        /// </summary>
        /// <param name="provinces">Collection of provinces to validate</param>
        /// <param name="countries">Collection of countries to validate against</param>
        /// <param name="context">Optional context information for error reporting</param>
        /// <returns>A ValidationResult containing any cross-reference errors</returns>
        public ValidationResult ValidateCrossReferences(
            IEnumerable<ProvinceData> provinces,
            IEnumerable<CountryData> countries,
            string context = null)
        {
            var result = new ValidationResult();
            var countryTags = new HashSet<string>(countries.Select(c => c.Tag).Where(t => !string.IsNullOrEmpty(t)));

            foreach (var province in provinces)
            {
                if (!string.IsNullOrEmpty(province.Owner) && !countryTags.Contains(province.Owner))
                {
                    result.AddError("CrossReference", $"Province {province.ProvinceId} owner '{province.Owner}' does not exist", context);
                }

                if (!string.IsNullOrEmpty(province.Controller) && !countryTags.Contains(province.Controller))
                {
                    result.AddError("CrossReference", $"Province {province.ProvinceId} controller '{province.Controller}' does not exist", context);
                }

                if (province.Cores?.Count > 0)
                {
                    foreach (var core in province.Cores)
                    {
                        if (!countryTags.Contains(core))
                        {
                            result.AddError("CrossReference", $"Province {province.ProvinceId} core '{core}' does not exist", context);
                        }
                    }
                }
            }

            var provinceIds = new HashSet<int>(provinces.Select(p => p.ProvinceId));
            foreach (var country in countries)
            {
                if (country.Capital > 0 && !provinceIds.Contains(country.Capital))
                {
                    result.AddError("CrossReference", $"Country '{country.Tag}' capital province {country.Capital} does not exist", context);
                }
            }

            return result;
        }
    }
}