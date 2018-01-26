namespace midspace.shipscan
{
    using Sandbox.ModAPI;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class ScanDataManager
    {
        private const string WorldStorageDataFilename = "ScanSettings.xml";

        public static ScanServerEntity LoadData()
        {
            ScanServerEntity data;
            string xmlText;

            // new file name and location.
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage(WorldStorageDataFilename, typeof(ScanServerEntity)))
            {
                TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(WorldStorageDataFilename, typeof(ScanServerEntity));
                xmlText = reader.ReadToEnd();
                reader.Close();
            }
            else
            {
                data = InitData();
                ValidateAndUpdateData(data);
                return data;
            }

            if (string.IsNullOrWhiteSpace(xmlText))
            {
                data = InitData();
                ValidateAndUpdateData(data);
                return data;
            }

            try
            {
                data = MyAPIGateway.Utilities.SerializeFromXML<ScanServerEntity>(xmlText);
                MainChatCommandLogic.Instance.ServerLogger.WriteInfo("Loading existing ScanServerEntity.");
            }
            catch
            {
                // data failed to deserialize.
                MainChatCommandLogic.Instance.ServerLogger.WriteError("Failed to deserialize ScanServerEntity. Creating new ScanServerEntity.");
                data = InitData();
            }

            ValidateAndUpdateData(data);
            return data;
        }

        private static ScanServerEntity InitData()
        {
            MainChatCommandLogic.Instance.ServerLogger.WriteInfo("Creating new ScanServerEntity.");
            ScanServerEntity data = new ScanServerEntity();
            return data;
        }

        private static void ValidateAndUpdateData(ScanServerEntity data)
        {
            MainChatCommandLogic.Instance.ServerLogger.WriteInfo("Validating and Updating Data.");

            if (data.Clients == null)
                data.Clients = new List<ScanSettingsEntity>();

            // add validation and updates in here if ScanServerEntity changes.
        }

        public static void SaveData(ScanServerEntity data)
        {
            MainChatCommandLogic.Instance.ServerLogger.WriteInfo("Save Data Started");

            TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(WorldStorageDataFilename, typeof(ScanServerEntity));
            writer.Write(MyAPIGateway.Utilities.SerializeToXML<ScanServerEntity>(data));
            writer.Flush();
            writer.Close();

            MainChatCommandLogic.Instance.ServerLogger.WriteInfo("Save Data End");
        }

        public static ScanSettingsEntity GetPlayerScanData(ulong steamId)
        {
            ScanServerEntity scanServerData = ((MyShipScanModLogic)MainChatCommandLogic.Instance).ServerData;

            ScanSettingsEntity playerScanData = scanServerData.Clients.FirstOrDefault(e => e.SteamId == steamId);
            if (playerScanData == null)
            {
                playerScanData = new ScanSettingsEntity { SteamId = steamId };
                scanServerData.Clients.Add(playerScanData);
            }

            return playerScanData;
        }


    }
}
