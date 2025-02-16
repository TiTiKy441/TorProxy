using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using TorProxy.Proxy.Control;
using System.Diagnostics.Eventing.Reader;
using System.DirectoryServices.ActiveDirectory;

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

        public TorServicePort TorController;

        public static readonly Dictionary<string, string> Paths = new()
        {
            { "tor", Path.GetFullPath(AppContext.BaseDirectory + "tor\\") },
            { "obfs4proxy.exe", Path.GetFullPath(AppContext.BaseDirectory + "tor\\obfs4proxy.exe") },
            { "tor.exe", Path.GetFullPath(AppContext.BaseDirectory + "tor\\tor.exe") },
            { "torrc", Path.GetFullPath(AppContext.BaseDirectory + "tor\\torrc") },
        };

        public static readonly Dictionary<string, string> TorCache = new()
        {
            { "cached_certs", Path.GetFullPath(AppContext.BaseDirectory + "tor\\cached-certs") },
            { "cached_descriptors", Path.GetFullPath(AppContext.BaseDirectory + "tor\\cached-descriptors") },
            { "cached_descriptos_new", Path.GetFullPath(AppContext.BaseDirectory + "tor\\cached-descriptors.new") },
        };

        private Dictionary<string, string[]> _torrcConfiguration = new()
        {
            { "ClientTransportPlugin", new string[] { "obfs4,webtunnel exec obfs4proxy.exe" } },
            { "SocksPort", new string[] { "9050" } },
            { "ControlPort", new string[] { "9051" } },
            { "UseBridges", new string[] { "1" } },
            { "DataDirectory", new string[] { AppContext.BaseDirectory.Replace(@"\", "/") + "/tor/" } },
            { "DNSPort", new string[] { "53" } },
        };

        private Process? _torProxyProcess;

        private ProxyStatus _status = ProxyStatus.Disabled;

        public ProxyStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnStatusChange?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private string _startupStatus = string.Empty;

        public string StartupStatus { 
            get
            {
                return _startupStatus;
            }
            set
            {
                _startupStatus = value;
                OnStartupStatusChange?.Invoke(this, EventArgs.Empty);
            }
        }

        public List<string> FilteredBridges { get; private set; } = new();
        public List<string> WorkingBridges { get; private set; } = new();
        public List<string> DeadBridges { get; private set; } = new();

        public bool BridgeProxyExhausted { get; private set; } = false;

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
            TorController = new TorServicePort(Convert.ToInt32(GetConfigurationValue("ControlPort").First()));
            OnStatusChange += TorService_OnStatusChange;
        }

        private void TorService_OnStatusChange(object? sender, EventArgs e)
        {
            switch (Status)
            {
                case ProxyStatus.Disabled:
                    TorController.Disconnect();
                    break;

                case ProxyStatus.Starting:
                    TorController.Connect();
                    TorController.Authenticate();
                    break;

                case ProxyStatus.Running:
                    break;
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

        public static void ClearCache()
        {
            foreach (string cachedFile in TorCache.Values)
            {
                if (File.Exists(cachedFile)) File.Delete(cachedFile);
            }
            /**
            if (ProxyRunning) return;
            foreach (string path in Directory.GetFiles(Paths["tor"]))
            {
                if (!Paths.ContainsValue(Path.GetFullPath(path))) File.Delete(path);
            }
            **/
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

        public void StopTorProxy(bool forced=false)
        {
            if (!ProxyRunning) return;
            if (TorController.IsUsable && !forced)
            {
                TorController.Shutdown();
            }
            else
            {
                _torProxyProcess?.StandardInput.Close();
                _torProxyProcess?.StandardOutput.Close();
                _torProxyProcess?.Kill(entireProcessTree: true);
            }
            WorkingBridges.Clear();
            FilteredBridges.Clear();
            DeadBridges.Clear();
        }

        public void WaitForEnd()
        {
            if (!ProxyRunning) return;
            _torProxyProcess?.WaitForExit();
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
                catch (Exception)
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
            StartTorProxy();
        }

        public void StartTorProxy()
        {
            if (ProxyRunning) return;
            if (_torrcConfiguration.ContainsKey("Bridge")) FilteredBridges = _torrcConfiguration["Bridge"].ToList();
            ClearCache();
            UpdateTorrc();
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

            StartupStatus = "Process started";

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
                // TODO: Switch to Utils
                Regex ipv4AddressSelector = new("(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)");
                Regex ipv6AddressSelector = new("(([0-9a-fA-F]{1,4}:){7,7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9]))");
                Regex logTypeSelector = new("\\[(.*?)\\]");
                Regex percentageSelector = new("(\\d+(\\.\\d+)?%)");

                try
                {
                    while (ProxyRunning)
                    {
                        if (!_torProxyProcess.StandardOutput.EndOfStream && _torProxyProcess.StandardOutput.BaseStream.CanRead)
                        {
                            try
                            {
                                line = _torProxyProcess.StandardOutput.ReadLine()?.ToUpper();
                                if (line == null) continue;

                                logType = logTypeSelector.Match(line).Value[1];
                                oldStatus = Status;

                                OnNewMessage?.Invoke(this, new NewMessageEventArgs(line));

                                if (line.Contains("BOOTSTRAPPED") || line.Contains("STARTING"))
                                {
                                    StartupStatus = "Conn: " + percentageSelector.Match(line).Value;
                                    Status = ProxyStatus.Starting;
                                }
                                if (line.Contains("DONE") && line.Contains("100%"))
                                {
                                    StartupStatus = "Done";
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
                            }
                            catch (Exception)
                            {
                            }
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