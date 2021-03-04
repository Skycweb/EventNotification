using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace RemoteNotificationCommon
{
    public class SocketRect: SocketAsyncEventArgs
    {
        public SocketWrite writeEventArgs;
        public int BufferIndex = 0;
        public int BufferSize = 0;
        public Stack<byte[]> writeDataBuffer = new Stack<byte[]>();
        public Action<SocketWrite> SendCallBack;
        public BytesBuffer BufferLoacl = new BytesBuffer(2048);
        public SocketRect()
        {
            writeEventArgs = new SocketWrite(this);
        }
        public void SendData(byte[] data) {
            Socket sk = this.UserToken as Socket;
            if (data != null)
            {
                writeDataBuffer.Push(data);
            }
            if ((writeEventArgs.Buffer == null || writeEventArgs.Offset == writeEventArgs.Buffer.Length - 1) && writeDataBuffer.Count > 0)
            {
                byte[] sendData = writeDataBuffer.Pop();
                writeEventArgs.SetBuffer(sendData, 0, sendData.Length);
                if (!sk.SendAsync(writeEventArgs)) {
                    this.SendCallBack?.Invoke(writeEventArgs);
                }
            }
            else if ((writeEventArgs.Buffer != null && writeEventArgs.Offset == writeEventArgs.Buffer.Length - 1))
            {
                if (!sk.SendAsync(writeEventArgs)) {
                    this.SendCallBack?.Invoke(writeEventArgs);
                }
            }
        }

        public void SendCmd(ActionEnum action, byte[] data) {
            if (data.Length + 2 > byte.MaxValue) {
                throw new Exception($"发送的数据不能大于{byte.MaxValue - 2}");
            }
            byte[] sendData = new byte[data.Length + 1+1];
            sendData[0] = (byte)(data.Length + 1);// 发送的数据不能大于255;
            sendData[1] = (byte)action;
            Array.ConstrainedCopy(data, 0, sendData, 2, data.Length);
            this.SendData(sendData);
        }
        public void SendCmd(ActionEnum actionName,object data) {
            byte[] SendData = Encoding.UTF8.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(data));
            //byte[] 
            this.SendCmd(actionName, SendData);
        }
        public void SendCmd(ActionEnum actionName, string data)
        {
            byte[] SendData = Encoding.UTF8.GetBytes(data);
            //byte[] 
            this.SendCmd(actionName, SendData);
        }
        public Action<SocketRect, byte[]> ReactData;
        public Action<SocketRect,bool> ConnectAction;
    }
}
