namespace midspace.shipscan
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Sandbox.ModAPI;
    using VRage;

    public class ScanSettings
    {
        private readonly string _settingsFileName;
        public ScanSettingsStruct Config;
        public readonly FastResourceLock ExecutionLock = new FastResourceLock();

        public ScanSettings()
        {
            Config = new ScanSettingsStruct()
            {
                //init default values
                IgnoreJunk = false,
                IgnoreTiny = false,
                IgnoreSmall = false,
                IgnoreLarge = false,
                IgnoreHuge = false,
                IgnoreEnormous = false,
                IgnoreRidiculous = false,
                IgnoreEntityId = new List<long>(),
                TrackEntites = new List<TrackEntity>()
            };
            _settingsFileName = string.Format("ScanSettings_{0}.cfg", Path.GetFileNameWithoutExtension(MyAPIGateway.Session.CurrentPath));
        }

        public void Save()
        {
            using (ExecutionLock.AcquireExclusiveUsing())
            {
                VRage.Utils.MyLog.Default.WriteLine("#### ShipScan SaveData");
                using (TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(_settingsFileName, typeof(ScanSettings)))
                {
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML(Config));
                    writer.Flush();
                    writer.Close();
                }
            }
        }

        public void Load()
        {
            if (MyAPIGateway.Utilities.FileExistsInLocalStorage(_settingsFileName, typeof(ScanSettings)))
            {
                string xmlText;
                using (TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(_settingsFileName, typeof(ScanSettings)))
                {
                    xmlText = reader.ReadToEnd();
                    reader.Close();
                }
                if (string.IsNullOrWhiteSpace(xmlText))
                    return;

                try
                {
                    Config = MyAPIGateway.Utilities.SerializeFromXML<ScanSettingsStruct>(xmlText);
                    if (Config.IgnoreEntityId == null)
                        Config.IgnoreEntityId = new List<long>();
                    if (Config.TrackEntites == null)
                        Config.TrackEntites = new List<TrackEntity>();
                }
                catch (Exception ex)
                {
                    VRage.Utils.MyLog.Default.WriteLine(string.Format("#### ShipScan LoadData from file '{0}' failed: {1}", _settingsFileName, ex));
                }
            }
        }
    }

    /// <summary>
    /// contains the configuration for saving.
    /// </summary>
    public struct ScanSettingsStruct
    {
        public bool IgnoreJunk;
        public bool IgnoreTiny;
        public bool IgnoreSmall;
        public bool IgnoreLarge;
        public bool IgnoreHuge;
        public bool IgnoreEnormous;
        public bool IgnoreRidiculous;

        /// <summary>
        /// List of all Grid entities that will be ignored during Scan.
        /// </summary>
        public List<long> IgnoreEntityId;

        ///// <summary>
        ///// List of all Cube entities that are Public LCD Displays that will be used to display live scan information.
        ///// </summary>
        //public List<long> PublicTrackDisplay;

        ///// <summary>
        ///// List of all Cube entities that are Private LCD Displays that will be used to display live scan information.
        ///// </summary>
        //public List<long> PrivateTrackDisplay;

        public List<TrackEntity> TrackEntites;

        /// <summary>
        /// Tracking is turned on for the player.
        /// </summary>
        public bool TrackingOn;
        public long TrackingCockpit;
        public string TrackTitle;
        public SerializableVector3D TrackPosition;
    }

    public struct TrackEntity
    {
        public int GpsHash;
        public List<long> Entities;
    }
}
