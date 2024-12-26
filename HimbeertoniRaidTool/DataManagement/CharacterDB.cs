using System.Numerics;
using Dalamud.Interface;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using Action = System.Action;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class CharacterDb(
    IIdProvider idProvider,
    IEnumerable<JsonConverter> converters,
    ILogger logger,
    IDataManager dataManager)
    : DataBaseTable<Character>(idProvider, converters, logger)
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
        Logger.Debug("Begin calculation of referenced Ids in character database");
        HashSet<HrtId> referencedIds = [];
        foreach (var playableClass in Data.Values.SelectMany(character => character))
        {
            foreach (var gearSet in playableClass.GearSets.Where(set => !set.LocalId.IsEmpty))
            {
                referencedIds.Add(gearSet.LocalId);
            }
            foreach (var gearSet in playableClass.BisSets.Where(set => !set.LocalId.IsEmpty))
            {
                referencedIds.Add(gearSet.LocalId);
            }
        }
        Logger.Debug("Finished calculation of referenced Ids in character database");
        return referencedIds;
    }
    public override void FixEntries(HrtDataManager hrtDataManager)
    {
        foreach (var playableClass in Data.Values.SelectMany(character => character.Classes))
        {
            playableClass.RemoveEmptySets();
            if (playableClass.CurGear.LocalId.IsEmpty)
                hrtDataManager.GearDb.TryAdd(playableClass.CurGear);
            if (playableClass.CurBis.LocalId.IsEmpty)
                hrtDataManager.GearDb.TryAdd(playableClass.CurBis);
        }
    }

    public override HrtWindow OpenSearchWindow(Action<Character> onSelect, Action? onCancel = null) =>
        new CharacterSearchWindow(this, onSelect, onCancel, dataManager);

    private class CharacterSearchWindow : SearchWindow<Character, CharacterDb>
    {
        private readonly IDataManager _dataManager;
        private uint _selectedWorld = 0;
        private readonly Dictionary<uint, string> _worldCache = [];

        private string GetWorldName(uint idx) =>
            _worldCache.TryGetValue(idx, out string? name) ? name : _worldCache[idx] =
                _dataManager.GetExcelSheet<World>().GetRow(idx).Name.ExtractText();

        public CharacterSearchWindow(CharacterDb dataBase, Action<Character> onSelect, Action? onCancel,
                                     IDataManager dataManager) : base(
            dataBase, onSelect, onCancel)
        {
            _dataManager = dataManager;
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
            foreach (var character in Database.GetValues()
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
                ImGui.Text(character.HomeWorld.HasValue ? character.HomeWorld.Value.Name.ExtractText() : string.Empty);
                ImGui.TableNextColumn();
                ImGui.Text(string.Format(GeneralLoc.CharacterSearchUi_txt_classCount, character.Classes.Count()));
            }
            ImGui.EndTable();
        }
    }
}