using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core
{

    internal class EggFather
    {
        internal int Species;
        internal int BaseSpecies;
        internal int[] DirectBreedingMoves;
        internal int[] ChainBreedingMoves;
    }

    internal class EggMoveFather
    {
        internal List<EggFather> Fathers;
        internal int[] Moves;
        internal int Chain;
    }

    internal class EggChainBreeding
    {
        internal EggChainBreeding(int chain)
        {
            Chain = chain;
            ValidEggMoves = new List<EggMoveFather>();
            //ValidEggMoves2 = new List<EggMoveFather>();
            //ValidEggMoves3 = new List<EggMoveFather>();
            //ValidEggMoves4 = new List<EggMoveFather>();
        }

        internal int Chain;
        internal List<EggMoveFather> ValidEggMoves;
        //internal List<EggMoveFather> ValidEggMoves2;
        //internal List<EggMoveFather> ValidEggMoves3;
        //internal List<EggMoveFather> ValidEggMoves4;
        //internal bool IsEmpty => ValidEggMoves1?.Count == 0 && ValidEggMoves2?.Count == 0 && ValidEggMoves3?.Count == 0 && ValidEggMoves4?.Count == 0;

        /*internal uint GetLenght()
        {
            return (uint)(2 /* Chain / + ValidEggMoves1.Count * 2 + ValidEggMoves2.Count * 4 + ValidEggMoves3.Count * 6 + ValidEggMoves4.Count * 8);
        }*/

        internal bool Contains(EggFather Father, int[] Moves)
        {
            var element = ValidEggMoves.FirstOrDefault(v => v.Moves.SequenceEqual(Moves));
            if (element == null)
                return false;
            return element.Fathers.Any(f => f.BaseSpecies == Father.BaseSpecies && f.Species == Father.Species);
        }

        internal int AddMoves(EggFather Father, int[] Moves)
        {
            var element = ValidEggMoves.FirstOrDefault(v => v.Moves.SequenceEqual(Moves));
            if (element == null)
            {
                EggMoveFather MoveFathers = new EggMoveFather() { Moves = Moves, Fathers = new List<EggFather> { Father } };
                ValidEggMoves.Add(MoveFathers);
                return 1;
            }
            else
            {
                element.Fathers.Add(Father);
                return element.Fathers.Count;
            }
        }
    }

    internal class EggBreeding
    {
        internal EggBreeding(int maxChains)
        {
            EggChains = new EggChainBreeding[maxChains];
            for (int i = 0; i < maxChains; i++)
            {
                EggChains[i] = new EggChainBreeding(i + 1);
            }
        }

        internal int TotalEggMoves;
        internal EggChainBreeding[] EggChains;
        internal int TotalCombinations2 => TotalEggMoves * (TotalEggMoves - 1);
        internal int TotalCombinations3 => TotalCombinations2 * (TotalEggMoves - 2);
        internal int TotalCombinations4 => TotalCombinations3 * (TotalEggMoves - 3);

        internal bool Contains( EggFather Father, int[] Moves)
        {
            return EggChains.Any(c => c != null && c.Contains(Father, Moves));
        }

        internal List<EggFather> GetFathers(IEnumerable<int> moves)
        {
            //var order = moves.OrderBy(mv => mv);
            return EggChains.SelectMany(c => c.ValidEggMoves).FirstOrDefault(m => m.Moves.SequenceEqual(moves))?.Fathers;
        }

        internal List<EggFather> GetFathers(IEnumerable<int> moves, int chain)
        {
            //var order = moves.OrderBy(mv => mv);
            return EggChains[chain].ValidEggMoves.FirstOrDefault(m => m.Moves.SequenceEqual(moves))?.Fathers;
        }

        internal bool IsValidEggMoveCombination(IEnumerable<int> moves, int chain)
        {
            //return IsValidEggMoveCombination(moves);
            //var order = move.OrderBy(mv => mv);
            if (chain < 0)
                return false;
            return EggChains[chain].ValidEggMoves.Any(m => m.Moves.SequenceEqual(moves));
        }

        internal bool IsValidEggMoveCombination(IEnumerable<int> moves)
        {
            //var order = move.OrderBy(mv => mv);
            return EggChains.Any(c => c != null && c.ValidEggMoves.Any(m => m.Moves.SequenceEqual(moves)));
        }

        internal int AddMoves(IEnumerable<EggFather> Fathers, int Move, int Chain)
        {
            return AddMoves(Fathers, new[] { Move }, Chain);
        }

        internal int AddMoves(IEnumerable<EggFather> Fathers, int[] Moves, int Chain)
        {
            int count = 0;
            //var order = Moves.OrderBy(mv => mv).ToArray();
            foreach (EggFather Father in Fathers)
            {
                if (Contains(Father, Moves))
                    continue;
                EggChains[Chain].AddMoves(Father, Moves);
                count++;
            }
            return count;
        }

        /*
        internal static byte[] PackEggBreedingsData(string Header, EggBreeding[] InheritanceData)
        {
            using (var s = new MemoryStream())
            using (var bw = new BinaryWriter(s))
            {
                bw.Write(Header.ToCharArray());
                bw.Write((ushort)InheritanceData.Length);
                uint offset = (uint)(4 + (InheritanceData.Length * 4) + 4);
                for (int i = 0; i < InheritanceData.Length; i++)
                {
                    bw.Write(offset);
                    offset += InheritanceData[i].GetLenght();
                }
                bw.Write(offset);
                for (int i = 0; i < InheritanceData.Length; i++)
                {
                    bw.Write(InheritanceData[i].PackEggBreedingsData());
                }
                return s.ToArray();
            }
        }
        
        private byte[] PackEggBreedingsData()
        {
            using (var s = new MemoryStream())
            using (var bw = new BinaryWriter(s))
            {
                bw.Write((ushort)TotalEggMoves);
                //bw.Write((ushort)LastChain);
                bw.Write((ushort)EggChains.Length);
                //for (int i = 0; i < LastChain ; i++)
                for (int i = 0; i < EggChains.Length; i++)
                {
                    bw.Write((ushort)EggChains[i].Chain);
                    bw.Write((ushort)EggChains[i].ValidEggMoves1.Count);
                    bw.Write((ushort)EggChains[i].ValidEggMoves2.Count);
                    bw.Write((ushort)EggChains[i].ValidEggMoves3.Count);
                    bw.Write((ushort)EggChains[i].ValidEggMoves4.Count);
                    for (int j = 0; j < EggChains[i].ValidEggMoves1.Count; j++)
                        bw.Write((ushort)EggChains[i].ValidEggMoves1[j]);
                    for (int j = 0; j < EggChains[i].ValidEggMoves2.Count; j++)
                    {
                        bw.Write((ushort)EggChains[i].ValidEggMoves2[j][0]);
                        bw.Write((ushort)EggChains[i].ValidEggMoves2[j][1]);
                    }
                    for (int j = 0; j < EggChains[i].ValidEggMoves3.Count; j++)
                    {
                        bw.Write((ushort)EggChains[i].ValidEggMoves3[j][0]);
                        bw.Write((ushort)EggChains[i].ValidEggMoves3[j][1]);
                        bw.Write((ushort)EggChains[i].ValidEggMoves3[j][2]);
                    }
                    for (int j = 0; j < EggChains[i].ValidEggMoves4.Count; j++)
                    {
                        bw.Write((ushort)EggChains[i].ValidEggMoves4[j][0]);
                        bw.Write((ushort)EggChains[i].ValidEggMoves4[j][1]);
                        bw.Write((ushort)EggChains[i].ValidEggMoves4[j][2]);
                        bw.Write((ushort)EggChains[i].ValidEggMoves4[j][3]);
                    }
                }
                return s.ToArray();
            }
        }
        
        private uint GetLenght()
        {
            return (uint)(4 TotalEggMoves,LastChain  + EggChains.Sum(n => n.GetLenght()));
        }*/
    }

    internal class EggBreedingExtractor
    {
        protected int GenOrigin;
        protected int GenFormat;
        protected List<EggFather>[] PotentialFathersEggGroup;
        protected int[] TotalEggMoves;
        protected PersonalTable Personal;
        protected EvolutionTree Evolves;
        protected EggBreeding[] InheritData;
        private const int maxAllowChains = 20;
        private int[] BaseSpecies;
        private void GenerateBaseSpecies(int maxSpecies)
        {
            BaseSpecies = new int[maxSpecies + 1];
            for(int i= 1; i  <= maxSpecies; i++)
            {
                BaseSpecies[i] = Evolves.getBaseSpecies(i, GenOrigin);
            }
        }
        private void GeneratePotentialFathers(int maxSpecies)
        {
            PotentialFathersEggGroup = new List<EggFather>[15];
            for (int i = 0; i < 15; i++)
            {
                PotentialFathersEggGroup[i] = new List<EggFather>();
            }
            for (int species = 1; species < maxSpecies; species++)
            {
                // Exclude female only,no gender and undiscover egg group species
                // Exclude unevolved species to simplify check,any move that can be legally breed can be legally breed by their final evolution forms too
                if (Personal[species].Gender >= 254 || Personal[species].EggGroups.Any(g => g == 15)) // || Evolves.CanEvolve(species))
                    continue;

                int splitbreed = Legal.getSplitBreedGeneration(GenOrigin).Contains(species) ? 1 : 0;

                for(int splitcount =0; splitcount <= splitbreed; splitcount ++)
                {
                    if(splitcount == 1)
                    {

                    }
                    EggFather Father = new EggFather();
                    Father.Species = species;
                    Father.BaseSpecies = Evolves.getBaseSpecies(species, splitcount);
                    // Moves that the father pokemon can inherit to eggs that are not from his egg moves, these moves can all be breed without chains
                    Father.DirectBreedingMoves = Legal.getCanBreedMove(Father.Species, Father.BaseSpecies, GenOrigin, GenFormat, TotalEggMoves).ToArray();

                    // Set of moves that the father can inherit to eggs but are exclusive from his egg moves, that means a chain egg happens, the chain should be analyze
                    var LearnEggMoves = Legal.getCanBreedChainEggMoves(Father.BaseSpecies, GenOrigin, GenFormat, TotalEggMoves).ToArray();

                    // A union of egg moves and not egg moves, when chains happens not all moves are egg moves in all the chain
                    Father.ChainBreedingMoves = LearnEggMoves.Concat(Father.DirectBreedingMoves).ToArray();

                    // Create list of potential fathers and also another list of potential fathers by egg group
                    PotentialFathersEggGroup[Personal[species].EggGroups[0]].Add(Father);
                    if (Personal[species].EggGroups[0] != Personal[species].EggGroups[1])
                        PotentialFathersEggGroup[Personal[species].EggGroups[1]].Add(Father);
                }
            }
        }

        private void GenerateValidNonChainEggMoves(int maxSpecies, EggMoves[] EggLearnSet)
        {
            InheritData = new EggBreeding[maxSpecies + 1];
            InheritData[0] = new EggBreeding(maxAllowChains);

            for (int i = 1; i <= maxSpecies; i++)
            {
                InheritData[i] = new EggBreeding(maxAllowChains);
                InheritData[i].TotalEggMoves = EggLearnSet[i].Moves.Length;
                if (Legal.NoHatchFromEgg.Contains(i) || !EggLearnSet[i].Moves.Any())
                    continue;
                var egggroups = getEggBreedingGroups(i);
                IEnumerable<EggFather> FatherSpecies = PotentialFathersEggGroup[egggroups[0]].Union(PotentialFathersEggGroup[egggroups[1]]).Distinct();
                var OrderedMoves = EggLearnSet[i].Moves.OrderBy(m => m).ToArray();

                for (int move1 = 0; move1 < OrderedMoves.Length; move1++)
                {
                    var validFatherSpecies1 = CanInheritMovesNoChain(FatherSpecies, OrderedMoves[move1], 0, 0, 0).ToArray();
                    if(validFatherSpecies1.Any())
                    {
                        var combination1 = (new[] { EggLearnSet[i].Moves[move1] }).OrderBy(mv => mv).ToArray();
                        var count = InheritData[i].AddMoves(validFatherSpecies1, combination1, 0);
                        if (count == 0)
                            continue;
                    }
                    else
                        continue;
                    for (int move2 = move1 + 1; move2 < OrderedMoves.Length; move2++)
                    {
                        var combination2 = (new[] { EggLearnSet[i].Moves[move1], OrderedMoves[move2] }).OrderBy(mv => mv).ToArray();
                        if (combination2[0] == 103 && combination2[1] == 172 && i == 19)
                        {

                        }
                        var validFatherSpecies2 = CanInheritMovesNoChain(validFatherSpecies1, OrderedMoves[move1], OrderedMoves[move2], 0, 0).ToArray(); ;
                        if (validFatherSpecies2.Any())
                        {
                          
                            var count = InheritData[i].AddMoves(validFatherSpecies2, combination2, 0);
                            if (count == 0)
                                continue;
                        }
                        else
                            continue;
                        for (int move3 = move2 + 1; move3 < OrderedMoves.Length; move3++)
                        {
                            var validFatherSpecies3 = CanInheritMovesNoChain(validFatherSpecies2, OrderedMoves[move1], OrderedMoves[move2], OrderedMoves[move3], 0).ToArray(); ;
                            if (validFatherSpecies3.Any())
                            {
                                var combination3 = (new[] { EggLearnSet[i].Moves[move1], EggLearnSet[i].Moves[move1], OrderedMoves[move2], OrderedMoves[move3] }).OrderBy(mv => mv).ToArray();
                                var count = InheritData[i].AddMoves(validFatherSpecies3, combination3 , 0);
                                if (count == 0)
                                    continue;
                            }
                            else
                                continue;
                            for (int move4 = move3 + 1; move4 < OrderedMoves.Length; move4++)
                            {
                                var validFatherSpecies4 = CanInheritMovesNoChain(validFatherSpecies3, OrderedMoves[move1], OrderedMoves[move2], OrderedMoves[move3], OrderedMoves[move4]).ToArray(); ;
                                if (validFatherSpecies4.Any())
                                {
                                    var combination4 = (new[] { EggLearnSet[i].Moves[move1], EggLearnSet[i].Moves[move1], OrderedMoves[move2], OrderedMoves[move3], OrderedMoves[move4] }).OrderBy(mv => mv).ToArray();
                                    var count = InheritData[i].AddMoves(validFatherSpecies4, combination4, 0);
                                    if (count == 0)
                                        continue;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void GenerateValidChainsEggMoves(int maxSpecies, EggMoves[] EggLearnSet)
        {
            bool lastonly = true;
            var datafound = 0;
            for (int chain = 1; chain < maxAllowChains; chain++)
            {
                var validdatafound = 0;
                for (int i = 1; i <= maxSpecies; i++)
                {
                    if (Legal.NoHatchFromEgg.Contains(i) || !EggLearnSet[i].Moves.Any())
                        continue;

                    var egggroups = getEggBreedingGroups(i);
                    var FatherSpecies = PotentialFathersEggGroup[egggroups[0]].Union(PotentialFathersEggGroup[egggroups[1]]).Distinct();
                    var OrderedMoves = EggLearnSet[i].Moves.OrderBy(m => m).ToArray();

                    for (int move1 = 0; move1 < OrderedMoves.Length; move1++)
                    {
                        var move = EggLearnSet[i].Moves[move1];
                        var combination1 = (new[] { EggLearnSet[i].Moves[move1] }).ToArray();

                        var validFather1 = CanInheritMovesChain(i, FatherSpecies, chain - 1, combination1, lastonly);
                        if(validFather1.Any())
                        {
                            var count = InheritData[i].AddMoves(validFather1, move, chain);
                            validdatafound += count;
                        }
                        else
                            continue;

                        for (int move2 = move1 + 1; move2 < OrderedMoves.Length; move2++)
                        {
                            var combination2 = (new[] { EggLearnSet[i].Moves[move1], EggLearnSet[i].Moves[move2] }).OrderBy(m => m).ToArray();
                            var validFather2 = CanInheritMovesChain(i, validFather1, chain - 1, combination2, lastonly);
                            if (validFather2.Any())
                            {
                                var count = InheritData[i].AddMoves(validFather2, combination2, chain);
                                validdatafound += count;
                            }
                            else
                                continue;

                            for (int move3 = move2 + 1; move3 < OrderedMoves.Length; move3++)
                            {
                                var combination3 = (new[] { EggLearnSet[i].Moves[move1], EggLearnSet[i].Moves[move2], EggLearnSet[i].Moves[move3] }).OrderBy(m => m).ToArray();
                                var validFather3 = CanInheritMovesChain(i, validFather2, chain - 1, combination3, lastonly);
                                if (validFather3.Any())
                                {
                                    var count = InheritData[i].AddMoves(validFather3,combination3, chain);
                                    validdatafound += count;
                                }
                                else
                                    continue;

                                for (int move4 = move3 + 1; move4 < OrderedMoves.Length; move4++)
                                {
                                    var combination4 = (new[] { EggLearnSet[i].Moves[move1], EggLearnSet[i].Moves[move2], EggLearnSet[i].Moves[move3], EggLearnSet[i].Moves[move4] }).OrderBy(m => m).ToArray();
                                    var validFather4 = CanInheritMovesChain(i, validFather3, chain - 1, combination4, lastonly);
                                    if (validFather4.Any())
                                    {
                                        var count = InheritData[i].AddMoves(validFather4,combination4, chain);
                                        validdatafound += count;
                                    }
                                }
                            }
                        }
                    }
                }
                if (validdatafound == 0)
                    break;
                datafound += validdatafound;
            }
            var total = InheritData.SelectMany(i => i.EggChains).SelectMany(c => c.ValidEggMoves).SelectMany(m => m.Fathers).Count();
        }

        internal EggBreedingExtractor(int gen, EvolutionTree Evos, PersonalTable Perso, EggMoves[] EggLearnSet)
        {
            GenOrigin = gen;
            GenFormat = gen;
            var maxSpecies = Legal.getMaxSpeciesOrigin(GenOrigin);
            Evolves = Evos;

            Personal = Perso;
            TotalEggMoves = EggLearnSet.SelectMany(e => e.Moves).Distinct().ToArray();

            GenerateBaseSpecies(maxSpecies);
            GeneratePotentialFathers(maxSpecies);
            GenerateValidNonChainEggMoves(maxSpecies, EggLearnSet);
            GenerateValidChainsEggMoves(maxSpecies, EggLearnSet);

            //byte[] BFile = EggBreeding.PackEggBreedingsData($"G{GenOrigin}", InheritData);
            //File.WriteAllBytes($"EggBreeding_g{GenOrigin}.pkl", BFile);
        }

        private IEnumerable<EggFather> CanInheritMovesNoChain(IEnumerable<EggFather> FatherSpecies, int move1, int move2, int move3, int move4)
        {
            foreach (EggFather father in FatherSpecies)
            {
                if (CanLearnBreedingMoves(father, move1, move2, move3, move4))
                    yield return father;
            }
        }

        private bool IsLoopChain(int CurrentSpecies, EggFather FatherSpecie, int chain, IEnumerable<int> chainmoves)
        {
            var ExcludedSpecies = new List<int>() { BaseSpecies[CurrentSpecies] };
            return IsLoopChain(ExcludedSpecies, FatherSpecie, chain, chainmoves);
        }

        private bool IsLoopChain(IEnumerable<int> ExcludedSpecies, EggFather FatherSpecie, int chain, IEnumerable<int> chainmoves)
        {
            var baseFather = BaseSpecies[FatherSpecie.Species];
            if (ExcludedSpecies.Contains(baseFather))
                return true;

            var nextfathers = InheritData[FatherSpecie.BaseSpecies].GetFathers(chainmoves, chain);
            if (nextfathers == null || !nextfathers.Any())
                return false;
               
            foreach(EggFather next in nextfathers)
            {
                var nextchainmoves = chainmoves.Except(next.DirectBreedingMoves).OrderBy(mv => mv).ToArray();
                var NextExcludedSpecies = new List<int>() { baseFather };
                NextExcludedSpecies.AddRange(ExcludedSpecies);
                if (!IsLoopChain(NextExcludedSpecies, next, chain - 1, nextchainmoves))
                    return false;
            }
            return true;
        }

        private IEnumerable<EggFather> CanInheritMovesChain(int CurrentSpecies, IEnumerable<EggFather> FatherSpecies, int chain, int[] eggmoves, bool lastonly)
        {
            var validFathers = new List<EggFather>();
            foreach (EggFather father in FatherSpecies)
            {
                if (BaseSpecies[father.Species] == BaseSpecies[CurrentSpecies])
                    continue;

                if (InheritData[CurrentSpecies].Contains(father, eggmoves))
                    continue;

                if (chain == 1 && eggmoves.All(value => father.DirectBreedingMoves.Any(c => c == value)))
                {
                    validFathers.Add(father);
                }

                if (chain > 1 && eggmoves.All(value => father.ChainBreedingMoves.Any(c => c == value)))
                {
                    var chainmoves = eggmoves.Except(father.DirectBreedingMoves).OrderBy(mv => mv).ToArray();
                    if (InheritData[father.BaseSpecies].IsValidEggMoveCombination(chainmoves, chain - 1))
                    {
                        validFathers.Add(father);
                    }
                }
            }
            return validFathers;
        }

        private bool CanLearnBreedingMoves(EggFather Father, int move1, int move2, int move3, int move4)
        {
            bool valid = false;
            valid = Father.DirectBreedingMoves.Any(b => b == move1);
            if (valid && move2 != 0)
                valid = Father.DirectBreedingMoves.Any(b => b == move2);
            if (valid && move3 != 0)
                valid = Father.DirectBreedingMoves.Any(b => b == move3);
            if (valid && move4 != 0)
                valid = Father.DirectBreedingMoves.Any(b => b == move4);
            return valid;
        }

        // Return the egg group of the species or the egg group of the first evolution for baby species (pichu -> pikachu)
        private int[] getEggBreedingGroups(int species)
        {
            if (Personal[species].EggGroups.Any(eg => eg == 15))
            {
                return Evolves.TreeEggGroup(species);
            }
            return Personal[species].EggGroups;
        }
    }
}