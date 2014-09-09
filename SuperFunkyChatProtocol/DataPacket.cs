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

using System.Collections.Generic;
using System.IO;
using System.Net;

namespace SuperFunkyChatProtocol
{
    public class DataPacket
    {
        private const int BLOCK_SIZE = 8192;

        byte[] _data;

        public byte[] Data 
        {
            get
            {
                return _data;
            }

            set
            {
                _data = value;
            }
        }

        public DataPacket(byte[] data)
        {
            _data = data;
        }

        public DataPacket(ProtocolPacket packet) : this(packet.GetData())
        {
        }

        private static int CalculateChecksum(byte[] data)
        {            
            int ret = 0;

            foreach (byte b in data)
            {
                ret = unchecked(ret + b);
            }

            return ret;         
        }

        private static bool VerifyChecksum(byte[] data, int chksum)
        {
            return CalculateChecksum(data) == chksum;            
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(IPAddress.HostToNetworkOrder(_data.Length));
            writer.Write(IPAddress.HostToNetworkOrder(CalculateChecksum(_data)));
            writer.Write(_data);
        }

        public static DataPacket ReadFrom(BinaryReader reader)
        {
            int len = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            int chksum = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            List<byte> currData = new List<byte>();
            int currLen = 0;            

            while (currLen < len)
            {
                int readLen = (len - currLen) > BLOCK_SIZE ? BLOCK_SIZE : (len - currLen);

                currData.AddRange(reader.ReadBytes(readLen));

                currLen = currData.Count;
            }

            byte[] data = currData.ToArray();

            if (!VerifyChecksum(data, chksum))
            {
                throw new InvalidDataException("Checksum does not match");
            }

            return new DataPacket(data);
        }
    }
}
