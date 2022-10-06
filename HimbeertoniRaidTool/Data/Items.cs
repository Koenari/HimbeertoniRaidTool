using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Lumina.Excel;
using Lumina.Excel.Extensions;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class GearItem : HrtItem
    {
        [JsonProperty]
        public bool IsHq = false;
        private static KeyContainsDictionary<GearSource> SourceDic => CuratedData.GearSourceDictionary;
        [JsonIgnore]
        public GearSetSlot Slot => Item?.EquipSlotCategory.Value?.ToSlot() ?? GearSetSlot.None;
        [JsonIgnore]
        public GearSource Source => SourceDic.GetValueOrDefault(Name, GearSource.undefined);
        [JsonProperty("Materia")]
        public List<HrtMateria> Materia = new();
        [JsonIgnore]
        public uint ItemLevel => Item?.LevelItem.Row ?? 0;
        public bool Filled => ID > 0;
        public int GetStat(StatType type, bool includeMateria = true)
        {
            if (Item is null) return 0;
            int result = 0;
            switch (type)
            {
                case StatType.PhysicalDamage: result += Item.DamagePhys; break;
                case StatType.MagicalDamage: result += Item.DamageMag; break;
                case StatType.Defense: result += Item.DefensePhys; break;
                case StatType.MagicDefense: result += Item.DefenseMag; break;
                default:
                    if (IsHq)
                        foreach (Item.ItemUnkData73Obj param in Item.UnkData73.Where(x => x.BaseParamSpecial == (byte)type))
                            result += param.BaseParamValueSpecial;

                    foreach (Item.ItemUnkData59Obj param in Item.UnkData59.Where(x => x.BaseParam == (byte)type))
                        result += param.BaseParamValue;
                    break;
            }
            if (includeMateria)
                foreach (HrtMateria materia in Materia.Where(x => x.StatType == type))
                    result += materia.GetStat();
            return result;
        }
        public GearItem(uint ID = 0) : base(ID) { }
        public bool Equals(GearItem other)
        {
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
        [JsonProperty("ID", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        protected readonly uint _ID = 0;
        public virtual uint ID => _ID;
        public Item? Item => _itemSheet.GetRow(ID);
        public string Name => Item?.Name.RawString ?? "";
        public bool IsGear => Item?.EquipSlotCategory.Value is not null;
        /// <summary>
        /// Is done this way since HrtMateria cannot be created from ItemID alone 
        /// and always will be of type HrtMateria
        /// </summary>
        public bool IsMateria => GetType().IsAssignableTo(typeof(HrtMateria));
        public bool IsExhangableItem => CuratedData.ExchangedFor.ContainsKey(ID);
        public bool IsContainerItem => CuratedData.ItemContainerDB.ContainsKey(ID);
        [JsonIgnore]
        protected static readonly ExcelSheet<Item> _itemSheet = Services.DataManager.Excel.GetSheet<Item>()!;

        public HrtItem(uint ID) => _ID = ID;

        public override bool Equals(object? obj)
        {
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
        private readonly MateriaCategory Category;
        [JsonProperty("MateriaLevel")]
        public readonly byte MateriaLevel;
        [JsonIgnore]
        private static readonly ExcelSheet<Materia> _materiaSheet = Services.DataManager.Excel.GetSheet<Materia>()!;
        [JsonIgnore]
        public override uint ID => Materia?.Item[MateriaLevel].Row ?? 0;
        public Materia? Materia => _materiaSheet.GetRow((ushort)Category);
        public StatType StatType => (StatType)(Materia?.BaseParam.Row ?? 0);
        public HrtMateria() : this(0, 0) { }
        public HrtMateria((MateriaCategory cat, byte lvl) mat) : this(mat.cat, mat.lvl) { }
        [JsonConstructor]
        public HrtMateria(MateriaCategory cat, byte lvl) : base(0) => (Category, MateriaLevel) = (cat, lvl);


        public int GetStat() => Materia?.Value[MateriaLevel] ?? 0;
    }
    /// <summary>
    /// Models an item that can be exchanged for another item
    /// Items with FilterGroup 16??
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ExchangableItem : HrtItem
    {
        public List<GearItem> PossiblePurchases =>
            CuratedData.ExchangedFor.GetValueOrDefault(_ID)?.AsList.ConvertAll(id => new GearItem(id))
            ?? new();

        public ExchangableItem(uint id) : base(id) { }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ContainerItem : HrtItem
    {
        public List<GearItem> PossiblePurchases =>
            CuratedData.ItemContainerDB.GetValueOrDefault(_ID)?.AsList.ConvertAll(id => new GearItem(id))
            ?? new();

        public ContainerItem(uint id) : base(id) { }
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
    public class ItemIDRange : ItemIDCollection
    {
        public static implicit operator ItemIDRange(uint id) => new(id, id);
        public static implicit operator ItemIDRange((uint, uint) id) => new(id.Item1, id.Item2);
        private readonly uint StartID;
        private readonly uint EndID;
        private bool InRange(uint id) => StartID <= id && id <= EndID;
        public List<uint> AsList => Enumerable.Range((int)StartID, (int)(EndID - StartID + 1)).ToList().ConvertAll(x => (uint)x);
        public ItemIDRange(uint start, uint end) => (StartID, EndID) = (start, end);
        public override bool Equals(object? obj)
        {
            if (!obj?.GetType().IsAssignableTo(typeof(ItemIDRange)) ?? false)
                return false;
            return Equals((ItemIDRange)obj!);
        }
        public bool Contains(uint obj) => InRange(obj);
        public bool Equals(ItemIDRange obj) => StartID == obj.StartID && EndID == obj.EndID;
        public override int GetHashCode() => (StartID, EndID).GetHashCode();

    }
    public class ItemIDList : ItemIDCollection
    {
        private readonly ReadOnlyCollection<uint> _IDs;
        public static implicit operator ItemIDList(uint[] ids) => new(ids);
        public List<uint> AsList => _IDs.ToList();
        public bool Contains(uint id) => _IDs.Contains(id);
        public ItemIDList(params uint[] ids)
        {
            _IDs = new ReadOnlyCollection<uint>(ids);
        }
    }
    public interface ItemIDCollection
    {
        public abstract List<uint> AsList { get; }
        public abstract bool Contains(uint id);
    }

}
