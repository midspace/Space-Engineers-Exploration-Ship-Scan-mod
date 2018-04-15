namespace MidSpace.ShipScan.SeModCore
{
    using ProtoBuf;

    /// <summary>
    /// This creates a tightly defined contract for the custom ClientConfig so it can be sent from the server to the client.
    /// </summary>
    [ProtoContract]
    [ProtoInclude(50, typeof(MidSpace.ShipScan.ClientConfig))]  // TODO: This refers directly into the Mod code. Find a better way to seperate framework from mod.
    public abstract class ClientConfigBase
    {
        // TODO: Not sure if there is a better way to seperate this, unless we can use an Interface, Action or handler for the Mod to send it's response.
        // Client config needs to be handled by the mod, not the ModCore.
    }
}
