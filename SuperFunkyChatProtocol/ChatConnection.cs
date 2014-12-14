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

using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace SuperFunkyChatProtocol
{
    public class ChatConnection : IDisposable
    {
        public static int DEFAULT_CHAT_PORT = 12345;

        private BinaryReader _reader;
        private BinaryWriter _writer;
        private TcpClient _client;
        private XorStream _baseStream;

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

        private ProtocolPacket DoConnect(TcpClient client, string hostname, bool ssl, string username, bool supportsUpgrade)
        {
            Stream stm;

            _client = client;

            if (ssl)
            {
                SslStream sslStream = new SslStream(_client.GetStream(), false, ValidateRemoteConnection);

                int lastTimeout = sslStream.ReadTimeout;
                sslStream.ReadTimeout = 3000;

                sslStream.AuthenticateAsClient(hostname);

                sslStream.ReadTimeout = lastTimeout;

                stm = sslStream;
            }
            else
            {
                stm = _client.GetStream();
            }

            _baseStream = new XorStream(stm);
            _writer = new BinaryWriter(_baseStream);
            _reader = new BinaryReader(_baseStream);

            WritePacket(new HelloProtocolPacket(username, Environment.MachineName, supportsUpgrade, 0));

            ProtocolPacket packet = ReadPacket(3000);

            if (packet is GoodbyeProtocolPacket)
            {
                throw new EndOfStreamException(((GoodbyeProtocolPacket)packet).Message);
            }
            else
            {
                HelloProtocolPacket p = packet as HelloProtocolPacket;
                if (p != null)
                {
                    if (p.SupportsSecurityUpgrade)
                    {
                        UpgradeSecurity(p.XorKey);
                    }
                }

                return packet;
            }
        }

        public void UpgradeSecurity(byte xorkey)
        {
            _baseStream.XorKey = xorkey;
        }

        private static IPAddress GetHostIP(string hostname)
        {
            IPAddress hostaddr;

            if (IPAddress.TryParse(hostname, out hostaddr))
            {
                return hostaddr;
            }   

            IPHostEntry ent = Dns.GetHostEntry(hostname);

            foreach (IPAddress addr in ent.AddressList)
            {
                if (addr.AddressFamily == AddressFamily.InterNetwork)
                {
                    return addr;
                }
            }

            throw new ArgumentException("Cannot get a IPv4 address for host");
        }

        private TcpClient Connect(string hostname, int port)
        {
            IPAddress hostaddr = GetHostIP(hostname);

            return new TcpClient(hostaddr.ToString(), port);
        }

        private TcpClient ConnectThroughSocks(string hostname, int port, string proxyaddr, int proxyport)
        {
            bool connected = false;

            TcpClient client = new TcpClient(proxyaddr, proxyport);

            try
            {
                IPAddress hostaddr = GetHostIP(hostname);

                byte[] req = new byte[9];
                req[0] = 4;
                req[1] = 1;
                req[2] = (byte)(port >> 8);
                req[3] = (byte)(port & 0xFF);

                byte[] addrbytes = hostaddr.GetAddressBytes();

                req[4] = addrbytes[0];
                req[5] = addrbytes[1];
                req[6] = addrbytes[2];
                req[7] = addrbytes[3];
                Stream stm = client.GetStream();
                stm.Write(req, 0, req.Length);

                byte[] resp = new byte[8];

                for (int i = 0; i < 8; ++i)
                {
                    int b = stm.ReadByte();
                    if (b < 0)
                    {
                        throw new EndOfStreamException();
                    }

                    resp[i] = (byte)b;
                }

                if (resp[1] == 0x5A)
                {
                    connected = true;
                }
                else
                {
                    throw new EndOfStreamException("Failed to connect through SOCKS proxy");
                }
            }
            finally
            {
                if (!connected && (client != null))
                {
                    client.Close();
                }
            }

            return client;
        }

        public ProtocolPacket Connect(string hostname, int port, bool ssl, string username, bool supportsUpgrade)
        {
            return DoConnect(Connect(hostname, ssl ? port+1 : port), hostname, ssl, username, supportsUpgrade);            
        }

        public ProtocolPacket Connect(string hostname, int port, bool ssl, string proxyaddr, int proxyport, string username, bool supportsUpgrade)
        {
            return DoConnect(ConnectThroughSocks(hostname, ssl ? port+1 : port, proxyaddr, proxyport), hostname, ssl, username, supportsUpgrade);
        }

        protected void Dispose(bool dispose)
        {
            if (_client != null)
            {
                _client.Close();
                _client = null;
            }            
        }

        ~ChatConnection()
        {
            Dispose(false);
        }

        public void WritePacket(ProtocolPacket packet)
        {
            DataPacket p = new DataPacket(packet.GetData());
            p.WriteTo(_writer);
        }

        public ProtocolPacket ReadPacket()
        {
            return ProtocolPacket.FromData(DataPacket.ReadFrom(_reader).Data);
        }

        public ProtocolPacket ReadPacket(int timeout)
        {
            int lastTimeout = _baseStream.ReadTimeout;
            try
            {
                _baseStream.ReadTimeout = timeout;
                return ProtocolPacket.FromData(DataPacket.ReadFrom(_reader).Data);
            }
            finally
            {
                _baseStream.ReadTimeout = lastTimeout;
            }
        }

        public void SendMessage(string username, string message)
        {
            MessageProtocolPacket packet = new MessageProtocolPacket(username, message);

            WritePacket(packet);
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
