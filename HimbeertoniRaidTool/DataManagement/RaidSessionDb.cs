using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;
using Serilog;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class RaidSessionDb(IIdProvider idProvider, IEnumerable<JsonConverter> converters, ILogger logger)
    : DataBaseTable<RaidSession>(idProvider, converters, logger)
{

    public override HrtWindow GetSearchWindow(IUiSystem uiSystem, Action<RaidSession> onSelect, Action? onCancel = null)
        => new RaidSessionSearchWindow(uiSystem, this, onSelect, onCancel);

    public override HashSet<HrtId> GetReferencedIds()
    {
        HashSet<HrtId> result = new();
        foreach (var raidSession in Data.Values)
        {
            if (raidSession.Group is not null) result.Add(raidSession.Group.LocalId);
            foreach (var participant in raidSession.Participants)
            {
                result.Add(participant.Player.Data.LocalId);
            }
        }
        return result;
    }

    private class RaidSessionSearchWindow : SearchWindow<RaidSession, RaidSessionDb>
    {
        public RaidSessionSearchWindow(IUiSystem uiSystem,
                                       RaidSessionDb db,
                                       Action<RaidSession> onSelect,
                                       Action? onCancel) : base(uiSystem, db, onSelect, onCancel)
        {
            SizeCondition = ImGuiCond.Appearing;
            Size = new Vector2(500, 300);
        }

        private string _searchTextTitle = "";
        private RaidGroup? _searchGroup = null;

        protected override void DrawContent()
        {
            ImGui.Text("Title");
            ImGui.SameLine();
            ImGui.InputText("##searchTitle", ref _searchTextTitle, 128);
            ImGui.Text("Group");
            ImGui.SameLine();
            using (var combo = ImRaii.Combo("Group", _searchGroup?.Name ?? GeneralLoc.GeneralTerm_Show_all))
            {
                if (combo)
                {
                    if (ImGui.Selectable(GeneralLoc.GeneralTerm_Show_all, _searchGroup is null))
                    {
                        _searchGroup = null;
                    }
                    foreach (var group in UiSystem.GetHrtDataManager().RaidGroupDb.GetValues())
                    {
                        if (ImGui.Selectable($"{group.Name} ({group.Count})##{group.LocalId}", group == _searchGroup))
                        {
                            _searchGroup = group;
                        }
                    }
                }
            }

            using var resultTable = ImRaii.Table("Sessions", 5,
                                                 ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg
                                                                         | ImGuiTableFlags.SizingStretchProp);
            if (!resultTable) return;
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Title", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Participants", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Duration", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();
            foreach (var session in Database.GetValues()
                                            .Where(e => e.Title.Contains(_searchTextTitle,
                                                                         StringComparison.InvariantCultureIgnoreCase)
                                                     && (_searchGroup is null || e.Group == _searchGroup)))
            {
                ImGui.TableNextColumn();
                if (ImGuiHelper.Button(FontAwesomeIcon.Check, $"{session.LocalId}",
                                       string.Format(GeneralLoc.SearchWindow_btn_tt_SelectEnty, RaidGroup.DataTypeName,
                                                     session)))
                {
                    Selected = session;
                    Save();
                }
                ImGui.TableNextColumn();
                ImGui.Text(session.Name);
                ImGui.TableNextColumn();
                ImGui.Text($"{session.NumParticipants}");
                ImGui.TableNextColumn();
                ImGui.Text($"{session.StartTime:g}");
                ImGui.TableNextColumn();
                ImGui.Text($"{session.Duration:hh\\:mm}");
            }
        }
    }
}