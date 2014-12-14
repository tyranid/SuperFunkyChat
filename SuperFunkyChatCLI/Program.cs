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
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading;
using NDesk.Options;
using SuperFunkyChatProtocol;

namespace SuperFunkyChatCLI
{
    class Program
    {
        static DnsEndPoint _endpoint;        
        static string _username;
        static DnsEndPoint _socksproxy;
        static bool _ssl;
        static bool _xor;
        static ChatConnection _conn;

        static DnsEndPoint ParseEndpoint(string endpoint)
        {
            int port = 0;
            string host = null;

            endpoint = endpoint.Trim();

            if (endpoint.Length == 0)
            {
                throw new ArgumentException("Invalid endpoint string");
            }

            int lastColon = endpoint.LastIndexOf(':');

            port = ParseInt(endpoint.Substring(lastColon + 1));
            host = endpoint.Substring(0, lastColon);

            host.Trim('[', ']');

            return new DnsEndPoint(host, port);
        }

        static int ParseInt(string s)
        {
            if(s.StartsWith("0x"))
            {
                return int.Parse(s, NumberStyles.HexNumber);
            }
            else
            {
                return int.Parse(s);
            }
        }        

        static bool ParseArgs(string[] args)
        {
            bool showhelp = false;

            OptionSet opts = new OptionSet()
            {                
                { "socks=", "Specify a socks proxy in host:port format", v => _socksproxy = ParseEndpoint(v) },
                { "ssl", "Use SSL for connection", v => _ssl = v != null },
                { "xor", "Indicates we support secure upgrade to XOR ;-)", v => _xor = v != null },
                { "u|username=", "Specify username, default is current name", v => _username = v },
                { "h|?|help", "Show help", v => showhelp = v != null },
            };

            try
            {
                List<string> res = opts.Parse(args);

                if (res.Count > 0)
                {
                    _endpoint = ParseEndpoint(res[0]);                    
                }
                else
                {
                    Console.WriteLine("ERROR: Must supply a hostname and port");
                    showhelp = true;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                showhelp = true;
            }

            if (_username == null)
            {
                _username = Environment.UserName;
            }

            if (showhelp)
            {
                Console.WriteLine("Usage: SuperFunkyChatCLI [options] host port");
                Console.WriteLine("Host can be a name, an IPv4 or IPv6 address");
                opts.WriteOptionDescriptions(Console.Out);
                return false;
            }
            else
            {
                return true;
            }
        }

        static void AddMessage(string username, string message)
        {
            Console.WriteLine("{0} : {1}", username, message);
        }

        static void AddImage(string message, byte[] imageData)
        {
            Console.WriteLine("User {0} sent you an image but you can't see it :-(");
        }

        static void SayGoodbye(string message)
        {
            AddMessage("Server", message);
        }

        static void ShowUserList(UserListProtocolPacket.UserListEntry[] users)
        {
            Console.WriteLine("User List");
            foreach (var entry in users)
            {
                Console.WriteLine("{0} - {1}", entry.UserName, entry.HostName);
            }
        }

        static void HandlePacket(ProtocolPacket packet)
        {
            if (packet is MessageProtocolPacket)
            {
                MessageProtocolPacket p = packet as MessageProtocolPacket;

                AddMessage(p.UserName, p.Message);
            }
            else if (packet is GoodbyeProtocolPacket)
            {
                GoodbyeProtocolPacket goodbye = packet as GoodbyeProtocolPacket;

                SayGoodbye(goodbye.Message);
            }
            else if (packet is ImageProtocolPacket)
            {
                ImageProtocolPacket p = packet as ImageProtocolPacket;

                AddImage(p.UserName, p.ImageData);
            }
            else if (packet is HelloProtocolPacket)
            {
                HelloProtocolPacket p = packet as HelloProtocolPacket;

                AddMessage(p.UserName, String.Format("Hey I just joined from {0}!!11!", p.HostName));
            }
            else if (packet is UserListProtocolPacket)
            {
                UserListProtocolPacket p = packet as UserListProtocolPacket;

                ShowUserList(p.UserList);
            }
            else if (packet is SendFileProtocolPacket)
            {
                Console.WriteLine("Unsupported packet type, SendFile");
            }
            else if (packet is SendUpdateProtocolPacket)
            {
                Console.WriteLine("Unsupported packet type, SendUpdate");
            }
        }

        static private void ProcessCommand(ChatConnection conn, string line)
        {
            string[] cmdargs = line.Split(' ');

            if (cmdargs.Length > 0)
            {
                switch (cmdargs[0].ToLower())
                {
                    case "/quit":
                        Environment.Exit(0);
                        break;
                    case "/list":
                        conn.WritePacket(new GetUserListProtocolPacket());
                        break;
                }
            }
            
        }

        static private void CommandLineThread(object o)
        {
            ChatConnection conn = (ChatConnection)o;
            string line;

            while ((line = Console.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.Length > 0)
                {
                    if (line[0] == '/')
                    {
                        ProcessCommand(conn, line);
                    }
                    else
                    {
                        conn.SendMessage(_username, line);
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("SuperFunkChat (c) 2014 James Forshaw");
            Console.WriteLine("WARNING: Don't use this for a real chat system!!!");

            if(ParseArgs(args))
            {
                try
                {
                    ProtocolPacket packet;

                    _conn = new ChatConnection();

                    if (_socksproxy == null)
                    {
                        packet = _conn.Connect(_endpoint.Host, _endpoint.Port, _ssl, _username, _xor);
                    }
                    else
                    {
                        packet = _conn.Connect(_endpoint.Host, _endpoint.Port, _ssl, 
                            _socksproxy.Host, _socksproxy.Port, _username, _xor);
                    }

                    try
                    {
                        HandlePacket(packet);

                        Thread thread = new Thread(CommandLineThread);
                        thread.IsBackground = true;
                        thread.Start(_conn);

                        while ((packet = _conn.ReadPacket()) != null)
                        {
                            HandlePacket(packet);
                        }
                    }
                    catch
                    {
                        HandlePacket(new GoodbyeProtocolPacket("Connection Close :("));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }
}
