using System;
using System.Collections.Generic;
using System.Text;

namespace PKHeX.Core
{
    internal class LearnLevel
    {
        internal LearnLevel(int _Move, int _MinLevel, GameVersion _Source, MoveSource _MoveSource, int _EvoPhase)
        {
            Move = _Move;
            MinLevel = _MinLevel;
            Source = _Source;
            EvoPhase = _EvoPhase;
            MoveSource = _MoveSource;
        }

        public int Move { get; set; }
        public int MinLevel { get; set; }
        public virtual int MaxLevel
        {
            get { return MinLevel; }
            set { MinLevel = value; }
        }
        public virtual bool IsTM => false;
        public GameVersion Source { get; set; }
        public int EvoPhase { get; set; }
        public bool NonTradeback => Move <= Legal.MaxMoveID_1;
        public int MinGeneration => NonTradeback ? 1 : 2;
        public int Generation => GameVersion.RBY.Contains(Source) ? 1 : 2;
        public MoveSource MoveSource { get; set; }
    }

    /*internal class LearnTM : LearnLevel
    {
        internal LearnTM(int _MinLevel, GameVersion _Source, int _EvoPhase, bool _NonTradeback)
            : base(_MinLevel, _Source, _EvoPhase, _NonTradeback)
        {

        }

        public override bool IsTM => true;
        public override int MaxLevel { get; set; }
    }*/
}
