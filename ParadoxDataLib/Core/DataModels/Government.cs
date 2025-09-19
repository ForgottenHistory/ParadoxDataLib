using System.Collections.Generic;

namespace ParadoxDataLib.Core.DataModels
{
    public class Government
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string GovernmentType { get; set; }
        public bool AllowNormalConversion { get; set; }
        public bool AllowConvertOnReligionChange { get; set; }
        public int FixedRank { get; set; }
        public bool RepublicanName { get; set; }

        public Dictionary<string, float> Modifiers { get; set; }
        public List<string> ValidForNewCountry { get; set; }
        public List<string> ValidForNationDesigner { get; set; }
        public List<string> NationDesignerTrigger { get; set; }
        public int NationDesignerCost { get; set; }

        public List<string> Reforms { get; set; }
        public List<string> ExclusiveReforms { get; set; }
        public List<string> StartingReforms { get; set; }

        public Government()
        {
            Modifiers = new Dictionary<string, float>();
            ValidForNewCountry = new List<string>();
            ValidForNationDesigner = new List<string>();
            NationDesignerTrigger = new List<string>();
            Reforms = new List<string>();
            ExclusiveReforms = new List<string>();
            StartingReforms = new List<string>();
        }

        public Government(string id, string name, string type) : this()
        {
            Id = id;
            Name = name;
            GovernmentType = type;
        }
    }
}