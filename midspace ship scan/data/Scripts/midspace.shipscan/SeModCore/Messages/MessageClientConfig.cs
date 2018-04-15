﻿namespace MidSpace.ShipScan.SeModCore.Messages
{
    using ProtoBuf;

    /// <summary>
    /// This handles the receiving the custom ClientConfig.
    /// </summary>
    [ProtoContract]
    public class MessageClientConfig : ModMessageBase
    {
        [ProtoMember(101)]
        public ClientConfigBase ClientConfigResponse { get; set; }

        public override void ProcessClient()
        {
            MainChatCommandLogic.Instance.ClientConfig = ClientConfigResponse;

            // stop further requests
            MainChatCommandLogic.Instance.CancelClientConnection();
            MainChatCommandLogic.Instance.ClientLogger.WriteInfo($"{MainChatCommandLogic.Instance.ModName} is ready.");
        }

        public override void ProcessServer()
        {
            // never processed on server
        }
    }
}
