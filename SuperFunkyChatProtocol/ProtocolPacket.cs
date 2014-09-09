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

namespace SuperFunkyChatProtocol
{
    public enum ProtocolCommandId : byte
    {
        Hello,
        Goodbye,
        Message,
        Image,
        UTF16Message,     
        SendFile,
        RequestUpdate,
        SendUpdate,
        Target,
        GetUserList,
        UserList,
        UpgradeSecurity,
    }

    public abstract class ProtocolPacket
    {
        protected ProtocolPacket()
        {
        }

        public abstract byte[] GetData();

        public static ProtocolPacket FromData(byte[] data)
        {                                    
            ProtocolCommandId cmd = (ProtocolCommandId)data[0];

            switch (cmd)
            {
                case ProtocolCommandId.Message:
                    return new MessageProtocolPacket(data);                    
                case ProtocolCommandId.UTF16Message:
                    return new UTF16MessageProtocolPacket(data);
                case ProtocolCommandId.Image:
                    return new ImageProtocolPacket(data);                    
                case ProtocolCommandId.Hello:
                    return new HelloProtocolPacket(data);                    
                case ProtocolCommandId.Goodbye:
                    return new GoodbyeProtocolPacket(data);                    
                case ProtocolCommandId.Target:
                    return new TargetProtocolPacket(data);
                case ProtocolCommandId.UserList:
                    return new UserListProtocolPacket(data);
                case ProtocolCommandId.SendUpdate:
                    return new SendUpdateProtocolPacket(data);
                case ProtocolCommandId.SendFile:
                    return new SendFileProtocolPacket(data);
                case ProtocolCommandId.UpgradeSecurity:
                    return new UpgradeSecurityProtocolPacket(data);
                default:
                    throw new ArgumentException("Invalid command code");
            }
        }
    }
}
