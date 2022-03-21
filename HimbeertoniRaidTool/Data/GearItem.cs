using ImGuiScene;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HimbeertoniRaidTool.Data
{
    public class GearItem : HrtItem
    {
        private static KeyContainsDictionary<GearSource> SourceDic => CuratedData.GearSourceDictionary;
        private GearSetSlot? SlotOverride => CuratedData.SlotOverrideDB.ContainsKey(_ID) ? CuratedData.SlotOverrideDB.GetValueOrDefault(_ID) : null;
        [JsonIgnore]
        public GearSetSlot Slot => SlotOverride ?? (Item.EquipSlotCategory.Value?.ToSlot()) ?? GearSetSlot.None;
        [JsonIgnore]
        public GearSource Source => _ID > 0 ? SourceDic.GetValueOrDefault(Name, GearSource.undefined) : GearSource.undefined;

        public List<Materia> Materia = new();

        public int GetStat(StatType type)
        {
            if (_ID == 0 || Item.Name is null) return 0;
            switch (type)
            {
                case StatType.PhysicalDamage: return Item.DamagePhys;
                case StatType.MagicalDamage: return Item.DamageMag;
                case StatType.Defense: return Item.DefensePhys;
                case StatType.MagicDefense: return Item.DefenseMag;
            }
            if (Item?.UnkData59 is null)
                return 0;
            foreach (Item.ItemUnkData59Obj param in Item.UnkData59)
            {
                if (param.BaseParam == (ushort)type)
                    return param.BaseParamValue;
            }
            return 0;
        }
        public GearItem() : base() { }
        public GearItem(uint id) : base(id) { }
    }
    public class HrtItem
    {
        protected uint _ID;
        public uint ID { get => _ID; set { _ID = value; } }
        [JsonIgnore]
        public TextureWrap? Icon => Services.DataManager.GetImGuiTextureIcon(Item.Icon);
        [JsonIgnore]
        public Item Item => Sheet.GetRow(_ID) ?? new Item();
        [JsonIgnore]
        public string Name => _ID > 0 ? Item.Name.RawString : "";
        [JsonIgnore]
        public uint ItemLevel => (Item.LevelItem is null) ? 0 : Item.LevelItem.Row;

        [JsonIgnore]
        public bool Filled => _ID > 0 && Name.Length > 0;
        [JsonIgnore]
        public bool Valid => ID > 0;

        protected static ExcelSheet<Item> Sheet => Services.DataManager.Excel.GetSheet<Item>()!;

        public HrtItem() : this(0) { }

        public HrtItem(uint idArg) => ID = idArg;

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

    public class Materia : HrtItem
    {

        public Materia() : this(0) { }

        public Materia(uint idArg) : base(idArg) { }
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
