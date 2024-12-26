using System.Numerics;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class PlayerDb(IIdProvider idProvider, IEnumerable<JsonConverter> converters, ILogger logger)
    : DataBaseTable<Player>(idProvider, converters, logger)
{

    public override HashSet<HrtId> GetReferencedIds()
    {
        HashSet<HrtId> referencedIds = new();
        foreach (var character in from player in Data.Values from character in player.Characters select character)
        {
            referencedIds.Add(character.LocalId);
        }
        return referencedIds;
    }
    public override HrtWindow OpenSearchWindow(Action<Player> onSelect, Action? onCancel = null) =>
        new PlayerSearchWindow(this, onSelect, onCancel);

    private class PlayerSearchWindow : SearchWindow<Player, PlayerDb>
    {
        public PlayerSearchWindow(PlayerDb dataBase, Action<Player> onSelect, Action? onCancel) : base(dataBase,
            onSelect, onCancel)
        {
            Size = new Vector2(300, 150);
            SizeCondition = ImGuiCond.Appearing;
        }

        protected override void DrawContent()
        {
            ImGui.Text(
                $"{GeneralLoc.DBSearchPlayerUi_txt_selected}: {(Selected is null ? $"{GeneralLoc.CommonTerms_None}" : $"{Selected.NickName} ({Selected.MainChar.Name})")}");
            ImGui.Separator();
            ImGui.Text($"{GeneralLoc.DBSearchPlayerUi_hdg_selectPlayer}:");
            if (ImGuiHelper.SearchableCombo("##search", out var
                                                newSelected, string.Empty, Database.GetValues(),
                                            p => $"{p.NickName} ({p.MainChar.Name})"))
            {
                Selected = newSelected;
            }
        }
    }
}