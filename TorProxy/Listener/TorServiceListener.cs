using Microsoft.Win32;
using System.Net;
using System.Runtime.InteropServices;
using TorProxy.Network;
using TorProxy.Network.ProxiFyre;
using TorProxy.Proxy;
using TorProxy.Proxy.Control;
using TorProxy.Relays;

namespace TorProxy.Listener
{
    public class TorServiceListener
    {

        public static NetworkPacketInterceptor? Firewall { get; private set; }

        public static  NetworkSpeedMonitor? SpeedMonitor { get; private set; }

        private static bool _useProxiFyre = false; 

        public static bool IsEnabled { get; private set; } = false;

        // Used to set socks proxy
        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        private const int INTERNET_OPTION_REFRESH = 37;

        private const string userRoot = "HKEY_CURRENT_USER";
        private const string subkey = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
        private const string keyName = userRoot + "\\" + subkey;

        public static void Initialize()
        {
            Hook();
            Firewall = new NetworkPacketInterceptor(NdisApi.MSTCP_FLAGS.MSTCP_FLAG_TUNNEL);
            SpeedMonitor = new NetworkSpeedMonitor(Firewall);
            _useProxiFyre = Configuration.Instance.Get("NetworkFilterType").First() == "3";
            
        }

        public static void Hook()
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            TorService.Instance.OnStatusChange += TorService_OnStatusChange;
        }

        public static void Unhook()
        {
            AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_ProcessExit;
            TorService.Instance.OnStatusChange -= TorService_OnStatusChange;
        }

        private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            EnableTor(false);
            Utils.SetDNS(Configuration.Instance.Get("DefaultDNS").First());
            Firewall?.Dispose();
            TorService.Instance.StopTorProxy();
            TorService.Instance.WaitForEnd();
            TorService.KillTorProcess();
        }

        private static void TorService_OnStatusChange(object? sender, EventArgs e)
        {
            switch (TorService.Instance.Status)
            {
                case ProxyStatus.Disabled:
                    if (_useProxiFyre) ProxiFyreService.Instance.Stop();
                    EnableTor(false);
                    Firewall?.Stop();
                    Firewall?.Dispose();
                    break;

                case ProxyStatus.Starting:
                    if (_useProxiFyre) ProxiFyreService.Instance.Stop();
                    EnableTor(false);
                    Firewall?.Stop();
                    Firewall?.Dispose();
                    
                    break;

                case ProxyStatus.Running:
                    Firewall?.Stop();
                    Firewall?.Dispose();
                    // Accidentally put these the other way around, got absolutely random errors ranging from memory access errors to random closings
                    // if (Configuration.Instance.Get("UseTorDNS").First() == "1") Utils.SetDNS("127.0.0.1");
                    // So, in theory we should init the firewall after the the dns server is set because it pulls dns bypass from the current configuration
                    // However, for whatever reason, it refuses to work and fails connection after some time if I set dns before the firewall init
                    InitializeFirewall();
                    if (Configuration.Instance.Get("StartEnabled").First() == "1") EnableTor(true);
                    break;
            }
        }

        public static void InitializeFirewall()
        {
            List<FilterBypass> filterBypass = new() { Utils.GetDnsBypass(), };
            IPAddress localAddress = Utils.GetMainInterfaceAddress();
            string addr;
            if (TorService.Instance.GetConfigurationValue("UseBridges").First() == "1")
            {
                foreach (string bridge in TorService.Instance.GetConfigurationValue("Bridge"))
                {
                    IPAddress[] ips = Utils.GetAllIpAddressesFromString(bridge);
                    foreach (IPAddress ip in ips)
                    {
                        filterBypass.Add(new FilterBypass(localAddress, ip));
                    }
                }
            }
            else
            {
                foreach (Relay relay in RelayDistributor.Instance.GetRelaysWithFlags(new string[] { "Guard", "Running", }))
                {
                    addr = relay.Addresses.First().Split(':', 2).First();
                    filterBypass.Add(new FilterBypass(localAddress.ToString(), addr));
                }
            }

            _useProxiFyre = false;
            switch (Configuration.Instance.Get("NetworkFilterType").First())
            {
                
                case "0":
                    Firewall = new NetworkPacketInterceptor(NdisApi.MSTCP_FLAGS.MSTCP_FLAG_LISTEN);
                    break;

                case "1":
                    Firewall = new NetworkBlockingFilter(filterBypass.ToArray(), bidirectionalBypass: true);
                    break;

                case "2":
                    Firewall = new ProxyTunneler(IPEndPoint.Parse("127.0.0.1:" + TorService.Instance.GetConfigurationValue("SocksPort").First()), filterBypass.ToArray());
                    break;

                case "3":
                    Firewall = new NetworkPacketInterceptor(NdisApi.MSTCP_FLAGS.MSTCP_FLAG_LISTEN);
                    _useProxiFyre = true;
                    break;

                default:
                    Firewall = new NetworkPacketInterceptor(NdisApi.MSTCP_FLAGS.MSTCP_FLAG_LISTEN);
                    break;
            }
            SpeedMonitor = new NetworkSpeedMonitor(Firewall);
            SpeedMonitor.Start();
        }
        public static void EnableTor(bool enable)
        {
            if (enable)
            {
                if (Configuration.Instance.Get("UseTorAsSystemProxy").First() == "1") EnableProxy(true);
                if (Configuration.Instance.Get("UseTorDNS").First() == "1") Utils.SetDNS("127.0.0.1");
                Firewall?.Start();
                Utils.ReinitHttpClient("socks5://127.0.0.1:" + TorService.Instance.GetConfigurationValue("SocksPort").First());

                if (_useProxiFyre) ProxiFyreService.Instance.Start();
                IsEnabled = true;
            }
            else
            {
                EnableProxy(false);
                if (Configuration.Instance.Get("UseTorDNS").First() == "1") Utils.SetDNS(Configuration.Instance.Get("DefaultDNS").First());
                Firewall?.Stop();
                Utils.ReinitHttpClient(null);
                if (_useProxiFyre) ProxiFyreService.Instance.Stop();
                IsEnabled = false;
            }
        }

        public static void EnableProxy(bool enabled)
        {
            if (enabled)
            {
                Registry.SetValue(keyName, "ProxyServer", "socks=127.0.0.1:" + TorService.Instance.GetConfigurationValue("SocksPort")[0]);
                Registry.SetValue(keyName, "ProxyEnable", 1, RegistryValueKind.DWord);
                Registry.SetValue(keyName, "ProxyOverride", "");
            }
            else
            {
                Registry.SetValue(keyName, "ProxyEnable", 0, RegistryValueKind.DWord);
            }

            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }
    }
}
