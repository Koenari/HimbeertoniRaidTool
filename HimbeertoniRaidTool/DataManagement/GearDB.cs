using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
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

    internal GearDb(IIdProvider idProvider, ILogger logger) : base(
        idProvider, Array.Empty<JsonConverter>(), logger)
    {
    }
    public new bool Load(JsonSerializerSettings settings, string serializedData)
    {
        base.Load(settings, serializedData);
        if (LoadError)
            return false;
        foreach (var (id, set) in Data)
        {
            if (set.ManagedBy == GearSetManager.Etro)
                _etroLookup.TryAdd(set.ExternalId, id);
        }
        return IsLoaded & !LoadError;
    }
    internal bool TryGetSetByEtroId(string etroId, [NotNullWhen(true)] out GearSet? set)
    {
        if (_etroLookup.TryGetValue(etroId, out var id))
            return TryGet(id, out set);
        id = Data.FirstOrDefault(s => s.Value.ExternalId == etroId).Key;
        if (id is not null)
        {
            _etroLookup.Add(etroId, id);
            set = Data[id];
            return true;
        }
        set = null;
        return false;
    }
    public override void FixEntries(HrtDataManager _)
    {
        foreach (var dataValue in Data.Values.Where(dataValue =>
                                                        dataValue is
                                                            { ManagedBy: GearSetManager.Etro, ExternalId: "" }))
        {
            dataValue.ManagedBy = GearSetManager.Hrt;
        }
    }

    public override HashSet<HrtId> GetReferencedIds() => new(Data.Keys);
    public override HrtWindow OpenSearchWindow(IUiSystem uiSystem, Action<GearSet> onSelect, Action? onCancel = null) =>
        new GearSearchWindow(uiSystem, this, onSelect, onCancel);

    private class GearSearchWindow : SearchWindow<GearSet, GearDb>
    {
        private int _iLvlMax;
        private int _iLvlMin;
        private Job _job = Job.ADV;
        private GearSetManager _manager = GearSetManager.Unknown;
        private string _searchTerm = string.Empty;

        public GearSearchWindow(IUiSystem uiSystem, GearDb dataBase, Action<GearSet> onSelect, Action? onCancel) : base(
            uiSystem, dataBase, onSelect, onCancel)
        {
            Size = new Vector2(800, 400);
            SizeCondition = ImGuiCond.Appearing;
            Title = GeneralLoc.DBSearchWindowGear_Title;
        }

        protected override void DrawContent()
        {
            /*
             * Selection
             */
            ImGui.Text("Search");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(150 * ScaleFactor);
            ImGui.InputText("##Name", ref _searchTerm, 50);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(55 * ScaleFactor);
            ImGuiHelper.Combo("##Job", ref _job);
            ImGui.SameLine();
            ImGui.Text("Service");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100 * ScaleFactor);
            ImGuiHelper.Combo("##Service", ref _manager);
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
            var table = ImRaii.Table("Sets", 7, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit);
            if (!table) return;
            ImGui.TableSetupColumn("");
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("iLvl");
            ImGui.TableSetupColumn("Jobs");
            ImGui.TableSetupColumn("ID");
            ImGui.TableSetupColumn("Service");
            ImGui.TableSetupColumn("External ID");
            ImGui.TableHeadersRow();
            foreach (var gearSet in Database.Data.Values.Where(set =>
                                                                   (_job == Job.ADV || set[GearSetSlot.MainHand]
                                                                       .Jobs.Contains(_job))
                                                                && (_manager == GearSetManager.Unknown
                                                                 || set.ManagedBy == _manager)
                                                                && (_iLvlMin == 0 || set.ItemLevel > _iLvlMin)
                                                                && (_iLvlMax == 0 || set.ItemLevel < _iLvlMax)
                                                                && (_searchTerm.Length == 0
                                                                 || set.Name.Contains(
                                                                        _searchTerm,
                                                                        StringComparison.InvariantCultureIgnoreCase)
                                                                 || set.LocalId.ToString()
                                                                       .Contains(_searchTerm,
                                                                           StringComparison.InvariantCultureIgnoreCase)
                                                                 || set.ExternalId.ToString()
                                                                       .Contains(_searchTerm,
                                                                           StringComparison.InvariantCultureIgnoreCase)
                                                                 || set.ManagedBy.FriendlyName()
                                                                       .Contains(_searchTerm,
                                                                           StringComparison.InvariantCultureIgnoreCase)
                                                                 || set.ItemLevel.ToString().Contains(_searchTerm)
                                                                 || set[GearSetSlot.MainHand].Jobs
                                                                        .Any(s => s.ToString()
                                                                                 .Contains(_searchTerm,
                                                                                     StringComparison
                                                                                         .InvariantCultureIgnoreCase)
                                                                        ))))
            {
                ImGui.TableNextColumn();
                if (ImGuiHelper.Button(FontAwesomeIcon.Check, $"{gearSet.LocalId}",
                                       string.Format(GeneralLoc.SearchWindow_btn_tt_SelectEnty,
                                                     GearSet.DataTypeNameStatic, gearSet)))
                {
                    Selected = gearSet;
                    Save();
                }
                ImGui.TableNextColumn();
                ImGui.Text(
                    $"{gearSet.Alias ?? gearSet.Name} {(gearSet.Alias is null ? string.Empty : $"({gearSet.Name})")}");
                ImGui.TableNextColumn();
                ImGui.Text($"{gearSet.ItemLevel}");
                ImGui.TableNextColumn();
                ImGui.Text($"{string.Join(',', gearSet[GearSetSlot.MainHand].Jobs)}");
                ImGui.TableNextColumn();
                ImGui.Text($"{gearSet.LocalId}");
                ImGui.TableNextColumn();
                ImGui.Text($"{gearSet.ManagedBy.FriendlyName()}");
                ImGui.TableNextColumn();
                ImGui.Text($"{gearSet.ExternalId}");
            }
            table.Dispose();
        }
    }
}