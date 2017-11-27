namespace midspace.shipscan
{
    using System.Collections.Generic;
    using System.Linq;
    using VRageMath;

    public class TrackDetail
    {
        public TrackDetail(Vector3D position, string title, IEnumerable<long> entityIds)
        {
            Position = position;
            Title = title;
            EntityIds = entityIds.ToArray();
        }

        public Vector3D Position { get; set; }
        public string Title { get; set; }
        public long[] EntityIds { get; set; }
    }
}
