using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Modules.Core.Ui;

internal class ChangeLogUi : HrtWindow
{
    private ChangeLog _log;
    public ChangeLogUi(ChangeLog log)
    {
        _log = log;
    }
    public override void Draw() => throw new NotImplementedException();
}