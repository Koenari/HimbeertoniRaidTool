using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ImGuiScene;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class GearItem : HrtItem
    {
        private static KeyContainsDictionary<GearSource> SourceDic => CuratedData.GearSourceDictionary;
        private GearSetSlot? SlotOverride => CuratedData.SlotOverrideDB.ContainsKey(_ID) ? CuratedData.SlotOverrideDB.GetValueOrDefault(_ID) : null;
        [JsonIgnore]
        public GearSetSlot Slot => SlotOverride ?? (Item.EquipSlotCategory.Value?.ToSlot()) ?? GearSetSlot.None;
        [JsonIgnore]
        public GearSource Source => _ID > 0 ? SourceDic.GetValueOrDefault(Name, GearSource.undefined) : GearSource.undefined;
        [JsonProperty("Materia")]
        public List<HrtMateria> Materia = new();
        [JsonIgnore]
        public uint ItemLevel => (Item.LevelItem is null) ? 0 : Item.LevelItem.Row;
        public int GetStat(StatType type, bool includeMateria = true)
        {
            int result = 0;
            if (_ID == 0 || Item.Name is null) return 0;
            switch (type)
            {
                case StatType.PhysicalDamage: result += Item.DamagePhys; break;
                case StatType.MagicalDamage: result += Item.DamageMag; break;
                case StatType.Defense: result += Item.DefensePhys; break;
                case StatType.MagicDefense: result += Item.DefenseMag; break;
                default:
                    if (Item?.UnkData59 is not null)
                        foreach (Item.ItemUnkData59Obj param in Item.UnkData59)
                            if (param.BaseParam == (ushort)type)
                                result += param.BaseParamValue;
                    break;
            }
            if (includeMateria)
                foreach (HrtMateria materia in Materia)
                    result += materia.GetStat(type);
            return result;
        }
        public GearItem() : base() { }
        [JsonConstructor]
        public GearItem(uint id) : base(id) { }
        public bool Equals(GearItem other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (ID != other.ID) return false;
            if (Materia.Count != other.Materia.Count) return false;
            var cnt = new Dictionary<HrtMateria, int>();
            foreach (HrtMateria s in Materia)
            {
                if (cnt.ContainsKey(s))
                    cnt[s]++;
                else
                    cnt.Add(s, 1);
            }
            foreach (HrtMateria s in other.Materia)
            {
                if (cnt.ContainsKey(s))
                    cnt[s]--;
                else
                    return false;
            }
            foreach (int s in cnt.Values)
                if (s != 0)
                    return false;
            return true;
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class HrtItem
    {
        protected uint _ID;
        [JsonProperty("ID")]
        public virtual uint ID { get => _ID; set { _ID = value; } }
        public TextureWrap? Icon => Services.DataManager.GetImGuiTextureIcon(Item.Icon);
        public Item Item => Sheet?.GetRow(ID) ?? new Item();
        public string Name => ID > 0 ? Item.Name.RawString : "";
        public bool Filled => ID > 0 && Name.Length > 0;
        public bool Valid => ID > 0;

        protected static ExcelSheet<Item>? Sheet => Services.DataManager.Excel.GetSheet<Item>();

        public HrtItem() : this(0) { }

        public HrtItem(uint idArg) => ID = idArg;

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj is not HrtItem other) return false;
            return ID == other.ID;
        }
        public override int GetHashCode() => ID.GetHashCode();
    }
    [JsonObject(MemberSerialization.OptIn)]
    public class HrtMateria : HrtItem
    {
        [JsonProperty("Category")]
        public MateriaCategory Category;
        [JsonProperty("MateriaLevel")]
        public byte MateriaLevel;
        private ExcelSheet<Materia> MateriaSheet => Services.DataManager.Excel.GetSheet<Materia>()!;
        public override uint ID => Category != MateriaCategory.None ? Materia?.Item[MateriaLevel].Row ?? 0 : 0;
        public Materia? Materia => MateriaSheet.GetRow((ushort)Category);
        public HrtMateria() : this(0, 0) { }
        public HrtMateria((MateriaCategory cat, byte lvl) mat) : this(mat.cat, mat.lvl) { }
        [JsonConstructor]
        public HrtMateria(MateriaCategory cat, byte lvl) => (Category, MateriaLevel) = (cat, lvl);
        public int GetStat(StatType type)
        {
            if (!Valid || Materia is null) return 0;
            if (Category.GetStatType() == type)
                return Materia.Value[MateriaLevel];
            return 0;
        }
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
