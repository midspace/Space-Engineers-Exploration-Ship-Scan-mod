namespace midspace.shipscan
{
    using ProtoBuf;

    [ProtoContract]
    public class MessageConnectionRequest : ModMessageBase
    {
        [ProtoMember(201)]
        public int ModCommunicationVersion { get; set; }

        [ProtoMember(202)]
        public long PrivateCommunicationKey { get; set; }

        public override void ProcessClient()
        {
            // never processed on client
        }

        public override void ProcessServer()
        {
            MainChatCommandLogic.Instance.ServerLogger.WriteVerbose("Player '{0}' connected", SenderDisplayName);

            if (ModCommunicationVersion != ModConfigurationConsts.ModCommunicationVersion)
            {
                // TODO: respond to the potentional communication conflict.
                // Could Client be older than Server?
                // It's possible, if the Client has trouble downloading from Steam Community which can happen on occasion.
            }

            //var account = MainChatCommandLogic.Instance.Data.Clients.FirstOrDefault(
            //    a => a.SteamId == SenderSteamId);

            //bool newAccount = false;
            //// Create the Bank Account when the player first joins.
            //if (account == null)
            //{
            //    MainChatCommandLogic.Instance.ServerLogger.WriteInfo("Creating new Bank Account for '{0}'", SenderDisplayName);
            //    account = AccountManager.CreateNewDefaultAccount(SenderSteamId, SenderDisplayName, SenderLanguage);
            //    MainChatCommandLogic.Instance.Data.Clients.Add(account);
            //    MainChatCommandLogic.Instance.Data.CreditBalance -= account.BankBalance;
            //    newAccount = true;
            //}
            //else
            //{
            //    AccountManager.UpdateLastSeen(SenderSteamId, SenderDisplayName, SenderLanguage);
            //}

            // Is Server version older than what Client is running, or Server version is newer than Client.
            MessageConnectionResponse.SendMessage(SenderSteamId,
                ModCommunicationVersion < ModConfigurationConsts.ModCommunicationVersion,
                ModCommunicationVersion > ModConfigurationConsts.ModCommunicationVersion,
                new ClientConfig
                {
                    //ServerConfig = MainChatCommandLogic.Instance.ServerConfig,
                    //BankBalance = account.BankBalance,
                    //OpenedDate = account.OpenedDate,
                    //NewAccount = newAccount,
                    //ClientHudSettings = account.ClientHudSettings,
                    //Missions = MainChatCommandLogic.Instance.Data.Missions.Where(m => m.PlayerId == SenderSteamId).ToList()
                });
        }

        public static void SendMessage(int modCommunicationVersion, long privateCommunicationKey)
        {
            MainChatCommandLogic.Instance.ClientLogger.WriteInfo("Sending Connection Request");
            MainChatCommandLogic.Instance.ServerLogger.WriteInfo("Sending Connection Request");

            ConnectionHelper.SendMessageToServer(new MessageConnectionRequest
            {
                ModCommunicationVersion = modCommunicationVersion,
                PrivateCommunicationKey = privateCommunicationKey
            });
        }
    }
}
