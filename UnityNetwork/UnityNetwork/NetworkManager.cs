using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace UnityNetwork
{
    public class NetworkManager
    {
        // 一个独立线程，与网络线程分开
        System.Threading.Thread myThread;

        // 代理回调函数
        public delegate void OnReceive(NetPacket packet);

        // 每个消息对应一个OnReceive函数
        public Dictionary<string, OnReceive> handlers;

        // 数据的队列
        private Queue Packets = new System.Collections.Queue();

        public NetworkManager()
        {
            handlers = new Dictionary<string, OnReceive>();
            AddHandler("OnAccepted", OnAccepted);
            AddHandler("OnConnected", OnConnected);
            AddHandler("OnConnectFailed", OnConnectFailed);
            AddHandler("OnLost", OnLost);
        }

        // 注册消息
        public void AddHandler(string msgid, OnReceive handler)
        {
            handlers.Add(msgid, handler);
        }

        // 数据包入队
        public void AddPacket(NetPacket packet)
        {
            lock (Packets)
            {
                Packets.Enqueue(packet);
            }
        }

        // 数据包出队
        public NetPacket GetPacket()
        {
            lock (Packets)
            {
                if (Packets.Count == 0)
                    return null;
                return (NetPacket)Packets.Dequeue();
            }
        }

        // 另一个处理逻辑的线程
        public void StartThreadUpdate()
        {
            myThread = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadUpdate));
            myThread.Start();
        }

        // 逻辑线程
        protected void ThreadUpdate()
        {
            while (true)
            {
                // 节约cpu, 每次循环暂停30帧
                System.Threading.Thread.Sleep(30);
                Update();
            }
        }

        // 处理数据包，更新逻辑
        public void Update()
        {
            NetPacket packet = null;
            for (packet = GetPacket(); packet != null;)
            {
                string msg = "";
                // 获得消息标识符
                packet.BeginRead(out msg);
                OnReceive handler = null;
                if (handlers.TryGetValue(msg, out handler))
                {
                    // 根据消息标识符找到对应的OnReceive代理函数
                    if (handler != null)
                        handler(packet);
                }
                packet = null;
            }
        }

        public virtual void OnAccepted(NetPacket packet)
        {

        }

        public virtual void OnConnected(NetPacket packet)
        {

        }

        public virtual void OnConnectFailed(NetPacket packet)
        {

        }

        public virtual void OnLost(NetPacket packet)
        {

        }
    }
}