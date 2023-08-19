using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ChessChallenge.Application
{
    class PVTables
    {
        public static void Run()
        {
            Console.WriteLine("Hello, World");
        }

        static ulong[] PackedPV = new ulong[]
        {
            0x32643C3732373732,0x32643C37322D3C32,0x3264463C32283C32,0x3264504D4B321932

        };

        static short[] PawnTable = new short[]
        {
             0,   0,   0,   0,
            50,  50,  50,  50,
            10,  10,  20,  30,
             5,   5,  10,  27,
             0,   0,   0,  25,
             5,  -5, -10,   0,
             5,  10,  10, -25,
             0,   0,   0,   0,
        };

        static short[] KnightTable = new short[]
        {
            -50,-40,-30,-30,-30,-30,-40,-50,
            -40,-20, 0, 0, 0, 0,-20,-40,
            -30, 0, 10, 15, 15, 10, 0,-30,
            -30, 5, 15, 20, 20, 15, 5,-30,
            -30, 0, 15, 20, 20, 15, 0,-30,
            -30, 5, 10, 15, 15, 10, 5,-30,
            -40,-20, 0, 5, 5, 0,-20,-40,
            -50,-40,-20,-30,-30,-20,-40,-50,
        };

        static short[] BishopTable = new short[]
        {
            -20,-10,-10,-10,-10,-10,-10,-20,
            -10, 0, 0, 0, 0, 0, 0,-10,
            -10, 0, 5, 10, 10, 5, 0,-10,
            -10, 5, 5, 10, 10, 5, 5,-10,
            -10, 0, 10, 10, 10, 10, 0,-10,
            -10, 10, 10, 10, 10, 10, 10,-10,
            -10, 5, 0, 0, 0, 0, 5,-10,
            -20,-10,-40,-10,-10,-40,-10,-20,
        };

        static short[] KingTable = new short[]
        {
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -20, -30, -30, -40, -40, -30, -30, -20,
            -10, -20, -20, -20, -20, -20, -20, -10,
             20, 20, 0, 0, 0, 0, 20, 20,
             20, 30, 10, 0, 0, 10, 30, 20
        };
    }
}
