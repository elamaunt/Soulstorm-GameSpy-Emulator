﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GameSpyEmulator
{
    public class TcpPortHandler
    {
        public interface ITcpSetting
        {
            Socket Create();
        }

        public class ChatTcpSetting : ITcpSetting
        {
            public Socket Create()
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    SendTimeout = 5000,
                    ReceiveTimeout = 5000,
                    SendBufferSize = 65535,
                    ReceiveBufferSize = 65535
                };

                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(false, 0));
                return socket;
            }
        }

        public class HttpTcpSetting : ITcpSetting
        {
            public Socket Create()
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    SendTimeout = 5000,
                    ReceiveTimeout = 5000,
                    SendBufferSize = 8192,
                    ReceiveBufferSize = 8192,
                    Blocking = false
                };

                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(false, 0));
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                return socket;
            }
        }

        public class LoginTcpSetting : ITcpSetting
        {
            public Socket Create()
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    SendTimeout = 5000,
                    ReceiveTimeout = 5000,
                    SendBufferSize = 8192,
                    ReceiveBufferSize = 8192,
                    Blocking = false
                };

                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(false, 0));
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                return socket;
            }
        }

        public class RetrieveTcpSetting : ITcpSetting
        {
            public Socket Create()
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    SendTimeout = 5000,
                    ReceiveTimeout = 5000,
                    SendBufferSize = 65535,
                    ReceiveBufferSize = 65535
                };

                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(false, 0));
                return socket;
            }
        }

        public class StatsTcpSetting : ITcpSetting
        {
            public Socket Create()
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    SendTimeout = 5000,
                    ReceiveTimeout = 5000,
                    SendBufferSize = 65535,
                    ReceiveBufferSize = 65535
                };

                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, true);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, new LingerOption(false, 0));
                return socket;
            }
        }

        static TcpPortHandler()
        {
            ServicePointManager.SetTcpKeepAlive(true, 60 * 1000 * 10, 1000);
        }

        readonly LinkedList<TcpClientNode> _clients = new LinkedList<TcpClientNode>();

        readonly WeakReference<TcpClientNode> _lastClient = new WeakReference<TcpClientNode>(null);

        public TcpClientNode LastClient
        {
            get
            {
                _lastClient.TryGetTarget(out TcpClientNode client);
                return client;
            }
        }

        Socket _listener;
        CancellationTokenSource _tokenSource;

        ExceptionHandler _exceptionHandlerDelegate;
        DataHandler _handlerDelegate;
        ZeroHandler _zeroHandlerDelegate;
        AcceptHandler _acceptDelegate;

        public delegate void ZeroHandler(TcpPortHandler handler, TcpClientNode node);
        public delegate void ExceptionHandler(Exception exception, bool send, int port);
        public delegate void AcceptHandler(TcpPortHandler handler, TcpClientNode node, CancellationToken token);
        public delegate void DataHandler(TcpPortHandler handler, TcpClientNode node, byte[] buffer, int count);

        readonly ITcpSetting _setting;
        readonly int _port;

        public TcpPortHandler(int port, ITcpSetting setting, DataHandler handlerDelegate, ExceptionHandler errorHandler = null, AcceptHandler acceptDelegate = null, ZeroHandler zeroHandler = null)
        {
            _setting = setting;
            _port = port;
            _zeroHandlerDelegate = zeroHandler;
            _exceptionHandlerDelegate = errorHandler;
            _handlerDelegate = handlerDelegate;
            _acceptDelegate = acceptDelegate;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Start()
        {
            _tokenSource = new CancellationTokenSource();

            var addresses = NetworkHelper.GetLocalIpAddresses();

            _listener = _setting.Create();
            _listener.Bind(new IPEndPoint(IPAddress.Loopback, _port));

            _listener.Listen(10);
            _listener.AcceptAsync().ContinueWith(OnAccept);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Stop()
        {
            _tokenSource?.Cancel();
            _tokenSource = null;

            _listener?.Close();

            var set = _clients.ToArray();
            _clients.Clear();

            for (int i = 0; i < set.Length; i++)
                set[i].Client.Dispose();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        void OnAccept(Task<Socket> task)
        {
            try
            {
                if (task.IsFaulted)
                    throw task.Exception.GetInnerException();

                var tokenSource = _tokenSource;
                var client = task.Result;

                if (tokenSource == null)
                {
                    client.Close();
                    client.Disconnect(true);
                    client.Dispose();
                    return;
                }

                var node = new TcpClientNode(client);

                _lastClient.SetTarget(node);
                _clients.AddLast(node);

                _acceptDelegate?.Invoke(this, node, tokenSource.Token);

                client.ReceiveAsync(new ArraySegment<byte>(node.Buffer, 0, node.Buffer.Length), SocketFlags.None).ContinueWith(t => OnReceive(node, t));
            }
            catch (OperationCanceledException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            catch (SocketException)
            {
            }
            catch (Exception ex)
            {
                _exceptionHandlerDelegate?.Invoke(ex, false, _port);
            }
            finally
            {
                try
                {
                    _listener?.AcceptAsync().ContinueWith(OnAccept);
                }
                catch(Exception ex)
                {

                }
            }
        }

        public bool SendUtf8(string message)
        {
            return Send(LastClient, message.ToUTF8Bytes());
        }

        public bool SendAskii(string message)
        {
            return Send(LastClient, message.ToAsciiBytes());
        }

        public bool Send(byte[] bytes)
        {
            return Send(LastClient, bytes);
        }

        public bool SendUtf8(TcpClientNode node, string message)
        {
            return Send(node, message.ToUTF8Bytes());
        }

        public bool SendAskii(TcpClientNode node, string message)
        {
            return Send(node, message.ToAsciiBytes());
        }

        public bool Send(TcpClientNode node, byte[] bytes)
        {
            if (node == null)
                return false;

            try
            {
                node.Client.Send(bytes, 0, bytes.Length, SocketFlags.None, out SocketError error);

                return true;
            }
            catch (OperationCanceledException ex)
            {
                KillClient(node);
            }
            catch (InvalidOperationException ex)
            {
                KillClient(node);
                _exceptionHandlerDelegate?.Invoke(ex, true, _port);
            }
            catch (SocketException ex)
            {
                KillClient(node);
            }
            catch (Exception ex)
            {
                KillClient(node);
                _exceptionHandlerDelegate?.Invoke(ex, true, _port);
            }

            return false;
        }

        void OnReceive(TcpClientNode node, Task<int> task)
        {
            try
            {
                if (task.IsCanceled)
                    return;

                if (task.IsFaulted)
                    throw task.Exception.GetInnerException();

                var count = task.Result;

                if (count == 0)
                    _zeroHandlerDelegate?.Invoke(this, node);
                else
                    _handlerDelegate(this, node, node.Buffer, count);
            }
            catch (OperationCanceledException)
            {
                KillClient(node);
            }
            catch (InvalidOperationException)
            {
                KillClient(node);
            }
            catch (SocketException)
            {
                KillClient(node);
            }
            catch (Exception ex)
            {
                KillClient(node);
                _exceptionHandlerDelegate?.Invoke(ex, false, _port);
            }
            finally
            {
                try
                {
                    if (node.Client.Connected)
                    {
                        var source = _tokenSource;

                        if (source != null)
                            node.Client.ReceiveAsync(new ArraySegment<byte>(node.Buffer, 0, node.Buffer.Length), SocketFlags.None).ContinueWith(t => OnReceive(node, t));
                    }
                }
                catch (OperationCanceledException ex)
                {
                    KillClient(node);
                }
                catch (InvalidOperationException ex)
                {
                    KillClient(node);
                }
                catch (SocketException ex)
                {
                    KillClient(node);
                }
                catch (Exception ex)
                {
                    KillClient(node);
                    _exceptionHandlerDelegate?.Invoke(ex, false, _port);
                }
            }
        }

        public void KillClient(TcpClientNode node)
        {
            if (_clients.Remove(node))
            {
                try
                {
                    node.Client.Shutdown(SocketShutdown.Both);
                    node.Client.Close(2000);
                    node.Client.Dispose();
                }
                catch (Exception ex)
                {

                }
            }
        }

        public class TcpClientNode
        {
            public readonly Socket Client;
            public readonly byte[] Buffer = new byte[65536];

            public IPEndPoint RemoteEndPoint => (IPEndPoint)Client?.RemoteEndPoint;

            public TcpClientNode(Socket client)
            {
                Client = client;
            }
        }
    }
}
