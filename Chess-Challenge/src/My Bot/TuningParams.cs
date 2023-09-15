using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Chess_Challenge.src.My_Bot
{
    public class TuningParams
    {
        public TuningParams(TuningParams toCopy)
        {
            values = new int[toCopy.values.Length];
            Array.Copy(toCopy.values, values, toCopy.values.Length);
        }

        public int[] values;

        public TuningParams()
        {
            values = new int[] { 100, 300, 300, 500, 900, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 0, 0, 0, 0, 0, 0, 0, 0, 0};
        }

        public string print()
        {
            return string.Join(", ", values);
        }

        public static bool skip(int index)
        {
            return index == 5 ||
                index == 12 ||
                index == 13 ||
                index == 20 ||
                index == 21 ||
                index == 28 ||
                index == 29 ||
                index == 26;
        }


        public int min(int index)
        {
            if (index < 229) return 0;
            return -200;
        }

        public int max(int index)
        {
            if (index < 5)
            {
                return 1000;
            }
            if (index < 229)
            {
                return 250;
            }

            return 200;
        }

        public int pawn => values[0];
        public int knight => values[1];
        public int bishop => values[2];
        public int rook => values[3];
        public int queen => values[4];

        public int mobility => values[229];
        public int kingAttack => values[230];
        public int pawnChain => values[231];
        public int doubledPawns => values[232];
        public int isolatedPawns => values[233];
        public int passedPawns => values[234];
        public int kingDefense => values[235];
        public int bishopPair => values[236];
        public int bishopEndgame => values[237];

        public ulong[] pv => buildPV();
        ulong[] buildPV()
        {
            ulong[] pv = new ulong[28];
            int offset = 5;
            for (int i = 0; i < 28; i++)
            {
                ulong value = 0;
                for (int j = 0; j < 8; j++)
                {
                    ulong truncated = (ulong)values[(8*i) + j + offset] & 0xFFul;
                    value |= truncated << (8 * j);
                }
                pv[i] = value;
            }
            return pv;
        }
    }
}
