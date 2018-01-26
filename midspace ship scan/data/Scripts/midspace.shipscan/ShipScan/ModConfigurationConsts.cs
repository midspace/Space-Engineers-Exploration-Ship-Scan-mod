namespace midspace.shipscan
{
    /// <summary>
    /// These options control the Mod's static configuration and are not designed to be dynamically configurable.
    /// </summary>
    internal class ModConfigurationConsts
    {
        /// <summary>
        /// This is used to indicate the base communication version.
        /// </summary>
        /// <remarks>
        /// If we change Message classes or add a new Message class in any way, we need to update this number.
        /// This is because of potential conflict in communications when we release a new version of the mod.
        /// ie., An established server will be running with version 1. We release a new version with different 
        /// communications classes. A Player will connect to the server, and will automatically download version 2.
        /// We would now have a Client running newer communication classes trying to talk to the Server with older classes.
        /// </remarks>
        internal const int ModCommunicationVersion = 20171231; // This will be based on the date of update.
        // TODO: this needs to be implmented

        /// <summary>
        /// The is the Id which this mod registers itself for sending and receiving messages through SE. 
        /// </summary>
        /// <remarks>
        /// This Id needs to be unique with SE and other mods, otherwise it can send/receive  
        /// messages to/from the other registered mod by mistake, and potentially cause SE to crash.
        /// This has been generated randomly.
        /// </remarks>
        internal const ushort ConnectionId = 13675;

        internal const string ClientLogFileName = "ShipScanClient.Log";

        internal const string ServerLogFileName = "ShipScanServer.Log";

        /// <summary>
        ///  Used for filenames. Shouldn't have unfriendly characters.
        /// </summary>
        internal const string ModName = "ShipScan";

        /// <summary>
        /// Used for display boxes and friendly information.
        /// </summary>
        internal const string ModTitle = "Ship Scan";

        /// <summary>
        /// Hardcoded list of SteamIDs, for testing stuff that hasn't been released to the public yet.
        /// It shouldn't be used to hide functionality away in the published mod, simply prevent
        /// incomplete or broken stuff from been used until it is ready.
        /// </summary>
        internal static ulong[] ExperimentalCreatorList = { 76561197961224864UL, 76561198048142826UL };
    }

}
