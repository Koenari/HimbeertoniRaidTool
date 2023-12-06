using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game;
using HimbeertoniRaidTool.Common.Data;

namespace HimbeertoniRaidTool.Plugin.Services;

internal static class OwnCharacterDataProvider
{
    private static Character? _curChar = null;
    private static bool _ready = _curChar != null;
    public static readonly HashSet<Currency> TrackedCurrencies = new()
    {
        Currency.Gil,
        Currency.TomestoneOfCausality,
        Currency.TomestoneOfComedy,
    };
    public static readonly HashSet<uint> TrackedItems = new()
    {

    };

    public static void Enable()
    {

    }
    private static void OnLogin()
    {
        //_curChar = ServiceManager.ClientState.LocalPlayer;
    }
    private static bool GetChar([NotNullWhen(true)] out Character? target,
        [NotNullWhen(true)] out PlayerCharacter? source)
    {
        target = null;
        source = ServiceManager.ClientState.LocalPlayer;
        if (source == null)
            return false;

        return ServiceManager.HrtDataManager.CharDb.SearchCharacter(source.HomeWorld.Id, source.Name.TextValue,
            out target);
    }

    private static unsafe void UpdateWallet()
    {
        if (!GetChar(out Character? target, out PlayerCharacter? source))
            return;
        var container = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Currency);
        for (int i = 0; i < container->Size; i++)
        {
            InventoryItem item = container->Items[i];
            var type = (Currency)item.ItemID;
            if (TrackedCurrencies.Contains(type))
            {
                target.Wallet[type] = item.Quantity;
            }
        }
    }
    private static void UpdateGear()
    {
        if (!GetChar(out Character? target, out PlayerCharacter? source))
            return;
        var job = (Job)source.ClassJob.Id;
        PlayableClass targetClass = target[job] ?? target.AddClass(job);
        Helpers.UpdateGearFromInventoryContainer(InventoryType.EquippedItems, targetClass);
    }
}