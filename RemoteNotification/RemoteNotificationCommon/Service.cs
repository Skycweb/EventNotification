using System;
using System.Collections.Generic;

namespace RemoteNotificationCommon
{
    public class Service
    {
        private Dictionary<string, SocketClient> clients = new Dictionary<string, SocketClient>();
        private ServerTcp<SocketService> sk = new ServerTcp<SocketService>(10,1024);
        public Service()
        {
            sk.Init();
            sk.Start(new System.Net.IPEndPoint(0, 10008));
        }
    }
}
