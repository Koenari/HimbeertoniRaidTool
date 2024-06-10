using System.Numerics;
using Dalamud.Interface;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using Action = System.Action;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class CharacterDb(IIdProvider idProvider, IEnumerable<JsonConverter> converters)
    : DataBaseTable<Character>(idProvider, converters)
{
    public static Func<Character?, bool> GetStandardPredicate(ulong charId, uint worldId, string name) => character =>
    {
        if (character is null) return false;
        if (charId > 0 && character.CharId == charId) return true;
        return worldId > 0 && name.Length > 0 && character.HomeWorldId == worldId
            && character.Name.Equals(name);
    };
    public override HashSet<HrtId> GetReferencedIds()
    {
        ServiceManager.Logger.Debug("Begin calculation of referenced Ids in character database");
        HashSet<HrtId> referencedIds = [];
        foreach (PlayableClass playableClass in Data.Values.SelectMany(character => character))
        {
            foreach (GearSet gearSet in playableClass.GearSets.Where(set => !set.LocalId.IsEmpty))
            {
                referencedIds.Add(gearSet.LocalId);
            }
            foreach (GearSet gearSet in playableClass.BisSets.Where(set => !set.LocalId.IsEmpty))
            {
                referencedIds.Add(gearSet.LocalId);
            }
        }
        ServiceManager.Logger.Debug("Finished calculation of referenced Ids in character database");
        return referencedIds;
    }
    public override void FixEntries()
    {
        foreach (PlayableClass playableClass in Data.Values.SelectMany(character => character.Classes))
        {
            playableClass.RemoveEmptySets();
            if (playableClass.CurGear.LocalId.IsEmpty)
                ServiceManager.HrtDataManager.GearDb.TryAdd(playableClass.CurGear);
            if (playableClass.CurBis.LocalId.IsEmpty)
                ServiceManager.HrtDataManager.GearDb.TryAdd(playableClass.CurBis);
        }
    }

    public override HrtWindow OpenSearchWindow(Action<Character> onSelect, Action? onCancel = null) =>
        new CharacterSearchWindow(this, onSelect, onCancel);

    private class CharacterSearchWindow : SearchWindow<Character, CharacterDb>
    {
        private uint _selectedWorld = 0;
        private readonly Dictionary<uint, string> _worldCache = [];

        private string GetWorldName(uint idx) =>
            _worldCache.TryGetValue(idx, out string? name) ? name : _worldCache[idx] =
                ServiceManager.DataManager.GetExcelSheet<World>()?.GetRow(idx)?.Name?.RawString ?? "";

        public CharacterSearchWindow(CharacterDb dataBase, Action<Character> onSelect, Action? onCancel) : base(
            dataBase, onSelect, onCancel)
        {
            Title = GeneralLoc.GetCharacterWindow_Title;
            (Size, SizeCondition) = (new Vector2(400, 500), ImGuiCond.Appearing);
            Flags = ImGuiWindowFlags.NoScrollbar;
        }

        protected override void DrawContent()
        {
            if (ImGui.BeginCombo(GeneralLoc.EditCharUi_in_HomeWorld, GeneralLoc.CommonTerms_All))
            {
                if (ImGui.Selectable(GeneralLoc.CommonTerms_All)) _selectedWorld = 0;
                foreach (uint world in Database.Data.Values.Select(entry => entry.HomeWorldId).Distinct()
                                               .Where(entry => entry != 0))
                {
                    if (ImGui.Selectable(GetWorldName(world)))
                        _selectedWorld = world;
                }
                ImGui.EndCombo();
            }
            if (!ImGui.BeginTable(
                    "Chars", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
                return;
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(GeneralLoc.CommonTerms_Name, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(GeneralLoc.EditCharUi_in_HomeWorld, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();
            foreach (Character character in Database.GetValues()
                                                    .Where(entry => _selectedWorld == 0
                                                                 || entry.HomeWorldId == _selectedWorld))
            {
                ImGui.TableNextColumn();
                if (ImGuiHelper.Button(FontAwesomeIcon.Check, $"{character.LocalId}",
                                       string.Format(GeneralLoc.SearchWindow_btn_tt_SelectEnty, character.DataTypeName,
                                                     character)))
                {
                    Selected = character;
                    Save();
                }
                ImGui.TableNextColumn();
                ImGui.Text(character.Name);
                ImGui.TableNextColumn();
                ImGui.Text(character.HomeWorld?.Name ?? "");
                ImGui.TableNextColumn();
                ImGui.Text(string.Format(GeneralLoc.CharacterSearchUi_txt_classCount, character.Classes.Count()));
            }
            ImGui.EndTable();
        }
    }
}