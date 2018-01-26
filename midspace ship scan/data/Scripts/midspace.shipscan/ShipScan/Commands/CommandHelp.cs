namespace midspace.shipscan
{
    public class CommandHelp : ChatCommand
    {
        public CommandHelp()
            : base(ChatCommandSecurity.User, ChatCommandAccessibility.Client, "scanhelp", new[] { "/scanhelp" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            // no help.
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            Sandbox.Game.MyVisualScriptLogicProvider.OpenSteamOverlay(@"http://steamcommunity.com/sharedfiles/filedetails/?id=364731781");
            return true;
        }
    }
}
