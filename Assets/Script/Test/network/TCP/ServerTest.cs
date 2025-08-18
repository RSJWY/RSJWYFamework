using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using RSJWYFamework.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ServerTest : MonoBehaviour,ILife
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
        ModuleManager.GetModule<TcpServerManager>().UnBind(ServerGUID);
    }

    private void Bind()
    {
        ServerGUID = ModuleManager.GetModule<TcpServerManager>().Bind(IPInputField.text, int.Parse(PortInputField.text),null);
        ModuleManager.GetModule<EventManager>().BindEvent<FromClientReceiveMsgEventArgs>(OnReciveMsgFromClient);
    }

    private void OnReciveMsgFromClient(object sender, EventArgsBase e)
    {
        if (e is FromClientReceiveMsgEventArgs args)
        {
            AppLogger.Log($"From Client Len:{args.MSGContainer.msgBytes.Length}");
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
        if (ModuleManager.GetModule<TcpServerManager>().IsExistServer(ServerGUID))
        {
            byte[] _msgText=RandomDataGenerator.GenerateRandomTextByteArray(3,1*1024*1024*5);
            var _msg=ServerToClientMsgEventArgs.CreateASTAC(_msgText);
            ModuleManager.GetModule<TcpServerManager>().SendMsgToClientEvent(null,_msg);
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
