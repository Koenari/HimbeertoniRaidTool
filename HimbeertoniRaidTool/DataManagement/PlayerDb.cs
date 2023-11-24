using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class PlayerDb : DataBaseTable<Player, Character>
{

    public PlayerDb(IIdProvider idProvider, string serializedData, HrtIdReferenceConverter<Character> conv, JsonSerializerSettings settings) : base(idProvider, serializedData, conv, settings)
    {
    }

    public override HrtWindow OpenSearchWindow(Action<Player> onSelect, Action? onCancel = null) => new PlayerSearchWindow(this, onSelect, onCancel);

    internal class PlayerSearchWindow : SearchWindow<Player, PlayerDb>
    {
        public PlayerSearchWindow(PlayerDb dataBase, Action<Player> onSelect, Action? onCancel) : base(dataBase, onSelect, onCancel) { }

        protected override void DrawContent()
        {
            ImGui.Text(Localization.Localize("Select player:"));
            if (ImGuiHelper.SearchableCombo("search", out Player?
                    newSelected, string.Empty, Database.GetValues(), p => p.NickName))
            {
                Selected = newSelected;
            }
        }
    }
}