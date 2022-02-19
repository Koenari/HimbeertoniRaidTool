using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HimbeertoniRaidTool.Data
{
    public class GearItem
    {
        private readonly static Dictionary<string, GearSource> SourceDic = new(new ContainsEqualityComparer())
        {
            { "Asphodelos", GearSource.Raid },
            { "Radiant", GearSource.Tome },
            { "Classical", GearSource.Crafted },
        };
        private uint _ID;
        public uint ID { get => _ID; set { _ID = value; UpdateStats(); } }
        [JsonIgnore]
        public Item Item { get; private set; } = new();
        [JsonIgnore]
        public string Name => (Item.Name??new("")).RawString;
        [JsonIgnore]
        public uint ItemLevel => (Item.LevelItem is null) ? 0: Item.LevelItem.Row;
        [JsonIgnore]
        public GearSource Source { get; private set; } = GearSource.undefined;
        [JsonIgnore]
        public bool Filled => Name.Length > 0;
        [JsonIgnore]
        public bool Valid => ID > 0;

        private static ExcelSheet<Item> Sheet => Services.Data.Excel.GetSheet<Item>()!;

        public GearItem() : this(0) { }

        public GearItem(uint idArg)
        {
            this.ID = idArg;
        }

        private void UpdateStats()
        {
            if (ID > 0)
            {
                Item = Sheet.GetRow(ID) ?? (new Item());
                Source = SourceDic.GetValueOrDefault(Name, GearSource.undefined);
            }
        }
    }
    public enum GearSource
    {
        Raid,
        Tome,
        Crafted,
        undefined,
    }
    class ContainsEqualityComparer : IEqualityComparer<string>
    {
        public bool Equals(string? x, string? y)
        {
            if (x is null || y is null)
                return false;
            return x.Contains(y) || y.Contains(x);
        }

        public int GetHashCode([DisallowNull] string obj) => obj.GetHashCode();
    }
}
