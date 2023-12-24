using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.UI;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool.Plugin.DataManagement;

internal class RaidGroupDb : DataBaseTable<RaidGroup, Player>
{
    public RaidGroupDb(IIdProvider idProvider, string serializedData, HrtIdReferenceConverter<Player>? conv,
        JsonSerializerSettings settings) : base(idProvider, serializedData, conv, settings)
    {
    }

    public override HashSet<HrtId> GetReferencedIds()
    {
        HashSet<HrtId> referencedIds = new();
        foreach (Player p in from g in Data.Values from player in g select player)
        {
            referencedIds.Add(p.LocalId);
        }
        return referencedIds;
    }
    public override HrtWindow OpenSearchWindow(Action<RaidGroup> onSelect, Action? onCancel = null) =>
        new GroupSearchWindow(this, onSelect, onCancel);

    internal class GroupSearchWindow : SearchWindow<RaidGroup, RaidGroupDb>
    {
        public GroupSearchWindow(RaidGroupDb dataBase, Action<RaidGroup> onSelect, Action? onCancel) : base(dataBase,
            onSelect, onCancel)
        {
        }

        protected override void DrawContent() => throw new NotImplementedException();
    }
}