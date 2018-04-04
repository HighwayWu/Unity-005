using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using UnityNetwork;

namespace ChatServer
{
    class Program
    {
        static void Main(string[] args)
        {
            ChatServer server = new ChatServer();
            // 启动服务器 绑定到本地ip地址 使用端口10001
            server.StartServer("127.0.0.1", 10001);
        }

        public class ChatServer : NetworkManager
        {
            // 保存客户端连接
            List<Socket> peerList;
            // 服务器
            TCPPeer server;

            public ChatServer()
            {
                peerList = new List<Socket>();
            }

            // 启动服务器
            public void StartServer(string ip, int port)
            {
                AddHandler("chat", OnChat);
                server = new TCPPeer(this);
                server.Listen(ip, port);
                // 启动另一个线程处理消息队列
                this.StartThreadUpdate();
                Console.WriteLine("启动聊天服务器");
            }

            // 处理服务器接受客户端的连接
            public override void OnAccepted(NetPacket packet)
            {
                Console.WriteLine("接受新的连接");
                peerList.Add(packet.socket);
            }

            // 处理丢失连接
            public override void OnLost(NetPacket packet)
            {
                Console.WriteLine("丢失连接");
                peerList.Remove(packet.socket);
            }

            // 处理聊天消息
            public void OnChat(NetPacket packet)
            {
                // 在服务器上显示聊天内容
                Chat.ChatProto proto = packet.ReadObject<Chat.ChatProto>();
                if (proto != null)
                    Console.WriteLine(proto.userName + ":" + proto.chatMsg);

                packet.BeginWrite("chat");
                packet.WriteObject<Chat.ChatProto>(proto);
                packet.EncodeHeader();

                // 将消息转发给所有客户端
                foreach (Socket sk in peerList)
                {
                    server.Send(sk, packet);
                }
            }
        }
    }
}
