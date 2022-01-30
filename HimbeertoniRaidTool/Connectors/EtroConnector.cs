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

        bool GearConnector.GetGearStats(GearItem item)
        {
            int itemID = item.GetID();
            string myJsonResponse = new StreamReader(Client.OpenRead(BaseUri + itemID)).ReadToEnd();
            EtroGearItem etroResponse = (EtroGearItem)JsonConvert.DeserializeObject(myJsonResponse);
            if (etroResponse == null)
                return false;
            item.name = etroResponse.name;
            item.description = etroResponse.description;
            item.itemLevel = etroResponse.itemLevel;
            return true;
        }
    }
}
