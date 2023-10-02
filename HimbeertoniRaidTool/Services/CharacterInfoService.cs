using System.Diagnostics.CodeAnalysis;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Lumina.Excel.GeneratedSheets;

namespace HimbeertoniRaidTool.Plugin.Services;

internal unsafe class CharacterInfoService
{
    private readonly IObjectTable _gameObjects;
    private readonly IPartyList _partyList;
    private readonly InfoModule* _infoModule;
    private InfoProxyParty* PartyInfo => (InfoProxyParty*)_infoModule->GetInfoProxyById(InfoProxyId.Party);
    private readonly Dictionary<string, uint> _cache = new();
    private readonly HashSet<string> _notFound = new();
    private DateTime _lastPrune;
    private static readonly TimeSpan PruneInterval = TimeSpan.FromSeconds(10);

    internal CharacterInfoService(IObjectTable gameObjects, IPartyList partyList)
    {
        _gameObjects = gameObjects;
        _partyList = partyList;
        _lastPrune = DateTime.Now;
        _infoModule = Framework.Instance()->GetUiModule()->GetInfoModule();
    }

    public long GetLocalPlayerContentId()
    {
        return (long)_infoModule->LocalContentId;
    }

    public long GetContentId(PlayerCharacter? character)
    {
        if (character == null)
            return 0;
        foreach (PartyMember partyMember in _partyList)
        {
            bool found =
                partyMember.ObjectId == character.ObjectId
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
            if (entry->HomeWorld != character.HomeWorld.Id) continue;
            string name = System.Text.Encoding.Default.GetString(entry->Name, 32);
            if (name.Equals(character.Name.TextValue))
                return (long)entry->ContentId;
        }

        return 0;
    }

    public bool TryGetChar([NotNullWhen(true)] out PlayerCharacter? result, string name, World? w = null)
    {
        Update();
        if (_cache.TryGetValue(name, out uint id))
        {
            result = _gameObjects.SearchById(id) as PlayerCharacter;
            if (result?.Name.TextValue == name
                && (w is null || w.RowId == result.HomeWorld.Id))
                return true;
            else
                _cache.Remove(name);
        }

        if (_notFound.Contains(name))
        {
            result = null;
            return false;
        }

        //This is really slow (comparatively)
        result = ServiceManager.ObjectTable.FirstOrDefault(
            o =>
            {
                var p = o as PlayerCharacter;
                return p != null
                       && p.Name.TextValue == name
                       && (w is null || p.HomeWorld.Id == w.RowId);
            }, null) as PlayerCharacter;
        if (result != null)
        {
            _cache.Add(name, result.ObjectId);
            return true;
        }
        else
        {
            _notFound.Add(name);
            return false;
        }
    }

    private void Update()
    {
        if (DateTime.Now - _lastPrune <= PruneInterval) return;
        _lastPrune = DateTime.Now;
        if (_notFound.Count == 0) return;
        _notFound.Clear();
    }
}