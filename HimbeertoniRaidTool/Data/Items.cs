using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Lumina.Excel;
using Lumina.Excel.Extensions;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class GearItem : HrtItem, IEquatable<GearItem>
    {
        [JsonProperty]
        public bool IsHq = false;
        [JsonIgnore]
        [Obsolete("Evaluate for all availbale slots")]
        public GearSetSlot Slot => (Item?.EquipSlotCategory.Value).ToSlot();
        [JsonIgnore]
        public List<Job> Jobs => Item?.ClassJobCategory.Value?.ToJob() ?? new List<Job>();
        [JsonIgnore]
        public IEnumerable<GearSetSlot> Slots => (Item?.EquipSlotCategory.Value).AvailableSlots();
        [JsonProperty("Materia")]
        public List<HrtMateria> Materia = new();
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
        public bool Equals(GearItem? other) => Equals(other, ItemComparisonMode.Full);
        public bool Equals(GearItem? other, ItemComparisonMode mode)
        {
            //idOnly
            if (ID != other?.ID) return false;
            if (mode == ItemComparisonMode.IdOnly) return true;
            //IgnoreMateria
            if (IsHq != other.IsHq) return false;
            if (mode == ItemComparisonMode.IgnoreMateria) return true;
            //Full
            if (Materia.Count != other.Materia.Count) return false;
            Dictionary<HrtMateria, int> cnt = new();
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
    public class HrtItem : IEquatable<HrtItem>
    {
        [JsonProperty("ID", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        protected readonly uint _ID = 0;
        public virtual uint ID => _ID;
        private Item? ItemCache = null;
        public Item? Item => ItemCache ??= _itemSheet.GetRow(ID);
        public string Name => Item?.Name.RawString ?? "";
        public bool IsGear => this is GearItem || (Item?.ClassJobCategory.Row ?? 0) != 0;
        public ItemSource Source => Services.ItemInfo.GetSource(this);
        [JsonIgnore]
        public uint? ILevelCache = null;
        [JsonIgnore]
        public uint ItemLevel => ILevelCache ??= Item?.LevelItem.Row ?? 0;
        public bool Filled => ID > 0;
        public string SourceShortName
        {
            get
            {
                if (Source == ItemSource.Loot && Services.ItemInfo.CanBeLooted(ID))
                    return Services.ItemInfo.GetLootSources(ID).First().InstanceType.FriendlyName();
                return Source.FriendlyName();
            }
        }
        public bool IsExchangableItem => Services.ItemInfo.UsedAsShopCurrency(ID);
        public bool IsContainerItem => Services.ItemInfo.IsItemContainer(ID);
        public IEnumerable<GearItem> PossiblePurchases
        {
            get
            {
                if (IsExchangableItem)
                    foreach (uint canBuy in Services.ItemInfo.GetPossiblePurchases(ID))
                        yield return new GearItem(canBuy);
                if (IsContainerItem)
                    foreach (uint id in Services.ItemInfo.GetContainerContents(ID))
                        yield return new GearItem(id);
            }
        }
        [JsonIgnore]
        protected static readonly ExcelSheet<Item> _itemSheet = Services.DataManager.Excel.GetSheet<Item>()!;

        public HrtItem(uint ID) => _ID = ID;

        public bool Equals(HrtItem? obj)
        {
            return ID == obj?.ID;
        }
        public override int GetHashCode() => ID.GetHashCode();
    }
    [JsonObject(MemberSerialization.OptIn)]
    public class HrtMateria : HrtItem, IEquatable<HrtMateria>
    {
        [JsonProperty("Category")]
        private readonly MateriaCategory Category;
        [JsonProperty("MateriaLevel")]
        public readonly byte MateriaLevel;
        [JsonIgnore]
        private static readonly ExcelSheet<Materia> _materiaSheet = Services.DataManager.Excel.GetSheet<Materia>()!;
        private uint? IDCache = null;
        [JsonIgnore]
        public override uint ID => IDCache ??= Materia?.Item[MateriaLevel].Row ?? 0;
        public Materia? Materia => _materiaSheet.GetRow((ushort)Category);
        public StatType StatType => Category.GetStatType();
        public HrtMateria() : this(0, 0) { }
        public HrtMateria((MateriaCategory cat, byte lvl) mat) : this(mat.cat, mat.lvl) { }
        [JsonConstructor]
        public HrtMateria(MateriaCategory cat, byte lvl) : base(0) => (Category, MateriaLevel) = (cat, lvl);
        public int GetStat() => Materia?.Value[MateriaLevel] ?? 0;
        public bool Equals(HrtMateria? other) => base.Equals(other);
    }
    public class ItemIDRange : ItemIDCollection
    {
        public ItemIDRange(uint start, uint end) : base(Enumerable.Range((int)start, Math.Max(0, (int)end - (int)start + 1)).ToList().ConvertAll(x => (uint)x)) { }
    }
    public class ItemIDList : ItemIDCollection
    {
        public static implicit operator ItemIDList(uint[] ids) => new(ids);

        public ItemIDList(params uint[] ids) : base(ids) { }
        public ItemIDList(ItemIDCollection col, params uint[] ids) : base(col.Concat(ids)) { }
        public ItemIDList(IEnumerable<uint> ids) : base(ids) { }
    }
    public abstract class ItemIDCollection : IEnumerable<uint>
    {
        public static ItemIDCollection Empty = new ItemIDList();
        private readonly ReadOnlyCollection<uint> _IDs;
        public int Count => _IDs.Count;
        protected ItemIDCollection(IEnumerable<uint> ids) => _IDs = new(ids.ToList());
        public IEnumerator<uint> GetEnumerator() => _IDs.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _IDs.GetEnumerator();
        public static implicit operator ItemIDCollection(uint id) => new ItemIDList(id);
        public static implicit operator ItemIDCollection((uint, uint) id) => new ItemIDRange(id.Item1, id.Item2);
    }

    public enum ItemComparisonMode
    {
        /// <summary>
        /// Ignores everything besides the item ID
        /// </summary>
        IdOnly,
        /// <summary>
        /// Ignores affixed materia when comparing
        /// </summary>
        IgnoreMateria,
        /// <summary>
        /// Compares all aspects of the item
        /// </summary>
        Full
    }
}
