using System;
using RSJWYFamework.Runtime;
using UnityEngine;

namespace Script.Test
{
    public class TestTCPServer: MonoBehaviour
    {
        private void Start()
        {
            ModuleManager.GetModule<TcpClientManager>().Bind("127.0.0.1", 2000,null);
        }
    }
}