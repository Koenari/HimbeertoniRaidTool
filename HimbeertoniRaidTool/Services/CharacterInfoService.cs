using System.Diagnostics.CodeAnalysis;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Lumina.Excel.Sheets;

namespace HimbeertoniRaidTool.Plugin.Services;

internal unsafe class CharacterInfoService(IObjectTable objectTable, IPartyList partyList, IPlayerState playerState)
{
    private static InfoProxyPartyMember* _partyInfo => InfoProxyPartyMember.Instance();
    private readonly Dictionary<string, ulong> _cache = new();
    private readonly HashSet<string> _notFound = [];
    private DateTime _lastPrune = DateTime.Now;
    private static readonly TimeSpan _pruneInterval = TimeSpan.FromSeconds(10);


    public ulong GetContentId(IPlayerCharacter? character)
    {
        if (character == null)
            return 0;
        foreach (var partyMember in partyList)
        {
            bool found =
                partyMember.EntityId == character.EntityId
             || partyMember.Name.Equals(character.Name) && partyMember.World.RowId == character.CurrentWorld.RowId;
            if (found)
                return (ulong)partyMember.ContentId;
        }

        if (_partyInfo == null)
            return 0;
        for (uint i = 0; i < _partyInfo->DataSize; ++i)
        {
            var entry = _partyInfo->GetEntry(i);
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
            result = objectTable.SearchById(id) as IPlayerCharacter;
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
        result = objectTable.FirstOrDefault(
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
    public bool IsSelf(Character character) =>
        playerState.IsLoaded && Character.CalcCharId(playerState.ContentId) == character.CharId;

    private void Update()
    {
        if (DateTime.Now - _lastPrune <= _pruneInterval) return;
        _lastPrune = DateTime.Now;
        if (_notFound.Count == 0) return;
        _notFound.Clear();
    }
}