namespace ParadoxDataLib.Core.Common
{
    public static class ParadoxConstants
    {
        public const string DateFormat = "yyyy.M.d";
        public const string CommentSymbol = "#";
        public const char AssignmentOperator = '=';
        public const char OpenBrace = '{';
        public const char CloseBrace = '}';
        public const string YesValue = "yes";
        public const string NoValue = "no";

        public static class FileExtensions
        {
            public const string TextFile = ".txt";
            public const string ModFile = ".mod";
            public const string SaveGame = ".eu4";
            public const string CompressedSave = ".eu4.gz";
        }

        public static class Keywords
        {
            public const string Owner = "owner";
            public const string Controller = "controller";
            public const string Culture = "culture";
            public const string Religion = "religion";
            public const string Capital = "capital";
            public const string Government = "government";
            public const string AddCore = "add_core";
            public const string RemoveCore = "remove_core";
            public const string DiscoveredBy = "discovered_by";
            public const string BaseTax = "base_tax";
            public const string BaseProduction = "base_production";
            public const string BaseManpower = "base_manpower";
            public const string TradeGoods = "trade_goods";
            public const string IsCity = "is_city";
            public const string Hre = "hre";
            public const string Buildings = "buildings";
            public const string Monarch = "monarch";
            public const string Heir = "heir";
            public const string Queen = "queen";
        }

        public static class Directories
        {
            public const string History = "history";
            public const string Provinces = "provinces";
            public const string Countries = "countries";
            public const string Common = "common";
            public const string Localisation = "localisation";
            public const string Map = "map";
            public const string Gfx = "gfx";
            public const string Interface = "interface";
            public const string Events = "events";
            public const string Decisions = "decisions";
            public const string Missions = "missions";
        }
    }
}