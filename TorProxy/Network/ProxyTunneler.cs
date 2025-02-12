using System.Net;
using System.Net.Sockets;
using Microsoft.VisualBasic.Devices;
using System.Security.Policy;
using System.Text;
using NdisApi;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using System.ComponentModel;
using System.Globalization;
using TorProxy;
using PacketDotNet;
using System.IO;

namespace TorProxy.Network
{

    // UNFINISHED
    // I literally dont understand what I am doing
    public class ProxyTunneler : NetworkBlockingFilter
    {

        public SocksProxy _proxy;

        private Dictionary<string, Socket> _openSockets = new Dictionary<string, Socket>();

        public ProxyTunneler(IPEndPoint proxyAddress, FilterBypass[] bypass)
            : base(
                  bypass.ToList().Append(new FilterBypass(Utils.GetMainInterfaceAddress(), proxyAddress.Address)).ToArray(),
                  bypassAction: StaticFilter.FILTER_PACKET_ACTION.FILTER_PACKET_PASS,
                  globalAction: StaticFilter.FILTER_PACKET_ACTION.FILTER_PACKET_REDIRECT
            )
        {
            _proxy = new SocksProxy(proxyAddress);
            //List<StaticFilter> filters = _ndisapi.GetPacketFilterTable().Item2;
            //filters.Add(new StaticFilter()
            //{

            //});
        }

        
        protected override void HandlePacket(RawPacket rawPacket)
        {
            if (rawPacket.DeviceFlags == PACKET_FLAG.PACKET_FLAG_ON_RECEIVE)
            {
                //base.HandlePacket(rawPacket);
                return;
            }
            Packet packet = Packet.ParsePacket(LinkLayers.Ethernet, rawPacket.Data);
            if (packet.PayloadPacket is IPPacket ippacket)
            {
                Console.WriteLine("IPPACKET");
                if (ippacket.PayloadPacket is TcpPacket tcppacket)
                {
                    Console.WriteLine("TCPPACKET");
                    Console.WriteLine(ippacket.SourceAddress + " -> " + ippacket.DestinationAddress);
                    //if (ippacket.DestinationAddress.ToString() == "192.168.1.4") return;

                    //using (Socket s = _proxy.CreateNewConnectionToHost(IPEndPoint.Parse(ippacket.DestinationAddress + ":" + tcppacket.DestinationPort)))
                    //{
                        //tcppacket.UpdateCalculatedValues();
                        //tcppacket.UpdateTcpChecksum();
                        //s.Send(tcppacket.Bytes);
                    //}

                    //Thread reforwarder = new Thread(() =>
                    //{
                        //try
                        //{
                            
                        //}catch(Exception e)
                        //{

                        //}
                        
                        try
                        {
                            string addr = ippacket.DestinationAddress + ":" + tcppacket.DestinationPort;
                            byte[] resp = new byte[8192];
                            if (_openSockets.ContainsKey(addr))
                            {
                                packet.UpdateCalculatedValues();
                                tcppacket.UpdateTcpChecksum();
                                _openSockets[addr].Send(tcppacket.PayloadData);
                                Console.WriteLine("Already existed!");
                            }
                            else
                            {
                                byte[] response = new byte[8192];
                                _openSockets[addr] = _proxy.CreateNewConnectionToHost(IPEndPoint.Parse(addr));
                                packet.UpdateCalculatedValues();
                                tcppacket.UpdateTcpChecksum();
                                _openSockets[addr].Send(tcppacket.PayloadData);
                                Console.WriteLine("New entry!");
                            }
                            //_openSockets[addr].Receive(resp);
                            //Console.WriteLine(Encoding.ASCII.GetString(resp));
                        }
                        catch (Exception e)
                        {
                            //throw e;
                            Console.WriteLine(e.Message);
                        }
                        //_proxy.ConnectToHost(IPEndPoint.Parse(ippacket.DestinationAddress + ":" + tcppacket.DestinationPort));

                        //_proxySocket.Send(tcppacket.PayloadData);
                        
                        
                    //});

                    //reforwarder.Start();
                }
            }
        }
    }
}