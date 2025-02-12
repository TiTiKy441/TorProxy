using System.Net;
using System.Runtime.CompilerServices;
using TorProxy.Proxy;
using TorProxy.Relays;
using TorProxy.GUI;
using TorProxy.Listener;
using System.Net.Sockets;
using System.Text;
using TorProxy.Network.ProxiFyre;

namespace TorProxy
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Utils.AllocConsole();
            Utils.HideConsole();
            if (!Utils.IsAdministrator())
            {
                Utils.ShowConsole();
                Console.WriteLine("Required to run as administator!");
                Console.ReadKey();
                Environment.Exit(0);
            }

            ApplicationConfiguration.Initialize();
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            TorService.KillTorProcess();

            Configuration.Initialize(Path.GetFullPath(AppContext.BaseDirectory + "\\configuration"));

            if (Configuration.Instance.Get("HideConsole")[0] == "1") 
            { 
                Utils.HideConsole(); 
            }
            else
            {
                Utils.ShowConsole();
            }

            TorServiceListener.Initialize();
            TorServiceListener.EnableTor(false);

            while (RelayDistributor.Instance == null)
            {
                try
                {
                    RelayDistributor.Initialize(Configuration.Instance.Get("RelayFile")[0]);
                }
                catch (Exception)
                {
                    Utils.ShowConsole();
                    Console.WriteLine("Relay file is corrupted or doesnt exist");
                    File.Delete(Configuration.Instance.Get("RelayFile")[0]);
                    DownloadRelays();
                }
            }

            string[] cmd;
            foreach (string customParameter in Configuration.Instance.Get("AdditionalTorrcConfiguration"))
            {
                cmd = customParameter.Split(" ", 2);
                TorService.Instance.SetConfigurationValue(cmd[0], cmd[1], false);
                Console.WriteLine("Overriding torrc parameter: " + cmd[0]);
            }

            IconUserInterface iconInterface = new();
            iconInterface.Show();

            TorControl torControlWindow = new();
            torControlWindow.Show();

            Settings settingsWindow = new();
            settingsWindow.Show();

            if (Configuration.Instance.Get("ConnectOnStart")[0] == "1") torControlWindow.connect_button.AccessibilityObject.DoDefaultAction();

            Console.WriteLine("Application running");
            Application.Run(iconInterface);
        }

        private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            Configuration.Instance.Save();
        }

        private static void DownloadRelays()
        {
            if (!File.Exists(Configuration.Instance.Get("RelayFile")[0]))
            {
                foreach (string mirror in Configuration.Instance.Get("RelayMirrors"))
                {
                    try
                    {
                        Console.WriteLine("Trying to download relay file from: " + mirror);
                        Utils.DownloadToFile(mirror, Configuration.Instance.Get("RelayFile")[0]);
                        break;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Unable to download relay mirror from: " + mirror);
                        //TODO: Error handling
                    }
                }
            }
        }
    }
}