using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using RSJWYFamework.Runtime;
using UnityEngine;
using UnityEngine.UI;

public class ClientTest : MonoBehaviour,ILife
{
    public Button bindButton;
    public Button unBindButton;
    
    public InputField IPInputField;
    public InputField PortInputField;
    // Start is called before the first frame update
    
    public Guid ServerGUID;
    
    void Start()
    {
        bindButton.onClick.AddListener(Bind);
        unBindButton.onClick.AddListener(UnBind);
        ModuleManager.AddLife(this);
    }

    private void UnBind()
    {
        ModuleManager.GetModule<TcpClientManager>().UnBind(ServerGUID);
    }

    private void Bind()
    {
        ServerGUID = ModuleManager.GetModule<TcpClientManager>().Bind(IPInputField.text, int.Parse(PortInputField.text),null);
        ModuleManager.GetModule<EventManager>().BindEvent<TCPClientReceivesMsgFromServer>(OnReciveMsgFromServer);
    }

    private void OnReciveMsgFromServer(object sender, EventArgsBase e)
    {
        if (e is TCPClientReceivesMsgFromServer args)
        {
            AppLogger.Log($"From Server Len:{Encoding.UTF8.GetString(args.Data).Length}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public int Priority { get; }
    public void LifeUpdate()
    {
        
    }

    public void LifePerSecondUpdate()
    {
        if (ModuleManager.GetModule<TcpClientManager>().IsExistClient(ServerGUID))
        {
            byte[] _msgText=RandomDataGenerator.GenerateRandomTextByteArray(3,1*1024*1024*5);
            var _msg=new TCPClientToAllServerMsgEventArgs(Guid.Empty,_msgText);
            ModuleManager.GetModule<TcpClientManager>().ClientSendToAllServerMsg(null,_msg);
        }
    }

    public void LifePerSecondUpdateUnScaleTime()
    {
    }

    public void LifeFixedUpdate()
    {
    }

    public void LifeLateUpdate()
    {
    }
}
