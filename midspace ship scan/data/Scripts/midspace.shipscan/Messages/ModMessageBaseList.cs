namespace midspace.shipscan
{
    using System.Xml.Serialization;
    using ProtoBuf;

    // ALL CLASSES DERIVED FROM MessageBase MUST BE ADDED HERE
    //[XmlInclude(typeof(MessageTest))]
    //[XmlInclude(typeof(MessageConfig))]

    [ProtoInclude(101, typeof(MessageTest))]
    //[ProtoInclude(103, typeof(MessageConfig))]
    public abstract partial class ModMessageBase
    {

    }
}
