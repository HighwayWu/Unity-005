using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace UnityNetwork
{
    public class TCPPeer
    {
        public bool isServe { get; set; }
        public Socket socket;
        NetworkManager networkMgr;

        public TCPPeer(NetworkManager netMgr)
        {
            networkMgr = netMgr;
        }

        // 添加内部消息
        private void AddInternalPacket(string msg, Socket sk)
        {
            // 通知丢失连接
            NetPacket p = new NetPacket();
            p.socket = sk;
            p.BeginWrite(msg);
            networkMgr.AddPacket(p);
        }

        // 作为服务器 开始监听
        public void Listen(string ip, int port, int backlog = 1000)
        {
            isServe = true;
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                // 将socket绑定到地址上
                socket.Bind(ipe);
                socket.Listen(backlog);
                // 异步接受连接
                socket.BeginAccept(new System.AsyncCallback(ListenCallback), socket);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        // 异步接受一个连接
        void ListenCallback(System.IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            try
            {
                // 获得客户端的socket
                Socket client = listener.EndAccept(ar);

                // 通知服务器接受一个新的连接
                AddInternalPacket("OnAccepted", client);

                // 接收数据的数据包
                NetPacket packet = new NetPacket();
                packet.socket = client;

                // 开始接收来自客户端的数据
                client.BeginReceive(packet.bytes, 0, NetPacket.headerLength, SocketFlags.None,
                    new System.AsyncCallback(ReceiveHeader), packet);
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);
            }

            // 继续接收其他连接
            listener.BeginAccept(new System.AsyncCallback(ListenCallback), listener);
        }

        // 作为客户端 开始连接服务器
        public void Connect(string ip, int port)
        {
            isServe = false;
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), port);
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.BeginConnect(ipe, new System.AsyncCallback(ConnectionCallback), socket);
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        // 异步连接回调
        void ConnectionCallback(System.IAsyncResult ar)
        {
            Socket client = (Socket)ar.AsyncState;
            try
            {
                // 与服务器取得连接
                client.EndConnect(ar);
                // 通知已经成功连接服务器
                AddInternalPacket("OnConnected", client);
                // 开始异步接收服务器信息
                NetPacket packet = new NetPacket();
                packet.socket = client;
                client.BeginReceive(packet.bytes, 0, NetPacket.headerLength,
                    SocketFlags.None, new System.AsyncCallback(ReceiveHeader), packet);
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);
                AddInternalPacket("OnConnectFailed", client);
            }
        }

        public void EndConnect()
        {
            if (socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }

        // 接收消息头
        void ReceiveHeader(System.IAsyncResult ar)
        {
            NetPacket packet = (NetPacket)ar.AsyncState;
            try
            {
                // 返回网络上接收的数据长度
                int read = packet.socket.EndReceive(ar);
                if (read < 1)
                {
                    // 丢失连接
                    AddInternalPacket("OnLost", packet.socket);
                    return;
                }

                packet.readLength += read;
                if (packet.readLength < NetPacket.headerLength)
                {
                    packet.socket.BeginReceive(packet.bytes,
                        packet.readLength,  // 存储偏移已读入的长度
                        NetPacket.headerLength - packet.readLength, // 这次只读入剩余的数据
                        SocketFlags.None,
                        new System.AsyncCallback(ReceiveHeader),
                        packet);
                }
                else
                {
                    // 消息长度
                    packet.DecodeHeader();
                    packet.readLength = 0;
                    // 开始读取消息
                    packet.socket.BeginReceive(packet.bytes,
                        NetPacket.headerLength,
                        packet.bodyLength,
                        SocketFlags.None,
                        new System.AsyncCallback(ReceiveBody),
                        packet);
                }
            }
            catch (System.Exception e)
            {
                Console.WriteLine("ReceiveHeader:" + e.Message);
            }
        }

        void ReceiveBody(System.IAsyncResult ar)
        {
            NetPacket packet = (NetPacket)ar.AsyncState;
            try
            {
                // 返回网络上接收的数据长度
                int read = packet.socket.EndReceive(ar);
                if (read < 1)
                {
                    Console.WriteLine("======172======");
                    // 丢失连接
                    AddInternalPacket("OnLost", packet.socket);
                    return;
                }
                packet.readLength += read;
                // 消息体必须读满指定的长度
                if (packet.readLength < packet.bodyLength)
                {
                    packet.socket.BeginReceive(packet.bytes,
                        NetPacket.headerLength + packet.readLength,
                        packet.bodyLength - packet.readLength,
                        SocketFlags.None,
                        new System.AsyncCallback(ReceiveBody),
                        packet);
                }
                else
                {
                    // 将消息传入逻辑处理队列
                    networkMgr.AddPacket(packet);

                    // 下一个读取
                    packet.Reset();
                    packet.socket.BeginReceive(packet.bytes, 0,
                        NetPacket.headerLength,
                        SocketFlags.None,
                        new System.AsyncCallback(ReceiveHeader),
                        packet);
                }

            }
            catch (System.Exception e)
            {
                Console.WriteLine("ReceiveBody: " + e.Message);
            }
        }

        // 向远程发送消息
        public void Send(Socket sk, NetPacket packet)
        {
            NetworkStream ns;
            lock (sk)
            {
                ns = new NetworkStream(sk);
                if (ns.CanWrite)
                {
                    try
                    {
                        ns.BeginWrite(packet.bytes, 0, packet.Length,
                            new System.AsyncCallback(SendCallback), ns);
                    }
                    catch (System.Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }

        // 发送回调
        private void SendCallback(System.IAsyncResult ar)
        {
            NetworkStream ns = (NetworkStream)ar.AsyncState;
            try
            {
                ns.EndWrite(ar);
                ns.Flush();
                ns.Close();
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}