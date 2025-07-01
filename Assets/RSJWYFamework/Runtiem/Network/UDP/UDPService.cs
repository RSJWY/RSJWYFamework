using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RSJWYFamework.Runtime
{
    internal class UDPService
    {
        /// <summary>
        /// 在UDPManager内的Token标记绑定
        /// </summary>
        public string UDPServiceToken;
        /// <summary>
        /// 监听端口
        /// </summary>
        private int _port;
        /// <summary>
        /// 监听IP
        /// </summary>
        IPAddress _ip ;
        /// <summary>
        /// UDP Socket
        /// </summary>
        System.Net.Sockets.Socket _udpClient;
        /// <summary>
        /// 消息发送队列
        /// </summary>
        ConcurrentQueue<UDPSendMsg> _SendMsgQueue = new ();
        
        /*
        /// <summary>
        /// 接收到的消息队列
        /// </summary>
        ConcurrentQueue<UDPReciveMsg> _ReciveMsgQueue = new ();*/
        /// <summary>
        /// 是否初始化过
        /// </summary>
        bool _isInit;
        /// <summary>
        /// 消息发送线程
        /// </summary>
        Thread _sendMsgThread;
        /// <summary>
        ///  消息队列发送锁
        /// </summary>
        object _msgSendThreadLock = new object();
        /// <summary>
        /// 通知多线程自己跳出
        /// </summary>
        private static CancellationTokenSource _cts;

        /// <summary>
        /// 写
        /// </summary>
        private SocketAsyncEventArgs _read;
        /// <summary>
        /// 读
        /// </summary>
        private SocketAsyncEventArgs _write;

        /// <summary>
        /// UDP接收数据的回调
        /// </summary>
        public Action<UDPReciveMsg> ReceiveMsgCallBack;
        /// <summary>
        /// 发送信息后的回调
        /// </summary>
        public Action<UDPSendCallBack> SendCallBack;

        internal UDPService(string _ip, int _port)
        {
            this._ip = IPAddress.Parse(_ip);
            this._port = _port;
        }

        internal UDPService(IPAddress _ipAddress, int _port)
        {
            _ip = _ipAddress;
            this._port = _port;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns>true成功监听了或者初始化成功过一次了</returns>
        internal bool Bind()
        {
            if (_isInit)
            {
                return true;
            }
            try
            {
                _udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                //支持广播消息
                _udpClient.EnableBroadcast = true;
                //配置监听
                IPEndPoint ipendpoint = new IPEndPoint(_ip, _port);
                _udpClient.Bind(ipendpoint);
                _cts = new CancellationTokenSource();
                //开启异步监听
                _sendMsgThread = new Thread(SendMsgThread);
                _sendMsgThread.IsBackground = true;//后台运行
                _sendMsgThread.Start();
                _isInit = true;
                AppLogger.Log($"UDP启动监听 {_udpClient.LocalEndPoint.ToString()} ");
                Start();
                return true;
            }
            catch (Exception e)
            {
                AppLogger.Error($"UDP启动监听 {_udpClient.LocalEndPoint.ToString()} 失败！！错误信息：\n {e.ToString()}");
                return false;
            }
        }
        /// <summary>
        /// 开启接收
        /// </summary>
        private void Start()
        {
            _read = new SocketAsyncEventArgs();
            _read.SetBuffer(new byte[1024*1024], 0, 1024*1024);
            _read.Completed += IO_Completed; 
            
            _write = new SocketAsyncEventArgs();
            _write.Completed += IO_Completed;
            
            //当为false时，需手动调用回调
            if (!_udpClient.ReceiveFromAsync(_read))
                Task.Run(() => ProcessReceived(_read));
        }
        /// <summary>
        /// 发送/接收共用回调
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // SocketAsyncEventArgs回调处理
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.ReceiveFrom:
                    Task.Run(() => ProcessReceived(e));
                    break;
                case SocketAsyncOperation.SendTo:
                    Task.Run(() => ProcessSend(e));
                    break;
                default:
                    AppLogger.Warning($"UDP IO_Completed 在套接字上完成的最后一个操作不是接收或发送，{e.LastOperation}");
                    break;
            }
        }
        /// <summary>
        /// UDP消息接收
        /// </summary>
        /// <param name="e"></param>
        private void ProcessReceived(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    byte[] data = new byte[e.BytesTransferred];
                    Buffer.BlockCopy(e.Buffer, 0, data, 0, e.BytesTransferred);
                    var msg = new UDPReciveMsg
                    {
                        UDPServiceToken = UDPServiceToken,
                        Success = true,
                        Bytes = data,
                        remoteEndPoint = e.RemoteEndPoint as IPEndPoint,
                        Error = string.Empty,
                    };
                    ReceiveMsgCallBack?.Invoke(msg);//交给执行回调
                }
                else
                {
                    AppLogger.Error($"UDP 接收时发生非成功时错误！: {e.SocketError}");
                    var msg = new UDPReciveMsg
                    {
                        UDPServiceToken = UDPServiceToken,
                        Success=false,
                        remoteEndPoint = e.RemoteEndPoint as IPEndPoint,
                        Error=e.SocketError.ToString()
                    };
                    ReceiveMsgCallBack?.Invoke(msg);
                }
            }
            catch (Exception exception)
            {
                AppLogger.Error($"UDP 接收时发生异常错误: {exception}");
                var msg = new UDPReciveMsg
                {
                    UDPServiceToken = UDPServiceToken,
                    Success=false,
                    remoteEndPoint = e.RemoteEndPoint as IPEndPoint,
                    Error=exception.ToString()
                };
                ReceiveMsgCallBack?.Invoke(msg);
            }
            finally
            {
                //无论是否异常，重设缓冲区，接收下一组数据
                e.SetBuffer(0,e.Buffer.Length);
                if (!_udpClient.ReceiveFromAsync(e))
                    Task.Run(() => ProcessReceived(e));
            }
        }
        
        
        /// <summary>
        /// 发送UDP消息
        /// </summary>
        /// <returns>仅仅提示是否进入了发送队列，消息是否发送成功由SendCallBack回调处理</returns>
        public bool SendUdpMessage(UDPSendMsg udpSendMsg)
        {
            if (_udpClient == null)
            {
                AppLogger.Warning("UDP服务未初始化或已关闭，无法发送消息。");
                return false;
            }
            // 发送数据
            _SendMsgQueue.Enqueue(udpSendMsg);
            return true;
        }
        /// <summary>
        /// 消息发送完成回调
        /// </summary>
        /// <param name="e"></param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError== SocketError.Success && e.BytesTransferred > 0)
                {
                    _SendMsgQueue.TryDequeue(out var _msg);
                    if (_msg.data.Length!= e.BytesTransferred)
                    {
                        AppLogger.Error( $"一个本不该发生的错误，ProcessSend UDP消息发送错误！！！已发送长度和消息本体长度不同");
                        SendCallBack?.Invoke(new UDPSendCallBack()
                        {
                            UDPServiceToken = UDPServiceToken,
                            Success=false,
                            Error="一个本不该发生的错误，ProcessSend UDP消息发送错误！！！已发送长度和消息本体长度不同",
                        });
                    }
                    SendCallBack?.Invoke(new UDPSendCallBack()
                    {
                        UDPServiceToken = UDPServiceToken,
                        Success=true,
                        Error=string.Empty,
                    });
                    //本条消息发送完成，激活线程
                    lock (_msgSendThreadLock)
                    {
                        Monitor.Pulse(_msgSendThreadLock);
                    }
                }
                else
                {
                    AppLogger.Warning($" ProcessSend UDP消息发送错误！！！SocketError：{e.SocketError}");
                }
            }
            catch (Exception ex)
            {

                AppLogger.Error($" ProcessSend UDP消息发送发生异常！：{ex}");
            }
        }
        /// <summary>
        /// 消息发送监控线程
        /// </summary>
        void SendMsgThread()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    //短暂休眠，避免CPU高占用
                    Thread.Sleep(100);
                    if (_SendMsgQueue.Count <= 0)
                    {
                        continue;
                    }
                    //取出数据
                    UDPSendMsg _data;
                    //取出不移除
                    _SendMsgQueue.TryPeek(out _data);
                    //绑定消息
                    _write.SetBuffer(_data.data, 0, _data.data.Length);
                    _write.RemoteEndPoint = _data.remoteEndPoint;
                    lock (_msgSendThreadLock)
                    {
                        //发送数据，并且让当前线程进入等待状态，等待当前数据发送完成后，再继续取出数据。
                        if (!_udpClient.SendToAsync(_write))
                            Task.Run(() => ProcessSend(_write));
                        Monitor.Wait(_msgSendThreadLock);
                    }
                }
                catch (Exception e)
                {
                    AppLogger.Error($"SendMsgThread 线程处理异常！！：{e}");
                    // 重启线程
                    Thread.Sleep(1000); // 等待一段时间再重启，避免立即重启可能导致的问题
                    if (_cts.IsCancellationRequested)
                    {
                        AppLogger.Warning($"请求取消任务");
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 关闭监听
        /// </summary>
        public void Close()
        {
            _cts?.Cancel();
            if (_udpClient != null)
            {
                _udpClient.Close();
            }

            _isInit = false;
        }

    }
}
