using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKHeX.Core
{
    public enum GBEncounterType
    {
        TradeEncounterG1 = 1,
        StaticEncounter = 2,
        WildEncounter = 3,
        EggEncounter = 9,
        TradeEncounterG2 = 10,
        SpecialEncounter = 20,
    }

    public class GBEncounterData
    {
        public int Level;
        public readonly int Species;
        public bool Gen2 => Generation == 2;
        public bool Gen1 => Generation == 1;
        public readonly int Generation;
        public readonly bool WasEgg;
        public readonly GBEncounterType Type;
        public readonly object Encounter;
        private GBEncounterData _G1Data;
        public GBEncounterData G1Data
        {
            get { if (Generation == 2) return _G1Data; return null; }
            set { _G1Data = value; }
        }

        public GBEncounterData(int species)
        {
            Generation = 2;
            Type = GBEncounterType.EggEncounter;
            Level = 5;
        }

        public GBEncounterData(PKM pkm, int gen, object enc)
        {
            Generation = gen;
            Encounter = enc;
            if (Encounter as EncounterTrade != null)
            {
                var trade = (EncounterTrade)Encounter;
                Level = trade.Level;
                Species = trade.Species;
                Type = gen== 2? GBEncounterType.TradeEncounterG2 : GBEncounterType.TradeEncounterG1;
            }
            else if (Encounter as EncounterStatic != null)
            {
                var statc = (EncounterStatic)Encounter;
                Level = statc.Level;
                Species = statc.Species;
                if(statc.Moves[0] != 0 && pkm.Moves.Contains(statc.Moves[0]))
                    Type = GBEncounterType.SpecialEncounter;
                else
                    Type = GBEncounterType.StaticEncounter;
            }   
            else if (Encounter as EncounterSlot1 != null)
            {
                var slot = (EncounterSlot1)Encounter;
                Level = slot.LevelMin;
                Species = slot.Species;
                Type = GBEncounterType.WildEncounter;
                if (pkm.HasOriginalMetLocation && slot.LevelMin >= pkm.Met_Level && pkm.Met_Level <= slot.LevelMax)
                    Level = pkm.Met_Level;
                else
                     Level = slot.LevelMin;
            }
        }
    }

}
