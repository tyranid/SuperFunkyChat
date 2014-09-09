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
    public class UpgradeSecurityProtocolPacket : ProtocolPacket 
    {
        public byte XorKey { get; set; }

        public override byte[] GetData()
        {
            MemoryStream stm = new MemoryStream();

            BinaryWriter writer = new BinaryWriter(stm, Encoding.ASCII);

            writer.Write((byte)ProtocolCommandId.UpgradeSecurity);
            writer.Write(XorKey);

            return stm.ToArray();
        }

        public UpgradeSecurityProtocolPacket(byte[] data)
        {
            // Do nothing
            if (data.Length < 2)
            {
                throw new InvalidDataException("No XOR key specified");
            }

            XorKey = data[1];
        }

        public UpgradeSecurityProtocolPacket(byte xorkey)
        {
            XorKey = xorkey;
        }
    }
}
