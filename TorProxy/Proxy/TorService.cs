using System.Text;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace TorProxy.Proxy
{
    public class TorService
    {

        public static bool DebugMode = true;

        private static TorService? _instance;

        public static TorService Instance { 
            get 
            {
                _instance ??= new TorService();
                return _instance;
            } 
        }

        public readonly Dictionary<string, string> Paths = new()
        {
            { "tor", AppContext.BaseDirectory + "tor\\" },
            { "obfs4proxy.exe", AppContext.BaseDirectory + "tor\\obfs4proxy.exe" },
            { "tor.exe", AppContext.BaseDirectory + "tor\\tor.exe" },
            { "torrc", AppContext.BaseDirectory + "tor\\torrc" },
        };

        private Dictionary<string, string[]> _torrcConfiguration = new()
        {
            { "ClientTransportPlugin", new string[] { "obfs2,obfs3,obfs4,scramblesuit,webtunnel exec obfs4proxy.exe" } },
            { "SocksPort", new string[] { "9050" } },
            { "UseBridges", new string[] { "1" } },
        };

        // Used to set socks proxy
        [DllImport("wininet.dll")]
        public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        public const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        public const int INTERNET_OPTION_REFRESH = 37;

        const string userRoot = "HKEY_CURRENT_USER";
        const string subkey = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
        const string keyName = userRoot + "\\" + subkey;

        private Process? _torProxyProcess;

        public ProxyStatus Status { get; private set; } = ProxyStatus.Disabled;

        public string StartupStatus { get; private set; } = string.Empty;

        public List<string> FilteredBridges = new();
        public List<string> WorkingBridges = new();

        public event EventHandler OnStartupStatusChange;
        public event EventHandler OnStatusChange;

        public bool ProxyRunning 
        { 
            get 
            {
                return _torProxyProcess != null && !_torProxyProcess.HasExited;
            }
        }

        private TorService() 
        {
            _instance = this;

            if (File.Exists(Paths["torrc"]))
            {
                _torrcConfiguration.Clear();
                string trimmedLine;
                string[] splitLine;
                List<string> value;
                foreach (string line in File.ReadAllLines(Paths["torrc"]))
                {
                    trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("#") || trimmedLine.StartsWith("//") || !trimmedLine.Contains(' ') || string.IsNullOrWhiteSpace(trimmedLine)) continue;
                    splitLine = trimmedLine.Split(' ', 2);
                    if (!_torrcConfiguration.ContainsKey(splitLine[0])) _torrcConfiguration[splitLine[0]] = Array.Empty<string>();
                    value = _torrcConfiguration[splitLine[0]].ToList();
                    value.Add(splitLine[1]);
                    _torrcConfiguration[splitLine[0]] = value.ToArray();
                }
            }
            else
            {
                UpdateTorrc();
            }
            if (_torrcConfiguration.ContainsKey("Bridge")) FilteredBridges = _torrcConfiguration["Bridge"].ToList();
        }

        public void UpdateTorrc()
        {
            StringBuilder generatedConfiguration = new("#" + DateTime.Now.ToString("hh:mm:ss") + "\n");
            foreach (string par in _torrcConfiguration.Keys)
            {
                foreach (string val in _torrcConfiguration[par])
                {
                    generatedConfiguration.AppendLine(par + " " + val);
                }
            }
            File.WriteAllText(Paths["torrc"], generatedConfiguration.ToString());
        }

        public string[] GetConfigurationValue(string key)
        {
            if (!_torrcConfiguration.ContainsKey(key)) return Array.Empty<string>();
            return _torrcConfiguration[key];
        }

        public void SetConfigurationValue(string key, string value, bool appendToEnd = false)
        {
            if (appendToEnd) _torrcConfiguration[key] = _torrcConfiguration[key].Append(value).ToArray();
            else _torrcConfiguration[key] = new string[] { value, };
        }

        public void SetConfigurationValue(string key, string[] value, bool appendToEnd = false)
        {
            if (appendToEnd) _torrcConfiguration[key].ToList().AddRange(value);
            else _torrcConfiguration[key] = value;
        }

        public void StopTorProxy()
        {
            if (!ProxyRunning) return;
            _torProxyProcess.StandardInput.Close();
            _torProxyProcess.StandardOutput.Close();
            _torProxyProcess.Kill(entireProcessTree: true);
            Status = ProxyStatus.Disabled;
            OnStatusChange?.Invoke(this, EventArgs.Empty);
        }

        public void WaitForEnd()
        {
            if (!ProxyRunning) return;
            _torProxyProcess.WaitForExit();
        }

        private void SetStartupStatus(string status)
        {
            StartupStatus = status;
            OnStartupStatusChange?.Invoke(this, EventArgs.Empty);
        }

        public static void EnableProxy(bool enabled)
        {
            if (enabled)
            {
                Registry.SetValue(keyName, "ProxyServer", "socks=127.0.0.1:" + TorService.Instance.GetConfigurationValue("SocksPort")[0]);
                Registry.SetValue(keyName, "ProxyEnable", 1, RegistryValueKind.DWord);
            }
            else
            {
                Registry.SetValue(keyName, "ProxyEnable", 0, RegistryValueKind.DWord);
            }

            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }

        public void StartTorProxy()
        {
            if (ProxyRunning) return;
            UpdateTorrc();
            _torProxyProcess = new Process();
            _torProxyProcess.StartInfo.FileName = "cmd.exe";
            _torProxyProcess.StartInfo.RedirectStandardInput = true;
            _torProxyProcess.StartInfo.RedirectStandardOutput = true;
            _torProxyProcess.StartInfo.UseShellExecute = false;
            _torProxyProcess.StartInfo.WorkingDirectory = Paths["tor"];
            _torProxyProcess.StartInfo.Arguments = "/c tor.exe -f torrc";

            _torProxyProcess.StartInfo.WindowStyle = DebugMode ? ProcessWindowStyle.Maximized : ProcessWindowStyle.Hidden;
            _torProxyProcess.StartInfo.CreateNoWindow = !DebugMode;

            _torProxyProcess.Start();

            SetStartupStatus("Process started");

            /**
             * Thread which listens to the output of tor.exe and updates information about current state of the connection
             * Following thread would kill the entire program if it crashes unexpectedly and if it couldnt stop tor process to prevent runaway tor service
             */
            Thread outputSniffer = 
            new(() =>
            {
                char logType;
                string? line;
                ProxyStatus oldStatus;
                Regex ipv4AddressSelector = new("(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)");
                Regex ipv6AddressSelector = new("(([0-9a-fA-F]{1,4}:){7,7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9]))");
                Regex logTypeSelector = new("\\[(.*?)\\]");
                Regex percentageSelector = new("(\\d+(\\.\\d+)?%)");
                try
                {
                    while (ProxyRunning)
                    {
                        if (!_torProxyProcess.StandardOutput.EndOfStream && !_torProxyProcess.HasExited && _torProxyProcess.StandardOutput.BaseStream.CanRead)
                        {
                            line = _torProxyProcess.StandardOutput.ReadLine()?.ToUpper();
                            if (line == null) continue;

                            if (DebugMode) Console.WriteLine(line);
                            logType = logTypeSelector.Match(line).Value[1];
                            oldStatus = Status;

                            if (line.Contains("BOOTSTRAPPED") || line.Contains("STARTING"))
                            {
                                SetStartupStatus("Conn: " + percentageSelector.Match(line).Value);
                                Status = ProxyStatus.Starting;
                            }
                            if (line.Contains("DONE") && line.Contains("100%"))
                            {
                                SetStartupStatus("Done");
                                Status = ProxyStatus.Running;
                            }
                            if (logType == 'E') Status = ProxyStatus.Disabled;

                            string serveripv4;
                            string serveripv6;
                            string bridgeString;
                            if (logType == 'W' && line.Contains("UNABLE TO CONNECT"))
                            {
                                serveripv4 = ipv4AddressSelector.Match(line).Value;
                                serveripv6 = ipv6AddressSelector.Match(line).Value.ToLower();
                                FilteredBridges.RemoveAll(x => (x.Contains(serveripv4) || x.Contains(serveripv6)));
                                WorkingBridges.RemoveAll(x => (x.Contains(serveripv4) || x.Contains(serveripv6)));
                                if (DebugMode) Console.WriteLine("BRIDGE " + serveripv4 + " WAS FILTERED");
                            }

                            if (logType == 'N' && line.Contains("NEW BRIDGE DESCRIPTOR"))
                            {
                                serveripv4 = ipv4AddressSelector.Match(line).Value;
                                serveripv6 = ipv6AddressSelector.Match(line).Value.ToLower();
                                bridgeString = _torrcConfiguration["Bridge"].ToList().FindAll(x => (x.Contains(serveripv4) || x.Contains(serveripv6)))[0];
                                if (!WorkingBridges.Contains(bridgeString)) WorkingBridges.Add(bridgeString);
                                if (!FilteredBridges.Contains(bridgeString)) FilteredBridges.Add(bridgeString);
                                if (!FilteredBridges.Any(x => (x.Contains(serveripv4) || x.Contains(serveripv6)))) FilteredBridges.Add(bridgeString); 
                                if (DebugMode) Console.WriteLine("BRIDGE " + bridgeString + " WAS ADDED");
                                if (DebugMode) Console.WriteLine("BRIDGE " + serveripv4 + " IS WORKING");
                            }

                            if (oldStatus != Status) OnStatusChange?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }catch(Exception)
                {
                    if (DebugMode) Console.WriteLine("SNIFFER THREAD CRASHED!");

                    // I dont trust StopTorProxy()
                    Process.GetProcessesByName("obfs4proxy").ToList().ForEach(p => p.Kill());
                    Process.GetProcessesByName("tor").ToList().ForEach(p => p.Kill());

                    EnableProxy(false);

                    Status = ProxyStatus.Disabled;
                    OnStatusChange?.Invoke(this, EventArgs.Empty);
                    return;
                }
                if (DebugMode) Console.WriteLine("SNIFFER THREAD ENDED");
                Status = ProxyStatus.Disabled;
                OnStatusChange?.Invoke(this, EventArgs.Empty);
                EnableProxy(false);
            }
            )
            { IsBackground = true, };
            outputSniffer.Start();
        }
    }
};