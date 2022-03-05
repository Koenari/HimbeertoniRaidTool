using ImGuiScene;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static HimbeertoniRaidTool.Services;

namespace HimbeertoniRaidTool.Data
{
    public class GearItem
    {
        private static KeyContainsDictionary<GearSource> SourceDic => CuratedData.GearSourceDictionary;
        private GearSetSlot? SlotOverride => CuratedData.SlotOverrideDB.ContainsKey(_ID) ? CuratedData.SlotOverrideDB.GetValueOrDefault(_ID) : null;
        private uint _ID;
        public uint ID { get => _ID; set { _ID = value; UpdateStats(); } }
        [JsonIgnore]
        public TextureWrap? Icon => DataManager.GetImGuiTexture(DataManager.GetIcon(Item.Icon));
        [JsonIgnore]
        public Item Item { get; private set; } = new();
        [JsonIgnore]
        public string Name => (Item.Name ?? new("")).RawString;
        [JsonIgnore]
        public uint ItemLevel => (Item.LevelItem is null) ? 0 : Item.LevelItem.Row;
        [JsonIgnore]
        public GearSource Source { get; private set; } = GearSource.undefined;
        [JsonIgnore]
        public bool Filled => Name.Length > 0;
        [JsonIgnore]
        public bool Valid => ID > 0;
        [JsonIgnore]
        public GearSetSlot Slot => SlotOverride ?? (Item.EquipSlotCategory.Value?.ToSlot()) ?? GearSetSlot.None;
        private static ExcelSheet<Item> Sheet => DataManager.Excel.GetSheet<Item>()!;

        public GearItem() : this(0) { }

        public GearItem(uint idArg) => ID = idArg;

        private void UpdateStats()
        {
            if (_ID > 0)
            {
                Item = Sheet.GetRow(_ID) ?? (new Item());
                Source = SourceDic.GetValueOrDefault(Name, GearSource.undefined);
            }
        }
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj == null) return false;
            GearItem? other = obj as GearItem;
            if (other == null) return false;
            return _ID == other._ID;
        }
        public override int GetHashCode() => _ID.GetHashCode();
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
            : base(new ContainsEqualityComparer()) { ((ContainsEqualityComparer)Comparer).parent = this; }
        public KeyContainsDictionary(int capacity)
            : base(capacity, new ContainsEqualityComparer()) { ((ContainsEqualityComparer)Comparer).parent = this; }
        public KeyContainsDictionary(IDictionary<string, TValue> dictionary, IEqualityComparer<string>? comparer)
            : base(dictionary, new ContainsEqualityComparer()) { ((ContainsEqualityComparer)Comparer).parent = this; }
        public KeyContainsDictionary(IEnumerable<KeyValuePair<string, TValue>> collection, IEqualityComparer<string>? comparer)
            : base(collection, new ContainsEqualityComparer()) { ((ContainsEqualityComparer)Comparer).parent = this; }
        public KeyContainsDictionary(int capacity, IEqualityComparer<string>? comparer)
            : base(capacity, new ContainsEqualityComparer()) { ((ContainsEqualityComparer)Comparer).parent = this; }

        private class ContainsEqualityComparer : IEqualityComparer<string>
        {
            internal Dictionary<string, TValue>? parent;
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
