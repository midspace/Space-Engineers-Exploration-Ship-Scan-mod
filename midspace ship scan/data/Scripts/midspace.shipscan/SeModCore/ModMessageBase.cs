namespace MidSpace.ShipScan.SeModCore
{
    using System;
    using Messages;
    using ProtoBuf;

    [ProtoContract]
    [ProtoInclude(50, typeof(MessageChatCommand))]
    [ProtoInclude(51, typeof(MessageClientDialogMessage))]
    [ProtoInclude(52, typeof(MessageClientNotification))]
    [ProtoInclude(53, typeof(MessageClientTextMessage))]
    [ProtoInclude(54, typeof(MessageConnectionRequest))]
    [ProtoInclude(55, typeof(MessageConnectionResponse))]
    [ProtoInclude(56, typeof(MessageClientSound))]
    [ProtoInclude(57, typeof(MessageClientConfig))]

    // This contains the ModCore Messages. Modder programmers should add their own Messages to the other Partial class.
    public abstract partial class ModMessageBase
    {
        #region properties

        /// <summary>
        /// The SteamId of the message's sender. Note that this will be set when the message is sent, so there is no need for setting it otherwise.
        /// </summary>
        [ProtoMember(1)]
        public ulong SenderSteamId;

        /// <summary>
        /// The display name of the message sender.
        /// </summary>
        [ProtoMember(2)]
        public string SenderDisplayName;

        /// <summary>
        /// The current display language of the sender.
        /// </summary>
        [ProtoMember(3)]
        public int SenderLanguage;

        /// <summary>
        /// Defines on which side the message should be processed. Note that this will be set when the message is sent, so there is no need for setting it otherwise.
        /// </summary>
        [ProtoMember(4)]
        public MessageSide Side;

        #endregion

        ///// <summary>
        ///// This will allow the serializer to automatically execute the Action in the same step as Deserialization,
        ///// and reduce the message handling in the ConnectionHelper.
        ///// Exception handling ConnectionHelper would have to be moved here too.
        ///// </summary>
        //[ProtoAfterDeserialization] // not yet whitelisted.
        //void AfterDeserialization() // is not invoked after deserialization from xml
        //{
        //    InvokeProcessing();
        //}

        public void InvokeProcessing()
        {
            switch (Side)
            {
                case MessageSide.ClientSide:
                    InvokeClientProcessing();
                    break;

                case MessageSide.ServerSide:
                    InvokeServerProcessing();
                    break;
            }
        }

        private void InvokeClientProcessing()
        {
            MainChatCommandLogic.Instance.ClientLogger.WriteVerbose("Received - {0}", this.GetType().Name);
            try
            {
                ProcessClient();
            }
            catch (Exception ex)
            {
                // TODO: send error to server and notify admins
                MainChatCommandLogic.Instance.ClientLogger.WriteException(ex, "Could not process message on Client.");
            }
        }

        private void InvokeServerProcessing()
        {
            MainChatCommandLogic.Instance.ServerLogger.WriteVerbose("Received - {0}", this.GetType().Name);
            try
            {
                ProcessServer();
            }
            catch (Exception ex)
            {
                MainChatCommandLogic.Instance.ServerLogger.WriteException(ex, "Could not process message on Server.");
            }
        }

        /// <summary>
        /// When the message is recieved on the Client side, it will invoke this method.
        /// </summary>
        public abstract void ProcessClient();

        /// <summary>
        /// When the message is recieved on the Server side, it will invoke this method.
        /// </summary>
        public abstract void ProcessServer();
    }
}
