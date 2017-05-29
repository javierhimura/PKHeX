using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKHeX.Core
{
    public static partial class Legal
    {
        internal static IEnumerable<int> getCanBreedMove(int species, int baseSpecies, int GenOrigin, int GenFormat, int[] EggMoves)
        {
            if(species == 235)
                return EggMoves.Except(InvalidSketch);

            List<int> r = new List<int>();
            var table = getEvolutionTable(GenOrigin);
            DexLevel[] preevos = table.getValidPreEvolutions(species, GenOrigin).ToArray();

            int minindex = Math.Max(0, Array.FindIndex(preevos, p => p.Species == baseSpecies));
            Array.Resize(ref preevos, minindex + 1);

            foreach (DexLevel vs in preevos)
            {
                r.AddRange(getCanBreedMove(vs, GenOrigin, GenFormat, EggMoves));
            }
            return r.Distinct();
        }
        internal static IEnumerable<int> getCanBreedMove(DexLevel vs, int mingen, int maxgen, int[] EggMoves)
        {
            List<int> r = new List<int>();
            for (int gen = mingen; gen <= maxgen; gen++)
                r.AddRange(getCanBreedMove(vs, gen, EggMoves));
            return r.Distinct();
        }
        internal static IEnumerable<int> getCanBreedMove(DexLevel vs, int Generation, int[] EggMoves)
        {
            List<int> r = new List<int>();

            switch (Generation)
            {
                case 1:
                    {
                        int index = PersonalTable.RB.getFormeIndex(vs.Species, 0);
                        if (index == 0)
                            return r;

                        var pi_rb = (PersonalInfoG1)PersonalTable.RB[index];
                        var pi_y = (PersonalInfoG1)PersonalTable.Y[index];
                        r.AddRange(pi_rb.Moves);
                        r.AddRange(pi_y.Moves);
                        r.AddRange(LevelUpRB[index].getMoves(100));
                        r.AddRange(LevelUpY[index].getMoves(100));
                        r.AddRange(TMHM_RBY.Where((t, m) => pi_rb.TMHM[m]));
                        r.AddRange(TMHM_RBY.Where((t, m) => pi_y.TMHM[m]));
                        break;
                    }
                case 2:
                    {
                        int index = PersonalTable.C.getFormeIndex(vs.Species, 0);
                        if (index == 0)
                            return r;

                        r.AddRange(LevelUpGS[index].getMoves(100));
                        r.AddRange(LevelUpC[index].getMoves(100));

                        var pi_c = (PersonalInfoG2)PersonalTable.C[index];
                        r.AddRange(TMHM_GSC.Where((t, m) => pi_c.TMHM[m]));
                        r.AddRange(getCanBreedTutorMoves(vs.Species, Generation));
                        break;
                    }
                case 3:
                    {
                        int index = PersonalTable.E.getFormeIndex(vs.Species, 0);
                        if (index == 0)
                            return r;

                        r.AddRange(LevelUpE[index].getMoves(100));

                        var pi_c = PersonalTable.E[index];
                        r.AddRange(TM_3.Where((t, m) => pi_c.TMHM[m]));
                        r.AddRange(getCanBreedTutorMoves(vs.Species, Generation));
                        break;
                    }
                case 4:
                    {
                        int index = PersonalTable.HGSS.getFormeIndex(vs.Species, 0);
                        if (index == 0)
                            return r;

                        r.AddRange(LevelUpDP[index].getMoves(100));
                        r.AddRange(LevelUpPt[index].getMoves(100));
                        r.AddRange(LevelUpHGSS[index].getMoves(100));

                        var pi_hgss = PersonalTable.HGSS[index];
                        var pi_dppt = PersonalTable.Pt[index];
                        r.AddRange(TM_4.Where((t, m) => pi_hgss.TMHM[m]));
                        r.AddRange(getCanBreedTutorMoves(vs.Species, Generation));
                        break;
                    }
                case 5:
                    {
                        int index = PersonalTable.B2W2.getFormeIndex(vs.Species, 0);
                        if (index == 0)
                            return r;
                        r.AddRange(LevelUpBW[index].getMoves(100));
                        r.AddRange(LevelUpB2W2[index].getMoves(100));

                        var pi_c = PersonalTable.B2W2[index];
                        r.AddRange(TMHM_BW.Where((t, m) => pi_c.TMHM[m]));
                        r.AddRange(getCanBreedTutorMoves(vs.Species, Generation));
                        break;
                    }
                default:
                    return r;
            }
            return r.Intersect(EggMoves).Distinct();
        }
        internal static IEnumerable<int> getCanBreedChainEggMoves(int Species, int GenOrigin, int GenFormat, int[] EggMoves)
        {
            IEnumerable<int> r = new List<int>();

            switch (GenOrigin)
            {
                case 1:
                case 2:
                    r = EggMovesGS[Species].Moves.Union(EggMovesC[Species].Moves);
                    if (GenOrigin == 1)
                        r = r.Where(m => m <= MaxMoveID_1);
                    break;
                case 3:
                    r = EggMovesRS[Species].Moves;
                    break;
                case 4:
                    r = EggMovesDPPt[Species].Moves.Union(EggMovesHGSS[Species].Moves);
                    break;
                case 5:
                    r = EggMovesBW[Species].Moves;
                    break;
            }
            if (GenOrigin != GenFormat)
                r = r.Intersect(EggMoves);
            return r;
        }
        private static IEnumerable<int> getCanBreedTutorMoves(int species, int generation)
        {
            List<int> moves = new List<int>();
            PersonalInfo info;
            switch (generation)
            {
                // Gen 1 only tutor move is surf, is not a egg move
                case 2:
                    info = PersonalTable.C[species];
                    moves.AddRange(Tutors_GSC.Where((t, i) => info.TMHM[57 + i]));
                    break;
                case 3:
                    // E Tutors (Free)
                    // E Tutors (BP)
                    info = PersonalTable.E[species];
                    moves.AddRange(Tutor_E.Where((t, i) => info.TypeTutors[i]));
                    // FRLG Tutors
                    // Only special tutor moves, normal tutor moves are already included in Emerald data
                    moves.AddRange(SpecialTutors_FRLG.Where((t, i) => SpecialTutors_Compatibility_FRLG[i].Any(e => e == species)));
                    // XD
                    moves.AddRange(SpecialTutors_XD_Exclusive.Where((t, i) => SpecialTutors_Compatibility_XD_Exclusive[i].Any(e => e == species)));
                    break;
                case 4:
                    info = PersonalTable.HGSS[species];
                    moves.AddRange(Tutors_4.Where((t, i) => info.TypeTutors[i]));
                    moves.AddRange(SpecialTutors_4.Where((t, i) => SpecialTutors_Compatibility_4[i].Any(e => e == species)));
                    break;
                case 5:
                    info = PersonalTable.B2W2[species];
                    moves.AddRange(TypeTutor6.Where((t, i) => info.TypeTutors[i]));

                    PersonalInfo pi = PersonalTable.B2W2.getFormeEntry(species, 0);
                    for (int i = 0; i < Tutors_B2W2.Length; i++)
                        for (int b = 0; b < Tutors_B2W2[i].Length; b++)
                            if (pi.SpecialTutors[i][b])
                                moves.Add(Tutors_B2W2[i][b]);

                    break;
            }
            return moves.Distinct();
        }
    }
}

