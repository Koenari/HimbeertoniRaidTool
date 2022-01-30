using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Data
{
    class PlayableClass
    {
        public PlayableClass (AvailabbleClasses ClassNameArg)
        {
            this.ClassName = ClassNameArg;
        }
        public readonly AvailabbleClasses ClassName;
        public GearSet Gear = new();
        public GearSet BIS = new();

    }

    enum AvailabbleClasses
    {
        AST,
        BLM,
        BLU,
        BRD,
        DNC,
        DRG,
        DRK,
        GNB,
        MCH,
        MNK,
        NIN,
        PLD,
        RDM,
        RPR,
        SAM,
        SCH,
        SGE,
        SMN,
        WAR,
        WHM,
        ALC,
        ARM,
        BSM,
        BTN,
        CRP,
        CUL,
        FSH,
        GSM,
        LTW,
        MIN,
        WVR
    }
}