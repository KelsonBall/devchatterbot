namespace DevChatter.Bot.Core.Games
{
    public static class StaticResults
    {
        public static JoinGameResult SuccessJoinResult(string displayName)
            => new JoinGameResult(true, $"{displayName} joined the game!");
        public static JoinGameResult NotJoinTimeResult(string displayName)
            => new JoinGameResult(false, $"Sorry, {displayName} this is not the time to join!");
        public static JoinGameResult AlreadyInGameResult(string displayName)
            => new JoinGameResult(false, $"You're already in this game, {displayName} and you aren't a multi-tasker.");
    }
}
