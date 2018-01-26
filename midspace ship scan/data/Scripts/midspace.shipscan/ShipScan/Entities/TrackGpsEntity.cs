namespace midspace.shipscan
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [XmlType("TrackGps")]
    public class TrackGpsEntity
    {
        public int GpsHash { get; set; }
        public List<long> Entities { get; set; } = new List<long>();
    }
}
