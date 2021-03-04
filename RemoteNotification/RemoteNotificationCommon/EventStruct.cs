using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace RemoteNotificationCommon
{
    public enum ActionEnum: byte
    {
        Login,
        Exit,
        PostEvent,
        BindEvent,
        RemoveEvent
    }
}
