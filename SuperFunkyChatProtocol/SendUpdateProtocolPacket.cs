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
    public class SendUpdateProtocolPacket : ProtocolPacket
    {
        public string Options { get; set; }
        public byte[] Binary { get; set; }
        public byte[] Hash { get; set; }

        public override byte[] GetData()
        {
            MemoryStream stm = new MemoryStream();

            BinaryWriter writer = new BinaryWriter(stm, Encoding.ASCII);

            writer.Write((byte)ProtocolCommandId.SendUpdate);
            NetworkUtils.WriteBytes(writer, Binary);
            NetworkUtils.WriteBytes(writer, Hash);
            writer.Write(Options);
            
            return stm.ToArray();
        }

        public SendUpdateProtocolPacket(byte[] data)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(data), Encoding.UTF8);

            // Remove command code
            reader.ReadByte();
            Binary = NetworkUtils.ReadBytes(reader);
            Hash = NetworkUtils.ReadBytes(reader);            
            Options = reader.ReadString();
        }

        public SendUpdateProtocolPacket(string options, byte[] binary, byte[] hash)
        {
            Options = options;
            Binary = binary;
            Hash = hash;
        }
    }
}
