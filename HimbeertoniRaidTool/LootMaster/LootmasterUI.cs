using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using HimbeertoniRaidTool.Data;

namespace HimbeertoniRaidTool.LootMaster
{
    public class LootmasterUI : HrtUI
    {
        private RaidGroup Group;
        public LootmasterUI(HRTPlugin plugin, RaidGroup group) : base(plugin)
        {
            this.Group = group;
        }
        public override void Dispose()
        {

        }

        public override void Draw()
        {
            ImGui.SetNextWindowSize(new Vector2(100, 100),ImGuiCond.Always);
            throw new NotImplementedException();
        }
        private void DrawPlayer()
        {

        }
    }
}
