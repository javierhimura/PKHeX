using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKHeX.Core
{
    internal class EggChain
    {
        internal int Species;
        internal IEnumerable<int> Moves;
    }

    internal class EggFather
    {
        internal int EvolveSpecies;
        internal int EggSpecies;
        internal int Generation;
        internal int[] BreedingMoves;
        internal int[] ChainBreedingMoves;
        internal int[] EggBreedingMoves;
    }

    internal class EggBreeding
    {
        protected int GenOrigin;
        protected int GenFormat;
        protected List<int> PotentialEggChains;
        protected List<int>[] PotentialEggChainsEggGroup;
        protected int[][] BreedingMoves;
        protected int[][] ChainBreedingMoves;
        protected int[][] EggBreedingMoves;
        protected int[] EggMoves;
        protected PersonalTable Personal;
        protected EvolutionTree Evolves;
        EggBreeding[] CrossgenBreeding;
        protected EggBreeding[] SpecialBreeding;
        bool AllowCrossGen => CrossgenBreeding != null;

        internal EggBreeding()
        {

        }

        internal EggBreeding(int gFormat, EvolutionTree Evos, PersonalTable Perso, EggMoves[][] EggLearnSet)
            : this(gFormat, gFormat, Evos, Perso, EggLearnSet)
        {

        }

        internal EggBreeding(int gOrigin, int gFormat, EvolutionTree Evos, PersonalTable Perso, EggMoves[][] EggLearnSet)
        {
            GenOrigin = gOrigin;
            GenFormat = gFormat;
            Evolves = Evos;
            PotentialEggChains = new List<int>();
            PotentialEggChainsEggGroup = new List<int>[15];
            for (int i = 0; i < 15; i++)
            {
                PotentialEggChainsEggGroup[i] = new List<int>();
            }
            Personal = Perso;
            var maxSpecies = Legal.getMaxSpeciesOrigin(GenOrigin);
            BreedingMoves = new int[maxSpecies][];
            EggBreedingMoves = new int[maxSpecies][];
            ChainBreedingMoves = new int[maxSpecies][];
            EggMoves = EggLearnSet.SelectMany( e => e).SelectMany(e => e.Moves).Distinct().ToArray();
            for (int i = 1; i < maxSpecies; i++)
            {
                // Exclude female only,no gender and undiscover egg group species
                // Exclude unevolved species to simplify check,any move that can be legally breed can be legally breed by their final evolution forms too
                if (Personal[i].Gender >= 254 || Personal[i].EggGroups.Any(g => g == 15) || Evolves.CanEvolve(i))
                    continue;
                
                // Moves that the father pokemon can inherit to eggs that are not from his egg moves, these moves can all be breed without chains
                var nonegg_moves = Legal.getCanBreedMove(i, GenOrigin, GenFormat, EggMoves).ToArray();

                foreach (int eggspecie in Evolves.getEggSpecies(i))
                {               
                    // Set of moves that the father can inherit to eggs but are exclusive from his egg moves, that means a chain egg happens, the chain should be analyze
                    var LearnEggMoves = Legal.getCanBreedChainEggMoves(i, GenOrigin, GenFormat, EggMoves).ToArray();
                    var egg_moves = LearnEggMoves.Except(BreedingMoves[eggspecie]).Distinct().ToArray();
                    // A union of egg moves and not egg moves, when chains happens not all moves are egg moves in all the chain
                    var all_moves = EggBreedingMoves[i].Concat(BreedingMoves[i]).ToArray();

                    EggFather Father = new EggFather()
                    {
                        EvolveSpecies = i,
                        EggSpecies = eggspecie,
                        BreedingMoves = nonegg_moves,
                        EggBreedingMoves = egg_moves,
                        ChainBreedingMoves = all_moves

                    };
                    // Create list of potential fathers and also another list of potential fathers by egg group
                    PotentialEggChains.Add(i);
                    PotentialEggChainsEggGroup[Personal[i].EggGroups[0]].Add(i);
                    if (Personal[i].EggGroups[0] != Personal[i].EggGroups[1])
                        PotentialEggChainsEggGroup[Personal[i].EggGroups[1]].Add(i);
                }
            }

            foreach (int father in PotentialEggChains)
            {
                // Moves that the father pokemon can inherit to eggs that are not from his egg moves, these moves can all be breed without chains
                BreedingMoves[father] = Legal.getCanBreedMove(father, GenOrigin, GenFormat, EggMoves).ToArray();
                // Set of moves that the father can inherit to eggs but are exclusive from his egg moves, that means a chain egg happens, the chain should be analyze
                var LearnEggMoves = Legal.getCanBreedChainEggMoves(father, GenOrigin, GenFormat, EggMoves).ToArray();
                EggBreedingMoves[father] = LearnEggMoves.Except(BreedingMoves[father]).Distinct().ToArray();
                // A union of egg moves and not egg moves, when chains happens not all moves are egg moves in all the chain
                ChainBreedingMoves[father] = EggBreedingMoves[father].Concat(BreedingMoves[father]).ToArray();
            }
        }

        internal bool HaveNonChainFather(int species, IEnumerable<int> eggmoves)
        {
            IEnumerable<int> fathers = getCompatibleFathers(species, null);
            foreach (int father in fathers)
            {
                if (eggmoves.All(value => BreedingMoves[father].Contains(value)))
                    return true;
            }
            return false;
        }

        internal int[] getEggGroups(int species)
        {
            if (Personal[species].EggGroups.Contains(15))
            {
                return Evolves.TreeEggGroup(species);
            }
            return Personal[species].EggGroups;
        }

        internal IEnumerable<int> getCompatibleFathers(int species, IEnumerable<int> excludedfathers)
        {
            var eggGroup = getEggGroups(species);
            IEnumerable<int> fathers = PotentialEggChainsEggGroup[eggGroup[0]];
            if (eggGroup[0] != eggGroup[1])
                fathers = fathers.Union(PotentialEggChainsEggGroup[eggGroup[1]]).Distinct();
            if(excludedfathers != null)
                fathers = fathers.Where(f => !excludedfathers.Any(e => Evolves.getBaseSpecies(e, GenOrigin) == Evolves.getBaseSpecies(f, GenOrigin)));
            return fathers;
        }

        internal IEnumerable<EggChain> getEggChainCompatibleFathers(int species, IEnumerable<int> eggmoves, IEnumerable<int> fathers)
        {
            var getChainEggFathers = new List<EggChain>();
            foreach (int father in fathers)
            {
                if (eggmoves.All(value => ChainBreedingMoves[father].Contains(value)))
                {
                    var chainmoves = eggmoves.Except(BreedingMoves[father]);
                    getChainEggFathers.Add(new EggChain() { Species = father, Moves = chainmoves });
                }
            }
            return getChainEggFathers;
        }

        internal void CopyFromTemplate(EggBreeding GenOriginTemplate, EggBreeding GenFormatTemplate)
        {
            PotentialEggChains = GenOriginTemplate.PotentialEggChains;
            PotentialEggChainsEggGroup = GenOriginTemplate.PotentialEggChainsEggGroup;

            Evolves = GenFormatTemplate.Evolves;
            Personal = GenFormatTemplate.Personal;
            EggMoves = GenFormatTemplate.EggMoves;

            EggBreedingMoves = new int[GenOriginTemplate.EggBreedingMoves.Length][];
            BreedingMoves = new int[GenOriginTemplate.BreedingMoves.Length][];
            ChainBreedingMoves = new int[GenOriginTemplate.ChainBreedingMoves.Length][];
        }

        internal void SetCrossGenBreeding(EggBreeding[] Crossgen)
        {
            CrossgenBreeding = Crossgen;
        }

        internal bool ValidEggMoves(int species, IEnumerable<int> eggmoves)
        {
            return ValidEggMoves(species, eggmoves, new List<int>());
        }

        internal bool ValidEggMovesSpecial(int species, IEnumerable<int> eggmoves)
        {
            if (SpecialBreeding == null)
                return false;
            foreach (EggBreeding Special in SpecialBreeding)
            {
                if (Special.ValidEggMoves(species, eggmoves))
                    return true;
            }
            return false;
        }

        internal virtual bool ValidEggMoves(int species, IEnumerable<int> eggmoves, IEnumerable<int> PrevExcludedFathers)
        {
            if (HaveNonChainFather(species, eggmoves))
                return true;

            if (ValidEggMovesSpecial(species, eggmoves))
                return true;

            IEnumerable<int> ExcludedFathers = PrevExcludedFathers.Concat(new List<int> { species });
            var fathers = getCompatibleFathers(species, ExcludedFathers);
            var Chains = getEggChainCompatibleFathers(species, eggmoves, fathers);
            foreach (EggChain Chain in Chains)
            {
                if (ValidEggMoves(Chain.Species, Chain.Moves, ExcludedFathers))
                    return true;
            }

            if (AllowCrossGen)
            {
                foreach (EggBreeding CrossGen in CrossgenBreeding)
                {
                    if (CrossGen.ValidEggMoves(species, eggmoves))
                        return true;
                }
            }

            return false;
        }

        internal int[] GetBreedingMoves(int species)
        {
            return BreedingMoves[species];
        }

        internal static void TestBreeding(EggBreeding EB, int generation, EvolutionTree Evolves, EggMoves[] EggLearnSet)
        {
            int speciesegg = 0;
            int[] combinations = new int[] { 0, 0, 0, 0, 0 };
            int[] combinations_ok = new int[] { 0, 0, 0, 0, 0 };
            int[] combinations_ko = new int[] { 0, 0, 0, 0, 0 };
            DateTime Start = DateTime.Now;

            for(int species = 1; species <= Legal.getMaxSpeciesOrigin(generation); species ++)
            {
                if (Legal.NoHatchFromEgg.Contains(species))
                    continue;
                if (Evolves.getBaseSpecies(species, generation) != species)
                    continue;
                speciesegg++;

                if(species  == 172)
                {

                }

                for(int move1 = 0; move1 < EggLearnSet[species].Moves.Length; move1 ++)
                {
                    bool Move1Ok = EB.ValidEggMoves(species, new int[] { EggLearnSet[species].Moves[move1] });
                    combinations[1]++;
                    if (Move1Ok) combinations_ok[1]++; else combinations_ko[1]++;
                    for (int move2 = move1+1; move2 < EggLearnSet[species].Moves.Length; move2++)
                    {
                        bool Move2Ok = EB.ValidEggMoves(species, new int[] { EggLearnSet[species].Moves[move1], EggLearnSet[species].Moves[move2] });
                        combinations[2]++;
                        if (Move2Ok) combinations_ok[2]++; else combinations_ko[2]++;
                        for (int move3 = move2 + 1; move3 < EggLearnSet[species].Moves.Length; move3++)
                        {
                            bool Move3Ok = EB.ValidEggMoves(species, new int[] { EggLearnSet[species].Moves[move1], EggLearnSet[species].Moves[move2], EggLearnSet[species].Moves[move3] });
                            combinations[3]++;
                            if (Move3Ok) combinations_ok[3]++; else combinations_ko[3]++;
                            for (int move4 = move3 + 1; move4 < EggLearnSet[species].Moves.Length; move4++)
                            {
                                bool Move4Ok = EB.ValidEggMoves(species, new int[] { EggLearnSet[species].Moves[move1], EggLearnSet[species].Moves[move2], EggLearnSet[species].Moves[move3], EggLearnSet[species].Moves[move4] });
                                combinations[4]++;
                                if (Move4Ok) combinations_ok[4]++; else combinations_ko[4]++;
                            }
                        }
                    }
                }
            }

            DateTime End = DateTime.Now;
            TimeSpan Diff = End - Start;
        }
    }

    internal class EggBreedingGen2 : EggBreeding
    {
        bool IsTradeBack;
        EggBreedingGen2 AltBreeding;

        internal EggBreedingGen2(int GenOrigin, int GenFormat, EvolutionTree Evos, PersonalTable Perso, EggMoves[][] EggLearnSet)
               : base(GenOrigin, GenFormat, Evos, Perso, EggLearnSet)
        {
 
        }

        internal void SetTradebackBreesgin(EggBreedingGen2 Tradeback)
        {
            AltBreeding = Tradeback;
            IsTradeBack = false;
            AltBreeding.AltBreeding = this;
            AltBreeding.IsTradeBack = true;
        }

        internal IEnumerable<int> getCompatibleFathers(int species, IEnumerable<EggFather> excludedfathers)
        {
            var getChainEggFathers = new List<EggChain>();
            IEnumerable<int> fathers = PotentialEggChainsEggGroup[Personal[species].EggGroups[0]];
            if (Personal[species].EggGroups[0] != Personal[species].EggGroups[1])
                fathers = fathers.Union(PotentialEggChainsEggGroup[Personal[species].EggGroups[1]]).Distinct();
            fathers = fathers.Where(f => !excludedfathers.Any(e => e.Generation == GenOrigin && Evolves.getBaseSpecies(e.EvolveSpecies, GenOrigin) == Evolves.getBaseSpecies(f, GenOrigin)));
            return fathers;
        }

        private bool ValidEggMovesG1(int species, IEnumerable<int> eggmoves, IEnumerable<EggFather> PrevExcludedFathers)
        {
            if (!IsTradeBack)
                return AltBreeding.ValidEggMovesG1(species, eggmoves, PrevExcludedFathers);

            if (HaveNonChainFather(species, eggmoves))
                return true;

            IEnumerable<EggFather> ExcludedFathers = PrevExcludedFathers.Concat(new [] { new EggFather() { EvolveSpecies = species, Generation = 1 } });

            var fathers = getCompatibleFathers(species, ExcludedFathers);
            var Chains = getEggChainCompatibleFathers(species, eggmoves, fathers);
            foreach (EggChain Chain in Chains)
            {
                if (ValidEggMovesG1(Chain.Species, Chain.Moves, ExcludedFathers))
                    return true;
            }

            return false;
        }

        private bool ValidEggMovesG2(int species, int generation, IEnumerable<int> eggmoves, IEnumerable<EggFather> PrevExcludedFathers)
        {
            var have_gen2_eggmoves = eggmoves.Any(m => m > Legal.MaxMoveID_1);
            if (!have_gen2_eggmoves)
                return ValidEggMovesG1(species, eggmoves, PrevExcludedFathers);

            if (HaveNonChainFather(species, eggmoves))
                return true;
            if (AltBreeding.HaveNonChainFather(species, eggmoves))
                return true;

            if (ValidEggMovesSpecial(species, eggmoves))
                return true;

            IEnumerable<EggFather> ExcludedFathers = PrevExcludedFathers.Concat(new[] { new EggFather() { EvolveSpecies = species, Generation = generation } });

            var fathers = getCompatibleFathers(species, ExcludedFathers);
            var Chains = getEggChainCompatibleFathers(species, eggmoves, fathers);
            foreach (EggChain Chain in Chains)
            {
                if (ValidEggMovesG2(Chain.Species, GenOrigin, Chain.Moves, ExcludedFathers))
                    return true;
            }

            var fathers_alt = AltBreeding.getCompatibleFathers(species, ExcludedFathers);
            var Chains_alt = AltBreeding.getEggChainCompatibleFathers(species, eggmoves, fathers_alt);
            foreach (EggChain Chain in Chains_alt)
            {
                if (AltBreeding.ValidEggMovesG1(Chain.Species, Chain.Moves, ExcludedFathers))
                    return true;
            }

            return false;
        }

        internal override bool ValidEggMoves(int species, IEnumerable<int> eggmoves, IEnumerable<int> PrevExcludedFathers)
        {
            try
            {

            if (eggmoves.Any(m => m > Legal.MaxMoveID_1))
                return ValidEggMovesG2(species, 2, eggmoves, new List<EggFather>());
            else
                return ValidEggMovesG1(species, eggmoves, new List<EggFather>());
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }

    internal class EggBreedingCrossGen : EggBreeding
    {
        EggBreeding OriginBreeding;
        internal EggBreedingCrossGen(int GenOrigin, int GenFormat, EggBreeding GenOriginTemplate, EggBreeding GenFormatTemplate)
               : base()
        {
            OriginBreeding = GenOriginTemplate;
            CopyFromTemplate(GenOriginTemplate, GenFormatTemplate);

            foreach (int father in PotentialEggChains)
            {
                BreedingMoves[father] = Legal.getCanBreedMove(father, GenOrigin, GenFormat, EggMoves).ToArray();
                var LearnEggMoves = Legal.getCanBreedChainEggMoves(father, GenOrigin, GenFormat, EggMoves).ToArray();
                EggBreedingMoves[father] = LearnEggMoves.Except(BreedingMoves[father]).Intersect(EggMoves).Distinct().ToArray();
                ChainBreedingMoves[father] = EggBreedingMoves[father].Concat(BreedingMoves[father]).ToArray();
            }
        }

        internal override bool ValidEggMoves(int species, IEnumerable<int> eggmoves, IEnumerable<int> PrevExcludedFathers)
        {
            IEnumerable<int> ExcludedFathers = PrevExcludedFathers.Concat(new List<int> { species });
            if (HaveNonChainFather(species, eggmoves))
                return true;
            IEnumerable<EggChain> Chains = getEggChainCompatibleFathers(species, eggmoves, ExcludedFathers);
            foreach (EggChain Chain in Chains)
            {
                if (OriginBreeding.ValidEggMoves(Chain.Species, Chain.Moves, ExcludedFathers))
                    return true;
            }
            
            return false;
        }

    }

    internal class EggBreedingSpecial: EggBreeding
    {
        protected List<EggChain> SpecialBreedingMoves = new List<EggChain>();
        EggBreeding MovesTemplate;

        internal EggBreedingSpecial(int GenFormat, EggBreeding GenOriginTemplate, EggBreeding GenFormatTemplate, EggBreeding SourceTemplate)
               : base()
        {
            CopyFromTemplate(GenOriginTemplate, GenFormatTemplate);
            MovesTemplate = SourceTemplate;
            foreach (int father in PotentialEggChains)
            {
                BreedingMoves[father] = new int[] { };
                ChainBreedingMoves[father] = new int[] { };
            }
        }

        internal void AddSpecialFather(int GenOrigin, EggChain SpecialChain)
        {
            if(!BreedingMoves[SpecialChain.Species].Any())
            {
                BreedingMoves[SpecialChain.Species] = MovesTemplate.GetBreedingMoves(SpecialChain.Species);
            }
            SpecialBreedingMoves.Add(SpecialChain);
            ChainBreedingMoves[SpecialChain.Species] = BreedingMoves[SpecialChain.Species].Concat(SpecialBreedingMoves.Where(s=>s.Species == SpecialChain.Species).SelectMany(m=>m.Moves)).ToArray();
        }

        internal override bool ValidEggMoves(int species, IEnumerable<int> eggmoves, IEnumerable<int> PrevExcludedFathers)
        {
            return HaveSpecialFathers(species, eggmoves);
        }

        private bool HaveSpecialFathers(int species, IEnumerable<int> eggmoves)
        {
            IEnumerable<int> fathers = PotentialEggChainsEggGroup[Personal[species].EggGroups[0]];
            if (Personal[species].EggGroups[0] != Personal[species].EggGroups[1])
                fathers = fathers.Union(PotentialEggChainsEggGroup[Personal[species].EggGroups[1]]).Distinct();
            foreach( var chain in SpecialBreedingMoves.Where( s => fathers.Any( f=> f== s.Species)))
            {
                var chainmoves = eggmoves.Except(BreedingMoves[chain.Species]);
                if (chainmoves.All(value => chain.Moves.Contains(value)))
                {
                    return true;
                }
            }
            return false;
        }

    }
}
