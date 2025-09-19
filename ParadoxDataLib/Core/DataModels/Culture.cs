using System.Collections.Generic;

namespace ParadoxDataLib.Core.DataModels
{
    public class Culture
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string CultureGroup { get; set; }
        public string PrimaryCountry { get; set; }
        public List<string> MaleNames { get; set; }
        public List<string> FemaleNames { get; set; }
        public List<string> DynastyNames { get; set; }
        public Dictionary<string, float> CountryModifiers { get; set; }
        public Dictionary<string, float> ProvinceModifiers { get; set; }

        public Culture()
        {
            MaleNames = new List<string>();
            FemaleNames = new List<string>();
            DynastyNames = new List<string>();
            CountryModifiers = new Dictionary<string, float>();
            ProvinceModifiers = new Dictionary<string, float>();
        }

        public Culture(string id, string name, string cultureGroup) : this()
        {
            Id = id;
            Name = name;
            CultureGroup = cultureGroup;
        }
    }
}