using System.Diagnostics.CodeAnalysis;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Lumina.Excel.GeneratedSheets;

namespace HimbeertoniRaidTool.Plugin.Services;

internal unsafe class CharacterInfoService
{
    private readonly ObjectTable GameObjects;
    private readonly PartyList PartyList;
    private readonly InfoModule* InfoModule;
    private InfoProxyParty* PartyInfo => (InfoProxyParty*)InfoModule->GetInfoProxyById(InfoProxyId.Party);
    private readonly Dictionary<string, uint> Cache = new();
    private readonly HashSet<string> NotFound = new();
    private DateTime LastPrune;
    private static readonly TimeSpan PruneInterval = TimeSpan.FromSeconds(10);
    internal CharacterInfoService(ObjectTable gameObjects, PartyList partyList)
    {
        GameObjects = gameObjects;
        PartyList = partyList;
        LastPrune = DateTime.Now;
        InfoModule = Framework.Instance()->GetUiModule()->GetInfoModule();
    }
    public long GetLocalPlayerContentId()
    {
        return (long)InfoModule->LocalContentId;
    }
    public long GetContentID(PlayerCharacter? character)
    {
        if (character == null)
            return 0;
        foreach (var partyMember in PartyList)
        {
            bool found =
                (partyMember.ObjectId == character.ObjectId)
                || (partyMember.Name.Equals(character.Name) && partyMember.World.Id == character.CurrentWorld.Id);
            if (found)
                return partyMember.ContentId;
        }
        if (PartyInfo == null)
            return 0;
        for (uint i = 0; i < PartyInfo->InfoProxyCommonList.DataSize; ++i)
        {
            var entry = PartyInfo->InfoProxyCommonList.GetEntry(i);
            if (entry == null) continue;
            if (entry->HomeWorld == character.HomeWorld.Id)
            {
                string name = System.Text.Encoding.Default.GetString(entry->Name, 32);
                if (name.Equals(character.Name))
                    return entry->ContentId;
            }
        }
        return 0;
    }
    public bool TryGetChar([NotNullWhen(returnValue: true)] out PlayerCharacter? result, string name, World? w = null)
    {
        Update();
        if (Cache.TryGetValue(name, out uint id))
        {
            result = GameObjects.SearchById(id) as PlayerCharacter;
            if (result?.Name.TextValue == name
                && (w is null || w.RowId == result.HomeWorld.Id))
                return true;
            else
                Cache.Remove(name);
        }
        if (NotFound.Contains(name))
        {
            result = null;
            return false;
        }
        //This is really slow (comparatively)
        result = ServiceManager.ObjectTable.FirstOrDefault(
            o =>
            {
                PlayerCharacter? p = o as PlayerCharacter;
                return p != null
                    && p.Name.TextValue == name
                    && (w is null || p.HomeWorld.Id == w.RowId);
            }, null) as PlayerCharacter;
        if (result != null)
        {
            Cache.Add(name, result.ObjectId);
            return true;
        }
        else
        {
            NotFound.Add(name);
            return false;
        }
    }

    private void Update()
    {
        if (NotFound.Count == 0) return;
        if (DateTime.Now - LastPrune > PruneInterval)
        {
            LastPrune = DateTime.Now;
            NotFound.Clear();
        }

    }


}
