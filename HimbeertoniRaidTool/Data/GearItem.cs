using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HimbeertoniRaidTool.Data
{
    public class GearItem
    {
        private readonly static KeyContainsDictionary<GearSource> SourceDic = new()
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
        public uint ItemLevel => (Item.LevelItem is null) ? 0 : Item.LevelItem.Row;
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
                Item = Sheet.GetRow(_ID) ?? (new Item());
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
    [SuppressMessage("Style", "IDE0060:Nicht verwendete Parameter entfernen", Justification = "Override all constructors for safety")]
    public class KeyContainsDictionary<TValue> : Dictionary<string, TValue> 
    {
        public KeyContainsDictionary() : base(new ContainsEqualityComparer()) { ((ContainsEqualityComparer)Comparer).parent = this; }
        public KeyContainsDictionary(IDictionary<string, TValue> dictionary) 
            : base(dictionary, new ContainsEqualityComparer()) { ((ContainsEqualityComparer)Comparer).parent = this; }
        public KeyContainsDictionary(IEnumerable<KeyValuePair<string, TValue>> collection) 
            : base(collection, new ContainsEqualityComparer()) { ((ContainsEqualityComparer)Comparer).parent = this; }
        public KeyContainsDictionary(IEqualityComparer<string>? comparer) 
            :base(new ContainsEqualityComparer()) { ((ContainsEqualityComparer)Comparer).parent = this; }
        public KeyContainsDictionary(int capacity)
            : base(capacity, new ContainsEqualityComparer()) { ((ContainsEqualityComparer)Comparer).parent = this; }
        public KeyContainsDictionary(IDictionary<string, TValue> dictionary, IEqualityComparer<string>? comparer)
            : base(dictionary, new ContainsEqualityComparer()) { ((ContainsEqualityComparer)Comparer).parent = this; }
        public KeyContainsDictionary(IEnumerable<KeyValuePair<string, TValue>> collection, IEqualityComparer<string>? comparer)
            : base(collection, new ContainsEqualityComparer()) { ((ContainsEqualityComparer)Comparer).parent = this; }
        public KeyContainsDictionary(int capacity, IEqualityComparer<string>? comparer)
            : base(capacity, new ContainsEqualityComparer()) { ((ContainsEqualityComparer)Comparer).parent = this; }
        class ContainsEqualityComparer : IEqualityComparer<string>
        {
            internal Dictionary<string, TValue>? parent;

            //internal ContainsEqualityComparer(Dictionary<string, TValue> dic) => parent = dic;

            public bool Equals(string? x, string? y)
            {
                if (x is null || y is null)
                    return false;
                return x.Contains(y) || y.Contains(x);
            }

            public int GetHashCode([DisallowNull] string obj)
            {
                foreach (string key in parent!.Keys)
                {
                    if (obj.Contains(key))
                        return key.GetHashCode();
                }
                return obj.GetHashCode();
            }
        }
    }
    
}
