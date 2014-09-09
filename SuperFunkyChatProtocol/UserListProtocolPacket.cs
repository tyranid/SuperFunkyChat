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

using System.IO;
using System.Net;
using System.Text;

namespace SuperFunkyChatProtocol
{
    public class UserListProtocolPacket : ProtocolPacket
    {
        public class UserListEntry
        {
            public string UserName { get; private set; }
            public string HostName { get; private set; }

            public UserListEntry(string userName, string hostName)
            {
                UserName = userName;
                HostName = hostName;
            }
        }

        public UserListEntry[] UserList { get; private set; }

        public override byte[] GetData()
        {
            MemoryStream stm = new MemoryStream();

            BinaryWriter writer = new BinaryWriter(stm, Encoding.UTF8);

            writer.Write((byte)ProtocolCommandId.UserList);

            writer.Write(IPAddress.HostToNetworkOrder(UserList.Length));

            foreach(UserListEntry ent in UserList)
            {
                writer.Write(ent.UserName);
                writer.Write(ent.HostName);
            }
            
            return stm.ToArray();
        }

        public UserListProtocolPacket(byte[] data)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(data), Encoding.UTF8);

            // Remove command code
            reader.ReadByte();

            int len = IPAddress.NetworkToHostOrder(reader.ReadInt32());

            UserList = new UserListEntry[len];

            for (int i = 0; i < len; i++)
            {
                string userName = reader.ReadString();
                string hostName = reader.ReadString();

                UserList[i] = new UserListEntry(userName, hostName);
            }
        }

        public UserListProtocolPacket(UserListEntry[] entries)
        {
            UserList = entries;
        }
    }
}
