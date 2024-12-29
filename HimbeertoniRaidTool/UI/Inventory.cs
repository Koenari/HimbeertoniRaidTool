using System.Numerics;
using Dalamud.Interface;
using HimbeertoniRaidTool.Common.Localization;
using HimbeertoniRaidTool.Plugin.Localization;
using ImGuiNET;

namespace HimbeertoniRaidTool.Plugin.UI;

internal class InventoryWindow : HrtWindowWithModalChild
{
    private readonly HashSet<Currency> _hiddenCurrencies =
    [
        Currency.Mgp,
        Currency.Unknown,
        Currency.StormSeal,
        Currency.FlameSeal,
        Currency.SerpentSeal,
        Currency.AlliedSeal,
    ];
    private readonly CharacterInfoService _characterInfoService;
    private readonly Character _character;
    private Inventory Inventory => _character.MainInventory;
    private Wallet Wallet => _character.Wallet;

    private static readonly Vector2 IconSize = new(ImGui.GetTextLineHeightWithSpacing());
    internal InventoryWindow(IUiSystem uiSystem, Character c, CharacterInfoService characterInfoService) :
        base(uiSystem)
    {
        _characterInfoService = characterInfoService;
        Size = new Vector2(400f, 550f);
        SizeCondition = ImGuiCond.Appearing;
        Title = string.Format(LootmasterLoc.InventoryUi_Title, c.Name);
        _character = c;
        if (GameInfo.CurrentExpansion.CurrentSavage is null)
            return;
        foreach (var item in from boss in GameInfo.CurrentExpansion.CurrentSavage
                                                  .Bosses
                             from item in boss.GuaranteedItems
                             where !_character.MainInventory.Contains(item.Id)
                             select item)
        {
            _character.MainInventory.ReserveSlot(item);
        }
    }

    public override void Draw()
    {
        ImGui.Text($"{CommonLoc.CommonTerms_Wallet}");
        ImGui.NewLine();
        foreach (var (cur, _) in Wallet.Where(c => !_hiddenCurrencies.Contains(c.Key)))
        {
            int value = Wallet[cur];
            if (_characterInfoService.IsSelf(_character))
            {
                ImGui.Text($"{value:N0} {cur}");
            }
            else
            {
                ImGui.Text($"{cur}:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(125 * ScaleFactor);
                if (ImGui.InputInt($"##{cur}", ref value, 0, 0))
                    Wallet[cur] = value;

            }
        }
        ImGui.Separator();
        ImGui.Text(GeneralLoc.InventoryWindow_Hdg_Current_Savage_Books);
        ImGui.NewLine();
        if (GameInfo.CurrentExpansion.CurrentSavage is not null)
            foreach (var item in GameInfo.CurrentExpansion.CurrentSavage
                                         .Bosses.SelectMany(boss => boss.GuaranteedItems))
            {
                var icon = UiSystem.GetIcon(item.Icon);
                if (icon is not null)
                    ImGui.Image(icon.ImGuiHandle, IconSize);
                ImGui.SameLine();
                ImGui.Text(item.Name);
                ImGui.SameLine();
                var entry = Inventory[Inventory.IndexOf(item.Id)];
                ImGui.SetNextItemWidth(150f * ScaleFactor);
                ImGui.InputInt($"##{item.Name}", ref entry.Quantity);
                Inventory[Inventory.IndexOf(item.Id)] = entry;
            }

        ImGui.Separator();
        ImGui.Text(LootmasterLoc.InventoryUi_hdg_additionalGear);
        ImGui.NewLine();
        foreach ((int idx, var entry) in Inventory.Where(e => e.Value.IsGear))
        {
            ImGui.PushID(idx);
            if (entry.Item is not GearItem item)
                continue;
            var icon = UiSystem.GetIcon(item.Icon);
            if (ImGuiHelper.Button(FontAwesomeIcon.Trash, "##delete", null, true, IconSize * ScaleFactor))
                Inventory.Remove(idx);
            ImGui.SameLine();
            ImGui.BeginGroup();
            if (icon is not null)
            {
                ImGui.Image(icon.ImGuiHandle, IconSize * ScaleFactor);
                ImGui.SameLine();
            }
            ImGui.Text(item.Name);
            ImGui.EndGroup();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                item.Draw();
                ImGui.EndTooltip();
            }

            ImGui.PopID();
        }

        ImGui.BeginDisabled(ChildIsOpen);
        if (ImGuiHelper.Button(FontAwesomeIcon.Plus, "##add", null, true, IconSize * ScaleFactor))
            ModalChild = new SelectGearItemWindow(UiSystem, item => Inventory.ReserveSlot(item, 1),
                                                  _ => { },
                                                  null, null, null,
                                                  GameInfo.CurrentExpansion.CurrentSavage?.ArmorItemLevel
                                               ?? 0);
        ImGui.EndDisabled();
    }
}