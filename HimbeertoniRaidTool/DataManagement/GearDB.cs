using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using Action = System.Action;

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
    public override void FixEntries()
    {
        foreach (GearSet dataValue in Data.Values.Where(dataValue => dataValue is { ManagedBy: GearSetManager.Etro, EtroId: "" }))
        {
            dataValue.ManagedBy = GearSetManager.Hrt;
        }
    }

    public override HashSet<HrtId> GetReferencedIds() => new (Data.Keys);
    public override HrtWindow OpenSearchWindow(Action<GearSet> onSelect, Action? onCancel = null) => new GearSearchWindow(this,onSelect,onCancel);

    private class GearSearchWindow : SearchWindow<GearSet, GearDb>
    {
        private string _name = string.Empty;
        private Job _job = Job.ADV;
        private int _iLvlMin = 0;
        private int _iLvlMax = 0;

        public GearSearchWindow(GearDb dataBase,Action<GearSet> onSelect, Action? onCancel) : base(dataBase,onSelect, onCancel)
        {
            Size = new Vector2(500, 400);
            SizeCondition = ImGuiCond.Appearing;
        }

        protected override void DrawContent()
        {
            /*
             * Selection
             */
            ImGui.Text(Services.Localization.Localize("GearDB:Search:Name", "Name"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150*ScaleFactor);
            ImGui.InputText("##Name", ref _name, 50);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(55*ScaleFactor);
            ImGuiHelper.Combo("##Job", ref _job);
            ImGui.SameLine();
            ImGui.Text(Services.Localization.Localize("GearDB:Search:iLvl","iLvl range:"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50*ScaleFactor);
            ImGui.InputInt("##minLvl", ref _iLvlMin,0);
            ImGui.SameLine();
            ImGui.Text("-");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50*ScaleFactor);
            ImGui.InputInt("##maxLvl", ref _iLvlMax,0);
            /*
             * List
             */
            foreach (GearSet gearSet in Database.Data.Values.Where(set =>
                         (_job == Job.ADV || set[GearSetSlot.MainHand].Jobs.Contains(_job))
                         && (_iLvlMin == 0 || set.ItemLevel > _iLvlMin)
                         && (_iLvlMax == 0 || set.ItemLevel < _iLvlMax)
                         && (_name.Length == 0 || set.Name.Contains(_name))))
            {
                ImGuiHelper.Button(FontAwesomeIcon.Check,$"{gearSet.LocalId}",Services.Localization.Localize("GearDB:Search:Select","Select gearset"));
                ImGui.SameLine();
                ImGui.Text($"{gearSet.Name} ({gearSet.ItemLevel}) {(gearSet.ManagedBy==GearSetManager.Etro?" from Etro":"")}");
                Save();
            }
        }
    }
}