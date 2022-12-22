using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game;
using HimbeertoniRaidTool.Common.Data;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;

internal static class OwnCharacterDataProvider
{
    public static readonly HashSet<Currency> _trackedCurrencies = new()
    {
        Currency.Gil,
        Currency.TomestoneOfAstronomy,
        Currency.TomestoneOfCausality
    };
    public static readonly HashSet<uint> _trackedItems = new()
    {

    };
    private static bool GetChar([NotNullWhen(true)] out Character? target, [NotNullWhen(true)] out PlayerCharacter? source)
    {
        target = null;
        source = Services.ClientState.LocalPlayer;
        if (source == null)
            return false;

        target = new(source.Name.TextValue, source.HomeWorld.Id);
        return Services.HrtDataManager.GetManagedCharacter(ref target);
    }
    public static unsafe void UpdateWallet()
    {
        if (!GetChar(out Character? target, out PlayerCharacter? source))
            return;
        InventoryContainer* container = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Currency);
        for (int i = 0; i < container->Size; i++)
        {
            var item = container->Items[i];
            Currency type = (Currency)item.ItemID;
            if (_trackedCurrencies.Contains(type))
            {
                target.Wallet[type] = item.Quantity;
            }
        }
    }
}
