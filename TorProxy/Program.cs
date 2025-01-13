using System.Runtime.CompilerServices;
using TorProxy.Proxy;

namespace TorProxy
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            if (!File.Exists(Configuration.Instance.Get("RelayFile")[0]))
            {
                File.WriteAllText(Configuration.Instance.Get("RelayFile")[0], "{}");
                foreach(string mirror in Configuration.Instance.Get("RelayMirrors"))
                {
                    try
                    {
                        Utils.DownloadToFile(mirror, Configuration.Instance.Get("RelayFile")[0]);
                    }
                    catch (Exception)
                    {
                        //TODO: Error handling
                    }
                }
            }
            RelayDistributor distributor = new(Configuration.Instance.Get("RelayFile")[0]);

            string[] cmd;
            foreach (string customParameter in Configuration.Instance.Get("AdditionalTorrcConfiguration"))
            {
                cmd = customParameter.Split(" ", 2);
                TorService.Instance.SetConfigurationValue(cmd[0], cmd[1], true);
            }

            TorService.KillTorProcess();
            TorService.EnableProxy(false);

            IconUserInterface iconInterface = new();
            iconInterface.Show();

            SettingsWindow settingsWindow = new();
            settingsWindow.Show();

            Application.Run(iconInterface);
        }

        private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            TorService.EnableProxy(false);
            TorService.Instance.StopTorProxy();
            TorService.Instance.WaitForEnd();
            TorService.KillTorProcess();
            Configuration.Instance.Save();
        }
    }
}