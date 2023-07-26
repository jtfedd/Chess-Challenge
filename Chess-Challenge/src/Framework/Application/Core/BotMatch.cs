using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChessChallenge.Application.ConsoleHelper;


namespace ChessChallenge.Application
{
    static class BotMatch
    {
        public static void BotMatchMain()
        {
            MatchController controller = new();

            while(controller.MatchInProgress)
            {
                controller.Update();
            }
        }
    }
}
