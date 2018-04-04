using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityNetwork;
using UnityEngine.UI;

public class ChatClient : MonoBehaviour {

    ChatManager clientPeer;

    public string revString = "";
    public string inputName = System.DateTime.Now.Second.ToString();
    protected string inputString = "";
    private GameObject inputField;
    private Button btnSend;
    private Button btnExit;
    private Queue<string> queue;
    private string hisMsg = "";
    private Text hisMsgTextField;
    private GameObject inputMsg;
    private bool isConnect = false;


	// Use this for initialization
	void Start () {
        queue = new Queue<string>();
        inputField = GameObject.Find("InputField");
        inputMsg = GameObject.Find("InputMsg");
        btnSend = GameObject.Find("Send").GetComponent<Button>();
        btnExit = GameObject.Find("Exit").GetComponent<Button>();
        btnSend.enabled = false;
        hisMsgTextField = GameObject.Find("HisMsg").GetComponent<Text>();
        hisMsgTextField.text = hisMsg;
        
        clientPeer = new ChatManager();
        clientPeer.AddHandler("chat", OnChat);	
	}
	
	// Update is called once per frame
	void Update () {
        // Unity中处理逻辑使用的是单线程
        clientPeer.Update();
	}

    public void SendChat()
    {
        inputString = inputMsg.GetComponent<InputField>().text;
        if (inputString == "")
            return;
        // 聊天数据包
        Chat.ChatProto proto = new Chat.ChatProto();
        proto.userName = this.inputName;
        proto.chatMsg = inputString;
        NetPacket p = new NetPacket();
        p.BeginWrite("chat");
        p.WriteObject<Chat.ChatProto>(proto);
        p.EncodeHeader();
        clientPeer.Send(p);

        // 清空消息
        if (queue.Count > 14)
            queue.Dequeue();
        queue.Enqueue(proto.userName + ": " + inputString + "\n");
        Que2Str();
        hisMsgTextField.text = hisMsg;
        inputMsg.GetComponent<InputField>().text = "";
    }

    // 处理聊天消息
    public void OnChat(NetPacket packet)
    {
        Debug.Log("收到服务器消息");
        Chat.ChatProto proto = packet.ReadObject<Chat.ChatProto>();
        revString = proto.userName + ":" + proto.chatMsg;
        if (proto.userName != this.inputName)
        {
            if (queue.Count > 14)
                queue.Dequeue();
            queue.Enqueue(proto.userName + ": " + proto.chatMsg + "\n");
            Que2Str();
            hisMsgTextField.text = hisMsg;
        }
    }

    public void EndEdit()
    {
        inputField.GetComponent<InputField>().enabled = false;
        string tmp = inputField.GetComponent<InputField>().text;
        if (tmp != "")
            this.inputName = tmp;
        btnSend.enabled = true;
        clientPeer.Start();
        isConnect = true;
    }

    public void ExitGame()
    {
        if(isConnect)
            clientPeer.Exit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void Que2Str()
    {
        hisMsg = "";
        foreach(string s in queue)
        {
            hisMsg += s;
        }
    }
}
