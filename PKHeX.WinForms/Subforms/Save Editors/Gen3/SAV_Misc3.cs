﻿using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PKHeX.Core;

namespace PKHeX.WinForms
{
    public partial class SAV_Misc3 : Form
    {
        private readonly SAV3 SAV = (SAV3)Main.SAV.Clone();
        public SAV_Misc3()
        {
            InitializeComponent();
            WinFormsUtil.TranslateInterface(this, Main.curlanguage);

            if (SAV.FRLG || SAV.E)
                readJoyful();
            else
                tabControl1.Controls.Remove(TAB_Joyful);

            if (SAV.E)
            {
                readFerry();
                readBF();
            }
            else
            {
                tabControl1.Controls.Remove(TAB_Ferry);
                tabControl1.Controls.Remove(TAB_BF);
            }

            if (SAV.FRLG)
                TB_OTName.Text = PKX.getString3(SAV.Data, SAV.getBlockOffset(4) + 0xBCC, 8, SAV.Japanese);
            else
                TB_OTName.Visible = L_TrainerName.Visible = false;
            
            NUD_BP.Value = Math.Min(NUD_BP.Maximum, SAV.BP);
            NUD_Coins.Value = Math.Min(NUD_Coins.Maximum, SAV.Coin);
        }
        private void B_Save_Click(object sender, EventArgs e)
        {
            if (tabControl1.Controls.Contains(TAB_Joyful))
                saveJoyful();
            if (tabControl1.Controls.Contains(TAB_Ferry))
                saveFerry();
            if (tabControl1.Controls.Contains(TAB_BF))
                saveBF();
            if (SAV.FRLG)
                SAV.setData(SAV.setString(TB_OTName.Text, TB_OTName.MaxLength), SAV.getBlockOffset(4) + 0xBCC);

            SAV.BP = (ushort)NUD_BP.Value;
            SAV.Coin = (ushort)NUD_Coins.Value;

            SAV.Data.CopyTo(Main.SAV.Data, 0);
            Main.SAV.Edited = true;
            Close();
        }
        private void B_Cancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        #region Joyful
        private int JUMPS_IN_ROW, JUMPS_SCORE, JUMPS_5_IN_ROW;
        private int BERRIES_IN_ROW, BERRIES_SCORE, BERRIES_5_IN_ROW;
        private void readJoyful()
        {
            switch (SAV.Version)
            {
                case GameVersion.E:
                    JUMPS_IN_ROW = SAV.getBlockOffset(0) + 0x1fc;
                    JUMPS_SCORE = SAV.getBlockOffset(0) + 0x208;
                    JUMPS_5_IN_ROW = SAV.getBlockOffset(0) + 0x200;

                    BERRIES_IN_ROW = SAV.getBlockOffset(0) + 0x210;
                    BERRIES_SCORE = SAV.getBlockOffset(0) + 0x20c;
                    BERRIES_5_IN_ROW = SAV.getBlockOffset(0) + 0x214;
                    break;
                case GameVersion.FRLG:
                    JUMPS_IN_ROW = SAV.getBlockOffset(0) + 0xB00;
                    JUMPS_SCORE = SAV.getBlockOffset(0) + 0xB0C;
                    JUMPS_5_IN_ROW = SAV.getBlockOffset(0) + 0xB04;

                    BERRIES_IN_ROW = SAV.getBlockOffset(0) + 0xB14;
                    BERRIES_SCORE = SAV.getBlockOffset(0) + 0xB10;
                    BERRIES_5_IN_ROW = SAV.getBlockOffset(0) + 0xB18;
                    break;
            }
            TB_J1.Text = Math.Min((ushort)9999, BitConverter.ToUInt16(SAV.Data, JUMPS_IN_ROW)).ToString();
            TB_J2.Text = Math.Min((ushort)9999, BitConverter.ToUInt16(SAV.Data, JUMPS_SCORE)).ToString();
            TB_J3.Text = Math.Min((ushort)9999, BitConverter.ToUInt16(SAV.Data, JUMPS_5_IN_ROW)).ToString();
            TB_B1.Text = Math.Min((ushort)9999, BitConverter.ToUInt16(SAV.Data, BERRIES_IN_ROW)).ToString();
            TB_B2.Text = Math.Min((ushort)9999, BitConverter.ToUInt16(SAV.Data, BERRIES_SCORE)).ToString();
            TB_B3.Text = Math.Min((ushort)9999, BitConverter.ToUInt16(SAV.Data, BERRIES_5_IN_ROW)).ToString();
        }
        private void saveJoyful()
        {
            BitConverter.GetBytes((ushort)Util.ToUInt32(TB_J1.Text)).CopyTo(SAV.Data, JUMPS_IN_ROW);
            BitConverter.GetBytes((ushort)Util.ToUInt32(TB_J2.Text)).CopyTo(SAV.Data, JUMPS_SCORE);
            BitConverter.GetBytes((ushort)Util.ToUInt32(TB_J3.Text)).CopyTo(SAV.Data, JUMPS_5_IN_ROW);
            BitConverter.GetBytes((ushort)Util.ToUInt32(TB_B1.Text)).CopyTo(SAV.Data, BERRIES_IN_ROW);
            BitConverter.GetBytes((ushort)Util.ToUInt32(TB_B2.Text)).CopyTo(SAV.Data, BERRIES_SCORE);
            BitConverter.GetBytes((ushort)Util.ToUInt32(TB_B3.Text)).CopyTo(SAV.Data, BERRIES_5_IN_ROW);
        }
        #endregion

        #region Ferry
        private int ofsFerry;
        private void B_GetTickets_Click(object sender, EventArgs e)
        {
            var Pouches = SAV.Inventory;
            string[] itemlist = GameInfo.Strings.getItemStrings(SAV.Generation, SAV.Version);
            for (int i = 0; i < itemlist.Length; i++)
                if (string.IsNullOrEmpty(itemlist[i]))
                    itemlist[i] = $"(Item #{i:000})";

            int[] tickets = {0x109, 0x113, 0x172, 0x173, 0x178}; // item IDs

            var p = Pouches.FirstOrDefault(z => z.Type == InventoryType.KeyItems);
            if (p == null)
                return;
            
            // check for missing tickets
            var missing = tickets.Where(z => !p.Items.Any(item => item.Index == z && item.Count == 1)).ToList();
            var have = tickets.Except(missing).ToList();
            if (missing.Count == 0)
            {
                WinFormsUtil.Alert("Already have all tickets.");
                B_GetTickets.Enabled = false;
                return;
            }

            // check for space
            int end = Array.FindIndex(p.Items, z => z.Index == 0);
            if (end + missing.Count >= p.Items.Length)
            {
                WinFormsUtil.Alert("Not enough space in pouch.", "Please use the InventoryEditor.");
                B_GetTickets.Enabled = false;
                return;
            }

            // insert items at the end
            for (int i = 0; i < missing.Count; i++)
            {
                var item = p.Items[end + i];
                item.Index = missing[i];
                item.Count = 1;
            }

            var added = string.Join(", ", missing.Select(u => itemlist[u]));
            string alert = "Inserted the following items to the Key Items Pouch:" + Environment.NewLine + added;
            if (have.Any())
            {
                string had = string.Join(", ", have.Select(u => itemlist[u]));
                alert += string.Format("{0}{0}Already had the following items:{0}{1}", Environment.NewLine, had);
            }
            WinFormsUtil.Alert(alert);
            SAV.Inventory = Pouches;

            B_GetTickets.Enabled = false;
        }
        private void readFerry()
        {
            ofsFerry = SAV.getBlockOffset(2) + 0x2F0;
            CHK_Catchable.Checked = getFerryFlagFromNum(0x864);
            CHK_ReachSouthern.Checked = getFerryFlagFromNum(0x8B3);
            CHK_ReachBirth.Checked = getFerryFlagFromNum(0x8D5);
            CHK_ReachFaraway.Checked = getFerryFlagFromNum(0x8D6);
            CHK_ReachNavel.Checked = getFerryFlagFromNum(0x8E0);
            CHK_ReachBF.Checked = getFerryFlagFromNum(0x1D0);
            CHK_InitialSouthern.Checked = getFerryFlagFromNum(0x1AE);
            CHK_InitialBirth.Checked = getFerryFlagFromNum(0x1AF);
            CHK_InitialFaraway.Checked = getFerryFlagFromNum(0x1B0);
            CHK_InitialNavel.Checked = getFerryFlagFromNum(0x1DB);
        }
        private bool getFerryFlagFromNum(int n)
        {
            return (SAV.Data[ofsFerry + (n >> 3)] >> (n & 7) & 1) != 0;
        }
        private void setFerryFlagFromNum(int n, bool b)
        {
            SAV.Data[ofsFerry + (n >> 3)] = (byte)(SAV.Data[ofsFerry + (n >> 3)] & ~(1 << (n & 7)) | (b ? 1 : 0) << (n & 7));
        }
        private void saveFerry()
        {
            setFerryFlagFromNum(0x864, CHK_Catchable.Checked);
            setFerryFlagFromNum(0x8B3, CHK_ReachSouthern.Checked);
            setFerryFlagFromNum(0x8D5, CHK_ReachBirth.Checked);
            setFerryFlagFromNum(0x8D6, CHK_ReachFaraway.Checked);
            setFerryFlagFromNum(0x8E0, CHK_ReachNavel.Checked);
            setFerryFlagFromNum(0x1D0, CHK_ReachBF.Checked);
            setFerryFlagFromNum(0x1AE, CHK_InitialSouthern.Checked);
            setFerryFlagFromNum(0x1AF, CHK_InitialBirth.Checked);
            setFerryFlagFromNum(0x1B0, CHK_InitialFaraway.Checked);
            setFerryFlagFromNum(0x1DB, CHK_InitialNavel.Checked);
        }
        #endregion

        #region BattleFrontier
        private int[] Symbols;
        private int ofsSymbols;
        private Color[] SymbolColorA;
        private Button[] SymbolButtonA;
        private bool editingcont;
        private bool editingval;
        private RadioButton[] StatRBA;
        private NumericUpDown[] StatNUDA;
        private Label[] StatLabelA;
        private bool loading;
        private int[][] BFF;
        private string[][] BFT;
        private int[][] BFV;
        private string[] BFN;
        private void ChangeStat1(object sender, EventArgs e)
        {
            if (loading) return;
            int facility = CB_Stats1.SelectedIndex;
            if (facility < 0 || facility >= BFN.Length) return;
            editingcont = true;
            CB_Stats2.Items.Clear();
            foreach (RadioButton r in StatRBA)
                r.Checked = false;

            if (BFT[BFF[facility][1]] == null) CB_Stats2.Visible = false;
            else
            {
                CB_Stats2.Visible = true;
                for (int i = 0; i < BFT[BFF[facility][1]].Length; i++)
                    CB_Stats2.Items.Add(BFT[BFF[facility][1]][i]);
                CB_Stats2.SelectedIndex = 0;
            }

            for (int i = 0; i < StatLabelA.Length; i++)
                StatLabelA[i].Visible = StatLabelA[i].Enabled = StatNUDA[i].Visible = StatNUDA[i].Enabled = Array.IndexOf(BFV[BFF[facility][0]], i) >= 0;

            editingcont = false;
            StatRBA[0].Checked = true;
        }
        private void ChangeStat(object sender, EventArgs e)
        {
            if (editingcont) return;
            StatAddrControl(SetValToSav: -2, SetSavToVal: true);
        }
        private void StatAddrControl(int SetValToSav = -2, bool SetSavToVal = false)
        {
            int Facility = CB_Stats1.SelectedIndex;
            if (Facility < 0) return;

            int BattleType = CB_Stats2.SelectedIndex;
            if (BFT[BFF[Facility][1]] == null) BattleType = 0;
            else if (BattleType < 0) return;
            else if (BattleType >= BFT[BFF[Facility][1]].Length) return;

            int RBi = -1;
            for (int i = 0, j = 0; i < StatRBA.Length; i++)
            {
                if (!StatRBA[i].Checked) continue;
                if (++j > 1) return;
                RBi = i;
            }
            if (RBi < 0) return;

            if (SetValToSav >= 0)
            {
                ushort val = (ushort)StatNUDA[SetValToSav].Value;
                SetValToSav = Array.IndexOf(BFV[BFF[Facility][0]], SetValToSav);
                if (SetValToSav < 0) return;
                if (val > 9999) val = 9999;
                BitConverter.GetBytes(val).CopyTo(SAV.Data, SAV.getBlockOffset(0) + BFF[Facility][2 + SetValToSav] + 4 * BattleType + 2 * RBi);
                return;
            }
            if (SetValToSav == -1)
            {
                int p = BFF[Facility][2 + BFV[BFF[Facility][0]].Length + BattleType] + RBi;
                int offset = SAV.getBlockOffset(0) + 0xCDC;
                BitConverter.GetBytes(BitConverter.ToUInt32(SAV.Data, offset) & (uint)~(1 << p) | (uint)((CHK_Continue.Checked ? 1 : 0) << p)).CopyTo(SAV.Data, offset);
                return;
            }
            if (!SetSavToVal)
                return;
            
            editingval = true;
            for (int i = 0; i < BFV[BFF[Facility][0]].Length; i++)
            {
                int vali = BitConverter.ToUInt16(SAV.Data, SAV.getBlockOffset(0) + BFF[Facility][2 + i] + 4 * BattleType + 2 * RBi);
                if (vali > 9999) vali = 9999;
                StatNUDA[BFV[BFF[Facility][0]][i]].Value = vali;
            }
            CHK_Continue.Checked = (BitConverter.ToUInt32(SAV.Data, SAV.getBlockOffset(0) + 0xCDC) & 1 << (BFF[Facility][2 + BFV[BFF[Facility][0]].Length + BattleType] + RBi)) != 0;
            editingval = false;
        }
        private void ChangeStatVal(object sender, EventArgs e)
        {
            if (editingval) return;
            int n = Array.IndexOf(StatNUDA, sender);
            if (n < 0) return;
            StatAddrControl(SetValToSav: n, SetSavToVal: false);
        }

        private void CHK_Continue_CheckedChanged(object sender, EventArgs e)
        {
            if (editingval) return;
            StatAddrControl(SetValToSav: -1, SetSavToVal: false);
        }

        private void readBF()
        {
            loading = true;
            BFF = new[] {
                // { BFV, BFT, addr(BFV.len), checkBitShift(BFT.len)
                new[] { 0, 2, 0xCE0, 0xCF0, 0x00, 0x0E, 0x10, 0x12 },
                new[] { 1, 1, 0xD0C, 0xD14, 0xD1C, 0x02, 0x14 },
                new[] { 0, 1, 0xDC8, 0xDD0, 0x04, 0x16 },
                new[] { 0, 0, 0xDDA, 0xDDE, 0x06 },
                new[] { 2, 1, 0xDE2, 0xDF2, 0xDEA, 0xDFA, 0x08, 0x18 },
                new[] { 1, 0, 0xE04, 0xE08, 0xE0C, 0x0A },
                new[] { 0, 0, 0xE1A, 0xE1E, 0x0C },
            };
            BFV = new[]
            {
                new[] { 0, 2 }, // Current, Max
                new[] { 0, 2, 3 }, // Current, Max, Total
                new[] { 0, 1, 2, 3 }, // Current, Trade, Max, Trade
            };
            BFT = new[] {
                null,
                new[] { "Singles", "Doubles" },
                new[] { "Singles", "Doubles", "Multi", "Linked" },
            };
            BFN = new[]
            {
                "Tower","Dome","Palace","Arena","Factory","Pike","Pyramid"
            };
            StatNUDA = new[] { NUD_Stat0, NUD_Stat1, NUD_Stat2, NUD_Stat3 };
            StatLabelA = new[] { L_Stat0, L_Stat1, L_Stat2, L_Stat3 };
            StatRBA = new[] { RB_Stats3_01, RB_Stats3_02 };
            SymbolColorA = new[] { Color.Transparent, Color.Silver, Color.Silver, Color.Gold };
            SymbolButtonA = new[] { BTN_SymbolA, BTN_SymbolT, BTN_SymbolS, BTN_SymbolG, BTN_SymbolK, BTN_SymbolL, BTN_SymbolB };
            ofsSymbols = SAV.getBlockOffset(2) + 0x408;
            int iSymbols = BitConverter.ToInt32(SAV.Data, ofsSymbols) >> 4 & 0x7FFF;
            CHK_ActivatePass.Checked = (iSymbols >> 14 & 1) != 0;
            Symbols = new int[7];
            for (int i = 0; i < 7; i++)
                Symbols[i] = iSymbols >> i * 2 & 3;
            setSymbols();

            CB_Stats1.Items.Clear();
            foreach (string t in BFN)
                CB_Stats1.Items.Add(t);

            loading = false;
            CB_Stats1.SelectedIndex = 0;
        }
        private void setSymbols()
        {
            for (int i = 0; i < SymbolButtonA.Length; i++)
                SymbolButtonA[i].BackColor = SymbolColorA[Symbols[i]];
        }
        private void saveBF()
        {
            uint iSymbols = 0;
            for (int i = 0; i < 7; i++)
                iSymbols |= (uint)((Symbols[i] & 3) << i * 2);
            if (CHK_ActivatePass.Checked)
                iSymbols |= 1 << 14;
            BitConverter.GetBytes(BitConverter.ToUInt32(SAV.Data, ofsSymbols) & ~(0x7FFF << 4) | (iSymbols & 0x7FFF) << 4).CopyTo(SAV.Data, ofsSymbols);
        }
        private void BTN_Symbol_Click(object sender, EventArgs e)
        {
            int index = Array.IndexOf(SymbolButtonA, sender);
            if (index < 0) return;

            // 0 (none) | 1 (silver) | 2 (silver) | 3 (gold)
            // bit rotation 00 -> 01 -> 11 -> 00
            if (Symbols[index] == 1) Symbols[index] = 3;
            else Symbols[index] = (Symbols[index] + 1) & 3;

            setSymbols();
        }
        #endregion

    }
}
