using System.Diagnostics;
using System.Text.Json;
using TorProxy.Proxy;
using TorProxy.Relays;

namespace TorProxy.Network.ProxiFyre
{
    public class ProxiFyreService
    {

        public static readonly Dictionary<string, string> Paths = new()
        {
            { "proxifyre", Path.GetFullPath(AppContext.BaseDirectory + "proxifyre\\") },
            { "proxifyre.exe", Path.GetFullPath(AppContext.BaseDirectory + "proxifyre\\ProxiFyre.exe") },
            { "app-config", Path.GetFullPath(AppContext.BaseDirectory + "proxifyre\\app-config.json") },
        };

        private static ProxiFyreService? _instance;

        public static ProxiFyreService Instance { 
            get 
            {
                _instance ??= new ProxiFyreService();
                return _instance;
            }
        }

        public readonly ProxiFyreConfig Config;

        private Process? _proxiFyreProcess;

        public bool IsRunning
        {
            get
            {
                return _proxiFyreProcess != null && !_proxiFyreProcess.HasExited;
            }
        }

        private ProxiFyreService()
        {
            if (File.Exists(Paths["app-config"]))
            {
                using (FileStream stream = File.OpenRead(Paths["app-config"])) Config = JsonDocument.Parse(stream).Deserialize<ProxiFyreConfig>();
            }
            else
            {
                Config = new ProxiFyreConfig()
                {
                    LogLevel = "Info",
                    Proxies = new ProxiFyreProxy[]
                    {
                        new ProxiFyreProxy()
                        {
                            AppNames = Array.Empty<string>(),
                            ProxyEndpoint = "127.0.0.1:" + TorService.Instance.GetConfigurationValue("SocksPort").First(),
                            Protocols = new string[]
                            {
                                "TCP", "UDP" // I dont know what is the behaviour for UDP, so we will pass it too, even tho tor doesnt support it
                            },
                        },
                    },
                };
            }
        }

        public void Start()
        {
            if (IsRunning) return;
            UpdateConfig();
            _proxiFyreProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = Paths["proxifyre"],
                    Arguments = "/c proxifyre.exe",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                },
            };
            _proxiFyreProcess.Start();
            Console.WriteLine("ProxiFyre service started");
        }

        public void Stop()
        {
            if (!IsRunning) return;
            _proxiFyreProcess.StandardInput.Close();
            _proxiFyreProcess.StandardOutput.Close();
            _proxiFyreProcess.Kill(entireProcessTree: true);
            Console.WriteLine("ProxiFyre service stopped");
        }

        public void UpdateConfig()
        {
            if (IsRunning) throw new InvalidOperationException("Cant update ProxiFyre config while ProxiFyre is running");
            File.WriteAllText(Paths["app-config"], JsonSerializer.Serialize(Config));
        }

        public string[] GetApps()
        {
            return Config.Proxies.FirstOrDefault().AppNames;
        }

        public void SetApps(string[] apps)
        {
            Config.Proxies[0].AppNames = apps;
        }
    }
}
