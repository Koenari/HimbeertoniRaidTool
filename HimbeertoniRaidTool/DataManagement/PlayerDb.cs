using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class PlayerDb : DataBaseTable<Player, Character>
{

    public PlayerDb(IIdProvider idProvider, string serializedData, HrtIdReferenceConverter<Character> conv, JsonSerializerSettings settings) : base(idProvider, serializedData, conv, settings)
    {
    }

}