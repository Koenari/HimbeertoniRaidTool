using Dalamud.Game;
using Dalamud.Logging;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.UI;
using Lumina.Excel.GeneratedSheets;
using NetStone;
using NetStone.Model.Parseables.Character;
using NetStone.Model.Parseables.Character.Gear;
using NetStone.Search.Character;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Connectors
{
    internal class LodestoneConnector : NetstoneBase
    {
        private readonly Lumina.Excel.ExcelSheet<Item>? _itemSheet;
        private readonly Lumina.Excel.ExcelSheet<Materia>? _materiaSheet;
        private readonly Dictionary<char, byte> _romanNumerals;

        private const int NoOfAllowedLodestoneRequests = 8;

        internal LodestoneConnector(Framework fw) : base(fw, new(NoOfAllowedLodestoneRequests, new(0, 0, 30)))
        {
            this._itemSheet = Services.DataManager.GetExcelSheet<Item>();
            this._materiaSheet = Services.DataManager.GetExcelSheet<Materia>();
            this._romanNumerals = new() {
                { 'I', 1 }, { 'V', 5 }, { 'X', 10 }, { 'L', 50 }
            };
        }

        public async Task<HrtUiMessage> UpdateCharacter(Player p)
        {
            Job? foundJob = null;
            bool isHq = false;

            try
            {
                LodestoneCharacter? lodestoneCharacter = await FetchCharacterFromLodestone(p.MainChar);
                if (lodestoneCharacter == null)
                    return new HrtUiMessage("Character not found on Lodestone.", HrtUiMessageType.Failure);
                if (lodestoneCharacter.Gear.Soulcrystal != null)
                    foundJob = (Job?)GetItemByName(lodestoneCharacter.Gear.Soulcrystal.ItemName, out isHq)?.ClassJobUse.Row;
                else
                    foundJob = (Job?)GetItemByName(lodestoneCharacter.Gear.Mainhand.ItemName, out isHq)?.ClassJobUse.Row;
                if (foundJob == null || !Enum.IsDefined(typeof(Job), foundJob))
                    return new HrtUiMessage("Could not resolve currently used job or currently displayed job on " +
                        "Lodestone is not supported.", HrtUiMessageType.Failure);

                PlayableClass classToChange = p.MainChar.GetClass((Job)foundJob!);
                classToChange.Level = lodestoneCharacter.ActiveClassJobLevel;
                //Getting Race, Clan and Gender is not yet correctly implemented in Netstone 1.0.0
                //classToChange.Tribe = (unit)lodestoneCharacter.RaceClanGender;

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

                void FillItem(GearEntry gearPiece, GearSetSlot slot)
                {
                    if (gearPiece == null)
                    {
                        classToChange.Gear[slot] = new();
                        return;
                    }
                    Item? itemEntry = GetItemByName(gearPiece.ItemName, out isHq);
                    if (itemEntry == null)
                    {
                        PluginLog.Warning($"Tried parsing the item <{gearPiece.ItemName}> but found nothing.");
                        classToChange.Gear[slot] = new();
                        return;
                    }

                    uint gearId = itemEntry.RowId;
                    classToChange.Gear[slot] = new(gearId);
                    classToChange.Gear[slot].IsHq = isHq;
                    foreach (string materia in gearPiece.Materia)
                    {
                        if (string.IsNullOrEmpty(materia))
                            return;
                        uint? materiaCategoryId = _materiaSheet!.FirstOrDefault(el =>
                            Array.Exists(el.Item, item => item.Value.Name == materia))?.RowId;
                        MateriaCategory materiaCategory = (MateriaCategory)materiaCategoryId;
                        byte materiaLevel = TranslateMateriaLevel(materia.Remove(0, materia.LastIndexOf(" ")).Trim());

                        classToChange.Gear[slot].Materia.Add(new(materiaCategory, materiaLevel));
                    }
                }
                return new HrtUiMessage($"Updated {p.MainChar.Name}'s {classToChange.Job} gear from Lodestone.",
                    HrtUiMessageType.Success);
            }
            catch (Exception e)
            {
                PluginLog.LogError(e.Message);
                PluginLog.LogError(e.StackTrace ?? "");
                return new HrtUiMessage("Could not successfully update gear from Lodestone.", HrtUiMessageType.Error);
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
            return this._itemSheet!.FirstOrDefault(item => item.Name.RawString.Equals(
                name, System.StringComparison.InvariantCultureIgnoreCase));
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
                this._romanNumerals.TryGetValue(cur, out byte val);
                if (i + 1 < numeral.Length && this._romanNumerals[numeral[i + 1]] > this._romanNumerals[cur])
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

        internal NetstoneBase(Framework fw, RateLimit rateLimit = default, TimeSpan? cacheTime = null)
        {
            this._lodestoneClient = GetLodestoneClient();
            this._rateLimit = rateLimit;
            this._cacheTime = cacheTime ?? new(0, 15, 0);
            this._currentRequests = new();
            this._cachedRequests = new();
            fw.Update += Update;
        }

        private void Update(Framework fw)
        {
            foreach (var req in this._cachedRequests.Where(e => e.Value.time + _cacheTime < DateTime.Now))
            {
                this._cachedRequests.TryRemove(req.Key, out _);
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
            while (RateLimitHit() || _currentRequests.ContainsKey(c.Name))
                Thread.Sleep(1000);
            if (_cachedRequests.TryGetValue(c.Name, out var result))
                return result.response;

            _currentRequests.TryAdd(c.Name, DateTime.Now);
            try
            {
                World? homeWorld = c.HomeWorld;
                LodestoneCharacter? foundCharacter;
                if (c.LodestoneID == 0)
                {
                    if (c.HomeWorldID == 0 || homeWorld == null)
                        return null;
                    PluginLog.Log("Using name and homeworld to search...");
                    var netstoneResponse = await this._lodestoneClient!.SearchCharacter(new CharacterSearchQuery()
                    {
                        CharacterName = c.Name,
                        World = homeWorld.Name.RawString
                    });
                    var characterEntry = netstoneResponse.Results.FirstOrDefault(
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
            return (_currentRequests.Count + _cachedRequests.Count(e => e.Value.time + _rateLimit.Time > DateTime.Now)) > _rateLimit.MaxRequests;
        }

        private LodestoneClient GetLodestoneClient()
        {
            Task<LodestoneClient> result = GetLodestoneClientAsync();
            result.Wait();
            return result.Result;
        }

        private async Task<LodestoneClient> GetLodestoneClientAsync()
        {
            return await LodestoneClient.GetClientAsync();
        }
    }
}
