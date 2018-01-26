namespace midspace.shipscan
{
    using System.Xml.Serialization;
    using ProtoBuf;

    // Add the XmlInclude if you also intend to serialize to XML.
    // ALL CLASSES DERIVED FROM MessageBase MUST BE ADDED HERE
    //[XmlInclude(typeof(....))]

    // Modder programmers should add their own Messages here.
    // ProtoInclude tags needs to start from 100.
    [ProtoInclude(101, typeof(MessageInitiateScan))]
    [ProtoInclude(102, typeof(MessageClearScan))]
    [ProtoInclude(103, typeof(MessageSetIgnore))]
    [ProtoInclude(104, typeof(MessageSetTrack))]
    public abstract partial class ModMessageBase
    {
        // this is left empty.
    }
}
