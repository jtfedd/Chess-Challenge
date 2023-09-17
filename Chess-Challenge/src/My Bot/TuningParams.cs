using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        public int[] values;

        public TuningParams()
        {
            values = new int[] { 114, 301, 298, 469, 833, 100, 51, 49, 55, 87, 106, 219, 100, 100, 63, 43, 56, 91, 129, 203, 100, 100, 61, 53, 63, 95, 100, 219, 100, 100, 43, 55, 70, 88, 148, 219, 100, 27, 66, 72, 79, 137, 152, 134, 1, 58, 62, 95, 105, 99, 112, 149, 47, 59, 86, 97, 113, 149, 145, 135, 76, 51, 100, 91, 120, 143, 117, 80, 124, 82, 102, 104, 80, 117, 138, 107, 90, 104, 113, 99, 80, 106, 127, 101, 105, 94, 109, 112, 104, 116, 120, 103, 43, 87, 107, 116, 111, 118, 74, 77, 75, 82, 58, 81, 79, 130, 113, 129, 139, 95, 77, 77, 74, 127, 139, 103, 137, 95, 81, 70, 101, 109, 123, 117, 149, 110, 93, 79, 92, 116, 143, 126, 120, 103, 84, 74, 84, 96, 91, 69, 97, 60, 62, 65, 34, 63, 81, 56, 70, 56, 67, 53, 40, 83, 87, 52, 38, 81, 72, 58, 60, 57, 53, 72, 120, 147, 160, 40, 18, 115, 102, 216, 100, 153, 126, 28, 36, 97, 216, 0, 0, 101, 68, 16, 0, 0, 0, 0, 0, 107, 53, 20, 0, 0, 127, 46, 0, 60, 86, 42, 38, 162, 0, 46, 216, 49, 59, 64, 99, 147, 71, 142, 206, 50, 63, 98, 92, 114, 168, 121, 164, 82, 81, 96, 91, 109, 24, 172, 29, 7, 20, 16, 16, 13, 24, 19, 45, 18 };
            //values = new int[] { 100, 300, 300, 500, 900, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 0, 0, 0, 0, 0, 0, 0, 0, 0};
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


        public static int min(int index)
        {
            if (index < 229) return 0;
            return -200;
        }

        public static int max(int index)
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
