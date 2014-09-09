//    SuperFunkyChat - Example Binary Network Application
//    Copyright (C) 2014 James Forshaw
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <http://www.gnu.org/licenses/>.

using NDesk.Options;
using SuperFunkyChatProtocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace SuperFunkyChatServer
{
    class Program
    {
        static int _port = SuperFunkyChatProtocol.ChatConnection.DEFAULT_CHAT_PORT;
        static bool _global = false;
        static X509Certificate2 _serverCert;

        static ManualResetEvent _exitEvent = new ManualResetEvent(false);

        static readonly object _lock = new object();
        static List<ConnectionEntry> _clients = new List<ConnectionEntry>();
        static LockedQueue<DataPacketEntry> _packets = new LockedQueue<DataPacketEntry>();
        static Random _rand = new Random();

        class DataPacketEntry
        {
            public DataPacket Data { get; set; }
            public ConnectionEntry Connection { get; set; }
        }

        class ConnectionEntry
        {
            public string UserName { get; set; }
            public string HostName { get; set; }
            public TcpClient Client { get; private set; }
            public BinaryWriter Writer { get; private set; }
            public BinaryReader Reader { get; private set; }
            public bool Secure { get; private set; }
            public XorStream Stream { get; private set; }
            
            public ConnectionEntry(TcpClient client, Stream stm, bool secure)
            {
                Stream = new XorStream(stm);

                Client = client;

                Writer = new BinaryWriter(Stream);
                Reader = new BinaryReader(Stream);

                Secure = secure;
            }
        }

        static void HandleConnection(ConnectionEntry ent)
        {
            bool done = false;

            Console.WriteLine("Client connected from {0}", ent.Client.Client.RemoteEndPoint);

            lock (_lock)
            {
                _clients.Add(ent);
            }

            try
            {
                BinaryReader reader = ent.Reader;

                while (!done)
                {
                    DataPacket packet = DataPacket.ReadFrom(reader);

                    Console.WriteLine("Read packet of length {0}", packet.Data.Length);

                    if (packet.Data.Length > 0)
                    {
                        // Hidden command CTF
                        if (packet.Data[0] == 0x42)
                        {
                            Console.WriteLine("{0} got the protocol challenge", ent.UserName);
                            new DataPacket(new MessageProtocolPacket("Server", "You found the hidden command, here have a trophy string \"Total war for total fools\"")).WriteTo(ent.Writer);
                        }
                        else
                        {
                            ProtocolCommandId cmd = (ProtocolCommandId)packet.Data[0];

                            switch (cmd)
                            {
                                case ProtocolCommandId.Hello:
                                    HelloProtocolPacket hello = new HelloProtocolPacket(packet.Data);
                                    lock (_clients)
                                    {
                                        Console.WriteLine("Received a hello packet from {0}", hello.UserName);
                                        ent.UserName = hello.UserName;
                                        ent.HostName = hello.HostName;

                                        foreach (ConnectionEntry curr in _clients.ToArray())
                                        {
                                            if (curr != ent)
                                            {
                                                if(string.Equals(curr.UserName, hello.UserName, StringComparison.OrdinalIgnoreCase))
                                                {
                                                    Console.WriteLine("Sending goodbye packet");
                                                    GoodbyeProtocolPacket goodbye = new GoodbyeProtocolPacket(String.Format("Please choose a different username, '{0}' is already in use",
                                                        hello.UserName));

                                                    new DataPacket(goodbye.GetData()).WriteTo(ent.Writer);
                                                    done = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    if (!done)
                                    {
                                        if (hello.SupportsSecurityUpgrade)
                                        {
                                            byte[] randkey = new byte[1];
                                            _rand.NextBytes(randkey);

                                            Console.WriteLine("Upgrading to super secure mode (key {0})", randkey[0]);
                                            UpgradeSecurityProtocolPacket security = new UpgradeSecurityProtocolPacket(randkey[0]);

                                            new DataPacket(security.GetData()).WriteTo(ent.Writer);

                                            ent.Stream.XorKey = randkey[0];
                                        }

                                        _packets.Enqueue(new DataPacketEntry() { Data = packet, Connection = null });
                                    }

                                    break;
                                case ProtocolCommandId.SendUpdate:
                                    // Ignore update messages
                                    break;
                                case ProtocolCommandId.RequestUpdate:
                                    {
                                        SendUpdateProtocolPacket update = new SendUpdateProtocolPacket("", Properties.Resources.Updater, SHA256.Create().ComputeHash(Properties.Resources.Updater));
                                        new DataPacket(update).WriteTo(ent.Writer);
                                    }
                                    break;
                                case ProtocolCommandId.GetUserList:
                                    List<UserListProtocolPacket.UserListEntry> users = new List<UserListProtocolPacket.UserListEntry>();

                                    lock (_clients)
                                    {
                                        foreach (ConnectionEntry curr in _clients.ToArray())
                                        {
                                            if (curr.UserName != null)
                                            {
                                                users.Add(new UserListProtocolPacket.UserListEntry(curr.UserName, curr.HostName));
                                            }
                                        }

                                        users.Add(new UserListProtocolPacket.UserListEntry(String.Empty, "squiggle.com"));
                                    }

                                    new DataPacket(new UserListProtocolPacket(users.ToArray())).WriteTo(ent.Writer);
                                    break;
                                case ProtocolCommandId.Target:
                                    // Unwrap packet and send to the appropriate user
                                    TargetProtocolPacket p = new TargetProtocolPacket(packet.Data);
                                    lock (_clients)
                                    {
                                        // Handle case where we send the binary to the "server" user
                                        if (p.UserName == String.Empty)
                                        {
                                            SendUpdateProtocolPacket update = p.Packet as SendUpdateProtocolPacket;

                                            if (update != null)
                                            {
                                                // Check if a exe file (simple check, but enough for our purposes
                                                if ((update.Binary.Length > 2) && (update.Binary[0] == 'M') && (update.Binary[1] == 'Z'))
                                                {
                                                    if (NetworkUtils.VerifyHash(update.Binary, update.Hash) && !NetworkUtils.VerifyHash(Properties.Resources.Updater, update.Hash))
                                                    {
                                                        Console.WriteLine("{0} got the update challenge", ent.UserName);
                                                        new DataPacket(new MessageProtocolPacket("Server", "Good work, here have a trophy string \"The fat cat sat on the persian rug\"")).WriteTo(ent.Writer);
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine("{0} sent me an update but it had either an invalid hash or was the original", ent.UserName);
                                                    }
                                                }
                                                else
                                                {
                                                    Console.WriteLine("{0} tried to send me an update but failed", ent.UserName);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            foreach (ConnectionEntry curr in _clients.ToArray())
                                            {
                                                if (p.UserName.Equals(curr.UserName, StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    new DataPacket(p.Packet).WriteTo(curr.Writer);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    break;
                                default:
                                    _packets.Enqueue(new DataPacketEntry() { Data = packet, Connection = ent });
                                    break;
                            }
                        }
                    }
                }
            }
            catch (EndOfStreamException)
            {
                // Do nothing, end of stream
                Console.WriteLine("Closed: {0}", ent.Client.Client.RemoteEndPoint);
            }
            catch (IOException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidDataException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
            catch (OutOfMemoryException ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
            finally
            {
                lock (_lock)
                {
                    CloseEntry(ent);
                }
            }
        }

        static void AcceptCallback(IAsyncResult res)
        {
            TcpListener listener = (TcpListener)res.AsyncState;

            try
            {
                TcpClient client = null;
                try
                {
                    client = listener.EndAcceptTcpClient(res);
                }
                finally
                {
                    listener.BeginAcceptTcpClient(AcceptCallback, listener);
                }

                HandleConnection(new ConnectionEntry(client, client.GetStream(), false));
            }
            catch (SocketException)
            {
            }
            catch (IOException)
            {
            }
        }

        private static bool ValidateRemoteConnection(
                Object sender,
                X509Certificate certificate,
                X509Chain chain,
                SslPolicyErrors sslPolicyErrors
        )
        {
            // We always succeed
            return true;
        }

        static void AcceptCallbackSsl(IAsyncResult res)
        {
            TcpListener listener = (TcpListener)res.AsyncState;

            try
            {
                TcpClient client = null;
                try
                {
                    client = listener.EndAcceptTcpClient(res);
                }
                finally
                {
                    listener.BeginAcceptTcpClient(AcceptCallbackSsl, listener);
                }

                SslStream server = new SslStream(client.GetStream(), false, ValidateRemoteConnection);

                server.AuthenticateAsServer(_serverCert, false, System.Security.Authentication.SslProtocols.Default, false);

                HandleConnection(new ConnectionEntry(client, server, true));
            }
            catch (SocketException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            catch(AuthenticationException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            catch(InvalidOperationException ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        static void CloseEntry(ConnectionEntry ent)
        {            
            try
            {
                if (ent.Client != null)
                {
                    ent.Client.Close();
                }

                if (ent.Writer != null)
                {
                    ent.Writer.Dispose();
                }
            }
            catch
            {
            }

            _clients.Remove(ent); 
        }

        static void WriterThread()
        {
            while (true)
            {
                DataPacketEntry packet = _packets.Dequeue();

                lock (_lock)
                {
                    foreach (ConnectionEntry ent in _clients.ToArray())
                    {
                        // Don't send it back to the client which sent it
                        if (!Object.ReferenceEquals(ent, packet.Connection))
                        {
                            try
                            {
                                packet.Data.WriteTo(ent.Writer);
                            }
                            catch
                            {
                                CloseEntry(ent);
                            }
                        }
                    }
                }
            }
        }

        static int ParseNumber(string arg)
        {
            if (arg.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return int.Parse(arg.Substring(2), NumberStyles.HexNumber);
            }
            else
            {
                return int.Parse(arg);
            }
        }

        static bool ParseArgs(string[] args)
        {
            bool showhelp = false;

            OptionSet opts = new OptionSet()
            {
                { "g|global", "Bind to globally to all interfaces", v => _global = v != null },
                { "p|port=", String.Format("Specify listenting TCP port (def:{0})", _port), v => _port = ParseNumber(v) },
                { "cert=", "Specify a certificate to use for SSL", v => _serverCert = new X509Certificate2(File.ReadAllBytes(v)) },
                { "h|?|help", "Show help", v => showhelp = v != null },
            };

            try
            {
                opts.Parse(args);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                showhelp = true;
            }

            if (showhelp)
            {
                opts.WriteOptionDescriptions(Console.Out);
                return false;
            }
            else
            {
                return true;
            }
        }

        static TcpListener BindPort(int port, AsyncCallback callback)
        {
            TcpListener listener = new TcpListener(_global ? IPAddress.Any : IPAddress.Loopback, port);
            listener.Start();

            listener.BeginAcceptTcpClient(callback, listener);

            return listener;
        }

        static void Main(string[] args)
        {
            if (ParseArgs(args))
            {
                try
                {
                    Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
                    Console.WriteLine("Running server on port {0} Global Bind {1}", _port, _global);
                    List<TcpListener> listeners = new List<TcpListener>();

                    if (_serverCert != null)
                    {
                        Console.WriteLine("Binding SSL on port {0}", _port+1);
                    }

                    listeners.Add(BindPort(_port, AcceptCallback));

                    if (_serverCert != null)
                    {
                        listeners.Add(BindPort(_port+1, AcceptCallbackSsl));
                    }

                    Thread th = new Thread(WriterThread);
                    th.IsBackground = true;
                    th.Start();

                    _exitEvent.WaitOne(Timeout.Infinite);

                    foreach (TcpListener l in listeners)
                    {
                        try
                        {
                            l.Stop();
                        }
                        catch
                        { }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: {0}", ex.Message);
                }
            }

        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            _exitEvent.Set();
        }
    }
}
