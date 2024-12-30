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
        
        private static TorService _instance;

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
            { "ClientTransportPlugin", new string[] { "obfs2,obfs3,obfs4,scramblesuit exec obfs4proxy.exe" } },
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

        public List<string> FilteredBridges = new List<string>();
        public List<string> WorkingBridges = new List<string>();

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
            //_torProxyProcess.Close();
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
            UpdateTorrc();
            _torProxyProcess = new Process();
            _torProxyProcess.StartInfo.FileName = "cmd.exe";
            _torProxyProcess.StartInfo.RedirectStandardInput = true;
            _torProxyProcess.StartInfo.RedirectStandardOutput = true;
            _torProxyProcess.StartInfo.UseShellExecute = false;
            _torProxyProcess.StartInfo.WorkingDirectory = Paths["tor"];

            _torProxyProcess.StartInfo.WindowStyle = DebugMode ? ProcessWindowStyle.Maximized : ProcessWindowStyle.Hidden;
            _torProxyProcess.StartInfo.CreateNoWindow = !DebugMode;

            _torProxyProcess.StartInfo.Arguments = "/c tor.exe -f torrc";
            SetStartupStatus("Process started");
            _torProxyProcess.Start();

            /**
             * Thread which listens to the output of tor.exe and updates information about current state of the connection
             * Following thread would kill the entire program if it crashes unexpectedly and if it couldnt stop tor process to prevent runaway tor service
             */
            Thread outputSniffer = 
            new(() =>
            {
                char logType;
                string line;
                ProxyStatus oldStatus;
                Regex ipAddressSelector = new Regex("(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)");
                Regex logTypeSelector = new Regex("\\[(.*?)\\]");
                Regex percentageSelector = new Regex("(\\d+(\\.\\d+)?%)");
                try
                {
                    while (ProxyRunning)
                    {
                        if (!_torProxyProcess.StandardOutput.EndOfStream)
                        {
                            line = _torProxyProcess.StandardOutput.ReadLine().ToUpper();
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

                            string server;
                            if (logType == 'W' && (line.Contains("UNABLE TO CONNECT") || (line.Contains("PROBLEM BOOTSTRAPPING") && line.Contains("TIMEOUT"))))
                            {
                                server = ipAddressSelector.Match(line).Value;
                                FilteredBridges.RemoveAll(x => x.Contains(server));
                                if (DebugMode) Console.WriteLine("OBFS4 BRIDGE " + server + " WAS FILTERED");
                            }

                            if (logType == 'N' && line.Contains("NEW BRIDGE DESCRIPTOR"))
                            {
                                server = ipAddressSelector.Match(line).Value;
                                WorkingBridges.Add(_torrcConfiguration["Bridge"].ToList().FindAll(x => x.Contains(server))[0]);
                                if (DebugMode) Console.WriteLine("OBFS4 BRIDGE " + _torrcConfiguration["Bridge"].ToList().FindAll(x => x.Contains(server))[0] + " WAS ADDED");
                                if (DebugMode) Console.WriteLine("OBFS4 BRIDGE " + server + " IS WORKING");
                            }

                            if (oldStatus != Status) OnStatusChange?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }catch(Exception e)
                {
                    if (DebugMode) Console.WriteLine("SNIFFER THREAD CRASHED!");
                    try
                    {
                        StopTorProxy();
                        EnableProxy(false);
                        Status = ProxyStatus.Disabled;
                        OnStatusChange?.Invoke(this, EventArgs.Empty);
                    }
                    catch(Exception)
                    {
                        Console.WriteLine("RUNAWAY TOR SERVICE\nTRYING TO TERMINATE (LAST RESORT)");
                        EnableProxy(false);
                        Process.GetProcessesByName("tor").ToList().ForEach(p => p.Kill());
                        Process.GetProcessesByName("obfs4proxy").ToList().ForEach(p => p.Kill());
                        Process.GetProcessesByName("TorProxy").ToList().ForEach(p => p.Kill());
                        Application.Exit();
                    }
                    return;
                }
                if (DebugMode) Console.WriteLine("SNIFFER THREAD ENDED");
                EnableProxy(false);
            }
            )
            { IsBackground = true, };
            outputSniffer.Start();
        }
    }
};