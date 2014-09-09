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
using System.Text;

namespace SuperFunkyChatProtocol
{
    public class TargetProtocolPacket : ProtocolPacket
    {
        public string UserName { get; set; }
        public ProtocolPacket Packet { get; set; }

        public override byte[] GetData()
        {
            MemoryStream stm = new MemoryStream();

            BinaryWriter writer = new BinaryWriter(stm, Encoding.ASCII);

            writer.Write((byte)ProtocolCommandId.Target);
            writer.Write(UserName);
            byte[] data = Packet.GetData();

            NetworkUtils.WriteBytes(writer, Packet.GetData());
            
            return stm.ToArray();
        }

        public TargetProtocolPacket(byte[] data)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(data), Encoding.UTF8);

            // Remove command code
            reader.ReadByte();
            UserName = reader.ReadString();            
            Packet = ProtocolPacket.FromData(NetworkUtils.ReadBytes(reader));
        }

        public TargetProtocolPacket(string userName, ProtocolPacket packet)
        {
            UserName = userName;
            Packet = packet;
        }
    }
}
