using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game;
using HimbeertoniRaidTool.UI;

namespace HimbeertoniRaidTool
{
    public interface IHrtModule
    {
        string Name { get; }
        string Description { get; }
        IEnumerable<HrtCommand> Commands { get; }
        void HandleMessage(HrtUiMessage message);
        void Update(Framework fw);
        void Dispose();

    }
    public struct HrtCommand
    {
        /// <summary>
        /// Needs to start with a "/"
        /// </summary>
        internal string Command;
        internal string Description;
        internal bool ShowInHelp;
        internal Action<string> OnCommand;
    }
}
