using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityNetwork;

public class ChatManager : NetworkManager {

    TCPPeer client;

	// Use this for initialization
	public void Start () {
        // 连接服务器
        client = new TCPPeer(this);
        client.Connect("127.0.0.1", 10001);
	}
	
    public void Send(NetPacket packet)
    {
        client.Send(client.socket, packet);
    }

    public void Exit()
    {
        client.EndConnect();
    }

    public override void OnLost(NetPacket packet)
    {
        Debug.Log("丢失与服务器的连接");
    }

    public override void OnConnected(NetPacket packet)
    {
        Debug.Log("成功连接服务器");
    }

    public override void OnConnectFailed(NetPacket packet)
    {
        Debug.Log("连接服务器失败，请退出");
    }
}
