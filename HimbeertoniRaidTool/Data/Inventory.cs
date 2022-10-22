using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Data
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal class Inventory
    {
        [JsonProperty("Wallet", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        private readonly Dictionary<Currency, int> _wallet = new();

        [JsonProperty("ItemTypes", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        private readonly Dictionary<int, Type> _itemTypes = new();

        [JsonProperty("Gear", TypeNameHandling = TypeNameHandling.All, ObjectCreationHandling = ObjectCreationHandling.Replace)]
        private readonly Dictionary<int, GearItem> _gear = new();

        [JsonProperty("Items", TypeNameHandling = TypeNameHandling.All, ObjectCreationHandling = ObjectCreationHandling.Replace)]
        private readonly Dictionary<int, HrtItem> _items = new();

        private bool Remove(int key)
        {
            if(!_itemTypes.Remove(key, out Type? t))
                return false;
            if(t == typeof(GearItem))
                return _gear.Remove(key);
            else if(t == typeof(HrtItem))
                return _items.Remove(key);
            else
                return false;
        }
        private bool Set<T>(int key, T item) where T: HrtItem
        {
            Type t = typeof(T);
            if(_itemTypes.TryGetValue(key, out Type? t2))
            {
                if(t2 != t)
                {
                    _itemTypes[key] = t;
                    if(t2 == typeof(GearItem))
                        _gear.Remove(key);
                    else if(t2 == typeof(HrtItem))
                        _items.Remove(key);
                }
            }
            else
            {
                _itemTypes.Add(key, t);
            }
            if (item is GearItem item1)
                _gear[key]= item1;
            else if (item is HrtItem item2)
                _items[key] = item2;
            return true;
        }

        public int this[Currency c] 
        {
            get =>  _wallet.TryGetValue(c, out int val) ? val : _wallet[c] = 0;
            set => _wallet[c] = value < 0 ? 0 : value;
        }
        public HrtItem? this[int key]
        {
            get => _items.TryGetValue(key, out HrtItem? val) ? val : null;
            set 
            {   if (value is not null)
                    Set(key,value);
                else
                    Remove(key);
            }
        }
        public bool TryGet<T>(int key,[NotNullWhen(true)] out T? val) where T : HrtItem
        {
            val = null;
            if(!_items.TryGetValue(key,out HrtItem? fromDic))
                return false;
            val = fromDic as T;
            return val is not null;
        }
        public bool Contains<T>(T item) where T : HrtItem
            => Contains(item.ID);
        public bool Contains(uint id) => _items.Values.Any(i => i.ID == id);
    }
    internal class Currency
    {

    }
}
