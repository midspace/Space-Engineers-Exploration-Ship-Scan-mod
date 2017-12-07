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
            MyAPIGateway.Utilities.ShowMessage("/scanclear", "Will remove all currently displayed scanned GPS coordinates.");
        }

        public override bool Invoke(ulong steamId, long playerId, string messageText)
        {
            if (MainChatCommandLogic.Instance.Settings.Config.TrackEntites == null)
                return true;

            foreach (var trackEntity in MainChatCommandLogic.Instance.Settings.Config.TrackEntites)
            {
                MyAPIGateway.Session.GPS.RemoveGps(MyAPIGateway.Session.Player.IdentityId, trackEntity.GpsHash);
            }

            MainChatCommandLogic.Instance.Settings.Config.TrackEntites.Clear();

            return true;
        }
    }
}
