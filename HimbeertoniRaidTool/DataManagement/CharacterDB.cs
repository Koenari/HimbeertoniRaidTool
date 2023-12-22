using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using HimbeertoniRaidTool.Common;
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
        JsonSerializerSettings settings,GearDb gearDb) : base(idProvider,serializedData,conv,settings)
    {
        if(!LoadError)
        {
            HashSet<HrtId> knownGear = new();
            foreach ((HrtId id,Character c) in Data)
            {
                _usedWorlds.Add(c.HomeWorldId);
                if (!_nameLookup.TryAdd((c.HomeWorldId, c.Name), c.LocalId))
                {
                    ServiceManager.PluginLog.Warning(
                        $"Database contains {c.Name} @ {c.HomeWorld?.Name} twice. Characters were merged");
                    Data.TryGetValue(_nameLookup[(c.HomeWorldId, c.Name)], out Character? other);
                    _idReplacement.Add(c.LocalId, other!.LocalId);
                    other.MergeInfos(c);
                    Data.Remove(c.LocalId);
                    continue;
                }

                if (c.CharId > 0)
                    _charIdLookup.TryAdd(c.CharId, c.LocalId);
                foreach (PlayableClass job in c)
                {
                    if (knownGear.Contains(job.CurGear.LocalId))
                    {
                        //Only BiS gearset are meant to be shared
                        GearSet gearCopy = job.CurGear.Clone();
                        ServiceManager.PluginLog.Debug(
                            $"Found Gear duplicate with Sequence: {gearCopy.LocalId.Sequence}");
                        gearCopy.LocalId = HrtId.Empty;
                        gearDb.TryAdd(gearCopy);
                        job.CurGear = gearCopy;
                    }
                    else
                    {
                        knownGear.Add(job.CurGear.LocalId);
                    }
                }
                
            }
        }

        
        
    }

    internal IEnumerable<uint> GetUsedWorlds() => _usedWorlds;

    internal IReadOnlyList<string> GetKnownCharacters(uint worldId)
    {
        List<string> result = new();
        foreach (Character character in Data.Values.Where(c => c.HomeWorldId == worldId))
            result.Add(character.Name);
        return result;
    }

    public override bool TryAdd(in Character c)
    {
        if (!base.TryAdd(c))
            return false;

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

    internal IEnumerable<HrtId> FindOrphanedGearSets(IEnumerable<HrtId> possibleOrphans)
    {
        HashSet<HrtId> orphanSets = new(possibleOrphans);
        foreach (PlayableClass job in Data.Values.SelectMany(character => character.Classes))
        {
            orphanSets.Remove(job.CurGear.LocalId);
            orphanSets.Remove(job.CurBis.LocalId);
        }

        ServiceManager.PluginLog.Information($"Found {orphanSets.Count} orphaned gear sets.");
        return orphanSets;
    }

    internal string Serialize(HrtIdReferenceConverter<GearSet> conv, JsonSerializerSettings settings)
    {
        settings.Converters.Add(conv);
        string result = JsonConvert.SerializeObject(Data.Values, settings);
        settings.Converters.Remove(conv);
        return result;
    }

    public override HrtWindow OpenSearchWindow(Action<Character> onSelect, Action? onCancel = null)
    {
        return new CharacterSearchWindow(this,onSelect, onCancel);
    }
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