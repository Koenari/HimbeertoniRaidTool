using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class CharacterDb : DataBaseTable<Character,GearSet>
{
    private readonly Dictionary<ulong, HrtId> _charIdLookup = new();
    private readonly Dictionary<HrtId, HrtId> _idReplacement = new();
    private readonly Dictionary<(uint, string), HrtId> _nameLookup = new();
    private readonly HashSet<uint> _usedWorlds = new();

    internal CharacterDb(IIdProvider idProvider, string serializedData, HrtIdReferenceConverter<GearSet> conv,
        JsonSerializerSettings settings) : base(idProvider, serializedData, conv, settings)
    {
        if (LoadError) return;
        foreach (Character c in Data.Values)
        {
            /*
             * Fill up indices
             */
            _usedWorlds.Add(c.HomeWorldId);
            if (!_nameLookup.TryAdd((c.HomeWorldId, c.Name), c.LocalId))
            {
                ServiceManager.PluginLog.Warning(
                    $"Database contained {c.Name} @ {c.HomeWorld?.Name} twice. Characters were merged");
                if (Data.TryGetValue(_nameLookup[(c.HomeWorldId, c.Name)], out Character? other))
                {
                    _idReplacement.Add(c.LocalId, other.LocalId);
                    other.MergeInfos(c);
                    Data.Remove(c.LocalId);
                }
                continue;
            }
            if (c.CharId > 0)
                _charIdLookup.TryAdd(c.CharId, c.LocalId);
        }
    }

    internal IEnumerable<uint> GetUsedWorlds() => _usedWorlds;

    internal IReadOnlyList<string> GetKnownCharacters(uint worldId) => Data.Values.Where(c => c.HomeWorldId == worldId).Select(character => character.Name).ToList();

    public override bool TryAdd(in Character c)
    {
        if (!base.TryAdd(c))
            return false;
        //Add to indices
        _usedWorlds.Add(c.HomeWorldId);
        _nameLookup.TryAdd((c.HomeWorldId, c.Name), c.LocalId);
        if (c.CharId > 0)
            _charIdLookup.TryAdd(c.CharId, c.LocalId);
        return true;
    }

    internal bool TryGetCharacterByCharId(ulong charId, [NotNullWhen(true)] out Character? c)
    {
        c = null;
        if (_charIdLookup.TryGetValue(charId, out HrtId? id))
            return TryGet(id, out c);
        id = Data.FirstOrDefault(x => x.Value.CharId == charId).Key;
        if (id is not null)
        {
            _charIdLookup.Add(charId, id);
            c = Data[id];
            return true;
        }

        ServiceManager.PluginLog.Debug($"Did not find character with ID: {charId} in database");
        return false;
    }

    internal bool SearchCharacter(uint worldId, string name, [NotNullWhen(true)] out Character? c)
    {
        c = null;
        if (_nameLookup.TryGetValue((worldId, name), out HrtId? id))
            return TryGet(id, out c);
        id = Data.FirstOrDefault(x => x.Value.HomeWorldId == worldId && x.Value.Name.Equals(name)).Key;
        if (id is not null)
        {
            _nameLookup.Add((worldId, name), id);
            c = Data[id];
            return true;
        }

        ServiceManager.PluginLog.Debug($"Did not find character {name}@{worldId} in database");
        return false;
    }

    public override bool TryGet(HrtId id, [NotNullWhen(true)] out Character? c)
    {
        if (_idReplacement.ContainsKey(id))
            id = _idReplacement[id];
        return Data.TryGetValue(id, out c);
    }

    internal void ReindexCharacter(HrtId localId)
    {
        if (!TryGet(localId, out Character? c))
            return;
        _usedWorlds.Add(c.HomeWorldId);
        _nameLookup.TryAdd((c.HomeWorldId, c.Name), c.LocalId);
        if (c.CharId > 0)
            _charIdLookup.TryAdd(c.CharId, c.LocalId);
    }
    public override HashSet<HrtId> GetReferencedIds()
    {
        ServiceManager.PluginLog.Debug("Begin calculation of referenced Ids in character database");
        HashSet<HrtId> referencedIds = new();
        foreach (PlayableClass playableClass in Data.Values.SelectMany(character => character))
        {
            foreach (GearSet gearSet in playableClass.GearSets.Where(set => !set.LocalId.IsEmpty))
            {
                referencedIds.Add(gearSet.LocalId);
            }
            foreach (GearSet gearSet in playableClass.BisSets.Where(set => !set.LocalId.IsEmpty))
            {
                referencedIds.Add(gearSet.LocalId);
            }
        }
        ServiceManager.PluginLog.Debug("Finished calculation of referenced Ids in character database");
        return referencedIds;
    }
    public override void FixEntries()
    {
        foreach (PlayableClass playableClass in Data.Values.SelectMany(character => character.Classes))
        {
            playableClass.RemoveEmptySets();
            if (playableClass.CurGear.LocalId.IsEmpty)
                ServiceManager.HrtDataManager.GearDb.TryAdd(playableClass.CurGear);
            if (playableClass.CurBis.LocalId.IsEmpty)
                ServiceManager.HrtDataManager.GearDb.TryAdd(playableClass.CurBis);
        }
    }

    public override HrtWindow OpenSearchWindow(Action<Character> onSelect, Action? onCancel = null) => new CharacterSearchWindow(this,onSelect, onCancel);

    private class CharacterSearchWindow : SearchWindow<Character, CharacterDb>
    {
        private readonly uint[] _worlds;
        private readonly string[] _worldNames;
        private int _worldSelectIndex;
        private string[] _characterNames = Array.Empty<string>();
        private int _characterNameIndex;

            

        public CharacterSearchWindow(CharacterDb dataBase,Action<Character> onSelect, Action? onCancel) : base(dataBase,onSelect, onCancel)
        {
            _worlds = ServiceManager.HrtDataManager.CharDb.GetUsedWorlds().ToArray();
            _worldNames = Array.ConvertAll(_worlds,
                x => ServiceManager.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.World>()?.GetRow(x)?.Name
                    .RawString ?? "");
            Title = Localize("GetCharacterTitle", "Get character from DB");
            Size = new Vector2(350, 420);
            Flags = ImGuiWindowFlags.NoScrollbar;
        }

        protected override void DrawContent()
        {
            if (ImGui.ListBox("World", ref _worldSelectIndex, _worldNames, _worldNames.Length))
            {
                var list = ServiceManager.HrtDataManager.CharDb.GetKnownCharacters(_worlds[_worldSelectIndex]);
                _characterNames = list.ToArray();
                Array.Sort(_characterNames);
            }

            if (ImGui.ListBox("Name", ref _characterNameIndex, _characterNames, _characterNames.Length))
                Database.SearchCharacter(_worlds[_worldSelectIndex],_characterNames[_characterNameIndex],out Selected);
        }
    }
}