namespace midspace.shipscan
{
    using ProtoBuf;
    using Sandbox.ModAPI;

    [ProtoContract]
    public class MessageChatCommand : ModMessageBase
    {
        [ProtoMember(201)]
        public long IdentityId;

        [ProtoMember(202)]
        public string TextCommand;

        public override void ProcessClient()
        {
            // should only ever be sent from the server.
            if (MyAPIGateway.Multiplayer.ServerId == SenderSteamId)
            {
                if (!ChatCommandService.ProcessClientMessage(TextCommand))
                {
                    //MyAPIGateway.Utilities.SendMessage(SenderSteamId, "CHECK", "ProcessServerMessage failed.");
                }
            }
        }

        public override void ProcessServer()
        {
            if (!ChatCommandService.ProcessServerMessage(SenderSteamId, IdentityId, TextCommand))
            {
                //MyAPIGateway.Utilities.SendMessage(SenderSteamId, "CHECK", "ProcessServerMessage failed.");
            }
        }

        public static void SendMessage(long identityId, string textCommand)
        {
            ConnectionHelper.SendMessageToServer(new MessageChatCommand { IdentityId = identityId, TextCommand = textCommand });
        }
    }
}
