using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class RaidGroupDb(IIdProvider idProvider, IEnumerable<JsonConverter> converters, ILogger logger)
    : DataBaseTable<RaidGroup>(idProvider, converters, logger)
{

    public override HashSet<HrtId> GetReferencedIds()
    {
        HashSet<HrtId> referencedIds = [];
        foreach (var p in from g in Data.Values from player in g select player)
        {
            referencedIds.Add(p.LocalId);
        }
        return referencedIds;
    }
    public override HrtWindow
        GetSearchWindow(IUiSystem uiSystem, Action<RaidGroup> onSelect, Action? onCancel = null) =>
        new GroupSearchWindow(uiSystem, this, onSelect, onCancel);

    private class GroupSearchWindow : SearchWindow<RaidGroup, RaidGroupDb>
    {
        private string _searchText = "";
        public GroupSearchWindow(IUiSystem uiSystem,
                                 RaidGroupDb dataBase,
                                 Action<RaidGroup> onSelect,
                                 Action? onCancel) : base(uiSystem, dataBase,
                                                          onSelect, onCancel)
        {
            SizeCondition = ImGuiCond.Appearing;
            Size = new Vector2(300, 300);
        }
        protected override void DrawContent()
        {
            ImGui.Text(GeneralLoc.CommonTerms_Name);
            ImGui.SameLine();
            ImGui.InputText("##searchTerm", ref _searchText, 128);

            using var resultTable = ImRaii.Table("Groups", 3,
                                                 ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg
                                                                         | ImGuiTableFlags.SizingStretchProp);
            if (!resultTable) return;
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(GeneralLoc.CommonTerms_Name, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(GeneralLoc.EditGroupUi_in_type, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();
            foreach (var group in Database.GetValues()
                                          .Where(e => e.Name.Contains(_searchText,
                                                                      StringComparison.InvariantCultureIgnoreCase)))
            {
                ImGui.TableNextColumn();
                if (ImGuiHelper.Button(FontAwesomeIcon.Check, $"{group.LocalId}",
                                       string.Format(GeneralLoc.SearchWindow_btn_tt_SelectEnty, RaidGroup.DataTypeName,
                                                     group)))
                {
                    Selected = group;
                    Save();
                }
                ImGui.TableNextColumn();
                ImGui.Text(group.Name);
                ImGui.TableNextColumn();
                ImGui.Text(group.Type.FriendlyName());
            }
        }

    }
}