using System.Diagnostics.CodeAnalysis;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Lumina.Excel.GeneratedSheets;

namespace HimbeertoniRaidTool.Plugin.Services;

internal class CharacterInfoService
{
    private readonly ObjectTable GameObjects;
    private readonly Dictionary<string, uint> Cache = new();
    private readonly HashSet<string> NotFound = new();
    private DateTime LastPrune;
    private static readonly TimeSpan PruneInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MaxCacheTime = TimeSpan.FromSeconds(30);
    internal CharacterInfoService(ObjectTable gameObjects)
    {
        GameObjects = gameObjects;
        LastPrune = DateTime.Now;
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
