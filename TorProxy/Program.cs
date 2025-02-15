using System.Net;
using System.Runtime.CompilerServices;
using TorProxy.Proxy;
using TorProxy.Relays;
using TorProxy.GUI;
using TorProxy.Listener;
using System.Net.Sockets;
using System.Text;
using TorProxy.Network.ProxiFyre;
using NdisApi;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.IO.Compression;

namespace TorProxy
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Utils.AllocConsole();
            Utils.HideConsole();

            ApplicationConfiguration.Initialize();
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            TorService.KillTorProcess();

            Configuration.Initialize(Path.GetFullPath(AppContext.BaseDirectory + "\\configuration"));

            if (!Utils.IsAdministrator() && Configuration.Instance.Get("UseTorDNS").First() == "1")
            {
                Console.WriteLine("Cant set tor dns server while running not as administrator!");
                Configuration.Instance.Set("UseTorDNS", "0");
            }

            if (Configuration.Instance.Get("HideConsole")[0] == "1") 
            { 
                Utils.HideConsole(); 
            }
            else
            {
                Utils.ShowConsole();
            }

            CheckWinPkFilter();
            CheckProxiFyre();

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

        private static void CheckProxiFyre()
        {
            string proxiFyreDirectory = Path.GetFullPath(AppContext.BaseDirectory + @"\proxifyre\");
            if (Directory.Exists(proxiFyreDirectory)) return;

            Console.WriteLine("ProxiFyre directory was not found!");
            Console.WriteLine("Trying to download proxifyre");

            string url = @"https://github.com/wiresock/proxifyre/releases/download/v1.0.22/ProxiFyre-v1.0.22-" + System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture + "-signed.zip";
            string archive = Path.GetFullPath(AppContext.BaseDirectory + @"\proxifyre.zip");

            Console.WriteLine("Download URL: " + url);
            Console.WriteLine("Installation path: " + proxiFyreDirectory);
            Console.WriteLine("Installation archive: " + archive);

            try
            {
                Utils.DownloadToFile(url, archive);
                Directory.CreateDirectory(proxiFyreDirectory);
                ZipFile.ExtractToDirectory(archive, proxiFyreDirectory);
                Console.WriteLine("ProxiFyre downloaded!");
                File.Delete(archive);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to install proxifyre!");
                Console.WriteLine(ex);
                Console.WriteLine("Not critical");
            }

            Utils.DownloadToFile(url, archive);
        }

        private static void CheckWinPkFilter()
        {
            using (NdisApiDotNet ndisapi = new(null))
            {
                if (ndisapi.IsDriverLoaded()) return;

                Console.WriteLine("Windows packet filter driver was not found!");
                Console.WriteLine("Trying to download and install windows packet filter ");

                if (!Utils.IsAdministrator())
                {
                    Console.WriteLine("Restart program as admin!");
                    Console.ReadKey();
                    Environment.Exit(255);
                }

                string url = @"https://github.com/wiresock/ndisapi/releases/download/v3.6.1/Windows.Packet.Filter.3.6.1.1." + System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture + ".msi";
                string installerPath = Path.GetFullPath(AppContext.BaseDirectory + @"\WinPkFilter.msi");
                Console.WriteLine("Download URL: " + url);
                Console.WriteLine("Installer path: " + installerPath);
                try
                {
                    Utils.DownloadToFile(url, installerPath);
                    Process installerProcess = new();
                    ProcessStartInfo processInfo = new();
                    processInfo.Arguments = @"/i  " + installerPath + "  /q";
                    processInfo.FileName = "msiexec";
                    installerProcess.StartInfo = processInfo;
                    installerProcess.Start();
                    installerProcess.WaitForExit();
                    Console.WriteLine("WinPkFilter installed");
                    File.Delete(installerPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to install Windows packet filter driver!");
                    Console.WriteLine(ex);
                    Console.ReadKey();
                    Environment.Exit(255);
                }
            }
        }
    }
}