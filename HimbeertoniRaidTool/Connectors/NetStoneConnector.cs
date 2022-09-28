using Dalamud.Data;
using Dalamud.Logging;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.UI;
using Lumina.Excel.GeneratedSheets;
using Microsoft.VisualBasic;
using NetStone;
using NetStone.Model.Parseables.Character;
using NetStone.Search.Character;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Connectors
{
    internal static class NetStoneConnector
    {
        private static LodestoneClient? lodestoneClient = null;
        // Get Lumina Sheet over HRTPLUGIN.DataManger
        private static Lumina.Excel.ExcelSheet<Item>? itemSheet;
        
        public static Task<HrtUiMessage> Debug()
        {
            HrtUiMessage msg = new HrtUiMessage();
            string itemName = "Engraved Goatskin Grimoire";

            itemSheet ??= Services.DataManager.GetExcelSheet<Item>()!;
            List<Item> items = itemSheet.Where(item => item.Name.ToString().Equals(
                itemName, System.StringComparison.InvariantCultureIgnoreCase)).ToList();
            if (items.Count <= 0)
            {
                msg.Message = $"Items with the name {itemName} do not exist.";
                msg.MessageType = HrtUiMessageType.Failure;
                return Task.FromResult(msg);
            }
            msg.Message = $"Found item: {items[0].Name} can be used by {items[0].ClassJobUse.Value?.Abbreviation}.";
            msg.MessageType = HrtUiMessageType.Success;
            return Task.FromResult(msg);
        }


        public static bool GetCurrentGearFromLodestone(Player p)
        {
            string name = p.MainChar.Name;
            string world = p.MainChar.HomeWorld?.Name ?? "";
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(world))
                return false;

            var requestedChar = MakeCharacterSearchRequest(name, world);
            requestedChar.Wait();
            LodestoneCharacter? response = requestedChar.Result;
            // TODO: Add Lodestone ID to character if not already there,
            // if already there use lodestoneClient.GetCharacter() instead!
            if (response == null)
                return false;
            // TODO: Netstone does not expose the current active class in the LodestoneCharacter class, even though
            // it's technically there. Either fork and add that functionality, or write a short translator from
            // jobstone -> job
            PluginLog.Log($"Found {response.Name} on Lodestone.");
            itemSheet ??= Services.DataManager.GetExcelSheet<Item>()!;
            


            //TODO: Use Gear replacement code from XIVAPIConnector.cs
            
            //TODO: Translate item name to item id
            
            // Replace current Gear with new gear
            return true;
        }

        internal static async Task<LodestoneCharacter?> MakeCharacterSearchRequest(string name, string world)
        {
            lodestoneClient ??= await LodestoneClient.GetClientAsync();
            if (lodestoneClient == null)
                return null;

            PluginLog.Log($"Got client with id {lodestoneClient.GetType().Name}");

            try
            {
                var netstoneResponse = await lodestoneClient.SearchCharacter(new CharacterSearchQuery()
                {
                    CharacterName = name,
                    World = world,
                });
                return netstoneResponse.Results.FirstOrDefault(res => res.Name == name)?.GetCharacter().Result;
            }
            catch
            {
                PluginLog.Error("Something went wrong while fetching data from Lodestone.");
                return null;
            }
        }


    }
}
