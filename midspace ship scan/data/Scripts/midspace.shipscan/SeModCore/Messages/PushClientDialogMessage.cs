namespace MidSpace.ShipScan.SeModCore.Messages
{
    using ProtoBuf;
    using Sandbox.ModAPI;

    [ProtoContract]
    public class PushClientDialogMessage : PushMessageBase
    {
        [ProtoMember(201)]
        public string Title;

        [ProtoMember(202)]
        public string Prefix;

        [ProtoMember(203)]
        public string Content;

        public override void ProcessClient()
        {
            MyAPIGateway.Utilities.ShowMissionScreen(Title, Prefix, " ", Content);
        }

        public static void SendMessage(ulong steamId, string title, string prefix, string content)
        {
            ConnectionHelper.SendMessageToPlayer(steamId, new PushClientDialogMessage { Title = title, Prefix = prefix, Content = content });
        }
    }
}
