namespace midspace.shipscan
{
    using Sandbox.ModAPI;

    public class CommandScanClear : ChatCommand
    {
        public CommandScanClear()
            : base(ChatCommandSecurity.User, ChatCommandAccessibility.Client, "scanclear", new[] { "/scanclear" })
        {
        }

        public override void Help(ulong steamId, bool brief)
        {
            MyAPIGateway.Utilities.SendMessage(steamId, "/scanclear", "Will remove all currently displayed scanned GPS coordinates.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            MessageClearScan.SendMessage(ScanType.GpsCoordinates);
            return true;
        }
    }
}
