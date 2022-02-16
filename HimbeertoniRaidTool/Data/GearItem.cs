using static HimbeertoniRaidTool.Connectors.EtroConnector;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using Dalamud.Logging;

namespace HimbeertoniRaidTool.Data
{
    public class GearItem
    {
        private static Dictionary<int, GearItem> Dic = new();
        private static Dictionary<string, GearSource> SourceDic = new Dictionary<string, GearSource>
        {
            { "Asphodelos", GearSource.Raid },
            { "Radiant", GearSource.Tome },
            { "Classical", GearSource.Crafted },
        };
        public int ID { get; init; }
        private string _Name;
        public string Name { get => _Name; set { _Name = value; UpdateSource(); AddToDic(); } }
        private string _Description;
        public string Description { get => _Description; set { _Description = value; AddToDic(); } }
        private int _ItemLevel;
        public int ItemLevel { get => _ItemLevel; set { _ItemLevel = value; AddToDic(); } }
        public GearSource Source { get; set; } = GearSource.undefined;
        [JsonIgnore]
        public bool Filled => _ItemLevel > 0 && !_Name.Equals("") && !_Description.Equals("");
        [JsonIgnore]
        public bool NeedsStats => ID > 0 && Name == "";
        [JsonIgnore]
        public bool Valid => ID > 0;

        public static GearItem New(int ID){
            GearItem? result;
            Dic.TryGetValue(ID, out result);
            return result ?? new(ID);
        }
        public GearItem() : this(0) { }

        public GearItem(int idArg)
        {
            this.ID = idArg;
            this._Name = "";
            this._Description = "";
            this._ItemLevel = 0;
            GearItem? dicItem;
            if (Dic.TryGetValue(ID, out dicItem))
                this.Fill(dicItem);            
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
        private void AddToDic()
        {
            if (Filled && !Dic.ContainsKey(ID))
                Dic.Add(ID, this);
        }
        internal bool RetrieveItemData()
        {
            if (!NeedsStats)
                return true;
            GearItem? dicItem;
            if (Dic.TryGetValue(ID, out dicItem))
            {
                this.Fill(dicItem);
                return true;
            }
            return GetGearStats(this);
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
