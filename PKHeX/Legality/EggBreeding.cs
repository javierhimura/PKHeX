using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core
{
    internal class EggChainBreeding
    {
        internal EggChainBreeding(int chain)
        {
            Chain = chain;
            ValidEggMoves1 = new List<int>();
            ValidEggMoves2 = new List<int[]>();
            ValidEggMoves3 = new List<int[]>();
            ValidEggMoves4 = new List<int[]>();
        }

        internal int Chain;
        internal List<int> ValidEggMoves1;
        internal List<int[]> ValidEggMoves2;
        internal List<int[]> ValidEggMoves3;
        internal List<int[]> ValidEggMoves4;
        internal bool IsEmpty => ValidEggMoves1?.Count == 0 && ValidEggMoves2?.Count == 0 && ValidEggMoves3?.Count == 0 && ValidEggMoves4?.Count == 0;

        internal uint GetLenght()
        {
            return (uint)(2 /* Chain */ + ValidEggMoves1.Count * 2 + ValidEggMoves2.Count * 4 + ValidEggMoves3.Count * 6 + ValidEggMoves4.Count * 8);
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
        internal bool IsEmpty => EggChains?.All(n => n.IsEmpty) ?? true;
        internal int LastChain => IsEmpty ? 0 : EggChains.Where(n => !n.IsEmpty).Select((n, i) => i).Last() + 1;
        internal EggChainBreeding[] EggChains;
        internal int TotalCombinations2 => TotalEggMoves * (TotalEggMoves - 1);
        internal int TotalCombinations3 => TotalCombinations2 * (TotalEggMoves - 2);
        internal int TotalCombinations4 => TotalCombinations3 * (TotalEggMoves - 3);
        internal bool AllMovesValid => EggChains.Sum(c => c?.ValidEggMoves1.Count) == TotalEggMoves;
        internal bool AllCombinations2Valid => EggChains.Sum(c => c?.ValidEggMoves2.Count) == TotalCombinations2;
        internal bool AllCombinations3Valid => EggChains.Sum(c => c?.ValidEggMoves3.Count) == TotalCombinations3;
        internal bool AllCombinations4Valid => EggChains.Sum(c => c?.ValidEggMoves4.Count) == TotalCombinations4;

        internal bool IsValidEggMove(int move)
        {
            return AllMovesValid || EggChains.Any(c => c?.ValidEggMoves1.Any(m => m == move) ?? false);
        }

        internal bool IsValidEggMove(int move, int chain, bool lastonly)
        {
            if( lastonly)
                return AllMovesValid || EggChains[chain].ValidEggMoves1.Any(m => m == move);
            else
                return AllMovesValid || EggChains.Take(chain + 1).Any(c => c?.ValidEggMoves1.Any(m => m == move) ?? false);
        }

        internal bool IsValidEggMoveCombination(IEnumerable<int> move, int chain, bool lastonly)
        {
            var order = move.OrderBy(mv => mv);
  
            if (lastonly)
            {
                switch (order.Count())
                {
                    case 1: return IsValidEggMove(order.First(), chain, lastonly);
                    case 2: return AllCombinations2Valid || EggChains[chain].ValidEggMoves2.Any(m => m.SequenceEqual(order));
                    case 3: return AllCombinations3Valid || EggChains[chain].ValidEggMoves3.Any(m => m.SequenceEqual(order));
                    case 4: return AllCombinations4Valid || EggChains[chain].ValidEggMoves4.Any(m => m.SequenceEqual(order));
                }
            }
            else
            {
                switch (order.Count())
                {
                    case 1: return IsValidEggMove(order.First(), chain, lastonly);
                    case 2: return AllCombinations2Valid || EggChains.Take(chain + 1).Any(c => c.ValidEggMoves2.Any(m => m.SequenceEqual(order)));
                    case 3: return AllCombinations3Valid || EggChains.Take(chain + 1).Any(c => c.ValidEggMoves3.Any(m => m.SequenceEqual(order)));
                    case 4: return AllCombinations4Valid || EggChains.Take(chain + 1).Any(c => c.ValidEggMoves4.Any(m => m.SequenceEqual(order)));
                }
            }
            return false;
        }

        internal bool IsValidEggMoveCombination(IEnumerable<int> move)
        {
            var order = move.OrderBy(mv => mv);
            switch (order.Count())
            {
                case 1: return IsValidEggMove(order.First());
                case 2: return AllCombinations2Valid || EggChains.Any(c => c.ValidEggMoves2.Any(m => m.SequenceEqual(order)));
                case 3: return AllCombinations3Valid || EggChains.Any(c => c.ValidEggMoves3.Any(m => m.SequenceEqual(order)));
                case 4: return AllCombinations4Valid || EggChains.Any(c => c.ValidEggMoves4.Any(m => m.SequenceEqual(order)));
            }
            return false;
        }

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
            return (uint)(4 /*TotalEggMoves,LastChain */ + EggChains.Sum(n => n.GetLenght()));
        }
    }

    internal class EggBreedingExtractor
    {
        protected int GenOrigin;
        protected int GenFormat;
        protected List<int>[] PotentialFathersEggGroup;
        protected int[][] DirectBreedingMoves;
        protected int[][] ChainBreedingMoves;
        protected int[] TotalEggMoves;
        protected PersonalTable Personal;
        protected EvolutionTree Evolves;
        protected EggBreeding[] InheritData;
        private const int maxAllowChains = 20;

        private void GeneratePotentialFathers(int maxSpecies)
        {
            PotentialFathersEggGroup = new List<int>[15];
            for (int i = 0; i < 15; i++)
            {
                PotentialFathersEggGroup[i] = new List<int>();
            }
            for (int i = 1; i < maxSpecies; i++)
            {
                // Exclude female only,no gender and undiscover egg group species
                // Exclude unevolved species to simplify check,any move that can be legally breed can be legally breed by their final evolution forms too
                if (Personal[i].Gender >= 254 || Personal[i].EggGroups.Any(g => g == 15) || Evolves.CanEvolve(i))
                    continue;

                // Moves that the father pokemon can inherit to eggs that are not from his egg moves, these moves can all be breed without chains
                DirectBreedingMoves[i] = Legal.getCanBreedMove(i, GenOrigin, GenFormat, TotalEggMoves).ToArray();

                // Set of moves that the father can inherit to eggs but are exclusive from his egg moves, that means a chain egg happens, the chain should be analyze
                var LearnEggMoves = Legal.getCanBreedChainEggMoves(i, GenOrigin, GenFormat, TotalEggMoves).ToArray();

                // A union of egg moves and not egg moves, when chains happens not all moves are egg moves in all the chain
                ChainBreedingMoves[i] = LearnEggMoves.Concat(DirectBreedingMoves[i]).ToArray();

                // Create list of potential fathers and also another list of potential fathers by egg group
                PotentialFathersEggGroup[Personal[i].EggGroups[0]].Add(i);
                if (Personal[i].EggGroups[0] != Personal[i].EggGroups[1])
                    PotentialFathersEggGroup[Personal[i].EggGroups[1]].Add(i);
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
                IEnumerable<int> FatherSpecies = PotentialFathersEggGroup[egggroups[0]].Union(PotentialFathersEggGroup[egggroups[1]]).Distinct();
                var OrderedMoves = EggLearnSet[i].Moves.OrderBy(m => m).ToArray();

                for (int move1 = 0; move1 < OrderedMoves.Length; move1++)
                {
                    if (CanInheritMovesNoChain(FatherSpecies, OrderedMoves[move1], 0, 0, 0))
                        InheritData[i].EggChains[0].ValidEggMoves1.Add(EggLearnSet[i].Moves[move1]);
                    else
                        continue;
                    for (int move2 = move1 + 1; move2 < OrderedMoves.Length; move2++)
                    {
                        if (CanInheritMovesNoChain(FatherSpecies, OrderedMoves[move1], OrderedMoves[move2], 0, 0))
                            InheritData[i].EggChains[0].ValidEggMoves2.Add(new[] { EggLearnSet[i].Moves[move1], OrderedMoves[move2] });
                        else
                            continue;
                        for (int move3 = move2 + 1; move3 < OrderedMoves.Length; move3++)
                        {
                            if (CanInheritMovesNoChain(FatherSpecies, OrderedMoves[move1], OrderedMoves[move2], OrderedMoves[move3], 0))
                                InheritData[i].EggChains[0].ValidEggMoves3.Add(new[] { EggLearnSet[i].Moves[move1], OrderedMoves[move2], OrderedMoves[move3] });
                            else
                                continue;
                            for (int move4 = move3 + 1; move4 < OrderedMoves.Length; move4++)
                            {
                                if (CanInheritMovesNoChain(FatherSpecies, OrderedMoves[move1], OrderedMoves[move2], OrderedMoves[move3], OrderedMoves[move4]))
                                    InheritData[i].EggChains[0].ValidEggMoves4.Add(new[] { EggLearnSet[i].Moves[move1], OrderedMoves[move2], OrderedMoves[move3], OrderedMoves[move4] });
                            }
                        }
                    }
                }
            }
        }

        private void GenerateValidChainsEggMoves(int maxSpecies, EggMoves[] EggLearnSet)
        {
            bool lastonly = true;
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
                    var AllMovesValid = InheritData[i].AllMovesValid;
                    var AllCombinations2Valid = InheritData[i].AllCombinations2Valid;
                    var AllCombinations3Valid = InheritData[i].AllCombinations3Valid;
                    var AllCombinations4Valid = InheritData[i].AllCombinations4Valid;

                    for (int move1 = 0; move1 < OrderedMoves.Length; move1++)
                    {
                        var move = EggLearnSet[i].Moves[move1];
                        if (!AllMovesValid && !InheritData[i].IsValidEggMove(move))
                        {
                            if (CanInheritMoveChain(FatherSpecies, chain - 1, move, lastonly))
                            {
                                InheritData[i].EggChains[chain].ValidEggMoves1.Add(move);
                                validdatafound++;
                            }
                            else
                                continue;
                        }

                        for (int move2 = move1 + 1; move2 < OrderedMoves.Length; move2++)
                        {
                            var combination2 = (new[] { EggLearnSet[i].Moves[move1], EggLearnSet[i].Moves[move2] }).OrderBy(m => m);
                            if (!AllCombinations2Valid && !InheritData[i].IsValidEggMoveCombination(combination2))
                            {
                                if (CanInheritMovesChain(FatherSpecies, chain - 1, combination2, lastonly))
                                {
                                    InheritData[i].EggChains[chain].ValidEggMoves2.Add(combination2.ToArray());
                                    validdatafound++;
                                }
                                else
                                    continue;
                            }

                            for (int move3 = move2 + 1; move3 < OrderedMoves.Length; move3++)
                            {
                                var combination3 = (new[] { EggLearnSet[i].Moves[move1], EggLearnSet[i].Moves[move2], EggLearnSet[i].Moves[move3] }).OrderBy(m => m);
                                if (!AllCombinations3Valid && !InheritData[i].IsValidEggMoveCombination(combination3))
                                {
                                    if (CanInheritMovesChain(FatherSpecies, chain - 1, combination3, lastonly))
                                    {
                                        InheritData[i].EggChains[chain].ValidEggMoves3.Add(combination3.ToArray());
                                        validdatafound++;
                                    }
                                    else
                                        continue;
                                }
                                for (int move4 = move3 + 1; move4 < OrderedMoves.Length; move4++)
                                {
                                    var combination4 = (new[] { EggLearnSet[i].Moves[move1], EggLearnSet[i].Moves[move2], EggLearnSet[i].Moves[move3], EggLearnSet[i].Moves[move4] }).OrderBy(m => m);
                                    if (!AllCombinations4Valid && !InheritData[i].IsValidEggMoveCombination(combination4))
                                    {
                                        if (CanInheritMovesChain(FatherSpecies, chain - 1, combination4, lastonly))
                                        {
                                            InheritData[i].EggChains[chain].ValidEggMoves4.Add(combination4.ToArray());
                                            validdatafound++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (validdatafound == 0)
                    break;
            }
        }

        internal EggBreedingExtractor(int gen, EvolutionTree Evos, PersonalTable Perso, EggMoves[] EggLearnSet)
        {
            GenOrigin = gen;
            GenFormat = gen;
            var maxSpecies = Legal.getMaxSpeciesOrigin(GenOrigin);
            Evolves = Evos;

            Personal = Perso;
            DirectBreedingMoves = new int[maxSpecies + 1][];
            ChainBreedingMoves = new int[maxSpecies + 1][];
            TotalEggMoves = EggLearnSet.SelectMany(e => e.Moves).Distinct().ToArray();

            GeneratePotentialFathers(maxSpecies);
            GenerateValidNonChainEggMoves(maxSpecies, EggLearnSet);
            GenerateValidChainsEggMoves(maxSpecies, EggLearnSet);

            byte[] BFile = EggBreeding.PackEggBreedingsData($"G{GenOrigin}", InheritData);
            File.WriteAllBytes($"EggBreeding_g{GenOrigin}.pkl", BFile);
        }

        private bool CanInheritMovesNoChain(IEnumerable<int> FatherSpecies, int move1, int move2, int move3, int move4)
        {
            foreach (int father in FatherSpecies)
            {
                if (CanLearnBreedingMoves(father, move1, move2, move3, move4))
                    return true;
            }
            return false;
        }

        private bool CanInheritMovesChain(IEnumerable<int> FatherSpecies, int chain, IEnumerable<int> eggmoves, bool lastonly)
        {
            foreach (int father in FatherSpecies)
            {
                foreach (int fatheregg in Evolves.getEggSpecies(father))
                {
                    if (eggmoves.All(value => ChainBreedingMoves[father].Any(c => c == value)))
                    {
                        var chainmoves = eggmoves.Except(DirectBreedingMoves[father]);
                        if (InheritData[fatheregg].IsValidEggMoveCombination(chainmoves, chain, lastonly))
                            return true;
                    }
                }
            }
            return false;
        }

        private bool CanInheritMoveChain(IEnumerable<int> FatherSpecies, int chain, int eggmove, bool lastonly)
        {
            foreach (int father in FatherSpecies)
            {
                foreach (int fatheregg in Evolves.getEggSpecies(father))
                {
                    if (InheritData[fatheregg].IsValidEggMove(eggmove, chain, lastonly))
                        return true;
                }
            }
            return false;
        }

        private bool CanLearnBreedingMoves(int species, int move1, int move2, int move3, int move4)
        {
            bool valid = false;
            valid = DirectBreedingMoves[species].Any(b => b == move1);
            if (valid && move2 != 0)
                valid = DirectBreedingMoves[species].Any(b => b == move2);
            if (valid && move3 != 0)
                valid = DirectBreedingMoves[species].Any(b => b == move3);
            if (valid && move4 != 0)
                valid = DirectBreedingMoves[species].Any(b => b == move4);
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