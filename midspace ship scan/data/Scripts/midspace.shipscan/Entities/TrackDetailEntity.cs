namespace MidSpace.ShipScan.Entities
{
    using System.Xml.Serialization;
    using ProtoBuf;
    using VRage;
    using VRageMath;

    [ProtoContract]
    [XmlType("TrackDetail")]
    public class TrackDetailEntity
    {
        [ProtoMember(201)]
        public SerializableVector3D Position { get; set; }

        [ProtoMember(202)]
        public string Title { get; set; }

        // Unused currently.
        //public long[] EntityIds { get; set; }

        // Empty Constructor for Xml serialization.
        public TrackDetailEntity()
        {
        }

        public TrackDetailEntity(Vector3D position, string title /*, IEnumerable<long> entityIds*/)
        {
            Position = position;
            Title = title;
            //EntityIds = entityIds.ToArray();
        }

    }
}
