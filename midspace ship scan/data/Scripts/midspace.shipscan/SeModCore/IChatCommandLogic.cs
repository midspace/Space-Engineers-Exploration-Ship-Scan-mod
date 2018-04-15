//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace MidSpace.ShipScan
//{
//    // TODO: implement an interface for MainChatCommandLogic and this to implement it.
//    // I haven't gotten this working as expected. It's probably a waste of time, as the abstract class is doing what it should do.
//    public interface IChatCommandLogic
//    {
//        bool IsConnected { get; set; }
//        bool IsClientRegistered { get; }
//        bool IsServerRegistered { get; }

//        TextLogger ServerLogger { get; }
//        TextLogger ClientLogger { get; }

//        void CancelClientConnection();

//        List<ChatCommand> GetAllChatCommands();

//        void ClientLoad();
//        void ClientSave();
//        void ServerLoad();
//        void ServerSave();

//    }
//}
