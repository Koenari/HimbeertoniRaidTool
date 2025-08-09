using System.Diagnostics.CodeAnalysis;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Lumina.Excel.Sheets;

namespace HimbeertoniRaidTool.Plugin.Services;

internal unsafe class CharacterInfoService
{
    private readonly IObjectTable _objectTable;
    private readonly IPartyList _partyList;
    private readonly IClientState _clientState;
    private static InfoProxyPartyMember* PartyInfo => InfoProxyPartyMember.Instance();
    private readonly Dictionary<string, ulong> _cache = new();
    private readonly HashSet<string> _notFound = new();
    private DateTime _lastPrune;
    private static readonly TimeSpan PruneInterval = TimeSpan.FromSeconds(10);

    internal CharacterInfoService(IObjectTable objectTable, IPartyList partyList, IClientState clientState)
    {
        _objectTable = objectTable;
        _partyList = partyList;
        _clientState = clientState;
        _lastPrune = DateTime.Now;
    }

    public ulong GetContentId(IPlayerCharacter? character)
    {
        if (character == null)
            return 0;
        foreach (var partyMember in _partyList)
        {
            bool found =
                partyMember.ObjectId == character.GameObjectId
             || partyMember.Name.Equals(character.Name) && partyMember.World.RowId == character.CurrentWorld.RowId;
            if (found)
                return (ulong)partyMember.ContentId;
        }

        if (PartyInfo == null)
            return 0;
        for (uint i = 0; i < PartyInfo->DataSize; ++i)
        {
            var entry = PartyInfo->GetEntry(i);
            if (entry == null) continue;
            if (entry->HomeWorld != character.HomeWorld.RowId) continue;
            if (entry->NameString.Equals(character.Name.TextValue))
                return entry->ContentId;
        }

        return 0;
    }

    public bool TryGetChar([NotNullWhen(true)] out IPlayerCharacter? result, string name, World? w = null)
    {
        Update();
        if (_cache.TryGetValue(name, out ulong id))
        {
            result = _objectTable.SearchById(id) as IPlayerCharacter;
            if (result?.Name.TextValue == name
             && (w is null || w.Value.RowId == result.HomeWorld.RowId))
                return true;
            _cache.Remove(name);
        }

        if (_notFound.Contains(name))
        {
            result = null;
            return false;
        }

        //This is really slow (comparatively)
        result = _objectTable.FirstOrDefault(
            o =>
            {
                var p = o as IPlayerCharacter;
                return p != null
                    && p.Name.TextValue == name
                    && (w is null || p.HomeWorld.Value.RowId == w.Value.RowId);
            }, null) as IPlayerCharacter;
        if (result != null)
        {
            _cache.Add(name, result.GameObjectId);
            return true;
        }
        else
        {
            _notFound.Add(name);
            return false;
        }
    }
    public bool IsSelf(Character character) => Character.CalcCharId(_clientState.LocalContentId) == character.CharId;

    private void Update()
    {
        if (DateTime.Now - _lastPrune <= PruneInterval) return;
        _lastPrune = DateTime.Now;
        if (_notFound.Count == 0) return;
        _notFound.Clear();
    }
}