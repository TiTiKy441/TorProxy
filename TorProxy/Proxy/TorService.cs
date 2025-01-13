using System.Text;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Linq.Expressions;
using System.Collections.ObjectModel;

namespace TorProxy.Proxy
{
    public class TorService
    {

        private static TorService? _instance;

        public static TorService Instance { 
            get 
            {
                _instance ??= new TorService();
                return _instance;
            } 
        }

        public static readonly Dictionary<string, string> Paths = new()
        {
            { "tor", Path.GetFullPath(AppContext.BaseDirectory + "tor\\") },
            { "obfs4proxy.exe", Path.GetFullPath(AppContext.BaseDirectory + "tor\\obfs4proxy.exe") },
            { "tor.exe", Path.GetFullPath(AppContext.BaseDirectory + "tor\\tor.exe") },
            { "torrc", Path.GetFullPath(AppContext.BaseDirectory + "tor\\torrc") },
        };

        private Dictionary<string, string[]> _torrcConfiguration = new()
        {
            { "ClientTransportPlugin", new string[] { "obfs4,webtunnel exec obfs4proxy.exe" } },
            { "SocksPort", new string[] { "9050" } },
            { "UseBridges", new string[] { "1" } },
            { "DataDirectory", new string[] { AppContext.BaseDirectory.Replace(@"\", "/") + "/tor/" } },
        };

        // Used to set socks proxy
        [DllImport("wininet.dll")]
        private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        private const int INTERNET_OPTION_REFRESH = 37;

        private const string userRoot = "HKEY_CURRENT_USER";
        private const string subkey = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
        private const string keyName = userRoot + "\\" + subkey;

        private Process? _torProxyProcess;

        public ProxyStatus Status { get; private set; } = ProxyStatus.Disabled;

        public string StartupStatus { get; private set; } = string.Empty;

        public List<string> FilteredBridges = new();
        public List<string> WorkingBridges = new();
        public List<string> DeadBridges = new();

        public bool BridgeProxyExhausted = false;

        public event EventHandler? OnStartupStatusChange;
        public event EventHandler? OnStatusChange;
        public event EventHandler? OnBridgeProxyExhaustion;
        public event EventHandler<NewBridgeEventArgs>? OnNewWorkingBridge;
        public event EventHandler<NewBridgeEventArgs>? OnNewDeadBridge;
        public event EventHandler<NewMessageEventArgs>? OnNewMessage;

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
            ClearCache();
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
        }

        public void UpdateTorrc()
        {
            StringBuilder generatedConfiguration = new("#" + DateTime.Now.ToString("hh:mm:ss") + "\n");
            foreach (string par in _torrcConfiguration.Keys)
            {
                foreach (string val in _torrcConfiguration[par])
                {
                    if (val == null || string.IsNullOrEmpty(val)) continue;
                    generatedConfiguration.AppendLine(par + " " + val);
                }
            }
            File.WriteAllText(Paths["torrc"], generatedConfiguration.ToString());
        }

        public void ClearCache()
        {
            if (ProxyRunning) return;
            foreach (string path in Directory.GetFiles(Paths["tor"]))
            {
                if (!Paths.ContainsValue(Path.GetFullPath(path))) File.Delete(path);
            }
        }

        public string[] GetConfigurationValue(string key)
        {
            if (!_torrcConfiguration.ContainsKey(key)) return Array.Empty<string>();
            return _torrcConfiguration[key];
        }

        public bool ExistsConfigurationValue(string key)
        {
            return _torrcConfiguration.ContainsKey(key);
        }

        public void SetConfigurationValue(string key, string value, bool appendToEnd = false)
        {
            if (appendToEnd) _torrcConfiguration[key] = _torrcConfiguration[key].Append(value).ToArray();
            else _torrcConfiguration[key] = new string[] { value, };
        }

        public void SetConfigurationValue(string key, string[] value, bool appendToEnd = false)
        {
            if (appendToEnd)
            {
                List<string> c = _torrcConfiguration[key].ToList();
                c.AddRange(value);
                _torrcConfiguration[key] = c.ToArray();
            }
            else _torrcConfiguration[key] = value;
        }

        public void StopTorProxy()
        {
            if (!ProxyRunning) return;
            _torProxyProcess.StandardInput.Close();
            _torProxyProcess.StandardOutput.Close();
            _torProxyProcess.Kill(entireProcessTree: true);
            WorkingBridges.Clear();
            FilteredBridges.Clear();
            DeadBridges.Clear();
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

        public static void KillTorProcess()
        {
            List<string> usedFilenames = Paths.Values.ToList().ConvertAll(x => Path.GetFileName(x)).Where((x, i) => Path.GetExtension(x) == ".exe").ToList();
            List<Process> processesToKill = new();
            foreach (Process p in Process.GetProcesses().Where((x, i) => usedFilenames.Contains(x.ProcessName + ".exe")))
            {
                try
                {
                    if (p.Id == Environment.ProcessId || p.Modules == null) continue;
                    foreach (ProcessModule module in p.Modules)
                    {
                        if (module.FileName == null) return;
                        if (Paths.ContainsValue(module.FileName)) processesToKill.Add(p);
                    }
                }
                catch (Exception e)
                {
                }
            }
            processesToKill.ForEach(x => x.Kill(true));
        }

        public void ReloadWithWorkingBridges()
        {
            string[] working = WorkingBridges.ToArray();
            StopTorProxy();
            WaitForEnd();
            SetConfigurationValue("Bridge", working);
            EnableProxy(false);
            StartTorProxy();
        }

        public void StartTorProxy()
        {
            if (ProxyRunning) return;
            if (_torrcConfiguration.ContainsKey("Bridge")) FilteredBridges = _torrcConfiguration["Bridge"].ToList();
            ClearCache();
            UpdateTorrc();
            EnableProxy(false);
            if (_torProxyProcess != null) _torProxyProcess.Close();
            _torProxyProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = Paths["tor"],
                    Arguments = "/c tor.exe -f torrc",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                },
            };
            _torProxyProcess.Start();

            SetStartupStatus("Process started");

            /**
             * Thread which listens to the output of tor.exe and updates information about current state of the connection
             * Following thread would kill the entire tor service if it crashes unexpectedly and if it couldnt stop tor process to prevent runaway tor service
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
                        if (!_torProxyProcess.StandardOutput.EndOfStream && ProxyRunning && _torProxyProcess.StandardOutput.BaseStream.CanRead)
                        {
                            line = _torProxyProcess.StandardOutput.ReadLine()?.ToUpper();
                            if (line == null) continue;

                            OnNewMessage?.Invoke(this, new NewMessageEventArgs(line));

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

                            string serveripv4;
                            string serveripv6;
                            string bridgeString;
                            if (logType == 'W' && line.Contains("MAKE SURE THAT THE PROXY SERVER IS UP AND RUNNING") && !BridgeProxyExhausted)
                            {
                                BridgeProxyExhausted = true;
                                OnBridgeProxyExhaustion?.Invoke(this, EventArgs.Empty);
                            }
                            if (logType == 'W' && line.Contains("UNABLE TO CONNECT"))
                            {
                                serveripv4 = Utils.Ipv4AddressSelector.Match(line).Value;
                                serveripv6 = Utils.Ipv6AddressSelector.Match(line).Value.ToLower();
                                bridgeString = _torrcConfiguration["Bridge"].ToList().FindAll(x => ((serveripv4 != string.Empty ? x.Contains(serveripv4) : false) || (serveripv6 != string.Empty ? x.ToLower().Contains(serveripv6) : false)))[0];
                                WorkingBridges.RemoveAll(x => ((serveripv4 != string.Empty ? x.Contains(serveripv4) : false) || (serveripv6 != string.Empty ? x.ToLower().Contains(serveripv6) : false)));
                                if (!DeadBridges.Contains(bridgeString)) DeadBridges.Add(bridgeString);
                                if (FilteredBridges.Any(x => ((serveripv4 != string.Empty ? x.Contains(serveripv4) : false) || (serveripv6 != string.Empty ? x.ToLower().Contains(serveripv6) : false))))
                                {
                                    OnNewDeadBridge?.Invoke(this, new NewBridgeEventArgs(bridgeString));
                                    FilteredBridges.RemoveAll(x => ((serveripv4 != string.Empty ? x.Contains(serveripv4) : false) || (serveripv6 != string.Empty ? x.ToLower().Contains(serveripv6) : false)));
                                }

                            }

                            if (logType == 'N' && line.Contains("NEW BRIDGE DESCRIPTOR"))
                            {
                                serveripv4 = Utils.Ipv4AddressSelector.Match(line).Value;
                                serveripv6 = Utils.Ipv6AddressSelector.Match(line).Value.ToLower();
                                bridgeString = _torrcConfiguration["Bridge"].ToList().FindAll(x => ((serveripv4 != string.Empty ? x.Contains(serveripv4) : false) || (serveripv6 != string.Empty ? x.ToLower().Contains(serveripv6) : false)))[0];
                                if (!FilteredBridges.Contains(bridgeString)) FilteredBridges.Add(bridgeString);
                                if (DeadBridges.Contains(bridgeString)) DeadBridges.Remove(bridgeString);
                                if (!WorkingBridges.Contains(bridgeString))
                                {
                                    WorkingBridges.Add(bridgeString);
                                    OnNewWorkingBridge?.Invoke(this, new NewBridgeEventArgs(bridgeString));
                                }
                            }

                            if (oldStatus != Status) OnStatusChange?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }catch (Exception)
                {
                    KillTorProcess();

                    WorkingBridges.Clear();
                    FilteredBridges.Clear();
                    DeadBridges.Clear();
                }
                BridgeProxyExhausted = false;
                Status = ProxyStatus.Disabled;
                OnStatusChange?.Invoke(this, EventArgs.Empty);
                EnableProxy(false);
            }
            )
            { 
                IsBackground = true,
            };
            outputSniffer.Start();
        }
    }

    public sealed class NewBridgeEventArgs : EventArgs
    {

        public readonly string Bridge;

        public NewBridgeEventArgs(string bridgeString) : base()
        {
            Bridge = bridgeString;
        }
    }

    public sealed class NewMessageEventArgs : EventArgs
    {

        public readonly string Message;

        public NewMessageEventArgs(string message) : base()
        {
            Message = message;
        }
    }
    public enum ProxyStatus
    {
        Disabled,
        Starting,
        Running,
    }
};