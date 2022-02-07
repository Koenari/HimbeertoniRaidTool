using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#pragma warning disable IDE1006 // Benennungsstile
#pragma warning disable CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Erwägen Sie die Deklaration als Nullable.
namespace HimbeertoniRaidTool.Connectors
{
    class EtroGearItem
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int param0 { get; set; }
        public int param1 { get; set; }
        public int param2 { get; set; }
        public int param3 { get; set; }
        public object param4 { get; set; }
        public object param5 { get; set; }
        public int param0Value { get; set; }
        public int param1Value { get; set; }
        public int param2Value { get; set; }
        public int param3Value { get; set; }
        public int param4Value { get; set; }
        public int param5Value { get; set; }
        public MaxParams maxParams { get; set; }
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
        public string iconPath { get; set; }
        public int itemLevel { get; set; }
        public object itemSpecialBonus { get; set; }
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
        public string slotName { get; set; }
        public string jobName { get; set; }
        public int itemUICategory { get; set; }
        public int jobCategory { get; set; }
    }
    public class MaxParams
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
}
#pragma warning restore IDE1006 // Benennungsstile
#pragma warning restore CS8618 // Ein Non-Nullable-Feld muss beim Beenden des Konstruktors einen Wert ungleich NULL enthalten. Erwägen Sie die Deklaration als Nullable.