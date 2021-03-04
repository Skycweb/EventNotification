using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using RemoteNotificationCommon;

namespace RemoteNotificationCommon
{
    // Implements the connection logic for the socket server.
    // After accepting a connection, all data read from the client
    // is sent back to the client. The read and echo back to the client pattern
    // is continued until the client disconnects.
    public class ServerTcp<T> where T : SocketRect,new()
    {
        private int m_numConnections;   // the maximum number of connections the sample is designed to handle simultaneously
        private int m_receiveBufferSize;// buffer size to use for each socket I/O operation
        BufferManager<T> m_bufferManager;  // represents a large reusable set of buffers for all socket operations
        const int opsToPreAlloc = 2;    // read, write (don't alloc buffer space for accepts)
        Socket listenSocket;            // the socket used to listen for incoming connection requests
                                        // pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations
        SocketAsyncEventArgsPool<T> m_readWritePool;
        //int m_totalBytesRead;           // counter of the total # bytes received by the server
        int m_numConnectedSockets;      // the total number of clients connected to the server
        Semaphore m_maxNumberAcceptedClients;

        // Create an uninitialized server instance.
        // To start the server listening for connection requests
        // call the Init method followed by Start method
        //
        // <param name="numConnections">the maximum number of connections the sample is designed to handle simultaneously</param>
        // <param name="receiveBufferSize">buffer size to use for each socket I/O operation</param>
        public ServerTcp(int numConnections, int receiveBufferSize)
        {
            //m_totalBytesRead = 0;
            m_numConnectedSockets = 0;
            m_numConnections = numConnections;
            m_receiveBufferSize = receiveBufferSize;
            // allocate buffers such that the maximum number of sockets can have one outstanding read and
            //write posted to the socket simultaneously
            m_bufferManager = new BufferManager<T>(receiveBufferSize * numConnections * opsToPreAlloc,
                receiveBufferSize);

            m_readWritePool = new SocketAsyncEventArgsPool<T>(numConnections);
            m_maxNumberAcceptedClients = new Semaphore(numConnections, numConnections);
        }

        // Initializes the server by preallocating reusable buffers and
        // context objects.  These objects do not need to be preallocated
        // or reused, but it is done this way to illustrate how the API can
        // easily be used to create reusable objects to increase server performance.
        //
        public void Init()
        {
            // Allocates one large byte buffer which all I/O operations use a piece of.  This gaurds
            // against memory fragmentation
            m_bufferManager.InitBuffer();

            // preallocate pool of SocketAsyncEventArgs objects
            T readWriteEventArg;

            for (int i = 0; i < m_numConnections; i++)
            {
                //Pre-allocate a set of reusable SocketAsyncEventArgs
                readWriteEventArg = new T();
                readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                readWriteEventArg.UserToken = null;
                readWriteEventArg.SendCallBack = (SocketWrite obj)=> {
                    this?.ProcessSend(obj);
                };
                readWriteEventArg.writeEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                // assign a byte buffer from the buffer pool to the SocketAsyncEventArg object
                m_bufferManager.SetBuffer(readWriteEventArg);
                // add SocketAsyncEventArg to the pool
                m_readWritePool.Push(readWriteEventArg);
                }
        }
        // Starts the server such that it is listening for
        // incoming connection requests.
        //
        // <param name="localEndPoint">The endpoint which the server will listening
        // for connection requests on</param>
        public void Start(IPEndPoint localEndPoint)
        {
            // create the socket which listens for incoming connections
            listenSocket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(localEndPoint);
            // start the server with a listen backlog of 100 connections
            listenSocket.Listen(1000);

            // post accepts on the listening socket
            StartAccept(null);

            //Console.WriteLine("{0} connected sockets with one outstanding receive posted to each....press any key", m_outstandingReadCount);
        }
        public void Connect(IPEndPoint remotePoint,Action<SocketRect,bool> callBack)
        {
            Socket socket = new Socket(remotePoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            T model = this.m_readWritePool.Pop();
            model.UserToken = socket;
            model.RemoteEndPoint = remotePoint;
            model.ConnectAction = callBack;
            socket.ConnectAsync(model);
        }
        #region 连接接入处理
        // Begins an operation to accept a connection request from the client
        //
        // <param name="acceptEventArg">The context object to use when issuing
        // the accept operation on the server's listening socket</param>
        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                // socket must be cleared since the context object is being reused
                acceptEventArg.AcceptSocket = null;
            }
            m_maxNumberAcceptedClients.WaitOne();
            bool willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        // This method is the callback method associated with Socket.AcceptAsync
        // operations and is invoked when an accept operation is complete
        //
        void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            Interlocked.Increment(ref m_numConnectedSockets);
            Console.WriteLine("Client connection accepted. There are {0} clientsTconnected to the server",
                m_numConnectedSockets);

            // Get the socket for the accepted client connection and put it into the
            //ReadEventArg object user token
            T readEventArgs = m_readWritePool.Pop();
            readEventArgs.UserToken = e.AcceptSocket;

            // As soon as the client is connected, post a receive to the connection
            bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(readEventArgs);
            if (!willRaiseEvent)
            {
                ProcessReceive(readEventArgs);
            }

            // Accept the next connection request
            StartAccept(e);
        }

        // This method is called whenever a receive or send operation is completed on a socket
        //
        // <param name="e">SocketAsyncEventArg associated with the completed receive operation</param>
        void IO_Completed(object sender, SocketAsyncEventArgs e1)
        {
            SocketRect e = e1 as SocketRect;
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e as T);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e1 as SocketWrite);
                    break;
                case SocketAsyncOperation.Accept:
                    break;
                case SocketAsyncOperation.Connect:
                    e.ConnectAction?.Invoke(e, true);
                    if (!((Socket)e.UserToken).ReceiveAsync(e)) {
                        ProcessReceive(e as T);
                    }
                    break;
                case SocketAsyncOperation.Disconnect:
                    e.ConnectAction?.Invoke(e, false);
                    break;
                case SocketAsyncOperation.None:
                    break;
                case SocketAsyncOperation.ReceiveFrom:
                    break;
                case SocketAsyncOperation.ReceiveMessageFrom:
                    break;
                case SocketAsyncOperation.SendPackets:
                    break;
                case SocketAsyncOperation.SendTo:
     
              break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }
        #endregion

#region 接收信息处理
        private void ProcessReceive(T e)
        {
            Socket token = (Socket)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                    //Interlocked.Add(ref m_totalBytesRead, e.BytesTransferred);
                    Console.WriteLine("收到了{0}字节", e.BytesTransferred);
                    e.BufferLoacl.apped(e.Buffer,e.Offset,e.BytesTransferred);
                    while (e.BufferLoacl.LenghtOfRead > 0 && e.BufferLoacl[0] < e.BufferLoacl.LenghtOfRead)
                    {
                        byte[] len = e.BufferLoacl.read(1);
                        byte[] data = e.BufferLoacl.read(len[0]);
                        e.ReactData(e, data);
                    }
                    if (!token.ReceiveAsync(e)) {
                        ProcessReceive(e);
                    }
            }
            else
            {
                CloseClientSocket(e);
            }
        }
        #endregion


        #region 发送信息处理
        // This method is invoked when an asynchronous send operation completes.
        // The method issues another receive on the socket to read any additional
        // data sent from the client
        //
        // <param name="e"></param>
        public void ProcessSend(SocketWrite e)
        {
            if (e.BytesTransferred != 0 && e.SocketError == SocketError.Success)
            {
                Console.WriteLine("成功发送了{0}字节数",e.BytesTransferred);
                if (e.BytesTransferred == e.Buffer.Length)
                {
                    e.SetBuffer(null, 0, 0);
                }
                else {
                    e.SetBuffer(e.BytesTransferred + e.Offset,e.Buffer.Length - e.BytesTransferred);
                }
                e?.ReacSocket?.SendData(null);
            }
            else
            {
                CloseClientSocket(e.ReacSocket as T);
            }
        }
        #endregion
        private void CloseClientSocket(T e)
        {
            Socket token = e.UserToken as Socket;

            // close the socket associated with the client
            try
            {
                token.Shutdown(SocketShutdown.Both);
            }
            // throws if client process has already closed
            catch (Exception) {

            }
            token.Dispose();
            // decrement the counter keeping track of the total number of clients connected to the server
            Interlocked.Decrement(ref m_numConnectedSockets);

            // Free the SocketAsyncEventArg so they can be reused by another client
            m_readWritePool.Push(e);

            m_maxNumberAcceptedClients.Release();
            Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", m_numConnectedSockets);
        }
    }
}
