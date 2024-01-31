using System.Numerics;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class PlayerDb : DataBaseTable<Player, Character>
{

    public PlayerDb(IIdProvider idProvider, string serializedData, HrtIdReferenceConverter<Character> conv,
                    JsonSerializerSettings settings) : base(idProvider, serializedData, conv, settings)
    {
    }

    public override HashSet<HrtId> GetReferencedIds()
    {
        HashSet<HrtId> referencedIds = new();
        foreach (Character character in from player in Data.Values from character in player.Characters select character)
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
                $"{GeneralLoc.PlayerSearchWindow_Selected}: {(Selected is null ? $"{GeneralLoc.None}" : $"{Selected.NickName} ({Selected.MainChar.Name})")}");
            ImGui.Separator();
            ImGui.Text($"{GeneralLoc.PlayerSearchWindow_Select_player}:");
            if (ImGuiHelper.SearchableCombo("##search", out Player?
                                                newSelected, string.Empty, Database.GetValues(),
                                            p => $"{p.NickName} ({p.MainChar.Name})"))
            {
                Selected = newSelected;
            }
        }
    }
}