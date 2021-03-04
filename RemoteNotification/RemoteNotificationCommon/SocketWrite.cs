using System;
using System.Net.Sockets;

namespace RemoteNotificationCommon
{
    public class SocketWrite: SocketAsyncEventArgs
    {
        public SocketRect ReacSocket = null;
        public SocketWrite(SocketRect sk)
        {
            ReacSocket = sk;
            this.UserToken = null;
        }

    }
}
