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
using System.Security.Cryptography;

namespace SuperFunkyChatProtocol
{
    public static class NetworkUtils
    {
        const int BLOCK_SIZE = 8192;

        public static void WriteBytes(BinaryWriter writer, byte[] data)
        {
            writer.Write(IPAddress.HostToNetworkOrder(data.Length));
            writer.Write(data);
        }

        public static byte[] ReadBytes(BinaryReader reader)
        {
            int len = IPAddress.NetworkToHostOrder(reader.ReadInt32());
            List<byte> currData = new List<byte>();
            int currLen = 0;

            while (currLen < len)
            {
                int readLen = (len - currLen) > BLOCK_SIZE ? BLOCK_SIZE : (len - currLen);

                currData.AddRange(reader.ReadBytes(readLen));

                currLen = currData.Count;
            }

            return currData.ToArray();
        }

        public static bool VerifySignature(byte[] data, byte[] signature, string xmlkey)
        {
            RSA key = new RSACryptoServiceProvider();

            key.FromXmlString(xmlkey);

            RSAPKCS1SignatureDeformatter deformatter = new RSAPKCS1SignatureDeformatter(key);

            SHA1 sha1 = new SHA1CryptoServiceProvider();

            sha1.ComputeHash(data);

            return deformatter.VerifySignature(sha1, signature);
        }

        public static bool VerifyHash(byte[] data, byte[] hash)
        {
            bool ret = true;
            byte[] newhash = SHA256.Create().ComputeHash(data);

            if (newhash.Length == hash.Length)
            {
                for (int i = 0; i < newhash.Length; ++i)
                {
                    if (newhash[i] != hash[i])
                    {
                        ret = false;
                        break;
                    }
                }
            }
            else
            {
                ret = false;
            }

            return ret;
        }
    }
}
