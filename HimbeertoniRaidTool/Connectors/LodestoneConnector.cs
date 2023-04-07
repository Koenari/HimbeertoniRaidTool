using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Logging;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.UI;
using Lumina.Excel.GeneratedSheets;
using NetStone;
using NetStone.Model.Parseables.Character;
using NetStone.Model.Parseables.Character.Gear;
using NetStone.Search.Character;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.Connectors;

internal class LodestoneConnector : NetstoneBase
{
    private readonly Lumina.Excel.ExcelSheet<Item>? _itemSheet;
    private readonly Lumina.Excel.ExcelSheet<Materia>? _materiaSheet;
    private readonly Dictionary<char, byte> _romanNumerals;

    private const int NoOfAllowedLodestoneRequests = 8;

    internal LodestoneConnector() : base(new(NoOfAllowedLodestoneRequests, new(0, 0, 30)))
    {
        _itemSheet = ServiceManager.DataManager.GetExcelSheet<Item>(Dalamud.ClientLanguage.English);
        _materiaSheet = ServiceManager.DataManager.GetExcelSheet<Materia>(Dalamud.ClientLanguage.English);
        _romanNumerals = new() {
            { 'I', 1 }, { 'V', 5 }, { 'X', 10 }, { 'L', 50 }
        };
    }
    public HrtUiMessage UpdateCharacter(Player p)
    {
        var updateAsync = UpdateCharacterAsync(p);
        updateAsync.Wait();
        return updateAsync.Result;
    }
    public async Task<HrtUiMessage> UpdateCharacterAsync(Player p)
    {
        Job? foundJob = null;
        bool isHq = false;

        try
        {
            var lodestoneCharacter = await FetchCharacterFromLodestone(p.MainChar);
            if (lodestoneCharacter == null)
                return new HrtUiMessage(Localize("LodestoneConnector:CharNotFound", "Character not found on Lodestone."), HrtUiMessageType.Failure);
            if (lodestoneCharacter.Gear.Soulcrystal != null)
                foundJob = (Job?)GetItemByName(lodestoneCharacter.Gear.Soulcrystal.ItemName, out isHq)?.ClassJobUse.Row;
            else
                foundJob = (Job?)GetItemByName(lodestoneCharacter.Gear.Mainhand.ItemName, out isHq)?.ClassJobUse.Row;
            if (foundJob == null || !Enum.IsDefined(typeof(Job), foundJob))
                return new HrtUiMessage(
                    Localize("LodestoneConnector:JobIncompatible", "Could not resolve currently used job or currently displayed job on " +
                    "Lodestone is not supported."), HrtUiMessageType.Failure);

            var classToChange = p.MainChar[foundJob.Value];
            if (classToChange == null)
            {
                classToChange = p.MainChar.AddClass(foundJob.Value);
                bool hasError = false;
                hasError |= ServiceManager.HrtDataManager.GearDB.AddSet(classToChange.Gear);
                hasError |= ServiceManager.HrtDataManager.GearDB.AddSet(classToChange.BIS);
                if (hasError)
                    return new HrtUiMessage(
                        Localize("LodestoneConnector:FailedtoCreateGear", "Could not create new gear set."),
                        HrtUiMessageType.Failure);
            }
            classToChange.Level = lodestoneCharacter.ActiveClassJobLevel;
            //Getting Race, Clan and Gender is not yet correctly implemented in Netstone 1.0.0
            //classToChange.Tribe = (unit)lodestoneCharacter.RaceClanGender;

            // Populate GearSet with newGearset
            var newGearset = lodestoneCharacter.Gear;
            FillItem(newGearset.Mainhand, GearSetSlot.MainHand);
            FillItem(newGearset.Offhand, GearSetSlot.OffHand);
            FillItem(newGearset.Head, GearSetSlot.Head);
            FillItem(newGearset.Body, GearSetSlot.Body);
            FillItem(newGearset.Hands, GearSetSlot.Hands);
            FillItem(newGearset.Legs, GearSetSlot.Legs);
            FillItem(newGearset.Feet, GearSetSlot.Feet);
            FillItem(newGearset.Earrings, GearSetSlot.Ear);
            FillItem(newGearset.Necklace, GearSetSlot.Neck);
            FillItem(newGearset.Bracelets, GearSetSlot.Wrist);
            FillItem(newGearset.Ring1, GearSetSlot.Ring1);
            FillItem(newGearset.Ring2, GearSetSlot.Ring2);

            void FillItem(GearEntry gearPiece, GearSetSlot slot)
            {
                if (gearPiece == null)
                {
                    classToChange.Gear[slot] = new();
                    return;
                }
                var itemEntry = GetItemByName(gearPiece.ItemName, out isHq);
                if (itemEntry == null)
                {
                    PluginLog.Warning($"Tried parsing the item <{gearPiece.ItemName}> but found nothing.");
                    classToChange.Gear[slot] = new();
                    return;
                }

                uint gearId = itemEntry.RowId;
                classToChange.Gear[slot] = new(gearId)
                {
                    IsHq = isHq
                };
                foreach (string materia in gearPiece.Materia)
                {
                    if (string.IsNullOrEmpty(materia))
                        return;
                    uint? materiaCategoryId = _materiaSheet?.FirstOrDefault(el =>
                        Array.Exists(el.Item, item => item.Value?.Name.RawString == materia))?.RowId;
                    if (materiaCategoryId == null)
                        continue;
                    MateriaCategory materiaCategory = (MateriaCategory)materiaCategoryId;
                    byte materiaLevel = TranslateMateriaLevel(materia.Remove(0, materia.LastIndexOf(" ")).Trim());

                    classToChange.Gear[slot].AddMateria(new(materiaCategory, materiaLevel));
                }
            }
            return new HrtUiMessage(
                string.Format(
                    Localize("LodestoneConnector:Sucess", "Updated {0}'s {1} gear from Lodestone."),
                    p.MainChar.Name, classToChange.Job),
                HrtUiMessageType.Success);
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "Error in Lodestone Gear fetch");
            return new HrtUiMessage(Localize("LodestoneConnector:Failure", "Could not update gear from Lodestone."), HrtUiMessageType.Error);
        }
    }

    /// <summary>
    /// Get an item from Lumina sheet by name.
    /// Also sets an Item to HQ if the last char in the itemname is the HQ-Symbol.
    /// </summary>
    /// <param name="name">Name of the item to be fetched.</param>
    /// <param name="isHq">Decides if the item is HQ.</param>
    /// <returns></returns>
    private Item? GetItemByName(string name, out bool isHq)
    {
        isHq = false;
        if (!char.IsLetterOrDigit(name.Last()))
        {
            name = name.Remove(name.Length - 1, 1);
            isHq = true;
        }
        return _itemSheet!.FirstOrDefault(item => item.Name.RawString.Equals(
            name, StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>
    /// Translate the last part of a Materias Name (i.e the "X" in "Savage Might Materia X") into a the materia's level.
    /// We subtract 1 because materia levels start at 0 internally.
    /// </summary>
    /// <param name="numeral">The numeral to be translated.</param>
    /// <returns></returns>
    private byte TranslateMateriaLevel(string numeral)
    {
        byte sum = 0;
        for (int i = 0; i < numeral.Length; i++)
        {
            char cur = numeral[i];
            _romanNumerals.TryGetValue(cur, out byte val);
            if (i + 1 < numeral.Length && _romanNumerals[numeral[i + 1]] > _romanNumerals[cur])
                sum -= val;
            else
                sum += val;
        }
        sum -= 1;
        return sum;
    }
}

internal class NetstoneBase
{
    private readonly LodestoneClient _lodestoneClient;

    private readonly RateLimit _rateLimit;
    private readonly TimeSpan _cacheTime;
    private readonly ConcurrentDictionary<string, DateTime> _currentRequests;
    private readonly ConcurrentDictionary<string, (DateTime time, LodestoneCharacter response)> _cachedRequests;

    internal NetstoneBase(RateLimit rateLimit = default, TimeSpan? cacheTime = null)
    {
        _lodestoneClient = GetLodestoneClient();
        _rateLimit = rateLimit;
        _cacheTime = cacheTime ?? new(0, 15, 0);
        _currentRequests = new();
        _cachedRequests = new();
    }

    private void UpdateCache()
    {
        foreach (var req in _cachedRequests.Where(e => e.Value.time + _cacheTime < DateTime.Now))
        {
            _cachedRequests.TryRemove(req.Key, out _);
        }
    }

    /// <summary>
    /// Fetch a given character from lodestone either by it's name and homeworld or by it's lodestone id.
    /// Returns null if no character by that id or name could be found.
    /// </summary>
    /// <param name="c">Character to be fetched.</param>
    /// <returns></returns>
    internal async Task<LodestoneCharacter?> FetchCharacterFromLodestone(Character c)
    {
        UpdateCache();
        while (RateLimitHit() || _currentRequests.ContainsKey(c.Name))
            Thread.Sleep(1000);
        if (_cachedRequests.TryGetValue(c.Name, out var result))
            return result.response;

        _currentRequests.TryAdd(c.Name, DateTime.Now);
        try
        {
            var homeWorld = c.HomeWorld;
            LodestoneCharacter? foundCharacter;
            if (c.LodestoneID == 0)
            {
                if (c.HomeWorldID == 0 || homeWorld == null)
                    return null;
                PluginLog.Log("Using name and homeworld to search...");
                var netstoneResponse = await _lodestoneClient!.SearchCharacter(new CharacterSearchQuery()
                {
                    CharacterName = c.Name,
                    World = homeWorld.Name.RawString
                });
                var characterEntry = netstoneResponse?.Results.FirstOrDefault(
                    (res) => res.Name == c.Name);
                if (!int.TryParse(characterEntry?.Id, out c.LodestoneID))
                    PluginLog.Warning("Tried parsing LodestoneID but failed.");
                foundCharacter = characterEntry?.GetCharacter().Result;
            }
            else
            {
                PluginLog.Log("Using ID to search...");
                foundCharacter = await _lodestoneClient!.GetCharacter(c.LodestoneID.ToString());
            }
            if (foundCharacter == null)
                return null;
            _cachedRequests.TryAdd(c.Name, (DateTime.Now, foundCharacter));
            return foundCharacter;
        }
        catch (Exception e)
        {
            PluginLog.LogError(e.Message);
            return null;
        }
        finally
        {
            _currentRequests.TryRemove(c.Name, out _);
        }
    }

    private bool RateLimitHit()
    {
        return _currentRequests.Count + _cachedRequests.Count(e => e.Value.time + _rateLimit.Time > DateTime.Now) > _rateLimit.MaxRequests;
    }

    private static LodestoneClient GetLodestoneClient()
    {
        var result = GetLodestoneClientAsync();
        result.Wait();
        return result.Result;
    }

    private static async Task<LodestoneClient> GetLodestoneClientAsync()
    {
        return await LodestoneClient.GetClientAsync();
    }
}
