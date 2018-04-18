namespace MidSpace.ShipScan.Messages
{
    using Commands;
    using Entities;
    using ProtoBuf;
    using SeModCore;

    /// <summary>
    /// This will fetch the data required to track a scan detection, for the player to display
    /// </summary>
    [ProtoContract]
    public class PushSetTrack : PushMessageBase
    {
        #region properties

        /// <summary>
        /// The name of the ship or hotlist number.
        /// </summary>
        [ProtoMember(201)]
        public string ShipName;

        [ProtoMember(202)]
        public TrackDetailEntity TrackEntity;

        #endregion

        public override void ProcessClient()
        {
            // set client side properties...
           CommandScanTrack.SetTracking(TrackEntity);
        }
    }
}
