using System;
using System.Collections.Generic;
using Dalamud.Game;
using HimbeertoniRaidTool.UI;

namespace HimbeertoniRaidTool
{
    public interface IHrtModule<T, S> : IHrtModule where T : new() where S : IHrtConfigUi
    {
        HRTConfiguration<T, S> Configuration { get; }
    }
    public interface IHrtModule
    {
        string Name { get; }
        string InternalName { get; }
        string Description { get; }
        IEnumerable<HrtCommand> Commands { get; }
        void HandleMessage(HrtUiMessage message);
        void AfterFullyLoaded();
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
