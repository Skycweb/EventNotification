using System;
using System.Collections.Generic;
using System.Net.Sockets;
using RemoteNotificationCommon;

namespace RemoteNotificationCommon
{
    internal class SocketAsyncEventArgsPool<T> where T:SocketRect,new()
    {
        private readonly Stack<T> List = new Stack<T>();
        public SocketAsyncEventArgsPool(int numConnections)
        {
            NumConnections = numConnections;
        }

        public int NumConnections { get; }

        private void init() {
            
        }
        internal void Push(T readWriteEventArg)
        {
            readWriteEventArg.writeEventArgs.SetBuffer(null, 0, 0);
            if (readWriteEventArg.writeDataBuffer.Count > 0) {
                readWriteEventArg.writeDataBuffer.Clear();
            }
            this.List.Push(readWriteEventArg);
        }

        internal T Pop()
        {
            if (this.List.Count > 0)
            {
                return this.List.Pop();
            }
            else {
                return new T();
            }
        }
    }
}