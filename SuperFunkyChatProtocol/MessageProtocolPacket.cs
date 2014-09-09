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
    public class GenericMessageProtocolPacket : ProtocolPacket
    {
        public string UserName { get; set; }
        public string Message { get; set; }

        private bool _unicode;

        protected GenericMessageProtocolPacket(string username, string message, bool unicode)
        {
            UserName = username;
            Message = message;
            _unicode = unicode;
        }

        public override byte[] GetData()
        {
            MemoryStream stm = new MemoryStream();

            BinaryWriter writer = new BinaryWriter(stm, _unicode ? Encoding.Unicode : Encoding.ASCII);

            writer.Write((byte)(_unicode ? ProtocolCommandId.UTF16Message : ProtocolCommandId.Message));
            writer.Write(UserName);
            writer.Write(Message);

            return stm.ToArray();
        }

        protected GenericMessageProtocolPacket(byte[] data, bool unicode)
        {
            _unicode = unicode;
            BinaryReader reader = new BinaryReader(new MemoryStream(data), _unicode ? Encoding.Unicode : Encoding.ASCII);

            // Remove command code
            reader.ReadByte();
            UserName = reader.ReadString();
            Message = reader.ReadString();
        }
    }

    public class MessageProtocolPacket : GenericMessageProtocolPacket
    {
        public MessageProtocolPacket(string username, string message)
            : base(username, message, false)
        {
        }

        public MessageProtocolPacket(byte[] data) : base(data, false)
        {
        }
    }

    public class UTF16MessageProtocolPacket : GenericMessageProtocolPacket
    {
        public UTF16MessageProtocolPacket(string username, string message)
            : base(username, message, true)
        {
        }

        public UTF16MessageProtocolPacket(byte[] data)
            : base(data, true)
        {
        }
    }
}
