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
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Connectors
{
    internal static class NetStoneConnector
    {
        private static LodestoneClient? lodestoneClient = null;
        private static Lumina.Excel.ExcelSheet<Item>? itemSheet;
        private static Lumina.Excel.ExcelSheet<Materia>? materiaSheet;
        private static Dictionary<char, byte> romanNumerals = new() {
            { 'I', 1 }, { 'V', 5 }, { 'X', 10 }, { 'L', 50 }
        };

        // This setup deactives the button once it's clicked, but that deactivates all instances of this button.
        // If used multiple times, maybe a caching approach which doesn't care how often the users fetch from lodestone
        // is a far better approach then.
        private static bool isActive = false;
        public static bool Active => isActive;
        
        /// <summary>
        /// Update current main char of a given player with the currently displayed information
        /// on lodestone regarding current active job, current active job level and current active job gearset including 
        /// materia.
        /// </summary>
        /// <param name="p">Player to be updated.</param>
        /// <returns></returns>
        public static async Task<HrtUiMessage> UpdateCharacterFromLodestone(Player p)
        {
            isActive = true;
            itemSheet ??= Services.DataManager.GetExcelSheet<Item>()!;
            materiaSheet ??= Services.DataManager.GetExcelSheet<Materia>()!;
            lodestoneClient ??= await LodestoneClient.GetClientAsync();
            Job? foundJob;
            bool isHq;

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
                //Getting Race, Clan and Gender is not yet correctly implemented by Netstone 1.0.0
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
                        uint? materiaCategoryId = materiaSheet!.FirstOrDefault(el =>
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
            finally
            {
                isActive = false;
            }
        }

        /// <summary>
        /// Fetch a given character from lodestone either by it's name and homeworld or by it's lodestone id.
        /// </summary>
        /// <param name="c">Character to be fetched.</param>
        /// <returns></returns>
        private static async Task<LodestoneCharacter?> FetchCharacterFromLodestone(Character c)
        {
            World? homeWorld = c.HomeWorld;
            LodestoneCharacter? foundCharacter;

            if (c.LodestoneID == 0)
            {
                if (c.HomeWorldID == 0 || homeWorld == null)
                    return null;
                PluginLog.Log("Using name and homeworld to search...");
                var netstoneResponse = await lodestoneClient!.SearchCharacter(new CharacterSearchQuery()
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
                foundCharacter = await lodestoneClient!.GetCharacter(c.LodestoneID.ToString());
            }
            return foundCharacter;
        }

        private static Item? GetItemByName(string name, out bool isHq)
        {
            isHq = false;
            if (!char.IsLetterOrDigit(name.Last()))
            {
                name = name.Remove(name.Length - 1, 1);
                isHq = true;
            }
            return itemSheet!.FirstOrDefault(item => item.Name.RawString.Equals(
                name, System.StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Translate the last part of a Materias Name (i.e the "X" in "Savage Might Materia X") into a the materia's level.
        /// We subtract 1 because materia levels start at 0 internally.
        /// </summary>
        /// <param name="numeral">The numeral to be translated.</param>
        /// <returns></returns>
        private static byte TranslateMateriaLevel(string numeral)
        {
            byte sum = 0;
            for (int i = 0; i < numeral.Length; i++)
            {
                char cur = numeral[i];
                romanNumerals.TryGetValue(cur, out byte val);
                if (i + 1 < numeral.Length && romanNumerals[numeral[i + 1]] > romanNumerals[cur])
                    sum -= val;
                else
                    sum += val;
            }
            sum -= 1;
            return sum;
        }
    }
}
