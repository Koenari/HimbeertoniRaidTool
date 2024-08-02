﻿using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Dalamud;
using Dalamud.Game;
using HimbeertoniRaidTool.Plugin.Connectors.Utils;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using NetStone;
using NetStone.Model.Parseables.Character;
using NetStone.Model.Parseables.Character.Gear;
using NetStone.Model.Parseables.Search.Character;
using NetStone.Search.Character;

namespace HimbeertoniRaidTool.Plugin.Connectors;

internal class LodestoneConnector : NetStoneBase
{
    private const int NO_OF_ALLOWED_LODESTONE_REQUESTS = 8;
    private readonly ExcelSheet<Item>? _itemSheet;
    private readonly ExcelSheet<Materia>? _materiaSheet;
    private readonly Dictionary<char, byte> _romanNumerals;

    internal LodestoneConnector() : base(new RateLimit(NO_OF_ALLOWED_LODESTONE_REQUESTS, new TimeSpan(0, 1, 30)))
    {
        _itemSheet = ServiceManager.DataManager.GetExcelSheet<Item>(ClientLanguage.English);
        _materiaSheet = ServiceManager.DataManager.GetExcelSheet<Materia>(ClientLanguage.English);
        _romanNumerals = new Dictionary<char, byte>
        {
            { 'I', 1 }, { 'V', 5 }, { 'X', 10 }, { 'L', 50 },
        };
    }

    public bool CanBeUsed => Initialized;

    public HrtUiMessage UpdateCharacter(Player p)
    {
        var updateAsync = UpdateCharacterAsync(p);
        updateAsync.Wait();
        return updateAsync.Result;
    }

    public async Task<HrtUiMessage> UpdateCharacterAsync(Player p)
    {
        bool isHq;

        try
        {
            LodestoneCharacter? lodestoneCharacter = await FetchCharacterFromLodestone(p.MainChar);
            if (lodestoneCharacter == null)
                return new HrtUiMessage(GeneralLoc.LodestoneConnector_err_CharNotFound, HrtUiMessageType.Failure);
            Job? foundJob;
            if (lodestoneCharacter.Gear.Soulcrystal != null)
                foundJob = (Job?)GetItemByName(lodestoneCharacter.Gear.Soulcrystal.ItemName, out isHq)?.ClassJobUse.Row;
            else
                foundJob = (Job?)GetItemByName(lodestoneCharacter.Gear.Mainhand?.ItemName, out isHq)?.ClassJobUse.Row;
            if (foundJob == null || !Enum.IsDefined(typeof(Job), foundJob))
                return new HrtUiMessage(GeneralLoc.LodestoneConnector_err_JobIncompatible, HrtUiMessageType.Failure);

            PlayableClass? classToChange = p.MainChar[foundJob.Value];
            if (classToChange == null)
            {
                classToChange = p.MainChar.AddClass(foundJob.Value);
                bool hasError = false;
                hasError |= !ServiceManager.HrtDataManager.GearDb.TryAdd(classToChange.CurGear);
                hasError |= !ServiceManager.HrtDataManager.GearDb.TryAdd(classToChange.CurBis);
                if (hasError)
                    return new HrtUiMessage(GeneralLoc.LodestoneConnector_err_FailedToCreateGear,
                                            HrtUiMessageType.Failure);
            }

            classToChange.Level = lodestoneCharacter.ActiveClassJobLevel;
            //Getting Race, Clan and Gender is not yet correctly implemented in NetStone 1.0.0
            //classToChange.Parent.TribeID = (uint)lodestoneCharacter.RaceClanGender;

            // Populate GearSet with newGearset
            CharacterGear newGearset = lodestoneCharacter.Gear;
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

            void FillItem(GearEntry? gearPiece, GearSetSlot slot)
            {
                if (gearPiece == null)
                {
                    classToChange.CurGear[slot] = new GearItem();
                    return;
                }

                Item? itemEntry = GetItemByName(gearPiece.ItemName, out isHq);
                if (itemEntry == null)
                {
                    ServiceManager.Logger.Warning(
                        $"Tried parsing the item <{gearPiece.ItemName}> but found nothing.");
                    classToChange.CurGear[slot] = new GearItem();
                    return;
                }

                uint gearId = itemEntry.RowId;
                //ToDO: parse relic stats, until then skip relics already present
                if (classToChange.CurGear[slot].IsRelic() && classToChange.CurGear[slot].Id == gearId) return;
                classToChange.CurGear[slot] = new GearItem(gearId)
                {
                    IsHq = isHq,
                };
                foreach (string materia in gearPiece.Materia)
                {
                    if (string.IsNullOrEmpty(materia))
                        return;
                    uint? materiaCategoryId = _materiaSheet?.FirstOrDefault(el =>
                                                                                Array.Exists(
                                                                                    el.Item,
                                                                                    item => item.Value?.Name.RawString
                                                                                     == materia))?.RowId;
                    if (materiaCategoryId == null)
                        continue;
                    var materiaCategory = (MateriaCategory)materiaCategoryId;
                    MateriaLevel materiaLevel =
                        TranslateMateriaLevel(materia.Remove(0, materia.LastIndexOf(" ", StringComparison.Ordinal))
                                                     .Trim());

                    classToChange.CurGear[slot].AddMateria(new HrtMateria(materiaCategory, materiaLevel));
                }
            }

            return new HrtUiMessage(
                string.Format(GeneralLoc.LodestoneConnector_msg_Success, p.MainChar.Name, classToChange.Job),
                HrtUiMessageType.Success);
        }
        catch (Exception e)
        {
            ServiceManager.Logger.Error(e, "Error in Lodestone Gear fetch");
            return new HrtUiMessage(GeneralLoc.LodestoneConnector_err_GeneralFailure, HrtUiMessageType.Error);
        }
    }

    /// <summary>
    ///     Get an item from Lumina sheet by name.
    ///     Also sets an Item to HQ if the last char in the item name is the HQ-Symbol.
    /// </summary>
    /// <param name="name">Name of the item to be fetched.</param>
    /// <param name="isHq">Decides if the item is HQ.</param>
    /// <returns></returns>
    private Item? GetItemByName(string? name, out bool isHq)
    {
        isHq = false;
        if (name == null) return null;
        if (!char.IsLetterOrDigit(name.Last()))
        {
            name = name.Remove(name.Length - 1, 1);
            isHq = true;
        }

        return _itemSheet!.FirstOrDefault(item => item.Name.RawString.Equals(
                                              name, StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>
    ///     Translate the last part of a Materia name (i.e the "X" in "Savage Might Materia X") into a the
    ///     Materia level.
    ///     We subtract 1 because materia levels start at 0 internally.
    /// </summary>
    /// <param name="numeral">The numeral to be translated.</param>
    /// <returns></returns>
    private MateriaLevel TranslateMateriaLevel(string numeral)
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
        return (MateriaLevel)sum;
    }
}

internal class NetStoneBase : IDisposable
{
    private readonly ConcurrentDictionary<string, (DateTime time, LodestoneCharacter response)> _cachedRequests;
    private readonly TimeSpan _cacheTime;
    private readonly ConcurrentDictionary<string, DateTime> _currentRequests;
    private readonly LodestoneClient _lodestoneClient;

    private readonly RateLimit _rateLimit;
    protected readonly bool Initialized;

    internal NetStoneBase(RateLimit rateLimit = default, TimeSpan? cacheTime = null)
    {
        Initialized = false;
        try
        {
            _lodestoneClient = GetLodestoneClient();
            Initialized = true;
        }
        catch (Exception)
        {
            ServiceManager.Logger.Error("Lodestone Connector could not be initialized");
            _lodestoneClient = null!;
        }
        _rateLimit = rateLimit;
        _cacheTime = cacheTime ?? new TimeSpan(1, 30, 0);
        _currentRequests = new ConcurrentDictionary<string, DateTime>();
        _cachedRequests = new ConcurrentDictionary<string, (DateTime time, LodestoneCharacter response)>();
    }

    private void UpdateCache()
    {
        foreach (var req in _cachedRequests.Where(e => e.Value.time + _cacheTime < DateTime.Now))
        {
            _cachedRequests.TryRemove(req.Key, out _);
        }
    }

    /// <summary>
    ///     Fetch a given character from lodestone either by it's name and home world or by it's lodestone
    ///     id.
    ///     Returns null if no character by that id or name could be found.
    /// </summary>
    /// <param name="c">Character to be fetched.</param>
    /// <returns></returns>
    internal async Task<LodestoneCharacter?> FetchCharacterFromLodestone(Character c)
    {
        UpdateCache();
        while (RateLimitHit() || _currentRequests.ContainsKey(c.Name))
        {
            Thread.Sleep(1000);
        }
        if (_cachedRequests.TryGetValue(c.Name, out (DateTime time, LodestoneCharacter response) result))
            return result.response;

        _currentRequests.TryAdd(c.Name, DateTime.Now);
        try
        {
            World? homeWorld = c.HomeWorld;
            LodestoneCharacter? foundCharacter;
            if (c.LodestoneId == 0)
            {
                if (c.HomeWorldId == 0 || homeWorld == null)
                    return null;
                ServiceManager.Logger.Info("Using name and home world to search...");
                CharacterSearchPage? netStoneResponse = await _lodestoneClient.SearchCharacter(new CharacterSearchQuery
                {
                    CharacterName = c.Name,
                    World = homeWorld.Name.RawString,
                });
                CharacterSearchEntry? characterEntry = netStoneResponse?.Results.FirstOrDefault(
                    res => res.Name == c.Name);
                if (!int.TryParse(characterEntry?.Id, out c.LodestoneId))
                    ServiceManager.Logger.Warning("Tried parsing LodestoneID but failed.");
                foundCharacter = characterEntry?.GetCharacter().Result;
            }
            else
            {
                ServiceManager.Logger.Information($"Using ID ({c.LodestoneId}) to search...");
                foundCharacter = await _lodestoneClient.GetCharacter(c.LodestoneId.ToString());
            }

            if (foundCharacter == null)
                return null;
            _cachedRequests.TryAdd(c.Name, (DateTime.Now, foundCharacter));
            return foundCharacter;
        }
        catch (Exception e)
        {
            ServiceManager.Logger.Error(e.Message);
            return null;
        }
        finally
        {
            _currentRequests.TryRemove(c.Name, out _);
        }
    }

    private bool RateLimitHit() =>
        _currentRequests.Count + _cachedRequests.Count(e => e.Value.time + _rateLimit.Time > DateTime.Now) >
        _rateLimit.MaxRequests;

    private static LodestoneClient GetLodestoneClient()
    {
        var result = LodestoneClient.GetClientAsync();
        result.Wait();
        return result.Result;
    }

    public void Dispose() => _lodestoneClient.Dispose();
}