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

    internal class EggBreeding
    {
        protected int GenOrigin;
        protected int GenFormat;
        protected List<int> PotentialFathers;
        protected List<int>[] PotentialFathersEggGroup;
        protected int[][] BreedingMoves;
        protected int[][] ChainBreedingMoves;
        protected int[][] EggBreedingMoves;
        protected int[] EggMoves;
        protected PersonalTable Personal;
        protected EvolutionTree Evolves;

        internal EggBreeding(int gen, EvolutionTree Evos, PersonalTable Perso, EggMoves[] EggLearnSet)
        {
            GenOrigin = gen;
            GenFormat = gen;
            Evolves = Evos;
            PotentialFathers = new List<int>();
            PotentialFathersEggGroup = new List<int>[15];
            for (int i = 0; i < 15; i++)
            {
                PotentialFathersEggGroup[i] = new List<int>();
            }
            Personal = Perso;
            var maxSpecies = Legal.getMaxSpeciesOrigin(GenOrigin);
            BreedingMoves = new int[maxSpecies][];
            EggBreedingMoves = new int[maxSpecies][];
            ChainBreedingMoves = new int[maxSpecies][];
            EggMoves = EggLearnSet.SelectMany(e => e.Moves).Distinct().ToArray();
            for (int i = 1; i < maxSpecies; i++)
            {
                // Exclude female only,no gender and undiscover egg group species
                // Exclude unevolved species to simplify check,any move that can be legally breed can be legally breed by their final evolution forms too
                if (Personal[i].Gender >= 254 || Personal[i].EggGroups.Any(g => g == 15) || Evolves.CanEvolve(i))
                    continue;

                // Moves that the father pokemon can inherit to eggs that are not from his egg moves, these moves can all be breed without chains
                BreedingMoves[i] = Legal.getCanBreedMove(i, GenOrigin, GenFormat, EggMoves).ToArray();

                // Set of moves that the father can inherit to eggs but are exclusive from his egg moves, that means a chain egg happens, the chain should be analyze
                var LearnEggMoves = Legal.getCanBreedChainEggMoves(i, GenOrigin, GenFormat, EggMoves).ToArray();
                EggBreedingMoves[i] = LearnEggMoves.Except(BreedingMoves[i]).Distinct().ToArray();
                // A union of egg moves and not egg moves, when chains happens not all moves are egg moves in all the chain
                ChainBreedingMoves[i] = EggBreedingMoves[i].Concat(BreedingMoves[i]).ToArray();

                // Create list of potential fathers and also another list of potential fathers by egg group
                PotentialFathers.Add(i);
                PotentialFathersEggGroup[Personal[i].EggGroups[0]].Add(i);
                if (Personal[i].EggGroups[0] != Personal[i].EggGroups[1])
                    PotentialFathersEggGroup[Personal[i].EggGroups[1]].Add(i);
            }

            foreach (int father in PotentialFathers)
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

        // Check if there is a father species that can learn of the eggmoves for the given species 
        // without using egg moves in the father species, that means without using egg chains
        // If the father exits that means the combination of legal and eggmoves is legal
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

        // Return the egg group of the species or the egg group of the first evolution for baby species (pichu -> pikachu)
        internal int[] getEggBreedingGroups(int species)
        {
            if (Personal[species].EggGroups.Any(eg => eg == 15))
            {
                return Evolves.TreeEggGroup(species);
            }
            return Personal[species].EggGroups;
        }
        // Return species that can be father of the given species
        internal IEnumerable<int> getCompatibleFathers(int species)
        {
            return getCompatibleFathers(species, null);
        }
        // Return species that can be father of the given species, excluding species already checked in the chain analyzed
        internal IEnumerable<int> getCompatibleFathers(int species, IEnumerable<int> excludedfathers)
        {
            var eggGroup = getEggBreedingGroups(species);
            IEnumerable<int> fathers = PotentialFathersEggGroup.Where((t, i) => eggGroup.Any(g => g == i)).SelectMany(f => f).Distinct();
            if (excludedfathers != null)
                fathers = fathers.Where(f => !excludedfathers.Any(e => species == f));
            return fathers;
        }

        // Return species that can be father of the species with the egg moves combination throught egg chain
        // and the eggmoves that are also learned as egg moves by the father
        // The legallity of father species and father egg moves should be analyzed recursively
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

        internal bool ValidEggMoves(int species, IEnumerable<int> eggmoves)
        {
            return ValidEggMoves(species, eggmoves, new List<int>());
        }

        // Check if a combination of species and eggmoves is legal
        internal virtual bool ValidEggMoves(int species, IEnumerable<int> eggmoves, IEnumerable<int> PrevExcludedFathers)
        {
            //If there is one father that learn all the eggmoves 
            // without having itself any of these moves as egg moves then the combination is legal
            if (HaveNonChainFather(species, eggmoves))
                return true;

            // If there is no father without chains check potential father that inherit the moves in egg chains
            var fathers = getCompatibleFathers(species, PrevExcludedFathers);
            var Chains = getEggChainCompatibleFathers(species, eggmoves, fathers);
            foreach (EggChain Chain in Chains)
            {
                // For every potential father check the legallity of father species and moves that the father learns as egg move
                // The current species if added to a exclusion list to avoid the aplicattion to enter into egg chains loops
                IEnumerable<int> ExcludedFathers = PrevExcludedFathers.Concat(new List<int> { species });
                if (ValidEggMoves(Chain.Species, Chain.Moves, ExcludedFathers))
                    return true;
            }

            return false;
        }
    }
}