using System.Numerics;
using Dalamud.Bindings.ImGui;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;
using Serilog;

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
    public override HrtWindow GetSearchWindow(IUiSystem uiSystem, Action<Player> onSelect, Action? onCancel = null) =>
        new PlayerSearchWindow(uiSystem, this, onSelect, onCancel);

    private class PlayerSearchWindow : SearchWindow<Player, PlayerDb>
    {
        public PlayerSearchWindow(IUiSystem uiSystem, PlayerDb dataBase, Action<Player> onSelect, Action? onCancel) :
            base(uiSystem, dataBase, onSelect, onCancel)
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
            if (InputHelper.SearchableCombo("##search", out var
                                                newSelected, string.Empty, Database.GetValues(),
                                            p => $"{p.NickName} ({p.MainChar.Name})"))
            {
                Selected = newSelected;
            }
        }
    }
}