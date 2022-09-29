using Dalamud.Logging;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.UI;
using Lumina.Excel.GeneratedSheets;
using NetStone;
using NetStone.Model.Parseables.Character;
using NetStone.Model.Parseables.Character.Gear;
using NetStone.Search.Character;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Connectors
{
    internal static class NetStoneConnector
    {
        private static LodestoneClient? lodestoneClient = null;
        private static Lumina.Excel.ExcelSheet<Item>? itemSheet;
        
        public static async Task<HrtUiMessage> Debug(Player p)
        {
            itemSheet ??= Services.DataManager.GetExcelSheet<Item>()!;
            lodestoneClient ??= await LodestoneClient.GetClientAsync();
            Job? foundJob;

            try
            {
                // Lookup player lodestone id - if not found, search by name and add id
                //LodestoneCharacter? lodestoneCharacter = await FetchCharacterFromLodestone(p.MainChar);
                LodestoneCharacter? lodestoneCharacter = await FetchDebugCharacter("Pip Pipperino", "Odin");
                if (lodestoneCharacter == null)
                    return new HrtUiMessage("Character not found on Lodestone.", HrtUiMessageType.Failure);
                if (lodestoneCharacter.Gear.Soulcrystal != null)
                    foundJob = (Job?)GetItemByName(lodestoneCharacter.Gear.Soulcrystal.ItemName)?.ClassJobUse.Row;
                else
                    foundJob = (Job?)GetItemByName(lodestoneCharacter.Gear.Mainhand.ItemName)?.ClassJobUse.Row;
                if (foundJob == null || !Enum.IsDefined(typeof(Job), foundJob))
                    return new HrtUiMessage("Could not resolve currently used job or currently displayed job on " +
                        "Lodestone is not supported.", HrtUiMessageType.Failure);
                PluginLog.Log($"Found Job {foundJob}");
                PlayableClass classToChange = p.MainChar.GetClass((Job)foundJob!);
                //classToChange.Level = lodestoneCharacter.ActiveClassJobLevel;
                //Getting Race, Clan and Gender is not yet correctly implemented by Netstone 1.0.0
                //classToChange.Tribe = (unit)lodestoneCharacter.RaceClanGender;
                CharacterGear newGearset = lodestoneCharacter.Gear; 
                // Populate GearSet with newGearset
                
                
                void FillItem(GearEntry gearPiece, GearSetSlot slot)
                {
                    if (gearPiece == null)
                    {
                        classToChange.Gear[slot] = new();
                        return;
                    }
                    Item? itemEntry = GetItemByName(gearPiece.ItemName);
                    
                    if (itemEntry == null)
                        return;
                    uint gearId = itemEntry.RowId;
                    classToChange.Gear[slot] = new(gearId);
                }
                return new HrtUiMessage($"Updated {p.MainChar.Name}'s {classToChange.Job} gear from Lodestone.", HrtUiMessageType.Success);
            }
            catch
            {
                return new HrtUiMessage("Could not successfully update gear from Lodestone.", HrtUiMessageType.Error);
            }
        }

        private static Item? GetItemByName(string name)
        {
            return itemSheet!.FirstOrDefault(item => item.Name.RawString.Equals(
                name, System.StringComparison.InvariantCultureIgnoreCase));
        }

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

        private static async Task<LodestoneCharacter> FetchDebugCharacter(string name, string world)
        {
            return await lodestoneClient.SearchCharacter(new CharacterSearchQuery()
            {
                CharacterName = name,
                World = world
            }).Result.Results.FirstOrDefault(character => character.Name == name).GetCharacter();
        }

        public static bool GetCurrentGearFromLodestone(Player p)
        {
            string name = p.MainChar.Name;
            string world = p.MainChar.HomeWorld?.Name ?? "";
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(world))
                return false;

            /*var requestedChar = MakeCharacterSearchRequest(name, world);
            requestedChar.Wait();
            LodestoneCharacter? response = requestedChar.Result;
            // TODO: Add Lodestone ID to character if not already there,
            // if already there use lodestoneClient.GetCharacter() instead!
            if (response == null)
                return false;*/
            // TODO: Netstone does not expose the current active class in the LodestoneCharacter class, even though
            // it's technically there. Either fork and add that functionality, or write a short translator from
            // main hand -> job
            
            


            //TODO: Use Gear replacement code from XIVAPIConnector.cs
            
            //TODO: Translate item name to item id
            
            // Replace current Gear with new gear
            return true;
        }




    }
}
