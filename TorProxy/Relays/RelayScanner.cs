using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TorProxy.Relays
{
    public class RelayScanner
    {

        private static Thread? _filterProcess;

        private static bool _filtering = false;

        private static List<Relay>? _allRelays = new();
        private static List<Relay>? _workingRelays = new();

        public static event EventHandler<EventArgs>? OnScanCompleted;
        public static event EventHandler<OnNewWorkingRelayEventArgs>? OnNewWorkingRelay;

        public static void StartFilter()
        {
            _allRelays = RelayDistributor.Instance.RelayInfo.Relays.ToList();
            Utils.Random.Shuffle(_allRelays);
            _filterProcess = new Thread(() =>
            {
                FilterWork();
            });
            _filtering = true;
            _filterProcess.Start();
        }

        public static void StopFilter(bool waitForPendingConnections = false)
        {
            if (_filterProcess?.ThreadState != System.Threading.ThreadState.Running) return;
            _filtering = false;
            if (waitForPendingConnections) _filterProcess?.Join();
        }

        public static List<Relay>? GetWorkingRelays()
        {
            return _workingRelays;
        }

        private static void FilterWork()
        {
            Relay testRelay;
            int k = 0;
            Dictionary<int, Relay> taskIdToRelay = new();
            Dictionary<int, TcpClient> taskIdToClient = new();
            int timeout = Convert.ToInt32(Configuration.Instance.Get("RelayScannerTimeout").First());
            while (_filtering && k+1 < _allRelays?.Count)
            {
                k++;
                testRelay = _allRelays[k];
                try
                {
                    TcpClient client = new();
                    string[] addresses = testRelay.Addresses;
                    foreach (string addr in addresses)
                    {
                        Task t = client.ConnectAsync(IPEndPoint.Parse(addr)).WaitAsync(TimeSpan.FromMilliseconds(timeout));
                        taskIdToRelay[t.Id] = testRelay;
                        taskIdToClient[t.Id] = client;
                        t.ContinueWith(t =>
                        {
                            
                            if (!t.IsFaulted && _filtering)
                            {
                                _workingRelays?.Add(taskIdToRelay[t.Id]);
                                OnNewWorkingRelay?.Invoke(null, new OnNewWorkingRelayEventArgs(taskIdToRelay[t.Id].Addresses.First() + " " + taskIdToRelay[t.Id].Fingerprint));
                            }
                            taskIdToClient[t.Id].Close();
                            taskIdToClient[t.Id].Dispose();
                        });
                    }
                }
                catch (Exception)
                {
                }
            }
            _filtering = false;
            Console.WriteLine("Relay scan finished (" + _workingRelays + "/" + _allRelays + ")");
            OnScanCompleted?.Invoke(null, EventArgs.Empty);
        }
    }

    public class OnNewWorkingRelayEventArgs : EventArgs
    {

        public readonly string Relay;

        public OnNewWorkingRelayEventArgs(string relay) : base()
        {
            Relay = relay;
        }
    }
}
