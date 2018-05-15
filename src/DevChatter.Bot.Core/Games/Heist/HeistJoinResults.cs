namespace DevChatter.Bot.Core.Games.Heist
{
    public static class HeistJoinResults
    {
        public static JoinGameResult SuccessJoinResult(string displayName, HeistRoles role)
            => new JoinGameResult(true, $"{displayName} joined the heist as the {role}!");
        public static JoinGameResult RoleTakenResult(string displayName, HeistRoles role)
            => new JoinGameResult(false, $"Sorry, {displayName} we already have a {role}!");
        public static JoinGameResult HeistFullResult(string displayName)
            => new JoinGameResult(false, $"Sorry, {displayName} this heist is full!");
        public static JoinGameResult UnknownRoleResult(string displayName)
            => new JoinGameResult(false, $"I don't know what role you wanted to be, {displayName}. Try again?");
        public static JoinGameResult AlreadyInHeistResult(string displayName)
            => new JoinGameResult(false, $"You're already in this heist, {displayName} and you aren't a multi-tasker.");

    }
}
