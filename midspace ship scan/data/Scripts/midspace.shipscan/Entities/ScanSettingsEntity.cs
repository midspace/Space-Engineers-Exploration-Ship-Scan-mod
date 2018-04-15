namespace MidSpace.ShipScan.Entities
{
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using VRage;

    /// <summary>
    /// contains the configuration for saving.
    /// </summary>
    [XmlType("ScanSettings")]
    public class ScanSettingsEntity
    {
        public ulong SteamId { get; set; }

        public bool IgnoreJunk { get; set; }
        public bool IgnoreTiny { get; set; }
        public bool IgnoreSmall { get; set; }
        public bool IgnoreLarge { get; set; }
        public bool IgnoreHuge { get; set; }
        public bool IgnoreEnormous { get; set; }
        public bool IgnoreRidiculous { get; set; }

        /// <summary>
        /// List of all Grid entities that will be ignored during Scan.
        /// </summary>
        public List<long> IgnoreEntityId { get; set; } = new List<long>();

        ///// <summary>
        ///// List of all Cube entities that are Public LCD Displays that will be used to display live scan information.
        ///// </summary>
        //public List<long> PublicTrackDisplay;

        ///// <summary>
        ///// List of all Cube entities that are Private LCD Displays that will be used to display live scan information.
        ///// </summary>
        //public List<long> PrivateTrackDisplay;

        /// <summary>
        /// Used to track GPS scan entities.
        /// </summary>
        public List<TrackGpsEntity> ScanListGpsEntities { get; set; } = new List<TrackGpsEntity>();

        /// <summary>
        /// Used to track MissionScreen and ChatConsole entities.
        /// </summary>
        public List<TrackDetailEntity> ScanHostList { get; set; } = new List<TrackDetailEntity>();

        /// <summary>
        /// Tracking is turned on for the player.
        /// </summary>
        public bool TrackingOn { get; set; }
        public long TrackingCockpit { get; set; }
        public string TrackTitle { get; set; }
        public SerializableVector3D TrackPosition { get; set; }
    }
}
