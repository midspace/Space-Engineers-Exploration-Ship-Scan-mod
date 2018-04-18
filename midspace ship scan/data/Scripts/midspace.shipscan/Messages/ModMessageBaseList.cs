// Do not change the namespace. This must match the namespace of the other ModMessageBase class for it to work.
namespace MidSpace.ShipScan.SeModCore
{
    using System.Xml.Serialization;
    using ProtoBuf;
    using ShipScan.Messages;

    // Add the XmlInclude if you also intend to serialize to XML.
    // ALL CLASSES DERIVED FROM MessageBase MUST BE ADDED HERE
    //[XmlInclude(typeof(....))]

    /// <summary>
    /// This is for sending messages from Client to Server, where the Server will process them upon receipt.
    /// </summary>
    // Modder programmers should add their own Messages here.
    // ProtoInclude tags needs to start from 100.
    [ProtoInclude(100, typeof(PullClearScan))]
    [ProtoInclude(101, typeof(PullInitiateScan))]
    [ProtoInclude(102, typeof(PullSetTrack))]
    public abstract partial class PullMessageBase
    {
        // this is left empty.
    }

    /// <summary>
    /// This is for sending messages from Server to Client, where the Client will process them upon receipt.
    /// </summary>
    // Modder programmers should add their own Messages here.
    // ProtoInclude tags needs to start from 100.
    [ProtoInclude(100, typeof(PushSetTrack))]
    public abstract partial class PushMessageBase
    {
        // this is left empty.
    }
}
