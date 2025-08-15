using System;
using System.Collections;
using System.Collections.Generic;
using RSJWYFamework.Runtime;
using UnityEngine;

public class TestTCPClient : MonoBehaviour
{
    public Guid socketHandle;
    // Start is called before the first frame update
    private void OnEnable()
    {
        socketHandle = ModuleManager.GetModule<TcpClientManager>().Bind("127.0.0.1", 2000,null);
    }

    private void OnDisable()
    {
        ModuleManager.GetModule<TcpClientManager>().UnBind(socketHandle);
    }
}
