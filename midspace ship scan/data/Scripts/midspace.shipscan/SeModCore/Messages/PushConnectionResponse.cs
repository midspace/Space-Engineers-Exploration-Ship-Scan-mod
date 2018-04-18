namespace MidSpace.ShipScan.SeModCore.Messages
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
    public class PushConnectionResponse : PushMessageBase
    {
        [ProtoMember(203)]
        public bool IsOldCommunicationVersion;

        [ProtoMember(204)]
        public bool IsNewCommunicationVersion;

        [ProtoMember(205)]
        public uint UserSecurity { get; set; }

        public static void SendMessage(ulong steamdId, int clientModCommunicationVersion, int serverModCommunicationVersion, uint userSecurity)
        {
            ConnectionHelper.SendMessageToPlayer(steamdId, new PushConnectionResponse
            {
                IsOldCommunicationVersion = clientModCommunicationVersion < serverModCommunicationVersion,
                IsNewCommunicationVersion = clientModCommunicationVersion > serverModCommunicationVersion,
                UserSecurity = userSecurity,
            });

            // This will send the custom ClientConfig to the client also.
            // It needs to be seperated from the MessageConnectionResponse, so that updated version information 
            // can be passed safely without the chance of the binary deserialized failing because the mod changed.
            // This is the purpose of the ModCommunicationVersion. So that it can be passed safely, and thus disable the
            // mod client side if the server hasn't yet been restarted with the updated mod.

            ConnectionHelper.SendMessageToPlayer(steamdId, new PushClientConfig { ClientConfigResponse = MainChatCommandLogic.Instance.GetConfig() });
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
                MyAPIGateway.Utilities.ShowNotification($"Mod Warning: The version of {MainChatCommandLogic.Instance.ModTitle} running on your Server is wrong.", 5000, MyFontEnum.Blue);
                // TODO: display OldCommunicationVersion.

                // The server has a newer version!
                // This really shouldn't happen.
                MainChatCommandLogic.Instance.ClientLogger.WriteInfo($"Mod Warning: The {MainChatCommandLogic.Instance.ModTitle} is currently unavailable as it is out of date. Please check to make sure you have downloaded the latest version of the mod.");
            }
            if (IsNewCommunicationVersion)
            {
                isConnected = false;
                if (MyAPIGateway.Session.Player.IsAdmin())
                {
                    MyAPIGateway.Utilities.ShowMissionScreen("Server", "Mod Warning", " ", $"The version of {MainChatCommandLogic.Instance.ModTitle} running on your Server is out of date.\r\nPlease update and restart your server.\r\nThis mod will be disabled on the client side until the server has updated.");
                    MyAPIGateway.Utilities.ShowNotification($"Mod Warning: The version of {MainChatCommandLogic.Instance.ModTitle} running on your Server is out of date.", 5000, MyFontEnum.Blue);
                    // TODO: display NewCommunicationVersion.
                }
                else
                {
                    MyAPIGateway.Utilities.ShowMissionScreen("Server", "Mod Warning", " ", $"The {MainChatCommandLogic.Instance.ModTitle} mod is currently unavailable as it is out of date.\r\nPlease contact your game server Administrator.");
                    MyAPIGateway.Utilities.ShowNotification($"Mod Warning: The {MainChatCommandLogic.Instance.ModTitle} mod is currently unavailable as it is out of date.", 5000, MyFontEnum.Blue);
                    // TODO: display NewCommunicationVersion.
                }
                MainChatCommandLogic.Instance.ClientLogger.WriteInfo($"Mod Warning: The {MainChatCommandLogic.Instance.ModTitle} mod is currently unavailable as it is out of date on the server. Please contact your server Administrator to make sure they have the latest version of the mod.");
            }

            MainChatCommandLogic.Instance.IsConnected = isConnected;
            MainChatCommandLogic.Instance.ResponseReceived = true;
        }
    }
}
