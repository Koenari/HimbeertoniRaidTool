using HimbeertoniRaidTool.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Connectors
{
    class EtroConnector : GearConnector
    {
        private readonly String BaseUri;
        private WebClient Client;
        public EtroConnector() {
            BaseUri = "https://etro.gg/api/equipment/";
            Client = new WebClient();
            Client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
        }

        async Task<bool> GearConnector.GetGearStats(GearItem item)
        {
            int itemID = item.GetID();
            string myJsonResponse = await new StreamReader(Client.OpenRead(BaseUri + itemID)).ReadToEndAsync();
            EtroGearItem? etroResponse = JsonConvert.DeserializeObject<EtroGearItem>(myJsonResponse);
            if (etroResponse == null)
                return false;
            item.name = etroResponse.name == null ? "" : etroResponse.name;
            item.description = etroResponse.description == null ? "" : etroResponse.description;
            item.itemLevel = etroResponse.itemLevel;
            return true;
        }
    }
    class EtroGearItem
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? description { get; set; }
        public int param0 { get; set; }
        public int param1 { get; set; }
        public int param2 { get; set; }
        public int param3 { get; set; }
        public object? param4 { get; set; }
        public object? param5 { get; set; }
        public int param0Value { get; set; }
        public int param1Value { get; set; }
        public int param2Value { get; set; }
        public int param3Value { get; set; }
        public int param4Value { get; set; }
        public int param5Value { get; set; }
        public MaxParams? maxParams { get; set; }
        public bool advancedMelding { get; set; }
        public int block { get; set; }
        public int blockRate { get; set; }
        public bool canBeHq { get; set; }
        public int damageMag { get; set; }
        public int damagePhys { get; set; }
        public int defenseMag { get; set; }
        public int defensePhys { get; set; }
        public int delay { get; set; }
        public int iconId { get; set; }
        public string? iconPath { get; set; }
        public int itemLevel { get; set; }
        public object? itemSpecialBonus { get; set; }
        public int itemSpecialBonusParam { get; set; }
        public int level { get; set; }
        public int materiaSlotCount { get; set; }
        public int materializeType { get; set; }
        public bool PVP { get; set; }
        public int rarity { get; set; }
        public int slotCategory { get; set; }
        public bool unique { get; set; }
        public bool untradable { get; set; }
        public bool weapon { get; set; }
        public bool canCustomize { get; set; }
        public string? slotName { get; set; }
        public string? jobName { get; set; }
        public int itemUICategory { get; set; }
        public int jobCategory { get; set; }
    }
    class MaxParams
    {
        public int _6 { get; set; }
        public int _10 { get; set; }
        public int _11 { get; set; }
        public int _19 { get; set; }
        public int _22 { get; set; }
        public int _27 { get; set; }
        public int _44 { get; set; }
        public int _45 { get; set; }
        public int _46 { get; set; }
        public int _70 { get; set; }
        public int _71 { get; set; }
        public int _72 { get; set; }
        public int _73 { get; set; }
    }
}
