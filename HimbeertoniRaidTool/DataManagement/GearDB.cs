using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class GearDb : DataBaseTable<GearSet, GearSet>
{
    private readonly Dictionary<string, HrtId> _etroLookup = new();

    internal GearDb(IIdProvider idProvider, string gearData, JsonSerializerSettings settings) : base(idProvider, gearData, null, settings)
    {
        if (LoadError)
            return;
        foreach ((HrtId id,GearSet set) in Data)
        {
            if (set.ManagedBy == GearSetManager.Etro)
                _etroLookup.TryAdd(set.EtroId, id);
        }


    }
    internal bool TryGetSetByEtroId(string etroId, [NotNullWhen(true)] out GearSet? set)
    {
        if (_etroLookup.TryGetValue(etroId, out HrtId? id))
            return TryGet(id, out set);
        id = Data.FirstOrDefault(s => s.Value.EtroId == etroId).Key;
        if (id is not null)
        {
            _etroLookup.Add(etroId, id);
            set = Data[id];
            return true;
        }
        set = null;
        return false;
    }
    
    internal void Prune(CharacterDb charDb)
    {
        ServiceManager.PluginLog.Debug("Begin pruning of gear database.");
        foreach (HrtId toPrune in charDb.FindOrphanedGearSets(Data.Keys))
        {
            if (!Data.TryGetValue(toPrune, out GearSet? set)) continue;
            ServiceManager.PluginLog.Information($"Removed {set.Name} ({set.LocalId}) from DB");
            Data.Remove(toPrune);
        }
        ServiceManager.PluginLog.Debug("Finished pruning of gear database.");
    }

    public override HrtWindow OpenSearchWindow(Action<GearSet> onSelect, Action? onCancel = null)
    {
        return new GearSearchWindow(this,onSelect,onCancel);
    }

    private class GearSearchWindow : SearchWindow<GearSet, GearDb>
    {
        public GearSearchWindow(GearDb dataBase,Action<GearSet> onSelect, Action? onCancel) : base(dataBase,onSelect, onCancel)
        {
        }

        protected override void DrawContent()
        {
            throw new NotImplementedException();
        }
    }
}