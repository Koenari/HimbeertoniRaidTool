using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Lumina.Excel.GeneratedSheets;

namespace HimbeertoniRaidTool.HrtServices
{
    internal class CharacterInfoService
    {
        private readonly ObjectTable GameObjects;
        private readonly Dictionary<string, uint> Cache = new();
        private readonly HashSet<string> NotFound = new();
        private TimeSpan TimeSinceLastPrune;
        private static readonly TimeSpan PruneInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan MaxCacheTime = TimeSpan.FromSeconds(30);
        private DateTime LastAccess;
        internal CharacterInfoService(ObjectTable gameObjects, Framework fw)
        {
            GameObjects = gameObjects;
            TimeSinceLastPrune = TimeSpan.Zero;
            fw.Update += Update;
            LastAccess = DateTime.Now;
        }
        public bool TryGetChar([NotNullWhen(returnValue: true)] out PlayerCharacter? result, string name, World? w = null)
        {
            LastAccess = DateTime.Now;
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
            result = Services.ObjectTable.FirstOrDefault(
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

        private void Update(Framework fw)
        {
            TimeSinceLastPrune += fw.UpdateDelta;
            if (TimeSinceLastPrune > PruneInterval)
            {
                TimeSinceLastPrune = TimeSpan.Zero;
                if (NotFound.Any() && LastAccess + MaxCacheTime < DateTime.Now)
                {
                    NotFound.Clear();
                    return;
                }
                foreach (PlayerCharacter p in Services.ObjectTable.Where(p => p is PlayerCharacter))
                {
                    if (NotFound.Contains(p.Name.TextValue))
                        NotFound.Remove(p.Name.TextValue);
                }
            }

        }
        public void Dispose(Framework fw)
        {
            fw.Update -= Update;
        }
    }
}
