using System;
using RSJWYFamework.Runtime;
using UnityEngine;

namespace Script.Test
{
    public class TestTCPServer: MonoBehaviour
    {
        public Guid socketHandle;
        private void OnEnable()
        {
            socketHandle= ModuleManager.GetModule<TcpServerManager>().Bind("127.0.0.1", 2000,null);
        }
        private void OnDisable()
        {
            ModuleManager.GetModule<TcpServerManager>().UnBind(socketHandle);
        }
    }
}