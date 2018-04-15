namespace MidSpace.ShipScan.Messages
{
    using Entities;
    using ProtoBuf;
    using Sandbox.ModAPI;
    using SeModCore;

    /// <summary>
    /// this will clear gps scan markers.
    /// </summary>
    [ProtoContract]
    public class MessageClearScan : SeModCore.ModMessageBase
    {
        /// <summary>
        /// The type of scan type to carry out.
        /// </summary>
        [ProtoMember(201)]
        public ScanType DisplayType;

        public static void SendMessage(ScanType displayType)
        {
            ConnectionHelper.SendMessageToServer(new MessageClearScan { DisplayType = displayType });
        }

        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
            ScanSettingsEntity scanSettings = ScanDataManager.GetPlayerScanData(SenderSteamId);
            var player = MyAPIGateway.Players.FindPlayerBySteamId(SenderSteamId);

            switch (DisplayType)
            {
                case ScanType.GpsCoordinates:
                    if (scanSettings.ScanListGpsEntities == null || player == null)
                        return;

                    foreach (var trackEntity in scanSettings.ScanListGpsEntities)
                    {
                        MyAPIGateway.Session.GPS.RemoveGps(player.IdentityId, trackEntity.GpsHash);
                    }

                    scanSettings.ScanListGpsEntities.Clear();
                    break;

                case ScanType.ChatConsole:
                case ScanType.MissionScreen:
                    scanSettings.ScanHostList.Clear();
                    break;
            }
        }
    }
}
