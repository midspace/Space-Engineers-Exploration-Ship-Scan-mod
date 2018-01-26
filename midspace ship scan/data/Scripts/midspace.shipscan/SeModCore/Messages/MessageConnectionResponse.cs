namespace midspace.shipscan
{
    using ProtoBuf;
    using Sandbox.ModAPI;
    using VRage.Game;

    /// <summary>
    /// The contains the inital handshake response, and shouldn't contain any other custom code as it will affect 
    /// the structure of the serialized class if other classes are properties of this and are modified.
    /// This is so that version information is always passed intact.
    /// </summary>
    [ProtoContract]
    public class MessageConnectionResponse : ModMessageBase
    {
        [ProtoMember(203)]
        public bool IsOldCommunicationVersion;

        [ProtoMember(204)]
        public bool IsNewCommunicationVersion;

        [ProtoMember(205)]
        public uint UserSecurity { get; set; }

        // Client config needs to be handled by the mod, not the ModCore.

        public static void SendMessage(ulong steamdId, int clientModCommunicationVersion, int serverModCommunicationVersion, uint userSecurity)
        {
            MainChatCommandLogic.Instance.ServerLogger.WriteInfo("Sending Connection Response.");
            ConnectionHelper.SendMessageToPlayer(steamdId, new MessageConnectionResponse
            {
                IsOldCommunicationVersion = clientModCommunicationVersion < serverModCommunicationVersion,
                IsNewCommunicationVersion = clientModCommunicationVersion > serverModCommunicationVersion,
                UserSecurity = userSecurity
            });
            // TODO: Need an Action or handler for the Mod to send it's response.
        }

        public override void ProcessClient()
        {
            MainChatCommandLogic.Instance.ClientLogger.WriteInfo("Processing Connection Response");

            // stop further requests
            MainChatCommandLogic.Instance.CancelClientConnection();

            if (MainChatCommandLogic.Instance.ResponseReceived)
                return;

            ChatCommandService.UserSecurity = UserSecurity;
            bool isConnected = true;

            if (IsOldCommunicationVersion)
            {
                isConnected = false;
                MyAPIGateway.Utilities.ShowMissionScreen("Server", "Mod Warning", " ", "The version of {ModConfigurationConsts.ModTitle} running on your Server is wrong.\r\nPlease update and restart your server.");
                MyAPIGateway.Utilities.ShowNotification($"Mod Warning: The version of {ModConfigurationConsts.ModTitle} running on your Server is wrong.", 5000, MyFontEnum.Blue);
                // TODO: display OldCommunicationVersion.

                // The server has a newer version!
                // This really shouldn't happen.
                MainChatCommandLogic.Instance.ClientLogger.WriteInfo($"Mod Warning: The {ModConfigurationConsts.ModTitle} is currently unavailable as it is out of date. Please check to make sure you have downloaded the latest version of the mod.");
            }
            if (IsNewCommunicationVersion)
            {
                isConnected = false;
                if (MyAPIGateway.Session.Player.IsAdmin())
                {
                    MyAPIGateway.Utilities.ShowMissionScreen("Server", "Mod Warning", " ", $"The version of {ModConfigurationConsts.ModTitle} running on your Server is out of date.\r\nPlease update and restart your server.\r\nThis mod will be disabled on the client side until the server has updated.");
                    MyAPIGateway.Utilities.ShowNotification($"Mod Warning: The version of {ModConfigurationConsts.ModTitle} running on your Server is out of date.", 5000, MyFontEnum.Blue);
                    // TODO: display NewCommunicationVersion.
                }
                else
                {
                    MyAPIGateway.Utilities.ShowMissionScreen("Server", "Mod Warning", " ", $"The {ModConfigurationConsts.ModTitle} mod is currently unavailable as it is out of date.\r\nPlease contact your game server Administrator.");
                    MyAPIGateway.Utilities.ShowNotification($"Mod Warning: The {ModConfigurationConsts.ModTitle} mod is currently unavailable as it is out of date.", 5000, MyFontEnum.Blue);
                    // TODO: display NewCommunicationVersion.
                }
                MainChatCommandLogic.Instance.ClientLogger.WriteInfo($"Mod Warning: The {ModConfigurationConsts.ModTitle} mod is currently unavailable as it is out of date on the server. Please contact your server Administrator to make sure they have the latest version of the mod.");
            }
            MainChatCommandLogic.Instance.IsConnected = isConnected;
            MainChatCommandLogic.Instance.ResponseReceived = true;
        }

        public override void ProcessServer()
        {
            // never processed on server
        }
    }
}
