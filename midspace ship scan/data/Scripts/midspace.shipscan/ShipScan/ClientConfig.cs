namespace midspace.shipscan
{
    using ProtoBuf;

    [ProtoContract]
    public class ClientConfig
    {
        // This class is supposed to be for the Mod to return mod specific setting upon initial connection.
        // It needs to be seperated from the MessageConnectionResponse, so that updated version information 
        // can be passed safely without the chance of the binary deserialized failing because the mod changed.
        // This is the purpose of the ModCommunicationVersion. So that it can be passed safely, and thus disable the
        // mod client side if the server hasn't yet been restarted with the updated mod.


        // This is a test property for Serialization changes, and can be removed.
        public int Test1 { get; set; }


        internal static ClientConfig FetchClientResponse()
        {
            return new ClientConfig {};
        }
    }
}
