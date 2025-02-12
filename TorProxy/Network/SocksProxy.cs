using PacketDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TorProxy.Network
{
    public class SocksProxy
    {

        private const byte SOCKS5_ADDRTYPE_IPV4 = 0x01;
        //private const byte SOCKS5_ADDRTYPE_DOMAIN_NAME = 0x03;
        private const byte SOCKS5_ADDRTYPE_IPV6 = 0x04;

        public readonly IPEndPoint ProxyEndpoint;

        public readonly Socket ProxySocket;

        public bool IsConnected { get; private set; }

        public const int Timeout = 250;

        public SocksProxy(IPEndPoint proxy)
        {
            ProxyEndpoint = proxy;
            //ProxySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp)
            //{
                //ReceiveTimeout = 1000,
                //SendTimeout = 1000,
            //};
            //ProxySocket.Connect(ProxyEndpoint);
            //Connect(ProxySocket);
        }

        public Socket CreateNewConnectionToHost(IPEndPoint destination)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp)
            {
                ReceiveTimeout = Timeout,
                SendTimeout = Timeout,
            };
            socket.Connect(ProxyEndpoint);
            Connect(socket);

            byte[] request = new byte[257];
            byte[] response = new byte[257];

            byte[] destinationAddress = destination.Address.GetAddressBytes();

            request[0] = 0x05;
            request[1] = 0x01;
            request[2] = 0x00;
            request[3] = destination.Address.IsIPv6Multicast ? SOCKS5_ADDRTYPE_IPV6 : SOCKS5_ADDRTYPE_IPV4;

            destinationAddress.CopyTo(request, 4);
            BitConverter.GetBytes((UInt16)destination.Port).Reverse().ToArray().CopyTo(request, 4 + destinationAddress.Length);

            socket.Send(request);
            socket.Receive(response);

            if (response[1] != 0x00)
            {
                throw new IOException("Unable to connect to host: invalid connect response");
                
            }
            return socket;
        }

        private void Connect(Socket socket)
        {
            if (!socket.Connected) return;

            byte[] request = new byte[4] { 0x05, 0x02, 0x00, 0x00 };
            byte[] response = new byte[257];

            socket.Send(request);
            socket.Receive(response);

            if (response[1] == 0XFF)
            {
                throw new IOException("Unable to connect to socks proxy: invalid auth response");
            }

            IsConnected = true;
        }
    }
}
