using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class RaidGroupDb : DataBaseTable<RaidGroup, Player>
{
    public RaidGroupDb(IIdProvider idProvider, string serializedData, HrtIdReferenceConverter<Player>? conv, JsonSerializerSettings settings) : base(idProvider, serializedData, conv, settings)
    {
    }
}