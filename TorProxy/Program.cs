using System.Diagnostics;
using System.Linq.Expressions;
using TorProxy.Proxy;

namespace TorProxy
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            List<string> usedFilenames = TorService.Paths.Values.ToList().ConvertAll(x => Path.GetFileName(x));
            foreach (Process p in Process.GetProcesses().Where((x, i) => usedFilenames.Contains(x.ProcessName + ".exe")))
            {
                try
                {
                    if (p.Id == Environment.ProcessId || p.Modules == null) continue;
                    foreach (ProcessModule module in p.Modules)
                    {
                        if (module.FileName == null) return;
                        if (TorService.Paths.ContainsValue(module.FileName)) p.Kill(true);
                    }
                }catch(Exception) { }
            }

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            ApplicationConfiguration.Initialize();
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
        }
    }
}