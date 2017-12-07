namespace midspace.shipscan
{
    using Sandbox.ModAPI;

    public class CommandScanClear : ChatCommand
    {
        public CommandScanClear()
            : base(ChatCommandSecurity.User, "scanclear", new[] { "/scanclear" })
        {
        }

        public override void Help()
        {
            MyAPIGateway.Utilities.ShowMessage("/scanclear", "Will remove all currently displayed scanned GPS coordinates.");
        }

        public override bool Invoke(string messageText)
        {
            if (ChatCommandLogicShipScan.Instance.Settings.Config.TrackEntites == null)
                return true;

            foreach (var trackEntity in ChatCommandLogicShipScan.Instance.Settings.Config.TrackEntites)
            {
                MyAPIGateway.Session.GPS.RemoveGps(MyAPIGateway.Session.Player.IdentityId, trackEntity.GpsHash);
            }

            ChatCommandLogicShipScan.Instance.Settings.Config.TrackEntites.Clear();

            return true;
        }
    }
}
