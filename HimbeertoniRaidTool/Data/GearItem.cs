using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace HimbeertoniRaidTool.Data
{
    public class GearItem
    {
        private static Dictionary<string, GearSource> SourceDic = new Dictionary<string, GearSource>
        {
            { "Asphodelos", GearSource.Raid },
            { "Radiant", GearSource.Tome },
            { "Classical", GearSource.Crafted },
        };
        public uint ID { get; init; }
        private Item? FromSheet;
        private string _Name;
        public string Name { get => _Name; set { _Name = value; UpdateSource(); } }
        private string _Description;
        public string Description { get => _Description; set { _Description = value; } }
        private int _ItemLevel;
        public int ItemLevel { get => _ItemLevel; set { _ItemLevel = value; } }
        public GearSource Source { get; set; } = GearSource.undefined;
        [JsonIgnore]
        public bool Filled => (_ItemLevel > 0 && !_Name.Equals("") && !_Description.Equals("")) || FromSheet is not null;
        [JsonIgnore]
        public bool NeedsStats => ID > 0 && Name == "";
        [JsonIgnore]
        public bool Valid => ID > 0;

        private ExcelSheet<Item> Sheet => Services.Data.Excel.GetSheet<Item>()!;

        public GearItem() : this(0) { }

        public GearItem(uint idArg)
        {
            this.ID = idArg;
            this._Name = "";
            this._Description = "";
            this._ItemLevel = 0;
            if (ID > 0)
                FromSheet = Sheet.GetRow(ID);
        }

        private void Fill(GearItem dicItem)
        {
            if(this.ID == dicItem.ID)
            {
                this.Name = dicItem.Name;
                this.Description = dicItem.Description;
            }
        }
        private void UpdateSource()
        {
            string key = "";
            foreach (string curKey in SourceDic.Keys)
            {
                if (_Name.Contains(curKey))
                {
                    key = curKey;
                    break;
                }
            }
            this.Source = SourceDic.GetValueOrDefault(key,GearSource.undefined);
        }

    }
    public enum GearSource
    {
        Raid,
        Tome,
        Crafted,
        undefined,
    }
}
