using System.Diagnostics;
using TorProxy.Proxy;

namespace TorProxy
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            Process.GetProcessesByName("tor").ToList().ForEach(p => p.Kill());
            Process.GetProcessesByName("obfs4proxy").ToList().ForEach(p => p.Kill());
            Process.GetProcessesByName("lyrebird").ToList().ForEach(p => p.Kill());
            foreach (Process p in Process.GetProcessesByName("torproxy").ToList())
            {
                if (p.Id != Environment.ProcessId) p.Kill();
            }

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            ApplicationConfiguration.Initialize();
            TorService.EnableProxy(false);
            Settings window = new Settings();
            window.ShowDialog();
            //Application.Run(window);
        }

        private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            TorService.EnableProxy(false);
            TorService.Instance.StopTorProxy();
            TorService.Instance.WaitForEnd();
        }
    }
}