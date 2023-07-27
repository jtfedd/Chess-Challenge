using ChessChallenge.Application;

namespace ChessChallenge.BotMatch
{
    internal class Util
    {
        public static string GetPlayerName(ChessPlayer player) => player.PlayerType.ToString();
        public static string GetPlayerName(BotType type) => type.ToString();
    }
}
