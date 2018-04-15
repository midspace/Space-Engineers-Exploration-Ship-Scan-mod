namespace MidSpace.ShipScan
{
    using ProtoBuf;
    using SeModCore;

    [ProtoContract]
    public class ClientConfig : ClientConfigBase
    {
        // This class is supposed to be for the Mod to return mod specific setting upon initial connection.
        // It must inherit from ClientConfigBase, and all properties must use ProtoMemberAttribute.

        // This is a test property for Serialization changes, and can be removed.
        [ProtoMember(101)]
        public int Test1 { get; set; }

        /// <summary>
        /// This is for the Server to send the initial config to the Client.
        /// </summary>
        public static ClientConfigBase FetchClientResponse()
        {
            // Set up, create, read any values you need to send to your client. 
            return new ClientConfig { Test1 = 123 };
        }
    }
}
