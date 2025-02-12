using System.Collections.Immutable;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Security.Principal;
using System.Text.RegularExpressions;
using TorProxy.Network;

namespace TorProxy
{
    internal sealed class Utils
    {

        private static HttpClient _httpClient = new HttpClient();

        public readonly static Regex Ipv4AddressSelector = new("(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)");
        public readonly static Regex Ipv6AddressSelector = new("(([0-9a-fA-F]{1,4}:){7,7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9]))");
        public readonly static Regex SquareBracketsSelector = new (@"(?<=\[)[^][]*(?=])");
        public readonly static Regex UrlSelector = new(@"(www.+|http.+)([\s]|$)");

        public readonly static Random Random = new Random(Convert.ToInt32(DateTime.Now.ToString("FFFFFFF")));

        private static NetworkInterface? _cachedMainNetworkInterface;

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        public static void HideConsole()
        {
            ShowWindow(GetConsoleWindow(), SW_HIDE);
        }

        public static void ShowConsole()
        {
            ShowWindow(GetConsoleWindow(), SW_SHOW);
        }


        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();

        public static string Download(string url)
        {
            return _httpClient.Send(new HttpRequestMessage(HttpMethod.Get, url)).Content.ReadAsStringAsync().Result;
        }

        public static void ReinitHttpClient(string? proxy = null)
        {
            _httpClient.Dispose();
            if (proxy != null) HttpClient.DefaultProxy = new WebProxy(new Uri(proxy));
            else HttpClient.DefaultProxy = new WebProxy();
            _httpClient = new HttpClient();
        }

        public static void DownloadToFile(string url, string fileName)
        {
            using (Stream s = _httpClient.GetStreamAsync(url).Result)
            {
                using (FileStream fs = new(fileName, FileMode.OpenOrCreate))
                {
                    s.CopyTo(fs);
                }
            }
        }

        public async static Task DownloadToFileAsync(string url, string fileName)
        {
            using Stream s = await _httpClient.GetStreamAsync(url);
            using FileStream fs = new(fileName, FileMode.OpenOrCreate);
            await s.CopyToAsync(fs);
        }

        // TODO: redo
        // I actually dont know how to properly do it, maybe this should be a user input?
        // The current filter is to get all working ethernet|wireless80211 adapters that are not vEthernet or VBox
        // And then get the network interface with the most bytes received
        public static NetworkInterface GetMainInterface()
        {
            if (_cachedMainNetworkInterface != null) return _cachedMainNetworkInterface;
            _cachedMainNetworkInterface = NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up
                && !x.IsReceiveOnly
                && (x.NetworkInterfaceType == NetworkInterfaceType.Ethernet || x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                && !x.Name.StartsWith("vEthernet")
                && !x.Description.StartsWith("VirtualBox"))
                .OrderByDescending(x => x.GetIPStatistics().BytesReceived)
                .First();
            return _cachedMainNetworkInterface;
        }

        public static IPAddress GetMainInterfaceAddress()
        {
            return GetMainInterface().GetIPProperties().UnicastAddresses.Last().Address;
        }

        public static FilterBypass GetDnsBypass()
        {
            return new FilterBypass(GetMainInterfaceAddress(), GetDnsAddress());
        }

        public static IPAddress GetDnsAddress()
        {
            return GetMainInterface().GetIPProperties().DnsAddresses.Last();
        }

        public static NetworkInterface[] GetActiveInterfaces()
        {
            return NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up).ToArray();
        }

        public static void SetDNS(string DnsString)
        {
            string[] Dns = { DnsString };
            NetworkInterface CurrentInterface = GetMainInterface();
            if (CurrentInterface == null) return;

            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();
            foreach (ManagementObject objMO in objMOC)
            {
                if ((bool)objMO["IPEnabled"])
                {
                    if (objMO["Description"].ToString().Equals(CurrentInterface.Description))
                    {
                        ManagementBaseObject objdns = objMO.GetMethodParameters("SetDNSServerSearchOrder");
                        if (objdns != null)
                        {
                            objdns["DNSServerSearchOrder"] = Dns;
                            objMO.InvokeMethod("SetDNSServerSearchOrder", objdns, null);
                        }
                    }
                }
            }
        }

        public static bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                      .IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
