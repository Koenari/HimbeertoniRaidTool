using Dalamud.Logging;
using HimbeertoniRaidTool.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Connectors
{
    public class EtroConnector : GearConnector
    {
        private const string BaseUri = "https://etro.gg/api/";
        private string EquipmentBaseUri { get => (BaseUri + "equipment/"); }
        private string GearsetBaseUri { get => (BaseUri + "gearsets/"); }
        private WebClient Client;
        public EtroConnector() {
            Client = new WebClient();
            Client.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
        }
        public async Task<bool> GetGearStats(GearItem item)
        {
            if (item.ID < 1)
                return false;
            EtroGearItem? etroResponse;
            try
            {
                string jsonResponse = await new StreamReader(Client.OpenRead(EquipmentBaseUri + item.ID)).ReadToEndAsync();
                etroResponse = JsonConvert.DeserializeObject<EtroGearItem>(jsonResponse);
            } catch (WebException e)
            {
                PluginLog.LogError(e.Message);
                return false;
            }
            
            if (etroResponse == null)
                return false;
            item.name = etroResponse.name == null ? "" : etroResponse.name;
            item.description = etroResponse.description == null ? "" : etroResponse.description;
            item.itemLevel = etroResponse.itemLevel;
            return true;
        }

        public async Task<bool> GetGearSet(GearSet set)
        {
            string id = set.EtroID;
            if (id.Equals(""))
                return false;
            EtroGearSet? etroSet;
            try
            {
                string jsonResponse = await new StreamReader(Client.OpenRead(GearsetBaseUri + id)).ReadToEndAsync();
                etroSet = JsonConvert.DeserializeObject<EtroGearSet>(jsonResponse);
            }
            catch (WebException e)
            {
                PluginLog.LogError(e.Message);
                return false;
            }
            
            if (etroSet == null)
                return false;
            set.Weapon = new(etroSet.weapon);
            set.Head = new(etroSet.head);
            set.Head = new(etroSet.head);
            set.Body = new(etroSet.body);
            set.Gloves = new(etroSet.hands);
            set.Legs = new(etroSet.legs);
            set.Feet = new(etroSet.feet);
            set.Earrings = new(etroSet.ears);
            set.Necklace = new(etroSet.neck);
            set.Bracelet = new(etroSet.wrists);
            set.Ring1 = new(etroSet.fingerL);
            set.Ring2 = new(etroSet.fingerR);
            set.OffHand = new(etroSet.offHand);
            await set.FillStats(this);
            return true;
        }

    }
    class EtroGearSet
    {
        public string? id { get; set; }
        public string? jobAbbrev { get; set; }
        public string? jobIconPath { get; set; }
        public string? clanName { get; set; }
        public bool isOwner { get; set; }
        public string? name { get; set; }
        public DateTime lastUpdate { get; set; }
        public int minItemLevel { get; set; }
        public int maxItemLevel { get; set; }
        public int minMateriaTier { get; set; }
        public int maxMateriaTier { get; set; }
        //public Materia materia { get; set; }
        public List<TotalParam>? totalParams { get; set; }
        //public object buffs { get; set; }
        //public object relics { get; set; }
        public int patch { get; set; }
        public int job { get; set; }
        public int clan { get; set; }
        public int weapon { get; set; }
        public int head { get; set; }
        public int body { get; set; }
        public int hands { get; set; }
        public int legs { get; set; }
        public int feet { get; set; }
        public int offHand { get; set; }
        public int ears { get; set; }
        public int neck { get; set; }
        public int wrists { get; set; }
        public int fingerL { get; set; }
        public int fingerR { get; set; }
        public int food { get; set; }
        public int medicine { get; set; }
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
    class TotalParam
    {
        public int? id { get; set; }
        public string? name { get; set; }
        public decimal value { get; set; }
        public string? units { get; set; }
    }
}
