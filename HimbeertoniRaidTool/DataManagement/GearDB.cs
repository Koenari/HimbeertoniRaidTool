using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Dalamud.Interface;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;
using Action = System.Action;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class GearDb : DataBaseTable<GearSet>
{
    private readonly Dictionary<string, HrtId> _etroLookup = new();

    internal GearDb(IIdProvider idProvider) : base(
        idProvider, Array.Empty<JsonConverter>())
    {
    }
    public new bool Load(JsonSerializerSettings settings, string serializedData)
    {
        base.Load(settings, serializedData);
        if (LoadError)
            return false;
        foreach ((HrtId id, GearSet set) in Data)
        {
            if (set.ManagedBy == GearSetManager.Etro)
                _etroLookup.TryAdd(set.EtroId, id);
        }
        return IsLoaded & !LoadError;
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
        foreach (GearSet dataValue in Data.Values.Where(dataValue =>
                                                            dataValue is
                                                                { ManagedBy: GearSetManager.Etro, EtroId: "" }))
        {
            dataValue.ManagedBy = GearSetManager.Hrt;
        }
    }

    public override HashSet<HrtId> GetReferencedIds() => new(Data.Keys);
    public override HrtWindow OpenSearchWindow(Action<GearSet> onSelect, Action? onCancel = null) =>
        new GearSearchWindow(this, onSelect, onCancel);

    private class GearSearchWindow : SearchWindow<GearSet, GearDb>
    {
        private int _iLvlMax;
        private int _iLvlMin;
        private Job _job = Job.ADV;
        private string _name = string.Empty;

        public GearSearchWindow(GearDb dataBase, Action<GearSet> onSelect, Action? onCancel) : base(
            dataBase, onSelect, onCancel)
        {
            Size = new Vector2(500, 400);
            SizeCondition = ImGuiCond.Appearing;
            Title = GeneralLoc.DBSearchWindowGear_Title;
        }

        protected override void DrawContent()
        {
            /*
             * Selection
             */
            ImGui.Text(GeneralLoc.CommonTerms_Name);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150 * ScaleFactor);
            ImGui.InputText("##Name", ref _name, 50);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(55 * ScaleFactor);
            ImGuiHelper.Combo("##Job", ref _job);
            ImGui.SameLine();
            ImGui.Text(LootmasterLoc.GearSetSearchWindow_iLvlRange);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50 * ScaleFactor);
            ImGui.InputInt("##minLvl", ref _iLvlMin, 0);
            ImGui.SameLine();
            ImGui.Text("-");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50 * ScaleFactor);
            ImGui.InputInt("##maxLvl", ref _iLvlMax, 0);
            /*
             * List
             */
            foreach (GearSet gearSet in Database.Data.Values.Where(set =>
                                                                       (_job == Job.ADV || set[GearSetSlot.MainHand]
                                                                           .Jobs.Contains(_job))
                                                                    && (_iLvlMin == 0 || set.ItemLevel > _iLvlMin)
                                                                    && (_iLvlMax == 0 || set.ItemLevel < _iLvlMax)
                                                                    && (_name.Length == 0 || set.Name.Contains(_name))))
            {
                if (ImGuiHelper.Button(FontAwesomeIcon.Check, $"{gearSet.LocalId}",
                                       LootmasterLoc.GearSetSearchWindow_button_Select))
                {
                    Selected = gearSet;
                    Save();
                }
                ImGui.SameLine();
                ImGui.Text(
                    $"{gearSet.Name} ({gearSet.ItemLevel}) {(gearSet.ManagedBy == GearSetManager.Etro ? " from Etro" : "")}");
            }
        }
    }
}