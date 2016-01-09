﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Mud.Logger;

namespace Mud.Network.Socket
{
    // TODO: handling exception
    // TODO: forbidding start/stop/... depending on server state

    internal enum ServerStatus
    {
        Creating,
        Created,
        Initializing,
        Initialized,
        Starting,
        Started,
        Stopping,
        Stopped
    }

    // TODO: problem with simple telnet terminal without handshake
    public class SocketServer : INetworkServer, IDisposable
    {
        private static readonly Regex AsciiRegEx = new Regex(@"[^\u0020-\u007E]", RegexOptions.Compiled); // match ascii-only char

        private System.Net.Sockets.Socket _serverSocket;
        private ManualResetEvent _listenEvent;
        private Task _listenTask;
        private CancellationTokenSource _cancellationTokenSource;

        private ServerStatus _status;
        private readonly List<ClientSocketStateObject> _clients;

        public int Port { get; private set; }

        public SocketServer(int port)
        {
            _status = ServerStatus.Creating;
            Log.Default.WriteLine(LogLevels.Info, "Socket server creating");

            Port = port;

            _clients = new List<ClientSocketStateObject>();

            Log.Default.WriteLine(LogLevels.Info, "Socket server created");
            _status = ServerStatus.Created;
        }

        #region INetworkServer

        public event NewClientConnectedEventHandler NewClientConnected;
        public event ClientDisconnectedEventHandler ClientDisconnected;

        public void Initialize()
        {
            Log.Default.WriteLine(LogLevels.Info, "Socket server initializing");
            _status = ServerStatus.Initializing;

            // Establish the local endpoint for the socket.
            string hostName = Dns.GetHostName();
            IPHostEntry ipHostInfo = Dns.GetHostEntry(hostName);
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP socket.
            _serverSocket = new System.Net.Sockets.Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Listen task event
            _listenEvent = new ManualResetEvent(false);

            // Bind the socket to the local endpoint and listen for incoming connections.
            _serverSocket.Bind(localEndPoint);
            Log.Default.WriteLine(LogLevels.Info, "Socket server bound to " + hostName + " on port " + localEndPoint.Port);

            _status = ServerStatus.Initialized;
            Log.Default.WriteLine(LogLevels.Info, "Socket server initialized");
        }

        public void Start()
        {
            Log.Default.WriteLine(LogLevels.Info, "Socket server starting");

            _status = ServerStatus.Starting;

            _serverSocket.Listen(100);
            _cancellationTokenSource = new CancellationTokenSource();
            _listenTask = Task.Factory.StartNew(ListenTask, _cancellationTokenSource.Token);

            _status = ServerStatus.Started;

            Log.Default.WriteLine(LogLevels.Info, "Socket server started");
        }

        public void Stop()
        {
            Log.Default.WriteLine(LogLevels.Info, "Socket server stopping");

            _status = ServerStatus.Stopping;

            Broadcast("STOPPING SERVER NOW!!!");

            try
            {
                // Close clients socket
                List<ClientSocketStateObject> copy = _clients.Select(x => x).ToList(); // make a copy, because CloseConnection modify collection
                foreach (ClientSocketStateObject client in copy)
                    CloseConnection(client);
                _clients.Clear();
                
                // Stop listen task
                _cancellationTokenSource.Cancel();
                _listenEvent.Set(); // wakeup thread
                _listenTask.Wait(2000);

                // Close server socket
                //_serverSocket.Shutdown(SocketShutdown.Both); // TODO: exception on this line when closing application
                _serverSocket.Close();
            }
            catch (OperationCanceledException ex)
            {
                Log.Default.WriteLine(LogLevels.Warning, "Operation canceled exception while stopping. Exception: {0}", ex);
            }
            catch (AggregateException ex)
            {
                Log.Default.WriteLine(LogLevels.Warning, "Aggregate exception while stopping. Exception: {0}", ex.Flatten());
            }

            Log.Default.WriteLine(LogLevels.Info, "Socket server stopped");

            _status = ServerStatus.Stopped;
        }

        public void Broadcast(string data)
        {
            foreach(ClientSocketStateObject client in _clients)
                SendData(client, data);
        }

        #endregion

        private void ListenTask()
        {
            try
            {
                while (true)
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                    {
                        Log.Default.WriteLine(LogLevels.Info, "Stop ListenTask requested");
                        break;
                    }

                    // Set the event to nonsignaled state.
                    _listenEvent.Reset();

                    Log.Default.WriteLine(LogLevels.Debug, "Waiting for connection");

                    // Start an asynchronous socket to listen for connections.
                    _serverSocket.BeginAccept(AcceptCallback, _serverSocket);

                    // Wait until a connection is made before continuing.
                    _listenEvent.WaitOne();
                }
            }
            catch (TaskCanceledException ex)
            {
                Log.Default.WriteLine(LogLevels.Error, "ListenTask exception. Exception: {0}", ex);
            }

            Log.Default.WriteLine(LogLevels.Info, "ListenTask stopped");
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                // Signal the ListenTask to continue.
                _listenEvent.Set();

                // Get the socket that handles the client request.
                System.Net.Sockets.Socket listener = (System.Net.Sockets.Socket) ar.AsyncState;
                System.Net.Sockets.Socket clientSocket = listener.EndAccept(ar);

                Log.Default.WriteLine(LogLevels.Debug, "Client connected from " + ((IPEndPoint) clientSocket.RemoteEndPoint).Address);

                // Create the state object.
                ClientSocketStateObject client = new ClientSocketStateObject(this)
                {
                    ClientSocket = clientSocket,
                };
                _clients.Add(client);
                // TODO: NewClientConnected will be called once protocol handshake is done
                //
                client.State = ClientStates.Connected;
                if (NewClientConnected != null)
                    NewClientConnected(client);
                //
                clientSocket.BeginReceive(client.Buffer, 0, ClientSocketStateObject.BufferSize, 0, ReadCallback, client);
            }
            catch (ObjectDisposedException)
            {
                // If server status is stopping/stopped: ok
                // else, throw
                if (_status != ServerStatus.Stopping && _status != ServerStatus.Stopped)
                    throw;
            }
        }

        private void ReadCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the handler socket
                // from the asynchronous state object.
                ClientSocketStateObject client = (ClientSocketStateObject) ar.AsyncState;
                System.Net.Sockets.Socket clientSocket = client.ClientSocket;

                // Read data from the client socket. 
                int bytesRead = clientSocket.EndReceive(ar);

                Log.Default.WriteLine(LogLevels.Debug, "Received " + bytesRead + " bytes from client at " + ((IPEndPoint) clientSocket.RemoteEndPoint).Address);

                if (bytesRead == 0)
                {
                    // Something goes wrong, close connection
                    CloseConnection(client);
                }
                else
                {
                    //    // Putty handshake is 
                    //    //FF FB 1F window size
                    //    //FF FB 20 terminal speed
                    //    //FF FB 18 terminal type
                    //    //FF FB 27 Telnet Environment Option
                    //    //FF FD 01 echo
                    //    //FF FB 03 suppress go ahead
                    //    //FF FD 03 suppress go ahead

                    //    //FF FE 01 received after echo on

                    //Log.Default.WriteLine(LogLevels.Debug, "Data received from client at " + ((IPEndPoint)clientSocket.RemoteEndPoint).Address + " : " + ByteArrayToString(client.Buffer, bytesRead));

                    // Remove protocol character
                    if (bytesRead % 3 == 0 && client.Buffer[0] == 0xFF)// && client.Buffer[1] == 0xFB)
                    {
                        Log.Default.WriteLine(LogLevels.Info, "Protocol received from client at " + ((IPEndPoint)clientSocket.RemoteEndPoint).Address + " : " + ByteArrayToString(client.Buffer, bytesRead));
                    }
                    else if (client.State == ClientStates.Connected)
                    {
                        string dataReceived = Encoding.ASCII.GetString(client.Buffer, 0, bytesRead);

                        Log.Default.WriteLine(LogLevels.Debug, "Data received from client at " + ((IPEndPoint) clientSocket.RemoteEndPoint).Address + " : " + dataReceived);

                        // If data ends with CRLF, remove CRLF and consider command as complete
                        bool commandComplete = false;
                        if (dataReceived.EndsWith("\n") || dataReceived.EndsWith("\r"))
                        {
                            commandComplete = true;
                            dataReceived = dataReceived.TrimEnd('\r', '\n');
                        }

                        // Remove non-ascii char
                        dataReceived = AsciiRegEx.Replace(dataReceived, String.Empty);

                        // Append data in command
                        client.Command.Append(dataReceived);

                        // Command is complete, send it to command processor and start a new one
                        if (commandComplete)
                        {
                            // Get command
                            string command = client.Command.ToString();
                            Log.Default.WriteLine(LogLevels.Info, "Command received from client at " + ((IPEndPoint) clientSocket.RemoteEndPoint).Address + " : " + command);

                            // Reset command
                            client.Command.Clear();

                            // Process command
                            client.OnDataReceived(command);
                        }
                    }
                    // TODO: other states ?

                    // Continue reading
                    clientSocket.BeginReceive(client.Buffer, 0, ClientSocketStateObject.BufferSize, 0, ReadCallback, client);
                }
            }
            catch (ObjectDisposedException)
            {
                // If server status is stopping/stopped: ok
                // else, throw
                if (_status != ServerStatus.Stopping && _status != ServerStatus.Stopped)
                    throw;
            }
        }

        internal void SendData(ClientSocketStateObject client, string data)
        {
            try
            {
                System.Net.Sockets.Socket clientSocket = client.ClientSocket;

                Log.Default.WriteLine(LogLevels.Debug, "Send data to client at " + ((IPEndPoint)clientSocket.RemoteEndPoint).Address + " : " + data);

                // Colorize or strip color
                string colorizedData = client.ColorAccepted 
                    ? AnsiHelpers.Colorize(data) 
                    : AnsiHelpers.StripColor(data);

                // Convert the string data to byte data using ASCII encoding.
                byte[] byteData = Encoding.ASCII.GetBytes(colorizedData);

                // Begin sending the data to the remote device.
                clientSocket.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, client);
            }
            catch (ObjectDisposedException)
            {
                // If server status is stopping/stopped: ok
                // else, throw
                if (_status != ServerStatus.Stopping && _status != ServerStatus.Stopped)
                    throw;
            }
        }

        internal void SendData(ClientSocketStateObject client, byte[] byteData)
        {
            try
            {
                System.Net.Sockets.Socket clientSocket = client.ClientSocket;

                Log.Default.WriteLine(LogLevels.Debug, "Send data to client at " + ((IPEndPoint)clientSocket.RemoteEndPoint).Address + " : " + ByteArrayToString(byteData, byteData.Length));

                // Begin sending the data to the remote device.
                clientSocket.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, client);
            }
            catch (ObjectDisposedException)
            {
                // If server status is stopping/stopped: ok
                // else, throw
                if (_status != ServerStatus.Stopping && _status != ServerStatus.Stopped)
                    throw;
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                ClientSocketStateObject client = (ClientSocketStateObject)ar.AsyncState;
                System.Net.Sockets.Socket clientSocket = client.ClientSocket;

                // Complete sending the data to the remote device.
                int bytesSent = clientSocket.EndSend(ar);

                Log.Default.WriteLine(LogLevels.Debug, "Sent " + bytesSent + " bytes to client at " + ((IPEndPoint) clientSocket.RemoteEndPoint).Address);
            }
            catch (ObjectDisposedException)
            {
                // If server status is stopping/stopped: ok
                // else, throw
                if (_status != ServerStatus.Stopping && _status != ServerStatus.Stopped)
                    throw;
            }
        }

        internal void CloseConnection(ClientSocketStateObject client)
        {
            // Remove from 'client' collection
            _clients.Remove(client);

            //
            System.Net.Sockets.Socket clientSocket = client.ClientSocket;

            Log.Default.WriteLine(LogLevels.Info, "Client at " + ((IPEndPoint)clientSocket.RemoteEndPoint).Address + " has disconnected");
            
            // Close socket
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
            if (ClientDisconnected != null)
                ClientDisconnected(client);
        }

        private static string ByteArrayToString(byte[] ba, int length)
        {
            StringBuilder hex = new StringBuilder(length * 3);
            for(int i = 0; i < length; i++)
                hex.AppendFormat("{0:X2} ", ba[i]);
            return hex.ToString();
        }

        #region IDisposable

        public void Dispose()
        {
            _listenTask = null;

            _listenEvent.Dispose();
            _listenEvent.Close();
            _listenEvent = null;

            _serverSocket.Shutdown(SocketShutdown.Both);
            _serverSocket.Close();
            _serverSocket = null;
        }

        #endregion
    }
}
