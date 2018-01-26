namespace midspace.shipscan
{
    using ProtoBuf;
    using Sandbox.ModAPI;

    /// <summary>
    /// This will allow the player to set which mass types to ignore during scans.
    /// </summary>
    [ProtoContract]
    public class MessageSetIgnore : ModMessageBase
    {
        #region properties

        /// <summary>
        /// The minimum scan range. Items detected inside of this range will be ignored
        /// </summary>
        [ProtoMember(201)]
        public string Massclass;

        /// <summary>
        /// The type of scan type to carry out.
        /// </summary>
        [ProtoMember(202)]
        public bool Value;

        #endregion

        public static void SendMessage(string massclass, bool value)
        {
            ConnectionHelper.SendMessageToServer(new MessageSetIgnore { Massclass = massclass, Value = value });
        }

        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
            ScanSettingsEntity scanSettings = ScanDataManager.GetPlayerScanData(SenderSteamId);

            if (Massclass == MassCategory.Junk.ToString())
                scanSettings.IgnoreJunk = Value;
            if (Massclass == MassCategory.Tiny.ToString())
                scanSettings.IgnoreTiny = Value;
            if (Massclass == MassCategory.Small.ToString())
                scanSettings.IgnoreSmall = Value;
            if (Massclass == MassCategory.Large.ToString())
                scanSettings.IgnoreLarge = Value;
            if (Massclass == MassCategory.Huge.ToString())
                scanSettings.IgnoreHuge = Value;
            if (Massclass == MassCategory.Enormous.ToString())
                scanSettings.IgnoreEnormous = Value;
            if (Massclass == MassCategory.Ridiculous.ToString())
                scanSettings.IgnoreRidiculous = Value;

            MyAPIGateway.Utilities.SendMessage(SenderSteamId, $"Ignore {Massclass}", Value ? "On" : "Off");
        }
    }
}
