namespace MidSpace.ShipScan.SeModCore.Messages
{
    using ProtoBuf;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;

    [ProtoContract]
    public class MessageConnectionRequest : ModMessageBase
    {
        [ProtoMember(201)]
        public int ModCommunicationVersion { get; set; }

        [ProtoMember(202)]
        public long PrivateCommunicationKey { get; set; }

        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
            // TODO: The checks for the version conflict and protection are been done on the client side, so this check can probably be removed.
            //if (ModCommunicationVersion != ModConfigurationConsts.ModCommunicationVersion)
            //{
            //    MainChatCommandLogic.Instance.ServerLogger.WriteVerbose($"Player '{SenderDisplayName}' {SenderSteamId} connected with invalid version {ModCommunicationVersion}. Should be {ModConfigurationConsts.ModCommunicationVersion}.");

            //    // TODO: respond to the potentional communication conflict.
            //    // Could Client be older than Server?
            //    // It's possible, if the Client has trouble downloading from Steam Community which can happen on occasion.
            //    return;
            //}

            IMyPlayer player = MyAPIGateway.Players.GetPlayer(SenderSteamId);
            if (player == null)
            {
                MainChatCommandLogic.Instance.ServerLogger.WriteWarning($"Player '{SenderDisplayName}' {SenderSteamId} connected with invalid SteamId or before server has player information ready.");
                // If the Server isn't ready yet, the Client will send another Request automatically.
                // If the SteamId is invalid, we'll keep ignoring it anyhow.
                return;
            }

            MainChatCommandLogic.Instance.ServerLogger.WriteInfo($"Player '{SenderDisplayName}' {SenderSteamId} connected. Version {ModCommunicationVersion}");

            uint userSecurity = player.UserSecurityLevel();

            // Is Server version older than what Client is running, or Server version is newer than Client.
            MessageConnectionResponse.SendMessage(SenderSteamId, ModCommunicationVersion, MainChatCommandLogic.Instance.ModCommunicationVersion, userSecurity);
        }

        public static void SendMessage(int modCommunicationVersion, long privateCommunicationKey)
        {
            MainChatCommandLogic.Instance.ClientLogger.WriteInfo("Sending Connection Request");
            ConnectionHelper.SendMessageToServer(new MessageConnectionRequest
            {
                ModCommunicationVersion = modCommunicationVersion,
                PrivateCommunicationKey = privateCommunicationKey
            });
        }
    }
}
